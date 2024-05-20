using BigOneDashboard.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;

namespace BigOneDashboard.Repositories
{
    public interface IBotRepository
    {
        Task<List<SongHistoryItem>> GetPlayerHistory(string serverId);   
    }
    public class BotRepository(
        ApplicationDbContext context
        ) : IBotRepository
    {
        public async Task<List<SongHistoryItem>> GetPlayerHistory(string serverId)
        {
            return await context.SongHistory.Where(song => song.ServerId == serverId && song.Timestamp > DateTime.Now.AddDays(-1)).OrderByDescending(x => x.Timestamp).ToListAsync();
        }

    }
}
