using Microsoft.AspNetCore.Mvc;

namespace BigOneDashboard.Clients
{
    public interface IYoutubeClient
    {
        string GetVideoIdFromUrl(string url);
        Task<string> GetEmbedFromUrl(string url);
    }
    public class YoutubeClient(
        HttpClient httpClient) : IYoutubeClient
    {
        public string GetVideoIdFromUrl(string url)
        {
            var uri = new Uri(url);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            return query["v"];
        }

        public async Task<string> GetEmbedFromUrl(string url)
        {

        }
    }
}
