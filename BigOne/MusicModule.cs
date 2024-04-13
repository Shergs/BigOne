﻿namespace BigOne;

using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Vote;
using Lavalink4NET.Rest.Entities.Tracks;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using OpenAI_API.Moderation;

/// <summary>
///     Presents some of the main features of the Lavalink4NET-Library.
/// </summary>
[RequireContext(ContextType.Guild)]
public sealed class MusicModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IAudioService _audioService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MusicModule"/> class.
    /// </summary>
    /// <param name="audioService">the audio service</param>
    /// <exception cref="ArgumentNullException">
    ///     thrown if the specified <paramref name="audioService"/> is <see langword="null"/>.
    /// </exception>
    public MusicModule(IAudioService audioService)
    {
        ArgumentNullException.ThrowIfNull(audioService);
        _audioService = audioService;
    }

    /// <summary>
    ///     Disconnects from the current voice channel connected to asynchronously.
    /// </summary>
    /// <returns>a task that represents the asynchronous operation</returns>
    [SlashCommand("disconnect", "Disconnects from the current voice channel connected to", runMode: RunMode.Async)]
    public async Task Disconnect()
    {
        var player = await GetPlayerAsync().ConfigureAwait(false);

        if (player is null)
        {
            return;
        }

        await player.DisconnectAsync().ConfigureAwait(false);
        await RespondAsync("Disconnected.").ConfigureAwait(false);
    }

    /// <summary>
    ///     Plays music asynchronously.
    /// </summary>
    /// <param name="query">the search query</param>
    /// <returns>a task that represents the asynchronous operation</returns>
    [SlashCommand("play", description: "Plays music", runMode: RunMode.Async)]
    public async Task Play(string query)
    {
        await DeferAsync().ConfigureAwait(false);

        var player = await GetPlayerAsync(connectToVoiceChannel: true).ConfigureAwait(false);

        if (player is null)
        {
            return;
        }

        var track = await _audioService.Tracks
            .LoadTrackAsync(query, TrackSearchMode.YouTube)
            .ConfigureAwait(false);

        if (track is null)
        {
            await FollowupAsync("😖 No results.").ConfigureAwait(false);
            return;
        }

        var position = await player.PlayAsync(track).ConfigureAwait(false);


        if (position is 0)
        {
            var currentTrack = player.CurrentItem;
            if (currentTrack is null)
            { 
                await RespondAsync($"🔈Playing: {track.Uri}").ConfigureAwait(false);
                return;
            }
            var embedBuilder = new EmbedBuilder()
                            .WithColor(Color.Blue)
                            .WithDescription($"🔈Now playing: [{currentTrack.Track!.Title}]({currentTrack.Track!.Uri})\n" +
                                $"Link: {currentTrack.Track!.Uri}") // Make the title a clickable link
                            .AddField("Artist", currentTrack.Track!.Author, inline: true)
                            .AddField("Source", currentTrack.Track!.SourceName, inline: true)
                            .WithFooter(footer => footer.Text = "Play some more songs.")
                            .WithCurrentTimestamp();

            var embed = embedBuilder.Build();

            await Context.Channel.SendMessageAsync($"🔈Playing: {currentTrack.Track!.Uri}").ConfigureAwait(false);
            await FollowupAsync(embed: embed).ConfigureAwait(false);
        }
        else
        {
            var currentTrack = player.CurrentItem;
            if (currentTrack is null)
            {
                await RespondAsync($"🔈Playing: {track.Uri}").ConfigureAwait(false);
                return;
            }
            var embedBuilder = new EmbedBuilder()
                            .WithColor(Color.Blue)
                            .WithDescription($"🔈Added to Queue: [{currentTrack.Track!.Title}]({currentTrack.Track!.Uri})" +
                                $"Link: {currentTrack.Track!.Uri}")
                            .AddField("Artist", currentTrack.Track!.Author, inline: true)
                            .AddField("Source", currentTrack.Track!.SourceName, inline: true)
                            .WithFooter(footer => footer.Text = "Play some more songs.")
                            .WithCurrentTimestamp();

            var embed = embedBuilder.Build();

            await Context.Channel.SendMessageAsync($"Queing: {currentTrack.Track!.Uri}").ConfigureAwait(false);
            await FollowupAsync(embed: embed).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Shows the track position asynchronously.
    /// </summary>
    /// <returns>a task that represents the asynchronous operation</returns>
    [SlashCommand("position", description: "Shows the track position", runMode: RunMode.Async)]
    public async Task Position()
    {
        var player = await GetPlayerAsync(connectToVoiceChannel: false).ConfigureAwait(false);

        if (player is null)
        {
            return;
        }

        if (player.CurrentItem is null)
        {
            await RespondAsync("Nothing playing!").ConfigureAwait(false);
            return;
        }

        await RespondAsync($"Position: {player.Position?.Position} / {player.CurrentTrack.Duration}.").ConfigureAwait(false);
    }

    /// <summary>
    ///     Stops the current track asynchronously.
    /// </summary>
    /// <returns>a task that represents the asynchronous operation</returns>
    [SlashCommand("stop", description: "Stops the current track", runMode: RunMode.Async)]
    public async Task Stop()
    {
        var player = await GetPlayerAsync(connectToVoiceChannel: false);

        if (player is null)
        {
            return;
        }

        if (player.CurrentItem is null)
        {
            await RespondAsync("Nothing playing!").ConfigureAwait(false);
            return;
        }

        await player.StopAsync().ConfigureAwait(false);
        await RespondAsync("Stopped playing.").ConfigureAwait(false);
    }

    /// <summary>
    ///     Updates the player volume asynchronously.
    /// </summary>
    /// <param name="volume">the volume (1 - 1000)</param>
    /// <returns>a task that represents the asynchronous operation</returns>
    [SlashCommand("volume", description: "Sets the player volume (0 - 1000%)", runMode: RunMode.Async)]
    public async Task Volume(int volume = 100)
    {
        if (volume is > 1000 or < 0)
        {
            await RespondAsync("Volume out of range: 0% - 1000%!").ConfigureAwait(false);
            return;
        }

        var player = await GetPlayerAsync(connectToVoiceChannel: false).ConfigureAwait(false);

        if (player is null)
        {
            return;
        }

        await player.SetVolumeAsync(volume / 100f).ConfigureAwait(false);
        await RespondAsync($"Volume updated: {volume}%").ConfigureAwait(false);
    }

    [SlashCommand("skip", description: "Skips the current track", runMode: RunMode.Async)]
    public async Task Skip()
    {
        var player = await GetPlayerAsync(connectToVoiceChannel: false);

        if (player is null)
        {
            return;
        }

        if (player.CurrentItem is null)
        {
            await RespondAsync("Nothing playing!").ConfigureAwait(false);
            return;
        }

        await player.SkipAsync().ConfigureAwait(false);

        var track = player.CurrentItem;

        if (track is not null)
        {
            var currentTrack = player.CurrentItem;
            if (currentTrack is null)
            {
                await RespondAsync($"🔈Playing: {track.Track!.Uri}").ConfigureAwait(false);
                return;
            }
            var embedBuilder = new EmbedBuilder()
                            .WithColor(Color.Blue)
                            .WithDescription($" Now Playing: [{currentTrack.Track!.Title}]({currentTrack.Track!.Uri})\n" +
                                $"Link: {currentTrack.Track!.Uri}") // Make the title a clickable link
                            .AddField("Artist", currentTrack.Track!.Author, inline: true)
                            .AddField("Source", currentTrack.Track!.SourceName, inline: true)
                            .WithFooter(footer => footer.Text = "Play some more songs.")
                            .WithCurrentTimestamp();

            // Call Build() to create the Embed object
            var embed = embedBuilder.Build();

            // Use RespondAsync to send the embed. No need for the second RespondAsync call that you have.
            await Context.Channel.SendMessageAsync($"🔈Playing: {currentTrack.Track!.Uri}").ConfigureAwait(false);
            await FollowupAsync(embed: embed).ConfigureAwait(false);
        }
        else
        {
            await RespondAsync("Skipped. Stopped playing because the queue is now empty.").ConfigureAwait(false);
        }
    }

    [SlashCommand("pause", description: "Pauses the player.", runMode: RunMode.Async)]
    public async Task PauseAsync()
    {
        var player = await GetPlayerAsync(connectToVoiceChannel: false);

        if (player is null)
        {
            return;
        }

        if (player.State is PlayerState.Paused)
        {
            await RespondAsync("Player is already paused.").ConfigureAwait(false);
            return;
        }

        await player.PauseAsync().ConfigureAwait(false);
        await RespondAsync("Paused.").ConfigureAwait(false);
    }

    [SlashCommand("resume", description: "Resumes the player.", runMode: RunMode.Async)]
    public async Task ResumeAsync()
    {
        var player = await GetPlayerAsync(connectToVoiceChannel: false);

        if (player is null)
        {
            return;
        }

        if (player.State is not PlayerState.Paused)
        {
            await RespondAsync("Player is not paused.").ConfigureAwait(false);
            return;
        }

        await player.ResumeAsync().ConfigureAwait(false);
        await RespondAsync("Resumed.").ConfigureAwait(false);
    }

    [SlashCommand("queue", description: "Get queue count.", runMode: RunMode.Async)]
    public async Task Queue()
    {
        await DeferAsync().ConfigureAwait(false);
        var player = await GetPlayerAsync(connectToVoiceChannel: false);

        if (player is null)
        {
            return;
        }

        //Lavalink4NET.Players.Queued.ITrackQueue

        if (player.Queue.HasHistory)
        {
            var embedBuilder = new EmbedBuilder()
                            .WithColor(Color.Blue)
                            .WithDescription($"Queue Count: {player.Queue.Count}") // Make the title a clickable link
                            .WithFooter(footer => footer.Text = "Queue Will Continue.")
                            .WithCurrentTimestamp();
            var embed = embedBuilder.Build();
            await player.ResumeAsync().ConfigureAwait(false);
            await FollowupAsync(embed: embed).ConfigureAwait(false);
        }
        else 
        {
            var embedBuilder = new EmbedBuilder()
                            .WithColor(Color.Blue)
                            .WithDescription($"Queue Count: {player.Queue.Count}") // Make the title a clickable link
                            .WithFooter(footer => footer.Text = "Queue is Empty.")
                            .WithCurrentTimestamp();
            var embed = embedBuilder.Build();
            await player.ResumeAsync().ConfigureAwait(false);
            await FollowupAsync(embed: embed).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Gets the guild player asynchronously.
    /// </summary>
    /// <param name="connectToVoiceChannel">
    ///     a value indicating whether to connect to a voice channel
    /// </param>
    /// <returns>
    ///     a task that represents the asynchronous operation. The task result is the lavalink player.
    /// </returns>
    private async ValueTask<VoteLavalinkPlayer?> GetPlayerAsync(bool connectToVoiceChannel = true)
    {
        var retrieveOptions = new PlayerRetrieveOptions(
            ChannelBehavior: connectToVoiceChannel ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None);

        var result = await _audioService.Players
            .RetrieveAsync(Context, playerFactory: PlayerFactory.Vote, retrieveOptions)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            var errorMessage = result.Status switch
            {
                PlayerRetrieveStatus.UserNotInVoiceChannel => "You are not connected to a voice channel.",
                PlayerRetrieveStatus.BotNotConnected => "The bot is currently not connected.",
                _ => "Unknown error.",
            };

            await FollowupAsync(errorMessage).ConfigureAwait(false);
            return null;
        }

        return result.Player;
    }
}

