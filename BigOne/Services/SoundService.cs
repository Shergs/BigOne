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
        [FromKeyedServices("SoundBotSocketClient")] DiscordSocketClient discordSocketClient
        ) : ISoundService
    {
        public async Task PlaySound()
        { 
            
        }
    }
}
