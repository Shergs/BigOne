namespace BigOne;

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Vote;
using Lavalink4NET.Rest;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.Tracks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
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
    private ApplicationDbContext _applicationDbContext;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MusicModule"/> class.
    /// </summary>
    /// <param name="audioService">the audio service</param>
    /// <exception cref="ArgumentNullException">
    ///     thrown if the specified <paramref name="audioService"/> is <see langword="null"/>.
    /// </exception>
    public MusicModule(IAudioService audioService, ApplicationDbContext applicationDbContext)
    {
        ArgumentNullException.ThrowIfNull(audioService);
        _audioService = audioService;
        _applicationDbContext = applicationDbContext;
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

    #region sound
    [SlashCommand("sound", description: "Play soundboard sound", runMode: RunMode.Async)]
    public async Task Sound(string soundName)
    {
        await DeferAsync().ConfigureAwait(false);
        Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";C:\\Users\\sherg\\source\\repos\\BigOne\\BigOne\\opus.dll\"");
        var voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
        IAudioClient audioClient = null;
        if (voiceChannel == null)
        {
            await ReplyAsync("You need to be in a voice channel.");
            return;
        }
        Console.WriteLine("Attempting to connect to the voice channel...");
        try
        {
            audioClient = await voiceChannel.ConnectAsync();
            Console.WriteLine("Connected to the voice channel successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to connect to voice channel: " + ex.Message);
            return;
        }
        try
        {
            Sound? sound = await _applicationDbContext.Sounds.Where(x => x.Name.ToLower() == soundName.ToLower()).FirstOrDefaultAsync();
            if (sound == null)
            {
                Console.WriteLine("Error: Cannot find sound file.");
                await FollowupAsync("Can't find sound in database");
                return;
            }
            string path = $"C:\\Workspace_Git\\BigOne\\BigOne\\Sounds\\{sound.FilePath.Replace(" ","_")}";
            if (!File.Exists(path))
            {
                // Going to just do the thing here
                bool soundDownloaded = await API.TryGetSound(soundName, path);
                if (!soundDownloaded)
                {
                    Console.WriteLine("Error: File does not exist at the specified path.");
                    return;
                }
                else
                {
                    await FollowupAsync($"Sound Downloaded!");
                }
            }
            await FollowupAsync("SoundBoard: " + $"{path}").ConfigureAwait(false);
            using (var ffmpeg = CreateProcess(path))
            using (var stream = audioClient.CreatePCMStream(AudioApplication.Mixed))
            {
                try { await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream); }
                finally { await stream.FlushAsync(); }
            }
        }
        finally
        {
            await audioClient.StopAsync();
        }
    }

    [SlashCommand("soundboard", description:"List out all of the sounds on this server's soundboard", runMode: RunMode.Async)]
    public async Task SoundBoard()
    {
        await DeferAsync().ConfigureAwait(false);

        List<Sound> sounds = await _applicationDbContext.Sounds.Where(x => x.ServerId == Context.Guild.Id.ToString()).ToListAsync();
        if (sounds.Count == 0)
        { 
            await FollowupAsync("No sounds found for server").ConfigureAwait(false);
            return;
        }
        string result = "";
        for (int i = 0; i< sounds.Count; i++) 
        {
            result += $"{i}. {sounds[i].Emote}{sounds[i].Name}\n";
        }
        await FollowupAsync(result).ConfigureAwait(false);
    }

    public Process CreateProcess(string path)
    {
        Process process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -ar 48000 -f s16le -acodec pcm_s16le pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        process.ErrorDataReceived += (sender, e) => Console.WriteLine("FFmpeg Error: " + e.Data);
        process.BeginErrorReadLine();
        return process;
    }
    #endregion

    #region PlayService
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
    #endregion

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

    public class LavalinkTrackResponse
    {
        // Define properties based on Lavalink's response structure
    }

}

