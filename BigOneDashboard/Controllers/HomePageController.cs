using BigOneDashboard.Models;
using BigOneDashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace BigOneDashboard.Controllers
{
    public class HomePageController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public HomePageController(ILogger<HomeController> logger,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _configuration = configuration;
        }

        public async Task<IActionResult> Home()
        {
            return View();
        }
    }
}
