using BigOneDashboard.Models;
using BigOneDashboard.Clients;
using BigOneDashboard.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace BigOneDashboard.Services
{
    public interface IBotService
    {
        Task<Song> GetPlayerSong(string serverId);
        Task<List<Song>> GetPlayerSongs(string serverId);
        Task<List<SongHistoryItem>> GetPlayerHistory(string serverId);
        Task<int> GetPlayerPosition(string serverId);
    }
    public class BotService(
        IBotClient botClient,
        IBotRepository botRepository) : IBotService
    {
        //public async Task<IActionResult> DownloadYtMP3(string url)
        //{

        //    //var contentType = "audio/mp3";
        //    //var fileName = Path.GetFileName(fullPath);
        //    //return PhysicalFile(fullPath, contentType, fileName);
        //    return;
        //}
        public async Task<Song> GetPlayerSong(string serverId)
        { 
            return await botClient.GetPlayerSong(serverId);
        }

        public async Task<List<Song>> GetPlayerSongs(string serverId)
        {
            // Make the endpoint in the bot for these
            return await botClient.GetPlayerSongs(serverId);
        }

        public async Task<List<SongHistoryItem>> GetPlayerHistory(string serverId)
        {
            return await botRepository.GetPlayerHistory(serverId);
        }

        public async Task<int> GetPlayerPosition(string serverId)
        {
            return await botClient.GetPlayerPosition(serverId);
        }
    }
}
