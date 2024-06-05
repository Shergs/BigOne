namespace BigOne.Modules.SoundBotModules;

using Discord.Audio;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Lavalink4NET;
using BigOne.Util;
using BigOne.Services;

[RequireContext(ContextType.Guild)]
public sealed class SoundModule : InteractionModuleBase<SocketInteractionContext>
{
    private ApplicationDbContext _applicationDbContext;
    private readonly ISignalService _signalService;
    private readonly ISoundService _soundService;

    public SoundModule(ApplicationDbContext applicationDbContext, ISignalService signalService, ISoundService soundService)
    {
        _applicationDbContext = applicationDbContext;
        _signalService = signalService;
        _soundService = soundService;
    }

    [SlashCommand("sound", description: "Play soundboard sound", runMode: RunMode.Async)]
    public async Task Sound(string soundName)
    {
        await DeferAsync().ConfigureAwait(false);
        var voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
        await _soundService.PlaySoundAsync(Context.Guild.Id.ToString(), soundName, voiceChannel.Id.ToString(), Context.User.Username);
    }

    [SlashCommand("soundboard", description: "List out all of the sounds on this server's soundboard", runMode: RunMode.Async)]
    public async Task SoundBoard()
    {
        await DeferAsync().ConfigureAwait(false);

        Func<Embed, Task> followUpAction = async (embed) =>
        {
            await FollowupAsync(embed: embed).ConfigureAwait(false);
        };

        await _soundService.SoundboardAsync(Context.Guild.Id.ToString(), Context.User.Username, followUpAction: followUpAction);



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
