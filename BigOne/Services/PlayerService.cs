using Discord;
using Discord.Interactions;
using Google.Api;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Vote;
using Lavalink4NET.Rest.Entities.Tracks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace BigOne.Services
{
    public interface IPlayerService 
    {
        Task<Embed> PlayAsync(string serverId, string query);
        Task PlaySoundAsync();
    }
    public class PlayerService(
        IAudioService audioService,
        ISignalService signalService,
        IEmbedService embedService,
        ApplicationDbContext context
        ) : IPlayerService
    {
        public async Task<Embed> PlayAsync(string serverId, string query, string username, string voiceChannelId)
        {
            //await audioService.Players.JoinAsync(
            //    ulong.Parse(serverId),
            //    ulong.Parse(voiceChannelId),
            //    playerFactory: CreatePlayer,
            //    options: new VoteLavalinkPlayerOptions()
            //    );
            //VoteLavalinkPlayer? player = await audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(ulong.Parse(serverId));

            // Attempt to get the existing player first
            //VoteLavalinkPlayer player = await audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(ulong.Parse(serverId));

            //// If no player is found, join a new channel
            //if (player is null)
            //{
            //    player = await audioService.Players.JoinAsync<VoteLavalinkPlayer, VoteLavalinkPlayerOptions>(
            //        ulong.Parse(serverId),
            //        ulong.Parse(voiceChannelId),
            //        playerFactory: PlayerFactory.Vote,
            //        options: new VoteLavalinkPlayerOptions() // Ensure these options are correctly set up
            //    );
            //}

            //if (player.VoiceChannelId != ulong.Parse(voiceChannelId))
            //{ 

            //}

            //if (player is null)
            //{
            //    return embedService.GetErrorEmbed("😖 Player not found");
            //}

            VoteLavalinkPlayer player = await GetPlayerAsync(serverId, voiceChannelId);

            var track = await audioService.Tracks
                .LoadTrackAsync(query, TrackSearchMode.YouTube)
                .ConfigureAwait(false);

            if (track is null)
            {
                return embedService.GetErrorEmbed("😖 No results.");
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
                //if (currentTrack is null)
                //{
                //    await RespondAsync($"🔈Playing: {track.Uri}").ConfigureAwait(false);
                //    return;
                //}

                Embed embed = embedService.GetPlayerEmbed(
                    currentTrack.Track!.Title,
                    currentTrack.Track!.Uri.ToString(),
                    currentTrack.Track!.Author,
                    currentTrack.Track!.SourceName
                    );

                //var embedBuilder = new EmbedBuilder()
                //                .WithColor(Color.Blue)
                //                .WithDescription($"🔈Now playing: [{currentTrack.Track!.Title}]({currentTrack.Track!.Uri})\n" +
                //                    $"Link: {currentTrack.Track!.Uri}") // Make the title a clickable link
                //                .AddField("Artist", currentTrack.Track!.Author, inline: true)
                //                .AddField("Source", currentTrack.Track!.SourceName, inline: true)
                //                .WithFooter(footer => footer.Text = "Play some more songs.")
                //                .WithCurrentTimestamp();

                //var embed = embedBuilder.Build();

                await Context.Channel.SendMessageAsync($"🔈Playing: {currentTrack.Track!.Uri}").ConfigureAwait(false);
                await FollowupAsync(embed: embed).ConfigureAwait(false);
                await signalService.SendNowPlaying(serverId, track.Title, track.Uri.ToString(), username, DateTime.Now.ToString(), track.Author);
            }
            else
            {
                var currentTrack = player.CurrentItem;
                if (currentTrack is null)
                {
                    await RespondAsync($"🔈Playing: {track.Uri}").ConfigureAwait(false);
                    return;
                }

                Embed embed = embedService.GetPlayerEmbed(
                    track.Title,
                    track.Uri.ToString(),
                    track.Author,
                    track.SourceName
                    );

                //var embedBuilder = new EmbedBuilder()
                //                .WithColor(Color.Blue)
                //                .WithDescription($"🔈Added to Queue: [{track.Title}]({currentTrack.Track!.Uri})" +
                //                    $"Link: {currentTrack.Track!.Uri}")
                //                .AddField("Artist", currentTrack.Track!.Author, inline: true)
                //                .AddField("Source", currentTrack.Track!.SourceName, inline: true)
                //                .WithFooter(footer => footer.Text = "Play some more songs.")
                //                .WithCurrentTimestamp();

                //var embed = embedBuilder.Build();

                await Context.Channel.SendMessageAsync($"Queing: {currentTrack.Track!.Uri}").ConfigureAwait(false);
                await FollowupAsync(embed: embed).ConfigureAwait(false);
                await signalService.SendQueueUpdated(serverId, track.Title, track.Uri.ToString(), position.ToString(), "add", username, DateTime.Now.ToString());
            }
        }

        private async ValueTask<VoteLavalinkPlayer?> GetPlayerAsync(string serverId, string voiceChannelId, bool connectToVoiceChannel = true)
        {
            var retrieveOptions = new PlayerRetrieveOptions(
                ChannelBehavior: connectToVoiceChannel ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None);

            var result = await audioService.Players
                .RetrieveAsync(ulong.Parse(serverId), ulong.Parse(voiceChannelId), playerFactory: PlayerFactory.Vote, retrieveOptions)
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

        public async Task PlaySoundAsync()
        { 
            
        }
    }
}
