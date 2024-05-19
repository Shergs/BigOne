using System.Text.Json.Serialization;

public class Song
{
    public Song()
    {
        Name = "";
        Url = "";
        DiscordUsername = "";
    }

    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("url")]
    public string Url { get; set; }
    [JsonPropertyName("DiscordUsername")]
    public string DiscordUsername { get; set; }
    [JsonPropertyName("artist")]
    public string Artist { get; set; }
}
