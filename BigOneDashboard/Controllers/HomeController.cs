using BigOneDashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using BigOneDashboard.Data;
using System.Text.RegularExpressions;

namespace BigOneDashboard.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            TempData["Message"] = "Upload Successful!";
            TempData["MessageType"] = "Success";

            List<Sound> sounds = await _context.Sounds.ToListAsync();
            DashboardViewModel dashboardViewModel = new DashboardViewModel();
            dashboardViewModel.Sounds = sounds;
            dashboardViewModel.SaveNewSoundViewModel = new SaveNewSoundViewModel();
            dashboardViewModel.Server = "BigOne";
            dashboardViewModel.DiscordName = "Dicretes";

            return View(dashboardViewModel);
        }

        /// <summary>
        /// Save sales settings
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveNewSound([Bind(Prefix = "SaveNewSoundViewModel")] SaveNewSoundViewModel model)
        {
            // Regex to validate single emoji
            var regex = new Regex(@"^([\uD800-\uDBFF][\uDC00-\uDFFF])$");

            if (!regex.IsMatch(model.Emote))
            {
                TempData["Message"] = "Please enter exactly one emoji.";
                TempData["MessageType"] = "Error";
                ModelState.AddModelError("Emote", "Please enter exactly one emoji.");
                DashboardViewModel dashboardViewModel = new DashboardViewModel();
                List<Sound> sounds = await _context.Sounds.ToListAsync();
                dashboardViewModel.Sounds = sounds;
                dashboardViewModel.SaveNewSoundViewModel = new SaveNewSoundViewModel();
                dashboardViewModel.Server = "BigOne";
                dashboardViewModel.DiscordName = "Dicretes";
                return RedirectToAction("Index", dashboardViewModel);
            }

            if (_context.Sounds.Any(x => x.Name == model.Name))
            {
                TempData["Message"] = "Sound name already exists.";
                TempData["MessageType"] = "Error";
                ModelState.AddModelError("Emote", "Sound name already exists.");
                DashboardViewModel dashboardViewModel = new DashboardViewModel();
                List<Sound> sounds = await _context.Sounds.ToListAsync();
                dashboardViewModel.Sounds = sounds;
                dashboardViewModel.SaveNewSoundViewModel = new SaveNewSoundViewModel();
                dashboardViewModel.Server = "BigOne";
                dashboardViewModel.DiscordName = "Dicretes";
                return RedirectToAction("Index", dashboardViewModel);
            }

            if (ModelState.IsValid && model.File != null && model.File.Length > 0)
            {
                TempData["Message"] = "Only MP3 files are allowed.";
                TempData["MessageType"] = "Error";
                var fileName = Path.GetFileName(model.File.FileName);
                var extension = Path.GetExtension(fileName).ToLowerInvariant();

                if (extension != ".mp3")
                {
                    ModelState.AddModelError("File", "Only MP3 files are allowed.");
                    DashboardViewModel dashboardViewModel = new DashboardViewModel();
                    List<Sound> sounds = await _context.Sounds.ToListAsync();
                    dashboardViewModel.Sounds = sounds;
                    dashboardViewModel.SaveNewSoundViewModel = new SaveNewSoundViewModel();
                    dashboardViewModel.Server = "BigOne";
                    dashboardViewModel.DiscordName = "Dicretes";
                    return RedirectToAction("Index", dashboardViewModel);
                }

                var uploadsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Sounds");

                // Ensure the directory exists
                if (!Directory.Exists(uploadsDirectory))
                {
                    Directory.CreateDirectory(uploadsDirectory);
                }

                // Construct a file path
                var filePath = Path.Combine(uploadsDirectory, model.File.FileName.Replace(" ","_"));

                // Save the file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.File.CopyToAsync(fileStream);
                }

                Sound sound = new Sound();
                sound.Name = model.Name;
                sound.Emote = model.Emote;
                sound.FilePath = model.File.FileName;

                _context.Sounds.Add(sound);
                await _context.SaveChangesAsync();  

                TempData["Message"] = "Settings Saved Successfully!";
                TempData["MessageType"] = "Success";
                return RedirectToAction("Index", new DashboardViewModel());
            }
            else
            {
                TempData["Message"] = "Sound failed to be saved. Please try again.";
                TempData["MessageType"] = "Error";
                return View("Index", new DashboardViewModel { Sounds=new List<Sound>()});
            }
        }

        public IActionResult GetSound(string filePath)
        {
            var fullPath = Path.Combine("C:\\Workspace_Git\\BigOne\\BigOne\\Sounds\\", filePath);
            var contentType = "audio/mp3";
            var fileName = Path.GetFileName(fullPath);
            return PhysicalFile(fullPath, contentType, fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSound([Bind(Prefix = "EditSoundViewModel")] EditSoundViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Retrieve the existing sound from the database
                var sound = await _context.Sounds.FindAsync(model.Id);
                if (sound == null)
                {
                    TempData["Message"] = "Sound Settings Failed To Save.";
                    TempData["MessageType"] = "Error";
                    // If there are validation errors, reload the edit page
                    DashboardViewModel dashModel = new DashboardViewModel();
                    List<Sound> sounds1 = await _context.Sounds.ToListAsync();
                    dashModel.Sounds = sounds1;
                    dashModel.SaveNewSoundViewModel = new SaveNewSoundViewModel();
                    dashModel.Server = "BigOne";
                    dashModel.DiscordName = "Dicretes";
                    return RedirectToAction("Index", dashModel);
                }

                // Update the sound properties
                sound.Name = model.Name;
                sound.Emote = model.Emote;
                await _context.SaveChangesAsync();


                TempData["Message"] = "Settings Saved Successfully!";
                TempData["MessageType"] = "Success";
                DashboardViewModel dashboardModel = new DashboardViewModel();
                List<Sound> Sounds = await _context.Sounds.ToListAsync();
                dashboardModel.Sounds = Sounds;
                dashboardModel.SaveNewSoundViewModel = new SaveNewSoundViewModel();
                dashboardModel.Server = "BigOne";
                dashboardModel.DiscordName = "Dicretes";
                return RedirectToAction("Index", dashboardModel);
            }

            TempData["Message"] = "Sound Settings Failed To Save.";
            TempData["MessageType"] = "Error";
            // If there are validation errors, reload the edit page
            DashboardViewModel dashboardViewModel = new DashboardViewModel();
            List<Sound> sounds = await _context.Sounds.ToListAsync();
            dashboardViewModel.Sounds = sounds;
            dashboardViewModel.SaveNewSoundViewModel = new SaveNewSoundViewModel();
            dashboardViewModel.Server = "BigOne";
            dashboardViewModel.DiscordName = "Dicretes";
            return RedirectToAction("Index", dashboardViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSound([Bind(Prefix = "DeleteSoundViewModel")] DeleteSoundViewModel model)
        {
            if (ModelState.IsValid)
            {
                var sound = await _context.Sounds.FindAsync(model.Id);
                if (sound != null)
                {
                    _context.Sounds.Remove(sound);
                    await _context.SaveChangesAsync();
                }
                TempData["Message"] = "Settings Saved Successfully!";
                TempData["MessageType"] = "Success";
                DashboardViewModel dashboardModel = new DashboardViewModel();
                List<Sound> Sounds = await _context.Sounds.ToListAsync();
                dashboardModel.Sounds = Sounds;
                dashboardModel.SaveNewSoundViewModel = new SaveNewSoundViewModel();
                dashboardModel.Server = "BigOne";
                dashboardModel.DiscordName = "Dicretes";
                return RedirectToAction("Index", dashboardModel);
            }
            else
            {
                TempData["Message"] = "Failed To Delete Sound.";
                TempData["MessageType"] = "Error";
                DashboardViewModel dashboardViewModel = new DashboardViewModel();
                List<Sound> sounds = await _context.Sounds.ToListAsync();
                dashboardViewModel.Sounds = sounds;
                dashboardViewModel.SaveNewSoundViewModel = new SaveNewSoundViewModel();
                dashboardViewModel.Server = "BigOne";
                dashboardViewModel.DiscordName = "Dicretes";
                return RedirectToAction("Index", dashboardViewModel);
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
        }

        [HttpGet("get-sound/{soundName}")]
        public IActionResult GetServeSound(string soundName)
        {
            Sound? sound = await _context.Sounds.Where(x => x.Name == soundName).FirstOrDefaultAsync();

            if (sound == null)
            {
                return NotFound("soundName doesn't exist in DB.");
            }

            var filePath = $"wwwroot/sounds/{soundName}.mp3";
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
