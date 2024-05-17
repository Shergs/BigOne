using BigOne.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BigOne.Services
{
    public interface ISignalService 
    {
        Task SendNowPlaying(string groupName, string name, string url, string username);
        Task SendPaused(string groupName, string username);
        Task SendResume(string groupName, string username);
        Task SendSkip(string groupName, string username);    
        Task SendQueueUpdated(string groupName, string trackName, string addOrRemove, string username);    
        Task SendStop(string groupName, string username);
        Task SendSoundPlaying(string groupName, string username, string emoji, string name);  
    }
    public class SignalService(
        IHubContext<PlayerHub> _hubContext
        ) : ISignalService
    {
        public async Task SendNowPlaying(string groupName, string name, string url, string username)
        {
            // Correct way to send a message to a group using IHubContext
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNowPlaying", name, url, username);
        }

        public async Task SendPaused(string groupName, string username)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceivePaused", username);
        }

        public async Task SendResume(string groupName, string username)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveResume", username);
        }

        public async Task SendSkip(string groupName, string username)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveSkip", username);
        }

        public async Task SendQueueUpdated(string groupName, string trackName, string addOrRemove, string username)
        { 
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveQueueUpdated", trackName, addOrRemove, username);
        }

        public async Task SendStop(string groupName, string username)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveStopped", username);
        }
        public async Task SendSoundPlaying(string groupName, string username, string emoji, string name)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveSoundPlaying", username, emoji, name);
        }
    }
}
