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
            var response = await httpClient.GetFromJsonAsync<Song>(
                $"Player/getplayersong?serverId={serverId}");
            return response ?? new Song();
        }

        public async Task<List<Song>> GetPlayerSongs(string serverId)
        {
            var response = await httpClient.GetFromJsonAsync<List<Song>>(
                $"Player/getplayersongs?serverId={serverId}");
            return response ?? new List<Song>();
        }

        public async Task<string> GetPlayerPosition(string serverId)
        {
            var response = await httpClient.GetFromJsonAsync<string>(
                $"Player/getplayerposition?serverId={serverId}");
            return response ?? "";
        }
    }
}
