using BigOne.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BigOne.Services
{
    public interface ISignalService 
    {
        Task SendNowPlaying(string groupName, string name, string url, string username, string timestamp, string artist);
        Task SendPaused(string groupName, string username);
        Task SendResume(string groupName, string username);
        Task SendSkip(string groupName, string username, string title, string author);
        Task SendQueueUpdated(string groupName, string trackName, string url, string position, string addOrRemove, string username, string timestamp, string artist, string videoId);
        Task SendStop(string groupName, string username);
        Task SendSoundPlaying(string groupName, string username, string emoji, string name);
        Task SendMoveUpInQueue(string groupName, string username, string position);
        Task SendMoveDownInQueue(string groupName, string username, string position);
        Task SendDeleteFromQueue(string groupName, string username, string position);
        Task SendSeekVideo(string groupName, string username, string position);
    }
    public class SignalService(
        IHubContext<PlayerHub> _hubContext
        ) : ISignalService
    {
        public async Task SendNowPlaying(string groupName, string name, string url, string username, string timestamp, string artist)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNowPlaying", name, url, username, timestamp, artist);
        }

        public async Task SendPaused(string groupName, string username)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceivePaused", username);
        }

        public async Task SendResume(string groupName, string username)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveResume", username);
        }

        public async Task SendSkip(string groupName, string username, string title, string author)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveSkipped", username, title, author);
        }

        public async Task SendQueueUpdated(string groupName, string trackName, string url, string position, string addOrRemove, string username, string timestamp, string artist, string videoId)
        { 
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveQueueUpdated", trackName, url, position, addOrRemove, username, timestamp);
        }

        public async Task SendStop(string groupName, string username)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveStopped", username);
        }
        public async Task SendSoundPlaying(string groupName, string username, string emoji, string name)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveSoundPlaying", username, emoji, name);
        }

        public async Task SendMoveUpInQueue(string groupName, string username, string position)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveMoveUpInQueue", username, position);
        }

        public async Task SendMoveDownInQueue(string groupName, string username, string position)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveMoveDownInQueue", username, position);
        }

        public async Task SendDeleteFromQueue(string groupName, string username, string position)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveDeleteFromQueue", username, position);
        }

        public async Task SendSeekVideo(string groupName, string username, string position)
        {
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveSeekVideo", username, position);
        }
    }
}
