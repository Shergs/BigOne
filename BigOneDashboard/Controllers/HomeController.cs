using BigOneDashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using BigOneDashboard.Data;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using AspNet.Security.OAuth.Discord;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using BigOneData.Migrations;
using System.Text.Json;
using BigOneDashboard.SharedAPI;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using Azure.Core;
using BigOneDashboard.Areas.DiscordAuth;
using Google.Apis.Http;
using System.Globalization;

namespace BigOneDashboard.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index(string? serverId)
        {
            bool isAuth = await HandleAuth();
            if (isAuth)
            {
                if (_configuration["Env:IsLocal"] == "true")
                {
                    serverId = "783190942806835200";
                }
                if (serverId != null)
                {
                    HttpContext.Session.SetString("ServerId", serverId);
                }
                return View(await HydrateDashboardViewModel(serverId));
            }
            else
            {
                return View("LoginPage");
            }
        }

        public async Task<bool> HandleAuth()
        {
            if (!User.Identity.IsAuthenticated)
            {
                // Store this somewhere more persistent. Although server side session might be fine.
                string userId = HttpContext.Session.GetString("UserId") ?? "";

                bool isLocal = _configuration["Env:IsLocal"] == "true";
                if (isLocal)
                { 
                    userId = "140910636488982529";
                }

                TokenService tokenService = new TokenService(_context, _configuration);
                if (userId != "")
                {
                    string userToken = await tokenService.GetAccessTokenAsync(userId);
                    if (userToken == "")
                    {
                        return false;
                    }
                    string botAccessToken = _configuration["Discord:BotKey"] ?? "";

                    string userGuildsString = await DiscordAPI.GetUserGuilds(userToken);
                    string username = await DiscordAPI.GetUserInfo(userToken);
                    string botGuildsString = await DiscordAPI.GetBotGuilds(botAccessToken);

                    UserData user = JsonConvert.DeserializeObject<UserData>(username);

                    // Set session
                    HttpContext.Session.SetString("UserGuilds", userGuildsString);
                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetString("BotGuilds", botGuildsString);

                    List<Guild> userGuilds = JsonConvert.DeserializeObject<List<Guild>>(HttpContext.Session.GetString("UserGuilds"));
                    List<Guild> botGuilds = JsonConvert.DeserializeObject<List<Guild>>(HttpContext.Session.GetString("BotGuilds"));
                    List<Guild> availableGuilds = userGuilds.Where(x => botGuilds.Any(y => x.Id == y.Id)).ToList();
                    HttpContext.Session.SetString("AvailableGuilds", JsonConvert.SerializeObject(availableGuilds));

                    return true;
                }
                return false;
            }
            return true;
        }

        public async Task<DashboardViewModel> HydrateDashboardViewModel(string? serverId)
        {
            List<Guild> currentAvailableGuilds = new List<Guild>();
            if (HttpContext.Session.GetString("AvailableGuilds") != null)
            {
                currentAvailableGuilds = JsonConvert.DeserializeObject<List<Guild>>(HttpContext.Session.GetString("AvailableGuilds"));
            }
            Guild? guild = null;
            if (serverId != null)
            {
                guild = currentAvailableGuilds.Where(x => x.Id == serverId).FirstOrDefault();
                string guildString = JsonConvert.SerializeObject(guild);
                HttpContext.Session.SetString("CurrentGuild", guildString);
            }

            if (HttpContext.Session.GetString("CurrentGuild") != null)
            {
                guild = currentAvailableGuilds.Where(x => x.Id == JsonConvert.DeserializeObject<Guild>(HttpContext.Session.GetString("CurrentGuild")).Id).FirstOrDefault();
            }

            string? guilds = HttpContext.Session.GetString("AvailableGuilds");
            List<Guild>? availableGuilds = null;
            if (guilds != null)
            {
                availableGuilds = JsonConvert.DeserializeObject<List<Guild>>(guilds);
            }

            List<Sound> sounds = guild != null ? await _context.Sounds.Where(x => x.ServerId == guild.Id).ToListAsync() : new List<Sound>();
            DashboardViewModel dashboardViewModel = new DashboardViewModel();
            dashboardViewModel.Sounds = sounds;
            dashboardViewModel.SaveNewSoundViewModel = guild != null ? new SaveNewSoundViewModel(guild.Id) : new SaveNewSoundViewModel();
            dashboardViewModel.EditSoundViewModel = guild != null ? new EditSoundViewModel(guild.Id) : new EditSoundViewModel();
            dashboardViewModel.DeleteSoundViewModel = guild != null ? new DeleteSoundViewModel(guild.Id) : new DeleteSoundViewModel();
            dashboardViewModel.DiscordName = HttpContext.Session.GetString("Username") ?? "";
            dashboardViewModel.AvailableGuilds = availableGuilds ?? new List<Guild>();
            if (guild != null)
            {
                dashboardViewModel.Guild = guild;
                dashboardViewModel.serverId = guild.Id;
            }
            dashboardViewModel.botUrl = _configuration["Bot:BaseUrl"] ?? "";

            return dashboardViewModel;
        }

        #region DiscordAuth
        public async Task<IActionResult> LoginPage()
        {
            return View();
        }

        [HttpGet("signin-with-discord")]
        public async Task<IActionResult> DiscordSignIn()
        {
            return Challenge(new AuthenticationProperties { RedirectUri = Url.Action(nameof(HandleDiscordCallback)) }, _configuration["Discord:AuthScheme"]);
        }

        [HttpGet("signin-discord-callback")]
        public async Task<IActionResult> HandleDiscordCallback()
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(_configuration["Discord:AuthScheme"]);
            if (!authenticateResult.Succeeded)
                return BadRequest();

            //var claims = authenticateResult.Principal.Identities.FirstOrDefault().Claims;
            //string userId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            // Store tokens in session
            string accessToken = authenticateResult.Properties.Items[".Token.access_token"] ?? ""; 
            string refreshToken = authenticateResult.Properties.Items[".Token.refresh_token"] ?? "";

            // Extract the expiry date string
            if (!authenticateResult.Properties.Items.TryGetValue(".expires", out var expiryDateString))
            {
                return BadRequest("Expiration date not found.");
            }

            // Parse the GMT expiry date string to a DateTime object
            if (!DateTime.TryParseExact(expiryDateString, "ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var expiryDate))
            {
                return BadRequest("Invalid expiration date format.");
            }

            // Calculate the remaining seconds from now until the expiry date
            var secondsRemaining = (int)(expiryDate - DateTime.UtcNow).TotalSeconds;
            secondsRemaining = Math.Max(0, secondsRemaining);  // Ensure it doesn't go negative


            HttpContext.Session.SetString("access_token", accessToken);
            HttpContext.Session.SetString("refresh_token", refreshToken);
            // Get bot access token
            var botAccessToken = _configuration["Discord:BotKey"] ?? "";

            // store tokens with identity
            var tokens = new List<AuthenticationToken>
            {
                new AuthenticationToken { Name = "access_token", Value = authenticateResult.Properties.Items[".Token.access_token"] ?? "" },
                new AuthenticationToken { Name = "refresh_token", Value = authenticateResult.Properties.Items[".Token.refresh_token"] ?? "" }
            };

            var authProperties = new AuthenticationProperties() { IsPersistent = true };
            authProperties.StoreTokens(tokens);

            // Sign in the user
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, authenticateResult.Principal, authProperties);

            string userString = await DiscordAPI.GetUserInfo(accessToken);
            UserData user = JsonConvert.DeserializeObject<UserData>(userString);
            await StoreUser(user);
            await StoreToken(user.UserId, accessToken, refreshToken, secondsRemaining);

            string userGuildsString = await DiscordAPI.GetUserGuilds(accessToken);
            string botGuildsString = await DiscordAPI.GetBotGuilds(botAccessToken);

            HttpContext.Session.SetString("UserGuilds", userGuildsString);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("UserId", user.UserId);
            HttpContext.Session.SetString("UserInfo", userString);
            HttpContext.Session.SetString("BotGuilds", botGuildsString);

            List<Guild> userGuilds = JsonConvert.DeserializeObject<List<Guild>>(HttpContext.Session.GetString("UserGuilds"));
            List<Guild> botGuilds = JsonConvert.DeserializeObject<List<Guild>>(HttpContext.Session.GetString("BotGuilds"));
            List<Guild> availableGuilds = userGuilds.Where(x => botGuilds.Any(y => x.Id == y.Id)).ToList();
            HttpContext.Session.SetString("AvailableGuilds", JsonConvert.SerializeObject(availableGuilds));

            return RedirectToAction("Index");
        }

        public async Task StoreUser(UserData user)
        {
            ApplicationUser? currentUser = await _context.ApplicationUsers.OrderByDescending(x => x.Id).Where(x => x.UserId == user.UserId).FirstOrDefaultAsync();
            if (currentUser != null)
            {
                // In case their userId/username changes
                currentUser.UserId = user.UserId;
                currentUser.Username = user.Username;
                _context.Update(currentUser);
                await _context.SaveChangesAsync();
            }

            ApplicationUser appUser = new ApplicationUser();
            appUser.Username = user.Username;
            appUser.UserId = user.UserId;

            _context.ApplicationUsers.Add(appUser);
            await _context.SaveChangesAsync();
        }

        public async Task StoreToken(string userId, string accessToken, string refreshToken, int expires)
        {
            UserToken? currentToken = await _context.UserTokens.OrderByDescending(x => x.Expiry).Where(x => x.UserId == userId).FirstOrDefaultAsync();
            if (currentToken != null)
            { 
                currentToken.Expiry = DateTime.UtcNow.AddSeconds(expires);
                _context.Update(currentToken);
                await _context.SaveChangesAsync();
                return;
            }

            UserToken userToken = new UserToken();
            userToken.UserId = userId;
            userToken.AccessToken = accessToken;
            userToken.RefreshToken = refreshToken;
            userToken.Expiry = DateTime.UtcNow.AddSeconds(expires);
            _context.UserTokens.Add(userToken);
            await _context.SaveChangesAsync();
        }
        #endregion

        /// <summary>
        /// Save sales settings
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveNewSound([Bind(Prefix = "SaveNewSoundViewModel")] SaveNewSoundViewModel saveModel)
        {
            // Regex to validate single emoji
            var regex = new Regex(@"^([\uD800-\uDBFF][\uDC00-\uDFFF])$");

            if (!regex.IsMatch(saveModel.Emote))
            {
                TempData["Message"] = "Please enter exactly one emoji.";
                TempData["MessageType"] = "Error";
                ModelState.AddModelError("Emote", "Please enter exactly one emoji.");
                return View("Index", await HydrateDashboardViewModel(saveModel.serverId));
            }

            if (_context.Sounds.Any(x => x.Name == saveModel.Name && x.ServerId == saveModel.serverId))
            {
                TempData["Message"] = "Sound name already exists for this server";
                TempData["MessageType"] = "Error";
                ModelState.AddModelError("Emote", "Sound name already exists.");
                return View("Index", await HydrateDashboardViewModel(saveModel.serverId));
            }

            if (ModelState.IsValid && saveModel.File != null && saveModel.File.Length > 0)
            {
                TempData["Message"] = "Only MP3 files are allowed.";
                TempData["MessageType"] = "Error";
                var fileName = Path.GetFileName(saveModel.File.FileName);
                var extension = Path.GetExtension(fileName).ToLowerInvariant();

                if (extension != ".mp3")
                {
                    ModelState.AddModelError("File", "Only MP3 files are allowed.");
                    return View("Index", await HydrateDashboardViewModel(saveModel.serverId));
                }

                var uploadsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Sounds");

                // Ensure the directory exists
                if (!Directory.Exists(uploadsDirectory))
                {
                    Directory.CreateDirectory(uploadsDirectory);
                }

                // Construct a file path
                var filePath = Path.Combine(uploadsDirectory, saveModel.File.FileName.Replace(" ","_"));

                // Save the file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await saveModel.File.CopyToAsync(fileStream);
                }

                Sound sound = new Sound();
                sound.Name = saveModel.Name;
                sound.Emote = saveModel.Emote;
                sound.FilePath = saveModel.File.FileName.Replace(" ", "_");
                sound.ServerId = saveModel.serverId;

                _context.Sounds.Add(sound);
                await _context.SaveChangesAsync();  

                TempData["Message"] = "Sound Saved Successfully!";
                TempData["MessageType"] = "Success";
                return View("Index", await HydrateDashboardViewModel(saveModel.serverId));
            }
            else
            {
                TempData["Message"] = "Sound failed to be saved. Please try again.";
                TempData["MessageType"] = "Error";
                return View("Index", await HydrateDashboardViewModel(saveModel.serverId));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSound([Bind(Prefix = "EditSoundViewModel")] EditSoundViewModel editModel)
        {
            if (ModelState.IsValid)
            {
                var sound = await _context.Sounds.FindAsync(editModel.Id);
                if (sound == null)
                {
                    TempData["Message"] = "Sound Settings Failed To Save.";
                    TempData["MessageType"] = "Error";
                    return View("Index", await HydrateDashboardViewModel(editModel.serverId));
                }

                if (_context.Sounds.Any(x => x.Name == editModel.Name && x.ServerId == editModel.serverId))
                {
                    TempData["Message"] = "Sound Settings Failed To Save. Another sound exists with the same name on the selected server.";
                    TempData["MessageType"] = "Error";
                    return View("Index", await HydrateDashboardViewModel(editModel.serverId));
                }
                sound.Name = editModel.Name;
                sound.Emote = editModel.Emote;
                await _context.SaveChangesAsync();

                TempData["Message"] = "Settings Saved Successfully!";
                TempData["MessageType"] = "Success";
                return View("Index", await HydrateDashboardViewModel(editModel.serverId));
            }

            TempData["Message"] = "Sound Settings Failed To Save.";
            TempData["MessageType"] = "Error";
            return View("Index", await HydrateDashboardViewModel(editModel.serverId));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSound([Bind(Prefix = "DeleteSoundViewModel")] DeleteSoundViewModel deleteModel)
        {
            if (ModelState.IsValid)
            {
                var sound = await _context.Sounds.FindAsync(deleteModel.Id);
                if (sound != null)
                {
                    _context.Sounds.Remove(sound);
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Sound Deleted Successfully!";
                    TempData["MessageType"] = "Success";
                }
                else
                {
                    TempData["Message"] = "Failed To Delete Sound. Sound not found.";
                    TempData["MessageType"] = "Error";
                }
                return View("Index", await HydrateDashboardViewModel(deleteModel.serverId));
            }
            else
            {
                TempData["Message"] = "Failed To Delete Sound.";
                TempData["MessageType"] = "Error";
                return View("Index", await HydrateDashboardViewModel(deleteModel.serverId));
            }
        }

        #region SoundAPI
        public async Task<IActionResult> GetSound(string soundName, string filePath)
        {
            if (soundName == "")
            {
                return Json(new { audioUrl = $"/Sounds/{filePath}" });
            }
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "Sounds", filePath);
            if (!System.IO.File.Exists(fullPath))
            {
                // Download file from the bot first
                // Will implement a service to do this every certain amount of time at some point
                bool soundDownloaded = await TryGetSound(soundName, fullPath);
                if (!soundDownloaded)
                {
                    return null;
                }
            }
            var contentType = "audio/mp3";
            var fileName = Path.GetFileName(fullPath);
            return PhysicalFile(fullPath, contentType, fileName);
        }

        public async Task<bool> TryGetSound(string soundName, string path)
        {
            try
            {
                Console.WriteLine("Downloading file from API...");
                using (var client = new HttpClient())
                {
                    string endpoint = $"{_configuration["Bot:BaseUrl"]}/get-sound/{soundName.Replace(" ", "_")}";
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

        [HttpGet]
        public async Task<IActionResult> GetSoundDetails(int id)
        {
            var sound = await _context.Sounds.FindAsync(id);
            if (sound == null)
                return NotFound();

            return Json(new
            {
                name = sound.Name,
                emote = sound.Emote,
                filePath = sound.FilePath
            });
            //return SoundAPI.GetSoundDetails(id);
        }

        [HttpGet("get-sound/{soundName}")]
        public async Task<IActionResult> GetServeSound(string soundName)
        {
            Sound? sound = await _context.Sounds.Where(x => x.Name == soundName.Replace("_", " ")).FirstOrDefaultAsync();

            if (sound == null)
            {
                return NotFound("soundName doesn't exist in DB.");
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Sounds", sound.FilePath.Replace(" ","_"));

            if (!System.IO.File.Exists(filePath))
                return NotFound("Sound not found.");

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                stream.CopyTo(memory);
            }
            memory.Position = 0;

            return File(memory, "audio/mpeg", $"{sound.FilePath.Replace(" ","_")}");
        }
        #endregion

        #region TTS

        public async Task<IActionResult> TTSSubmit(string query, string action, string ttsServerId)
        {
            switch (action)
            {
                case "play":
                    // Handle play logic
                    //string outputFileName = await PlayTTS(query);
                    //return await GetSound("", outputFileName);
                    break;
                case "save":
                    // Handle save logic
                    // Just open a save modal to save the output{username}.mp3 with a different name and add to the DB as a sound
                    //await SaveTTS(query);
                    break;
                case "playToServer":
                    // Send a request to the discord bot to play this sound.
                    // Should actually just add playToServer functionality on the soundboard instead.
                    // Handle play to server logic
                    break;
            }
            return View("Index", await HydrateDashboardViewModel(ttsServerId));
        }

        public async Task<IActionResult> PlayTTS(string query)
        {
            // API key
            string apiKey = _configuration["Google:APIKey"];

            // Your Google Cloud Text-to-Speech API key
            string apiUrl = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={apiKey}";

            // Create an HttpClient
            using (HttpClient client = new HttpClient())
            {
                // Setup HTTP request data
                var requestData = new
                {
                    input = new { text = query },
                    voice = new { languageCode = "en-US", ssmlGender = "FEMALE" },
                    audioConfig = new { audioEncoding = "MP3" }
                };

                // Serialize request data to JSON
                string json = System.Text.Json.JsonSerializer.Serialize(requestData);
                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                // Send a POST request
                HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                // Handle response
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(responseContent))
                    {
                        // Extract the audio content from JSON response
                        string audioContent = doc.RootElement.GetProperty("audioContent").GetString();
                        byte[] audioBytes = Convert.FromBase64String(audioContent);

                        // Write the bytes to an MP3 file
                        string outputFileName = $"output{HttpContext.Session.GetString("Username") ?? ""}.mp3";
                        string path = Path.Combine(Directory.GetCurrentDirectory(), "Sounds", outputFileName);
                        System.IO.File.WriteAllBytes(path, audioBytes);
                        Console.WriteLine("Audio content written to file 'output.mp3'");
                        return await GetSound("", outputFileName);
                        //return outputFileName;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Error from API: " + errorContent);
                }
            }
            return null;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTTS([Bind(Prefix = "EditSoundViewModel")] EditSoundViewModel model)
        {
            if (ModelState.IsValid)
            {
                string? username = HttpContext.Session.GetString("Username");
                if (username == null)
                {
                    TempData["Message"] = "Failed To Save TTS. Could not find discord username. Please reauthenticate.";
                    TempData["MessageType"] = "Error";
                    return View("Index", await HydrateDashboardViewModel(model.serverId));
                }
                string path = Path.Combine(Directory.GetCurrentDirectory(), "Sounds", $"output{username}.mp3");
                string fileName = $"{model.Name.Replace(" ", "_")}{username}.mp3";
                string newFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Sounds", fileName);

                if (!System.IO.File.Exists(path))
                {
                    TempData["Message"] = "Failed To Save TTS. 'output.mp3' doesn't exist. Please try again";
                    TempData["MessageType"] = "Error";
                    return View("Index", await HydrateDashboardViewModel(model.serverId));
                }
                
                // this check will break after playing the sound first.
                //if (System.IO.File.Exists(newFilePath))
                //{
                //    TempData["Message"] = $"Failed To Save TTS. {fileName} already exists.";
                //    TempData["MessageType"] = "Error";
                //    return View("Index", await HydrateDashboardViewModel(model.serverId));
                //}

                // Rename file
                System.IO.File.Move(path, newFilePath);

                // Save to db
                Sound sound = new Sound();
                sound.Name = model.Name;
                sound.ServerId = model.serverId;
                sound.Emote = model.Emote;
                sound.FilePath = fileName;

                await _context.AddAsync(sound);
                await _context.SaveChangesAsync();

                TempData["Message"] = $"Failed To Save TTS. {fileName} already exists.";
                TempData["MessageType"] = "Error";
                return View("Index", await HydrateDashboardViewModel(model.serverId));
            }
            else
            {
                TempData["Message"] = $"Failed To Save TTS.";
                TempData["MessageType"] = "Error";
                return View("Index", await HydrateDashboardViewModel(model.serverId));
            }
        }
        #endregion

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
