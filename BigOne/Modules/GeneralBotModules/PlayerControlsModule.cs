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
        await DeferAsync().ConfigureAwait(false);
        VoteLavalinkPlayer? player = await audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(Context.Guild.Id);
        if (player == null)
        {
            await RespondAsync(embed: embedService.GetErrorEmbed("Could not find player in your voice channel!")).ConfigureAwait(false);
            return;
        }

        var user = Context.User as SocketGuildUser;
        if (user?.VoiceChannel != null)
        {
            Func<Embed, Task> followUpAction = async (Embed embed) =>
            {
                await FollowupAsync(embed: embed).ConfigureAwait(false);
            };
            await playerService.SkipAsync(
                Context.Guild.Id.ToString(),
                user.Username.ToString(),
                user.VoiceChannel.Id.ToString(),
                Context.Channel.Id.ToString(),
                followUpAction: followUpAction
            );
        }
        else
        {
            await FollowupAsync(embed: embedService.GetErrorEmbed("You must join a voice channel first!")).ConfigureAwait(false);
        }        
    }

    [ComponentInteraction("play_button")]
    public async Task PlayButtonHandler()
    {
        await DeferAsync().ConfigureAwait(false);
        VoteLavalinkPlayer? player = await audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(Context.Guild.Id);
        if (player == null)
        {
            await FollowupAsync(embed: embedService.GetErrorEmbed("Could not find player in your voice channel!")).ConfigureAwait(false);
            return;
        }

        var user = Context.User as SocketGuildUser;
        if (user?.VoiceChannel != null)
        {
            Func<Embed, Task> followUpAction = async (Embed embed) =>
            {
                await FollowupAsync(embed: embed).ConfigureAwait(false);
            };

            await playerService.ResumeAsync(
                Context.Guild.Id.ToString(),
                user.Username.ToString(),
                user.VoiceChannel.Id.ToString(),
                Context.Channel.Id.ToString(),
                followUpAction
            );
        }
        else
        {
            await FollowupAsync(embed: embedService.GetErrorEmbed("You must join a voice channel first!")).ConfigureAwait(false);
        }
    }

    [ComponentInteraction("pause_button")]
    public async Task PauseButtonHandler()
    {
        await DeferAsync().ConfigureAwait(false);
        VoteLavalinkPlayer? player = await audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(Context.Guild.Id);
        if (player == null)
        {
            await FollowupAsync(embed: embedService.GetErrorEmbed("Could not find player in your voice channel!")).ConfigureAwait(false);
        }

        var user = Context.User as SocketGuildUser;
        if (user?.VoiceChannel != null)
        {
            Func<Embed, Task> followUpAction = async (Embed embed) =>
            {
                await FollowupAsync(embed: embed).ConfigureAwait(false);
            };
            await playerService.PauseAsync(
                Context.Guild.Id.ToString(),
                user.Username.ToString(),
                user.VoiceChannel.Id.ToString(),
                Context.Channel.Id.ToString(),
                followUpAction
            );
        }
        else
        {
            await FollowupAsync(embed: embedService.GetErrorEmbed("You must join a voice channel first!")).ConfigureAwait(false);
        }
    }
}