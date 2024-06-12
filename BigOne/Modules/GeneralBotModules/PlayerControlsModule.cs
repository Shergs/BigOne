namespace BigOne.Modules.GeneralBotModules;

using BigOne.Services;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Players.Vote;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

public class PlayerControlsModule(
    IPlayerService playerService,
    IEmbedService embedService,
    IAudioService audioService
    ) : InteractionModuleBase<SocketInteractionContext>
{
    [ComponentInteraction("next_button")]
    public async Task NextButtonHandler()
    {
        VoteLavalinkPlayer? player = await audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(Context.Guild.Id);
        if (player == null)
        {
            await RespondAsync(embed: embedService.GetErrorEmbed("Could not find player in your voice channel!")).ConfigureAwait(false);
        }

        var user = Context.User as SocketGuildUser;
        if (user?.VoiceChannel != null)
        {
            await playerService.SkipAsync(
                Context.Guild.Id.ToString(),
                user.Username.ToString(),
                user.VoiceChannel.Id.ToString(),
                Context.Channel.Id.ToString()
            );
        }
        else
        {
            await RespondAsync(embed: embedService.GetErrorEmbed("You must join a voice channel first!")).ConfigureAwait(false);
        }        
    }

    [ComponentInteraction("play_button")]
    public async Task PlayButtonHandler()
    {
        VoteLavalinkPlayer? player = await audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(Context.Guild.Id);
        if (player == null)
        {
            await RespondAsync(embed: embedService.GetErrorEmbed("Could not find player in your voice channel!")).ConfigureAwait(false);
        }

        var user = Context.User as SocketGuildUser;
        if (user?.VoiceChannel != null)
        {
            await playerService.ResumeAsync(
                Context.Guild.Id.ToString(),
                user.Username.ToString(),
                user.VoiceChannel.Id.ToString(),
                Context.Channel.Id.ToString()
            );
        }
        else
        {
            await RespondAsync(embed: embedService.GetErrorEmbed("You must join a voice channel first!")).ConfigureAwait(false);
        }
    }

    [ComponentInteraction("pause_button")]
    public async Task PauseButtonHandler()
    {
        VoteLavalinkPlayer? player = await audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(Context.Guild.Id);
        if (player == null)
        {
            await RespondAsync(embed: embedService.GetErrorEmbed("Could not find player in your voice channel!")).ConfigureAwait(false);
        }

        var user = Context.User as SocketGuildUser;
        if (user?.VoiceChannel != null)
        {
            await playerService.PauseAsync(
                Context.Guild.Id.ToString(),
                user.Username.ToString(),
                user.VoiceChannel.Id.ToString(),
                Context.Channel.Id.ToString()
            );
        }
        else
        {
            await RespondAsync(embed: embedService.GetErrorEmbed("You must join a voice channel first!")).ConfigureAwait(false);
        }
    }
}