using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Controllers
{
    [Authorize]
    public class PublicHolidayController : Controller
    {
        private readonly GeoLocationService geoLocationService;
        private readonly IHttpClientFactory httpClientFactory;

        public PublicHolidayController(GeoLocationService geoLocationService, IHttpClientFactory httpClientFactory)
        {
            this.geoLocationService = geoLocationService;
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var countryCode = await geoLocationService.GetCountryCodeAsync(HttpContext);

            var client = httpClientFactory.CreateClient();
            var url = $"https://date.nager.at/api/v3/PublicHolidays/{DateTime.Now.Year}/{countryCode}";

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return View(new List<PublicHoliday>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var holidays = JsonConvert.DeserializeObject<List<PublicHoliday>>(json) ?? new();

            return View(holidays);
        }
    }
}