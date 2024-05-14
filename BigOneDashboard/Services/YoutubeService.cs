using Microsoft.AspNetCore.Mvc;
using BigOneDashboard.Clients;
using System.Diagnostics;

namespace BigOneDashboard.Services
{
    public interface IYoutubeService
    {
        Task<IActionResult> GetMP3FromYoutube(string url);
        Task<string> GetEmbedFromUrl(string url);
    }

    public class YoutubeService(
        IYoutubeClient youtubeClient) : ControllerBase, IYoutubeService
    {
        public async Task<IActionResult> GetMP3FromYoutube(string url)
        {
            var videoId = youtubeClient.GetVideoIdFromUrl(url);
            if (string.IsNullOrEmpty(videoId))
            {
                return BadRequest("Invalid YouTube URL");
            }

            var downloadFolder = Path.Combine("Downloads", videoId);
            Directory.CreateDirectory(downloadFolder);
            var filePath = Path.Combine(downloadFolder, $"{videoId}.mp3");

            if (System.IO.File.Exists(filePath))
            {
                // Return JSON with the file URL if it already exists
                var mp3Url = $"/downloads/{videoId}/{videoId}.mp3";
                return new JsonResult(new { audioUrl = mp3Url });
            }

            try
            {
                // Run youtube-dl command to download the MP3 file
                var youtubeDlArgs = $"--extract-audio --audio-format mp3 -o \"{filePath}\" {url}";
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "youtube-dl",
                        Arguments = youtubeDlArgs,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    return StatusCode(500, $"youtube-dl error: {error}");
                }

                // Return the MP3 file itself after downloading
                var contentType = "audio/mpeg";
                var fileName = Path.GetFileName(filePath);
                return PhysicalFile(filePath, contentType, fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        public async Task<string> GetEmbedFromUrl(string url)
        {
            var videoId = youtubeClient.GetVideoIdFromUrl(url);
            if (string.IsNullOrEmpty(videoId))
            {
                return null;
            }

            var embedUrl = $"https://www.youtube.com/embed/{videoId}";
            return await Task.FromResult(embedUrl);
        }    
    }
}
