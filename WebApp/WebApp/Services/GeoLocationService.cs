using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace WebApp.Services
{
    public class GeoLocationService
    {
        private readonly HttpClient httpClient;

        public GeoLocationService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<string> GetCountryCodeAsync(HttpContext httpContext)
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();

            if (string.IsNullOrEmpty(ipAddress))
                return "PL"; // Domyślnie Polska

            var response = await httpClient.GetAsync($"https://ipapi.co/{ipAddress}/json/");

            if (!response.IsSuccessStatusCode)
                return "PL";

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            if (doc.RootElement.TryGetProperty("country_code", out var countryCodeElement))
            {
                return countryCodeElement.GetString() ?? "PL";
            }

            return "PL";
        }
    }
}