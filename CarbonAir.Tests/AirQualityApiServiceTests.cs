using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CarbonAirWpf.Models;
using CarbonAirWpf.Services;
using Xunit;

namespace CarbonAir.Tests
{
    public class AirQualityApiServiceTests
    {
        private class FailingHttpMessageHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                throw new HttpRequestException("Simulated offline network");
            }
        }

        [Fact]
        public async Task GetCityAirQualityAsync_FetchesLiveOpenMeteoDataForYogyakarta()
        {
            var service = new AirQualityApiService();
            var yogya = CityLocation.FindByNameOrDefault("Yogyakarta");

            AirQualityData result = await service.GetCityAirQualityAsync(yogya);

            Assert.True(result.IsFromLiveApi, result.ErrorMessage);
            Assert.Equal("Yogyakarta", result.CityName);
            Assert.True(result.AqiIndex >= 0);
            Assert.True(result.HourlyAqi.Count >= 12);
            Assert.False(string.IsNullOrWhiteSpace(result.Category));
            Assert.False(string.IsNullOrWhiteSpace(result.HealthAdvice));
        }

        [Fact]
        public async Task GetIndonesiaStationsAsync_FetchesAllTwelvePresets()
        {
            var service = new AirQualityApiService();

            var stations = await service.GetIndonesiaStationsAsync();

            Assert.Equal(12, stations.Count);
            Assert.All(stations, s => Assert.True(s.IsFromLiveApi, s.ErrorMessage));
            Assert.Contains(stations, s => s.CityName == "Jakarta");
            Assert.Contains(stations, s => s.CityName == "Jayapura");
        }

        [Fact]
        public async Task SearchCityAsync_FindsIndonesianCityViaGeocodingApi()
        {
            var service = new AirQualityApiService();

            CityLocation? city = await service.SearchCityAsync("Bandung");

            Assert.NotNull(city);
            Assert.Contains("Bandung", city!.Name);
            Assert.InRange(city.Latitude, -11.0, 6.0);
            Assert.InRange(city.Longitude, 95.0, 141.0);
        }

        [Fact]
        public async Task TestConnectionAsync_ReportsLiveConnectivity()
        {
            var service = new AirQualityApiService();
            var (success, message) = await service.TestConnectionAsync();

            Assert.True(success, message);
            Assert.Contains("Open-Meteo", message);
        }

        [Fact]
        public async Task OfflineFailure_ReturnsGracefulFallbackWithoutThrowing()
        {
            using var failingClient = new HttpClient(new FailingHttpMessageHandler());
            var service = new AirQualityApiService(failingClient);
            var jakarta = CityLocation.FindByNameOrDefault("Jakarta");

            var single = await service.GetCityAirQualityAsync(jakarta);
            Assert.False(single.IsFromLiveApi);
            Assert.NotEmpty(single.HourlyAqi);
            Assert.Contains("Offline", single.ErrorMessage);

            var batch = await service.GetIndonesiaStationsAsync();
            Assert.Equal(12, batch.Count);
            Assert.All(batch, s => Assert.False(s.IsFromLiveApi));
        }
    }
}
