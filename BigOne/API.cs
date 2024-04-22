using System.IO;
using System.Configuration;

namespace BigOne
{
    public static class API
    {
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
    }
}
