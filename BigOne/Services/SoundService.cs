using Discord.WebSocket;

namespace BigOne.Services
{
    public interface ISoundService
    { 
    
    }
    public class SoundService(
        ISignalService signalService,
        IEmbedService embedService,
        [FromKeyedServices("SoundBotSocketClient")] DiscordSocketClient discordSocketClient
        ) : ISoundService
    {

    }
}
