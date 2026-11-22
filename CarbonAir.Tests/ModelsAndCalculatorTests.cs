using CarbonAirWpf.Models;
using CarbonAirWpf.Services;
using Xunit;

namespace CarbonAir.Tests
{
    public class ModelsAndCalculatorTests
    {
        [Fact]
        public void ElectricityAndFuelCalculators_UsePolymorphismCorrectly()
        {
            var service = new EmissionService();

            EmissionCalculator listrik = service.GetCalculator("Listrik");
            EmissionCalculator bbm = service.GetCalculator("BBM");

            Assert.IsType<ElectricityEmissionCalculator>(listrik);
            Assert.Equal("Listrik", listrik.EnergyType);
            Assert.Equal("kWh", listrik.Unit);
            Assert.Equal(0.85, listrik.EmissionFactor, 4);
            Assert.Equal(8.5, listrik.CalculateCo2(10), 4);
            Assert.Equal(0.0, listrik.CalculateCo2(-5), 4);

            Assert.IsType<FuelEmissionCalculator>(bbm);
            Assert.Equal("BBM", bbm.EnergyType);
            Assert.Equal("L", bbm.Unit);
            Assert.Equal(2.30, bbm.EmissionFactor, 4);
            Assert.Equal(23.0, bbm.CalculateCo2(10), 4);
            Assert.Equal(0.0, bbm.CalculateCo2(-2), 4);
        }

        [Fact]
        public void AccountHierarchy_SetsRolePermissionsAndEncapsulation()
        {
            Account admin = new AdminAccount("  user1  ", "Pengguna Utama", "", "");
            Account regular = new RegularAccount("user2", "Pengguna Biasa", "u2@mail.id", "Jakarta");

            Assert.True(admin.CanManageAllUsers);
            Assert.Equal("user1", admin.Username);
            Assert.Equal("user1@carbonair.local", admin.Email);
            Assert.Equal("Yogyakarta", admin.City);
            Assert.Contains("Administrator", admin.GetWelcomeMessage());

            Assert.False(regular.CanManageAllUsers);
            Assert.Equal("u2@mail.id", regular.Email);
            Assert.Equal("Jakarta", regular.City);
            Assert.Contains("Standar", regular.GetWelcomeMessage());
        }

        [Fact]
        public void EmissionRecord_EnforcesEncapsulationAndUnitFormatting()
        {
            var record = new EmissionRecord
            {
                EnergyType = "   ",
                Amount = -12,
                Co2 = -5
            };

            Assert.Equal("Listrik", record.EnergyType);
            Assert.Equal(0, record.Amount);
            Assert.Equal(0, record.Co2);

            record.EnergyType = "BBM";
            record.Amount = 12.5;
            record.Co2 = 28.75;

            Assert.Equal("12.5 L", record.FormattedAmount);
            Assert.Equal("+28.8 kg CO₂", record.FormatCo2("kg"));
            Assert.Equal("+0.0288 ton CO₂", record.FormatCo2("ton"));
            Assert.Equal("0.0414 ton CO₂", EmissionRecord.FormatTotalCo2(41.4, "ton"));
        }

        [Theory]
        [InlineData(25, "Baik", "Aman", "#2ECC71")]
        [InlineData(75, "Sedang", "Cukup Aman", "#F1C40F")]
        [InlineData(125, "Tidak Sehat (Sensitif)", "Waspada", "#E67E22")]
        [InlineData(175, "Tidak Sehat", "Bahaya", "#E74C3C")]
        [InlineData(250, "Sangat Tidak Sehat", "Bahaya Tinggi", "#9B59B6")]
        [InlineData(350, "Berbahaya", "Darurat", "#C0392B")]
        public void ClassifyAqi_MapsAllSixUsEpaTiersAccurately(int aqi, string expectedCat, string expectedStatus, string expectedHex)
        {
            var res = AirQualityData.ClassifyAqi(aqi);

            Assert.Equal(expectedCat, res.Category);
            Assert.Equal(expectedStatus, res.StatusLabel);
            Assert.Equal(expectedHex, res.ColorHex);
            Assert.False(string.IsNullOrWhiteSpace(res.HealthAdvice));
        }

        [Fact]
        public void CityPresets_ContainTwelveValidIndonesianCities()
        {
            Assert.Equal(12, CityLocation.IndonesiaPresets.Count);
            foreach (var city in CityLocation.IndonesiaPresets)
            {
                Assert.InRange(city.MapX, 0, 1030);
                Assert.InRange(city.MapY, 0, 515);
            }

            var yogya = CityLocation.FindByNameOrDefault("nonexistent_city");
            Assert.Equal("Yogyakarta", yogya.Name);
        }
    }
}
