using Microsoft.AspNetCore.SignalR;

namespace BigOne.Hubs
{
    public class PlayerHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            string? serverId = Context.GetHttpContext()?.Request.Query["serverId"];
            await AddToGroup(serverId ?? "");
            await Clients.All.SendAsync("ReceiveNowPlaying", $"{Context.ConnectionId} has joined");
        }

        public async Task SendPlayerInfo(string name, string url)
        {
            await Clients.All.SendAsync("ReceiveNowPlaying", name, url);
        }

        public async Task AddToGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await Clients.Group(groupName).SendAsync("Send", $"{Context.ConnectionId} has joined the group {groupName}.");
        }

        public async Task RemoveFromGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            await Clients.Group(groupName).SendAsync("Send", $"{Context.ConnectionId} has left the group {groupName}.");
        }

        public async Task SendPlayerInfoToGroup(string groupName, string message)
        {
            await Clients.Group(groupName).SendAsync("ReceiveNowPlaying", message);
        }
    }
}
