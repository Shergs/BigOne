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
        Task PlaySoundAsync(string serverId, string soundName, string voiceChannelId, string username);
    }
    public class SoundService(
        ISignalService signalService,
        IEmbedService embedService,
        [FromKeyedServices("SoundBotSocketClient")] DiscordSocketClient discordSocketClient,
        ApplicationDbContext context
        ) : ISoundService
    {
        public async Task PlaySoundAsync(string serverId, string soundName, string voiceChannelId, string username)
        {
            var guild = discordSocketClient.GetGuild(ulong.Parse(serverId));
            var channel = discordSocketClient.GetChannel(ulong.Parse(voiceChannelId)) as IVoiceChannel;
            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";C:\\Users\\sherg\\source\\repos\\BigOne\\BigOne\\opus.dll\"");
            IAudioClient audioClient = null;
            audioClient = await channel.ConnectAsync();
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
