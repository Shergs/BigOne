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
                // Handle the case where the response is not successful
                throw new HttpRequestException($"Request failed with status code {response.StatusCode}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(responseString))
            {
                // Handle the empty string response and return a default Song object
                return new Song();
            }

            // Deserialize the response string to a Song object
            try
            {
                // Deserialize the response string to a Song object
                Song song = JsonSerializer.Deserialize<Song>(responseString);
                return song;
            }
            catch (JsonException ex)
            {
                // Handle or log the exception if the JSON is invalid
                Console.WriteLine("Error deserializing the response: " + ex.Message);
                return new Song(); // or handle differently, perhaps return null
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
