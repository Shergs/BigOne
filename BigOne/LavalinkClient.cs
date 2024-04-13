using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

public class LavalinkClient
{
    private readonly HttpClient _httpClient;
    private readonly string _lavalinkBaseUrl;

    public LavalinkClient(string baseUrl, string authToken)
    {
        _lavalinkBaseUrl = baseUrl;
        _httpClient = new HttpClient();
        // Lavalink uses plain tokens, not Basic Auth.
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
    }

    public async Task<string> LoadTrackAsync(string identifier)
    {
        var uri = new Uri($"{_lavalinkBaseUrl}/loadtracks?identifier={Uri.EscapeDataString(identifier)}");

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(uri);
            string content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to load track: {response.StatusCode}");
                Console.WriteLine($"Response content: {content}");
                return null;
            }

            Console.WriteLine($"Loaded Track Info: {content}");
            return content; // You can also parse and return as a specific object
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in loading track: {ex.Message}");
            return null;
        }
    }
}

