namespace BigOne.Modules.SoundBotModules;

using Discord.Audio;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Lavalink4NET;
using BigOne.Util;

[RequireContext(ContextType.Guild)]
public sealed class SoundModule : InteractionModuleBase<SocketInteractionContext>
{
    private ApplicationDbContext _applicationDbContext;

    public SoundModule(ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext;
    }

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
            Sound? sound = _applicationDbContext.Sounds.Where(x => x.Name.ToLower() == soundName.ToLower()).FirstOrDefault();
            if (sound == null)
            {
                Console.WriteLine("Error: Cannot find sound file.");
                await FollowupAsync("Can't find sound in database");
                return;
            }
            string path = $"C:\\Workspace_Git\\BigOne\\BigOne\\Sounds\\{sound.FilePath.Replace(" ", "_")}";
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
            await FollowupAsync("SoundBoard: " + $"{soundName}").ConfigureAwait(false);
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

    [SlashCommand("soundboard", description: "List out all of the sounds on this server's soundboard", runMode: RunMode.Async)]
    public async Task SoundBoard()
    {
        await DeferAsync().ConfigureAwait(false);

        List<Sound> sounds = _applicationDbContext.Sounds.Where(x => x.ServerId == Context.Guild.Id.ToString()).ToList();
        if (sounds.Count == 0)
        {
            await FollowupAsync("No sounds found for server").ConfigureAwait(false);
            return;
        }
        string result = "";
        for (int i = 0; i < sounds.Count; i++)
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
}
