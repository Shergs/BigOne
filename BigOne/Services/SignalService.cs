using BigOne.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BigOne.Services
{
    public interface ISignalService 
    {
        Task SendNowPlaying(string groupName, string name, string url);
        Task SendPaused(string groupName);
        Task SendResume(string groupName);
        Task SendSkip(string groupName);    
        Task SendQueueUpdated(string groupName, string trackName, string addOrRemove);    
        Task SendStop(string groupName);
    }
    public class SignalService(
        IHubContext<PlayerHub> _hubContext
        ) : ISignalService
    {
        public async Task SendNowPlaying(string groupName, string name, string url)
        {
            // Correct way to send a message to a group using IHubContext
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNowPlaying", name, url);
        }

        public async Task SendPaused(string groupName)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceivePaused");
        }

        public async Task SendResume(string groupName)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveResume");
        }

        public async Task SendSkip(string groupName)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveSkip");
        }

        public async Task SendQueueUpdated(string groupName, string trackName, string addOrRemove)
        { 
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveQueueUpdated", trackName, addOrRemove);
        }

        public async Task SendStop(string groupName)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveStopped");
        }
    }
}
