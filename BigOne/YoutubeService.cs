using Google.Apis.YouTube.v3;
using Google.Apis.Services;
using System.Threading.Tasks;

public class YoutubeAPIService
{
    private readonly YouTubeService _youtubeService;

    public YoutubeAPIService(string apiKey)
    {
        _youtubeService = new YouTubeService(new BaseClientService.Initializer()
        {
            ApiKey = apiKey,
            ApplicationName = this.GetType().ToString()
        });
    }

    public async Task<string> GetYouTubeVideoUrl(string query)
    {
        var searchListRequest = _youtubeService.Search.List("snippet");
        searchListRequest.Q = query; // the search query
        searchListRequest.MaxResults = 1; // we need only one result

        // Call the search.list method to retrieve results matching the specified query term.
        var searchListResponse = await searchListRequest.ExecuteAsync();

        foreach (var searchResult in searchListResponse.Items)
        {
            if (searchResult.Id.Kind == "youtube#video")
            {
                // Return the first video's URL
                return $"https://www.youtube.com/watch?v={searchResult.Id.VideoId}";
            }
        }

        return null; // return null if no videos found
    }
}