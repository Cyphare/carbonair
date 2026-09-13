using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CarbonAir
{
    internal class AirQualityService
    {
        private string _apiKey;
        private HttpClient _httpClient;

        public AirQualityService(string apiKey)
        {
        }

        public async Task<AirQuality> GetCurrentAQIAsync(string city)
        {
            throw new NotImplementedException();
        }
    }
}