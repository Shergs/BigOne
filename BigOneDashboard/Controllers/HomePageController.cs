using BigOneDashboard.Models;
using BigOneDashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace BigOneDashboard.Controllers
{
    public class HomePageController (
        ILogger<HomePageController> logger,
        ApplicationDbContext context,
        IConfiguration configuration) : Controller
    {
        public async Task<IActionResult> Home()
        {
            return View();
        }
    }
}
