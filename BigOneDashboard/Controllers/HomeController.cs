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
            if (!User.Identity.IsAuthenticated)
            {
                return View("LoginPage");
            }

            return View(await HydrateDashboardViewModel(serverId));
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

            var claims = authenticateResult.Principal.Identities.FirstOrDefault().Claims;
            string userId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            // Store tokens in session
            HttpContext.Session.SetString("access_token", authenticateResult.Properties.Items[".Token.access_token"]);
            HttpContext.Session.SetString("refresh_token", authenticateResult.Properties.Items[".Token.refresh_token"]);

            // store tokens with identity
            var tokens = new List<AuthenticationToken>
            {
                new AuthenticationToken { Name = "access_token", Value = authenticateResult.Properties.Items[".Token.access_token"] },
                new AuthenticationToken { Name = "refresh_token", Value = authenticateResult.Properties.Items[".Token.refresh_token"] }
            };

            var authProperties = new AuthenticationProperties() { IsPersistent = true };
            authProperties.StoreTokens(tokens);

            // Sign in the user
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, authenticateResult.Principal, authProperties);

            var accessToken = HttpContext.Session.GetString("access_token") ?? "";
            var botAccessToken = _configuration["Discord:BotKey"] ?? "";

            string userGuildsString = await DiscordAPI.GetUserGuilds(accessToken);
            string username = await DiscordAPI.GetUserInfo(accessToken);
            string botGuildsString = await DiscordAPI.GetBotGuilds(botAccessToken);

            HttpContext.Session.SetString("UserGuilds", userGuildsString);
            HttpContext.Session.SetString("Username", username);
            HttpContext.Session.SetString("BotGuilds", botGuildsString);

            List<Guild> userGuilds = JsonConvert.DeserializeObject<List<Guild>>(HttpContext.Session.GetString("UserGuilds"));
            List<Guild> botGuilds = JsonConvert.DeserializeObject<List<Guild>>(HttpContext.Session.GetString("BotGuilds"));
            List<Guild> availableGuilds = userGuilds.Where(x => botGuilds.Any(y => x.Id == y.Id)).ToList();
            HttpContext.Session.SetString("AvailableGuilds", JsonConvert.SerializeObject(availableGuilds));

            return RedirectToAction("Index");
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
        public IActionResult GetSound(string filePath)
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "Sounds", filePath);
            var contentType = "audio/mp3";
            var fileName = Path.GetFileName(fullPath);
            return PhysicalFile(fullPath, contentType, fileName);
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
            Sound? sound = await _context.Sounds.Where(x => x.Name == soundName).FirstOrDefaultAsync();

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
