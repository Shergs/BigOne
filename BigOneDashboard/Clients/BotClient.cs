using BigOneDashboard.Models;

namespace BigOneDashboard.Clients
{
    public interface IBotClient
    {
        Task<List<Song>> GetPlayerSongs(string serverId);
        Task<string> GetPlayerPosition(string serverId);    
    }
    public class BotClient(
        HttpClient httpClient
        ) : IBotClient
    {
        public async Task<List<Song>> GetPlayerSongs(string serverId)
        {
            return await httpClient.GetFromJsonAsync<List<Song>>(
                $"/getplayersongs?serverId={serverId}");
        }

        public async Task<string> GetPlayerPosition(string serverId)
        {
            return await httpClient.GetFromJsonAsync<string>(
                $"/getplayerposition?serverId={serverId}");
        }
    }
}
