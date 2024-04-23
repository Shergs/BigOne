using System.IO;
using System.Configuration;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BigOne
{
    public class API : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public API(ApplicationDbContext context)
        { 
            _context = context;
        }

        public static async Task<bool> TryGetSound(string soundName, string path)
        {
            try
            {
                Console.WriteLine("Downloading file from API...");
                using (var client = new HttpClient())
                {
                    //string endpoint = $"{System.Configuration.ConfigurationManager.AppSettings["BaseAppUrl"]}/get-sound/{soundName.Replace(" ", "_")}";
                    string endpoint = $"https://bigonedashboard.azurewebsites.net/get-sound/{soundName.Replace(" ", "_")}";
                    var response = await client.GetAsync(endpoint);
                    if (response.IsSuccessStatusCode)
                    {
                        using (var fs = new FileStream(path, FileMode.CreateNew))
                        {
                            await response.Content.CopyToAsync(fs);
                        }
                        Console.WriteLine("File downloaded successfully.");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Failed to download file.");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("TryGetSound Failed");
                return false;
            }
        }

        [HttpGet("get-sound/{soundName}")]
        public async Task<IActionResult> GetServeSound(string soundName)
        {
            Sound? sound = await _context.Sounds.Where(x => x.Name == soundName.Replace("_", " ")).FirstOrDefaultAsync();

            if (sound == null)
            {
                return NotFound("soundName doesn't exist in DB.");
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Sounds", sound.FilePath.Replace(" ", "_"));

            if (!System.IO.File.Exists(filePath))
                return NotFound("Sound not found.");

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                stream.CopyTo(memory);
            }
            memory.Position = 0;

            return File(memory, "audio/mpeg", $"{sound.FilePath.Replace(" ", "_")}");
        }
    }
}
