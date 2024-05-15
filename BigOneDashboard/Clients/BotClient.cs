using BigOneDashboard.Models;

namespace BigOneDashboard.Clients
{
    public interface IBotClient
    {
        Task<Song> GetPlayerSong(string serverId);
        Task<List<Song>> GetPlayerSongs(string serverId);
        Task<string> GetPlayerPosition(string serverId);    
    }
    public class BotClient(
        HttpClient httpClient
        ) : IBotClient
    {
        public async Task<Song> GetPlayerSong(string serverId)
        {
            return await httpClient.GetFromJsonAsync<Song>(
                $"Player/getplayersong?serverId={serverId}");
        }

        public async Task<List<Song>> GetPlayerSongs(string serverId)
        {
            return await httpClient.GetFromJsonAsync<List<Song>>(
                $"Player/getplayersongs?serverId={serverId}");
        }

        public async Task<string> GetPlayerPosition(string serverId)
        {
            return await httpClient.GetFromJsonAsync<string>(
                $"Player/getplayerposition?serverId={serverId}");
        }
    }
}
