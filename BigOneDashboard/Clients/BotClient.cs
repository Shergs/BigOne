using BigOneDashboard.Models;
using System.Text.Json;

namespace BigOneDashboard.Clients
{
    public interface IBotClient
    {
        Task<Song> GetPlayerSong(string serverId);
        Task<List<Song>> GetPlayerSongs(string serverId);
        Task<int> GetPlayerPosition(string serverId);    
    }
    public class BotClient(
        HttpClient httpClient
        ) : IBotClient
    {
        public async Task<Song> GetPlayerSong(string serverId)
        {
            var response = await httpClient.GetAsync($"Player/getplayersong?serverId={serverId}");
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Request failed with status code {response.StatusCode}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(responseString))
            {
                return new Song();
            }

            try
            {
                Song song = JsonSerializer.Deserialize<Song>(responseString);
                return song;
            }
            catch (JsonException ex)
            {
                Console.WriteLine("Error deserializing the response: " + ex.Message);
                return new Song(); 
            }
        }

        public async Task<List<Song>> GetPlayerSongs(string serverId)
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<List<Song>>($"Player/getplayersongs?serverId={serverId}");
                return response ?? new List<Song>();
            }
            catch (Exception e)
            {
                return new List<Song>();
            }
        }

        public async Task<int> GetPlayerPosition(string serverId)
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<int>($"Player/getplayerposition?serverId={serverId}");
                return response;
            }
            catch (Exception e)
            {
                return 0;
            }
        }
    }
}
