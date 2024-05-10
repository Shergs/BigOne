using BigOne.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BigOne.Services
{
    public interface ISignalService 
    {
        Task SendNowPlaying(string groupName, string name, string url);
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
    }
}
