using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CarbonAirWpf.Models;

namespace CarbonAirWpf.Services
{
    /// <summary>
    /// Layanan Web REST API menggunakan Open-Meteo Air Quality, Weather, dan Geocoding API (tanpa API key).
    /// </summary>
    public class AirQualityApiService
    {
        private static readonly HttpClient SharedClient = CreateDefaultClient();
        private readonly HttpClient _httpClient;

        public AirQualityApiService(HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? SharedClient;
        }

        private static HttpClient CreateDefaultClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(12)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CarbonAir-MVP/1.0");
            return client;
        }

        public async Task<AirQualityData> GetCityAirQualityAsync(CityLocation city)
        {
            string lat = city.Latitude.ToString("F4", CultureInfo.InvariantCulture);
            string lon = city.Longitude.ToString("F4", CultureInfo.InvariantCulture);

            string aqiUrl = $"https://air-quality-api.open-meteo.com/v1/air-quality?latitude={lat}&longitude={lon}&current=us_aqi,pm2_5,pm10,carbon_monoxide,nitrogen_dioxide,ozone&hourly=us_aqi&past_hours=24&forecast_hours=1&timezone=Asia%2FJakarta";
            string weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,wind_speed_10m,relative_humidity_2m&timezone=Asia%2FJakarta";

            try
            {
                Task<string> aqiTask = _httpClient.GetStringAsync(aqiUrl);
                Task<string> weatherTask = _httpClient.GetStringAsync(weatherUrl);
                await Task.WhenAll(aqiTask, weatherTask).ConfigureAwait(false);

                using JsonDocument aqiDoc = JsonDocument.Parse(aqiTask.Result);
                using JsonDocument weatherDoc = JsonDocument.Parse(weatherTask.Result);

                var data = ParseSingleStation(city, aqiDoc.RootElement, weatherDoc.RootElement, includeHourly: true);
                data.IsFromLiveApi = true;
                data.FetchedAt = DateTime.Now;
                return data;
            }
            catch (Exception ex)
            {
                return BuildFallbackData(city, $"Mode Offline: gagal menghubungi Open-Meteo REST API ({ex.Message}).");
            }
        }

        public async Task<List<AirQualityData>> GetIndonesiaStationsAsync()
        {
            var presets = CityLocation.IndonesiaPresets;
            string lats = string.Join(",", presets.Select(c => c.Latitude.ToString("F4", CultureInfo.InvariantCulture)));
            string lons = string.Join(",", presets.Select(c => c.Longitude.ToString("F4", CultureInfo.InvariantCulture)));

            string aqiUrl = $"https://air-quality-api.open-meteo.com/v1/air-quality?latitude={lats}&longitude={lons}&current=us_aqi,pm2_5,pm10,carbon_monoxide,nitrogen_dioxide,ozone&timezone=Asia%2FJakarta";
            string weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={lats}&longitude={lons}&current=temperature_2m,wind_speed_10m,relative_humidity_2m&timezone=Asia%2FJakarta";

            try
            {
                Task<string> aqiTask = _httpClient.GetStringAsync(aqiUrl);
                Task<string> weatherTask = _httpClient.GetStringAsync(weatherUrl);
                await Task.WhenAll(aqiTask, weatherTask).ConfigureAwait(false);

                using JsonDocument aqiDoc = JsonDocument.Parse(aqiTask.Result);
                using JsonDocument weatherDoc = JsonDocument.Parse(weatherTask.Result);

                var aqiElements = ExtractArrayOrSingle(aqiDoc.RootElement);
                var weatherElements = ExtractArrayOrSingle(weatherDoc.RootElement);

                var results = new List<AirQualityData>(presets.Count);
                for (int i = 0; i < presets.Count; i++)
                {
                    JsonElement aqiEl = i < aqiElements.Count ? aqiElements[i] : default;
                    JsonElement weatherEl = i < weatherElements.Count ? weatherElements[i] : default;

                    if (aqiEl.ValueKind == JsonValueKind.Object)
                    {
                        var station = ParseSingleStation(presets[i], aqiEl, weatherEl, includeHourly: false);
                        station.IsFromLiveApi = true;
                        station.FetchedAt = DateTime.Now;
                        results.Add(station);
                    }
                    else
                    {
                        results.Add(BuildFallbackData(presets[i], "Data stasiun tidak tersedia."));
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                return presets
                    .Select(c => BuildFallbackData(c, $"Mode Offline ({ex.Message})"))
                    .ToList();
            }
        }

        public async Task<CityLocation?> SearchCityAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return null;
            }

            string url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(query.Trim())}&count=5&language=id&format=json";
            try
            {
                string json = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
                using JsonDocument doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("results", out JsonElement resultsEl) ||
                    resultsEl.ValueKind != JsonValueKind.Array ||
                    resultsEl.GetArrayLength() == 0)
                {
                    return null;
                }

