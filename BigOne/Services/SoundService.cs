using Discord;
using Discord.Audio;
using Discord.WebSocket;
using Google.Api;
using BigOne.Util;
using System.Diagnostics;

namespace BigOne.Services
{
    public interface ISoundService
    {
        Task PlaySoundAsync(string serverId, string soundName, string voiceChannelId, string username, string chatChannelId = "", Func<Embed, Task>? followUpAction = null);
        Task SoundboardAsync(string serverId, string username, string chatChannelId = "", Func<Embed, Task>? followUpAction = null);
    }
    public class SoundService(
        ISignalService signalService,
        IEmbedService embedService,
        [FromKeyedServices("SoundBotSocketClient")] DiscordSocketClient discordSocketClient,
        ApplicationDbContext context
        ) : ISoundService
    {
        public async Task PlaySoundAsync(string serverId, string soundName, string voiceChannelId, string username, string chatChannelId = "", Func<Embed, Task>? followUpAction = null)
        {
            var guild = discordSocketClient.GetGuild(ulong.Parse(serverId));
            var channel = discordSocketClient.GetChannel(ulong.Parse(voiceChannelId)) as IVoiceChannel;
            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";C:\\Users\\sherg\\source\\repos\\BigOne\\BigOne\\opus.dll\"");
            IAudioClient audioClient = null;
            audioClient = await channel.ConnectAsync();
            try 
            {
                Sound? sound = context.Sounds.Where(x => x.Name.ToLower() == soundName.ToLower()).FirstOrDefault();
                string path = $"C:\\Workspace_Git\\BigOne\\BigOne\\Sounds\\{sound.FilePath.Replace(" ", "_")}";
                if (!File.Exists(path))
                {
                    // Going to just do the thing here 
                    bool soundDownloaded = await Util.API.TryGetSound(soundName, path);
                    if (!soundDownloaded)
                    {
                        Console.WriteLine("Error: File does not exist at the specified path.");
                        return;
                    }
                    else
                    {
                        //await FollowupAsync($"Sound Downloaded!");
                        Console.WriteLine("Sound Downloaded!");
                    }
                }

                using (var ffmpeg = CreateProcess(path))
                using (var stream = audioClient.CreatePCMStream(AudioApplication.Mixed))
                {
                    try { await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream); }
                    finally { await stream.FlushAsync(); }
                }
            
                await signalService.SendSoundPlaying(serverId, sound.Emote, sound.Name, username);
            }
            finally
            {
                await audioClient.StopAsync();
            }
        }

        public async Task SoundboardAsync(string serverId, string username, string chatChannelId = "", Func<Embed, Task>? followUpAction = null)
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

            List<Sound> sounds = context.Sounds.Where(x => x.ServerId == serverId).ToList();
            if (sounds.Count == 0)
            {
                await followUpAction(embedService.GetErrorEmbed("😖 No results."));
                return;
            }

            (string, MessageComponent) messageComponent = embedService.GetSoundboardWithButtons(sounds, serverId);

            await followUpAction(embedService.GetMessageEmbed("Soundboard", "Soundboard for this server:"));
            await textChannel.SendMessageAsync(text: messageComponent.Item1, components: messageComponent.Item2);
        }

        private Process CreateProcess(string path)
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
    }
}
