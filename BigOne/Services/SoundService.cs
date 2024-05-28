using Discord.WebSocket;

namespace BigOne.Services
{
    public interface ISoundService
    {
        Task PlaySound();
    }
    public class SoundService(
        ISignalService signalService,
        IEmbedService embedService,
        [FromKeyedServices("SoundBotSocketClient")] DiscordSocketClient discordSocketClient,
        ApplicationDbContext context
        ) : ISoundService
    {
        public async Task PlaySound()
        {
            Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";C:\\Users\\sherg\\source\\repos\\BigOne\\BigOne\\opus.dll\"");

        }
    }
}
