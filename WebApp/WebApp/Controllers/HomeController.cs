using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using Newtonsoft.Json;

namespace WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> logger;

        public HomeController(ILogger<HomeController> logger)
        {
            this.logger = logger;
        }

        public IActionResult Index()
        {
            return View();
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
        [Route("PublicHolidays")]
        public async Task<IActionResult> PublicHolidays()
        {
            var apiUrl = "https://date.nager.at/api/v3/PublicHolidays/2025/PL";

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetStringAsync(apiUrl);
                    var holidays = JsonConvert.DeserializeObject<List<PublicHoliday>>(response);
                    logger.LogInformation("Successfully fetched public holidays.");
                    return View(holidays);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to fetch public holidays from {Url}", apiUrl);
                    return View(new List<PublicHoliday>());
                }
            }
        }
    }
}
