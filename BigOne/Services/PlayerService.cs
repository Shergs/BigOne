using Discord;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Vote;
using Lavalink4NET.Rest.Entities.Tracks;

namespace BigOne.Services
{
    public interface IPlayerService 
    {
        Task PlayAsync(string serverId, string query, string username, string voiceChannelId, string chatChannelId = "", Func<EmbedService.EmbedMessage, Task>? followUpAction = null);
        Task PauseAsync(string serverId, string username, string voiceChannelId, string chatChannelId = "", Func<Embed, Task>? followUpAction = null);
        Task ResumeAsync(string serverId, string username, string voiceChannelId, string chatChannelId = "", Func<Embed, Task>? followUpAction = null);
        Task SkipAsync(string serverId, string username, string voiceChannelId, string chatChannelId = "", Func<Embed, Task>? followUpAction = null);
        Task StopAsync(string serverId, string username, string voiceChannelId, string chatChannelId = "", Func<Embed, Task>? followUpAction = null);
    }
    public class PlayerService(
        IAudioService audioService,
        ISignalService signalService,
        IEmbedService embedService,
        ApplicationDbContext context,
        DiscordSocketClient discordSocketClient
        ) : IPlayerService
    {
        public async Task PlayAsync(string serverId, string query, string username, string voiceChannelId, string chatChannelId = "", Func<EmbedService.EmbedMessage, Task>? followUpAction = null)
        {
            var guild = discordSocketClient.GetGuild(ulong.Parse(serverId));
            SocketTextChannel? textChannel = null;

            if (!string.IsNullOrEmpty(chatChannelId))
            {
                if (ulong.TryParse(chatChannelId, out ulong channelId))
                {
                    textChannel = guild.GetTextChannel(channelId);
                }
            }
            else
            {
                textChannel = guild.Channels
                    .OfType<SocketTextChannel>() 
                    .FirstOrDefault(x => x.Name == "bot-commands");
            }

            if (textChannel == null && followUpAction == null)
            {
                Console.WriteLine("Response channel not found or is not a text channel");
                return;
            }

            VoteLavalinkPlayer? player = await GetPlayerAsync(serverId, voiceChannelId);

            var track = await audioService.Tracks
                .LoadTrackAsync(query, TrackSearchMode.YouTube)
                .ConfigureAwait(false);

            if (track is null)
            {
                if (followUpAction != null)
                {
                    await followUpAction(new EmbedService.EmbedMessage { Embed = embedService.GetErrorEmbed("😖 No results."), MessageComponent = null });
                }
                else
                {
                    await textChannel.SendMessageAsync(embed: embedService.GetErrorEmbed("😖 No results.")).ConfigureAwait(false);
                }
                return;
            }

            SongHistoryItem songHistory = new SongHistoryItem();
            songHistory.Timestamp = DateTime.Now;
            songHistory.Name = track.Title;
            songHistory.ServerId = serverId;
            songHistory.Url = track.Uri.ToString();
            songHistory.DiscordUsername = username;
            context.Add(songHistory);
            await context.SaveChangesAsync();

            var position = await player.PlayAsync(track).ConfigureAwait(false);

            if (position is 0)
            {
                var currentTrack = player.CurrentItem;

                (Embed, MessageComponent) embed = embedService.GetPlayerEmbedWithButtons(
                    currentTrack.Track!.Title,
                    currentTrack.Track!.Uri.ToString(),
                    currentTrack.Track!.Author,
                    currentTrack.Track!.SourceName
                    );

                if (followUpAction != null)
                {
                    await followUpAction(new EmbedService.EmbedMessage { Embed = embed.Item1, MessageComponent = embed.Item2 });
                }
                else
                {
                    await textChannel.SendMessageAsync(embed: embed.Item1, components: embed.Item2).ConfigureAwait(false);
                }
                await signalService.SendNowPlaying(serverId, track.Title, track.Uri.ToString(), username, DateTime.Now.ToString(), track.Author);
            }
            else
            {
                var currentTrack = player.CurrentItem;
                if (currentTrack is null)
                {
                    
                }

                Embed embed = embedService.GetPlayerEmbed(
                    track.Title,
                    track.Uri.ToString(),
                    track.Author,
                    track.SourceName,
                    addToQueue: true
                    );

                if (followUpAction != null)
                {
                    await followUpAction(new EmbedService.EmbedMessage { Embed = embed, MessageComponent = null });
                }
                else
                {
                    await textChannel.SendMessageAsync(embed: embed).ConfigureAwait(false);  
                }
                await signalService.SendQueueUpdated(serverId, track.Title, track.Uri.ToString(), position.ToString(), "add", username, DateTime.Now.ToString());
            }
        }

        public async Task PauseAsync(string serverId, string username, string voiceChannelId, string chatChannelId = "", Func<Embed, Task>? followUpAction = null)
        {
            var guild = discordSocketClient.GetGuild(ulong.Parse(serverId));
            SocketTextChannel? textChannel = null;

            if (!string.IsNullOrEmpty(chatChannelId))
            {
                if (ulong.TryParse(chatChannelId, out ulong channelId))
                {
                    textChannel = guild.GetTextChannel(channelId);
                }
            }
            else
            {
                textChannel = guild.Channels
                    .OfType<SocketTextChannel>()
                    .FirstOrDefault(x => x.Name == "bot-commands");
            }

            if (textChannel == null && followUpAction == null)
            {
                Console.WriteLine("Response channel not found or is not a text channel");
                return;
            }

            VoteLavalinkPlayer? player = await GetPlayerAsync(serverId, voiceChannelId);
            await player.PauseAsync();
            if (followUpAction != null)
            {
                await followUpAction(embedService.GetMessageEmbed("Pause", $"Player has been paused by {username}"));
            }
            else
            {
                await textChannel.SendMessageAsync(embed: embedService.GetMessageEmbed("Pause", $"Player has been paused by {username}")).ConfigureAwait(false);
            }
            await signalService.SendPaused(serverId, username);
        }

        public async Task ResumeAsync(string serverId, string username, string voiceChannelId, string chatChannelId = "", Func<Embed, Task>? followUpAction = null)
        {
            var guild = discordSocketClient.GetGuild(ulong.Parse(serverId));
            SocketTextChannel? textChannel = null;

            if (!string.IsNullOrEmpty(chatChannelId))
            {
                if (ulong.TryParse(chatChannelId, out ulong channelId))
                {
                    textChannel = guild.GetTextChannel(channelId);
                }
            }
            else
            {
                textChannel = guild.Channels
                    .OfType<SocketTextChannel>()
                    .FirstOrDefault(x => x.Name == "bot-commands");
            }

            if (textChannel == null && followUpAction == null)
            {
                Console.WriteLine("Response channel not found or is not a text channel");
                return;
            }

            VoteLavalinkPlayer? player = await GetPlayerAsync(serverId, voiceChannelId);
            await player.ResumeAsync();
            if (followUpAction != null)
            {
                await followUpAction(embedService.GetMessageEmbed("Resume", $"Player has been resumed by {username}"));
            }
            else
            {
                await textChannel.SendMessageAsync(embed: embedService.GetMessageEmbed("Resume", $"Player has been resumed by {username}")).ConfigureAwait(false);
            }
            await signalService.SendResume(serverId, username);
        }

        public async Task SkipAsync(string serverId, string username, string voiceChannelId, string chatChannelId = "", Func<Embed, Task>? followUpAction = null)
        {
            var guild = discordSocketClient.GetGuild(ulong.Parse(serverId));
            SocketTextChannel? textChannel = null;

            if (!string.IsNullOrEmpty(chatChannelId))
            {
                if (ulong.TryParse(chatChannelId, out ulong channelId))
                {
                    textChannel = guild.GetTextChannel(channelId);
                }
            }
            else
            {
                textChannel = guild.Channels
                    .OfType<SocketTextChannel>()
                    .FirstOrDefault(x => x.Name == "bot-commands");
            }

            if (textChannel == null && followUpAction == null)
            {
                Console.WriteLine("Response channel not found or is not a text channel");
                return;
            }
            VoteLavalinkPlayer? player = await GetPlayerAsync(serverId, voiceChannelId);
            await player.SkipAsync();
            if (followUpAction != null)
            {
                await followUpAction(embedService.GetMessageEmbed("Skipped", $"Song has been skipped by: {username}"));
            }
            else
            {
                await textChannel.SendMessageAsync(embed: embedService.GetMessageEmbed("Skipped", $"Song has been skipped by: {username}")).ConfigureAwait(false);
            }
            await signalService.SendSkip(serverId, username);
        }

        public async Task StopAsync(string serverId, string username, string voiceChannelId, string chatChannelId = "", Func<Embed, Task>? followUpAction = null)
        {
            var guild = discordSocketClient.GetGuild(ulong.Parse(serverId));
            SocketTextChannel? textChannel = null;

            if (!string.IsNullOrEmpty(chatChannelId))
            {
                if (ulong.TryParse(chatChannelId, out ulong channelId))
                {
                    textChannel = guild.GetTextChannel(channelId);
                }
            }
            else
            {
                textChannel = guild.Channels
                    .OfType<SocketTextChannel>()
                    .FirstOrDefault(x => x.Name == "bot-commands");
            }

            if (textChannel == null && followUpAction == null)
            {
                Console.WriteLine("Response channel not found or is not a text channel");
                return;
            }
            VoteLavalinkPlayer? player = await GetPlayerAsync(serverId, voiceChannelId);
            await player.StopAsync();
            if (followUpAction != null)
            {
                await followUpAction(embedService.GetMessageEmbed("Stopped", $"Player has been stopped by: {username}"));
            }
            else
            {
                await textChannel.SendMessageAsync(embed: embedService.GetMessageEmbed("Stopped", $"Player has been stopped by: {username}")).ConfigureAwait(false);
            }
            await signalService.SendStop(serverId, username);
        }


        private async ValueTask<VoteLavalinkPlayer?> GetPlayerAsync(string serverId, string voiceChannelId, bool connectToVoiceChannel = true)
        {
            var retrieveOptions = new PlayerRetrieveOptions(
                ChannelBehavior: connectToVoiceChannel ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None);

            var options = new VoteLavalinkPlayerOptions();
            var optionsWrapper = Microsoft.Extensions.Options.Options.Create(options);

            var result = await audioService.Players
                .RetrieveAsync(ulong.Parse(serverId), ulong.Parse(voiceChannelId), playerFactory: PlayerFactory.Vote, optionsWrapper, retrieveOptions: retrieveOptions)
                .ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                var errorMessage = result.Status switch
                {
                    PlayerRetrieveStatus.UserNotInVoiceChannel => "You are not connected to a voice channel.",
                    PlayerRetrieveStatus.BotNotConnected => "The bot is currently not connected.",
                    _ => "Unknown error.",
                };

                //await FollowupAsync(errorMessage).ConfigureAwait(false);
                return null;
            }

            return result.Player;
        }
    }
}
