using System.IO;
using System.Configuration;

namespace BigOne
{
    public static class API
    {
        public static async Task<bool> TryGetSound(string soundName, string path)
        {
            Console.WriteLine("Downloading file from API...");
            using (var client = new HttpClient())
            {
                var response = await client.GetAsync($"{System.Configuration.ConfigurationManager.AppSettings["BaseAppUrl"]}/get-sound/{soundName}");
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
    }
}