                JsonElement chosen = resultsEl[0];
                foreach (JsonElement item in resultsEl.EnumerateArray())
                {
                    if (item.TryGetProperty("country_code", out JsonElement cc) &&
                        string.Equals(cc.GetString(), "ID", StringComparison.OrdinalIgnoreCase))
                    {
                        chosen = item;
                        break;
                    }
                }

                string name = chosen.TryGetProperty("name", out var nEl) ? (nEl.GetString() ?? query.Trim()) : query.Trim();
                string admin1 = chosen.TryGetProperty("admin1", out var aEl) ? (aEl.GetString() ?? string.Empty) : string.Empty;
                string country = chosen.TryGetProperty("country", out var cEl) ? (cEl.GetString() ?? "Indonesia") : "Indonesia";
                string province = !string.IsNullOrWhiteSpace(admin1) ? admin1 : country;

                double lat = GetDouble(chosen, "latitude", -7.7956);
                double lon = GetDouble(chosen, "longitude", 110.3695);

                return new CityLocation(name, province, lat, lon);
            }
            catch
            {
                return null;
            }
        }

        public async Task<(bool Success, string Message)> TestConnectionAsync()
        {
            string testUrl = "https://air-quality-api.open-meteo.com/v1/air-quality?latitude=-7.7956&longitude=110.3695&current=us_aqi&timezone=Asia%2FJakarta";
            var sw = Stopwatch.StartNew();
            try
            {
                string json = await _httpClient.GetStringAsync(testUrl).ConfigureAwait(false);
                sw.Stop();
                using JsonDocument doc = JsonDocument.Parse(json);
                int aqi = 0;
                if (doc.RootElement.TryGetProperty("current", out JsonElement current))
                {
                    aqi = (int)Math.Round(GetDouble(current, "us_aqi", 0));
                }

                return (true, $"Koneksi REST API Open-Meteo aktif ({sw.ElapsedMilliseconds} ms). Sampel AQI Yogyakarta saat ini: {aqi}.");
            }
            catch (Exception ex)
            {
                sw.Stop();
                return (false, $"Gagal menghubungi REST API Open-Meteo ({sw.ElapsedMilliseconds} ms): {ex.Message}");
            }
        }

        private static List<JsonElement> ExtractArrayOrSingle(JsonElement root)
        {
            var list = new List<JsonElement>();
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    list.Add(item);
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                list.Add(root);
            }
            return list;
        }

        private static AirQualityData ParseSingleStation(CityLocation city, JsonElement aqiRoot, JsonElement weatherRoot, bool includeHourly)
        {
            var data = new AirQualityData
            {
                CityName = city.Name,
                Province = city.Province,
                Latitude = city.Latitude,
                Longitude = city.Longitude,
                MapX = city.MapX,
                MapY = city.MapY
            };

            if (aqiRoot.ValueKind == JsonValueKind.Object &&
                aqiRoot.TryGetProperty("current", out JsonElement currentAqi))
            {
                data.AqiIndex = (int)Math.Round(GetDouble(currentAqi, "us_aqi", 50));
                data.Pm25 = GetDouble(currentAqi, "pm2_5", 0);
                data.Pm10 = GetDouble(currentAqi, "pm10", 0);
                data.Co = GetDouble(currentAqi, "carbon_monoxide", 0);
                data.No2 = GetDouble(currentAqi, "nitrogen_dioxide", 0);
                data.Ozone = GetDouble(currentAqi, "ozone", 0);
                data.MainPollutant = DetermineMainPollutant(data.Pm25, data.Pm10, data.Ozone, data.No2, data.Co);
            }

            if (weatherRoot.ValueKind == JsonValueKind.Object &&
                weatherRoot.TryGetProperty("current", out JsonElement currentWeather))
            {
                data.Temperature = GetDouble(currentWeather, "temperature_2m", 27.0);
                data.WindSpeed = GetDouble(currentWeather, "wind_speed_10m", 10.0);
                data.Humidity = GetDouble(currentWeather, "relative_humidity_2m", 70.0);
            }

            if (includeHourly &&
                aqiRoot.ValueKind == JsonValueKind.Object &&
                aqiRoot.TryGetProperty("hourly", out JsonElement hourly) &&
                hourly.TryGetProperty("time", out JsonElement times) &&
                hourly.TryGetProperty("us_aqi", out JsonElement aqis))
            {
                int count = Math.Min(times.GetArrayLength(), aqis.GetArrayLength());
                for (int i = 0; i < count; i++)
                {
                    if (aqis[i].ValueKind != JsonValueKind.Number) continue;

                    string rawTime = times[i].GetString() ?? string.Empty;
                    string label = rawTime.Length >= 16 ? rawTime.Substring(11, 5) : rawTime;
                    int val = (int)Math.Round(aqis[i].GetDouble());
                    data.HourlyAqi.Add(new HourlyAqiPoint(label, val));
                }
            }

            data.ApplyClassification();
            return data;
        }

        private static string DetermineMainPollutant(double pm25, double pm10, double o3, double no2, double co)
        {
            var candidates = new (string Name, double Score)[]
            {
                ("PM2.5", pm25 / 35.0),
                ("PM10", pm10 / 150.0),
                ("O₃", o3 / 140.0),
                ("NO₂", no2 / 100.0),
                ("CO", co / 10000.0)
            };

            return candidates.OrderByDescending(c => c.Score).First().Name;
        }

        private static AirQualityData BuildFallbackData(CityLocation city, string errorMessage)
        {
            var fallback = new AirQualityData
            {
                CityName = city.Name,
                Province = city.Province,
                Latitude = city.Latitude,
                Longitude = city.Longitude,
                MapX = city.MapX,
                MapY = city.MapY,
                AqiIndex = 65,
                MainPollutant = "PM2.5",
                Pm25 = 16.5,
                Pm10 = 28.0,
                Co = 320.0,
                No2 = 14.0,
                Ozone = 42.0,
                Temperature = 28.0,
                WindSpeed = 12.0,
                Humidity = 75.0,
                IsFromLiveApi = false,
                FetchedAt = DateTime.Now,
                ErrorMessage = errorMessage,
                HourlyAqi = new List<HourlyAqiPoint>
                {
                    new("00:00", 52),
                    new("04:00", 48),
                    new("08:00", 64),
                    new("12:00", 72),
                    new("16:00", 68),
                    new("20:00", 65)
                }
            };
            fallback.ApplyClassification();
            return fallback;
        }

        private static double GetDouble(JsonElement element, string propertyName, double fallback)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(propertyName, out JsonElement prop) &&
                prop.ValueKind == JsonValueKind.Number)
            {
                return prop.GetDouble();
            }
            return fallback;
        }
    }
}
