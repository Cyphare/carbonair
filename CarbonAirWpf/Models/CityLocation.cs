using System;
using System.Collections.Generic;
using System.Linq;

namespace CarbonAirWpf.Models
{
    public class CityLocation
    {
        public string Name { get; set; }
        public string Province { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double MapX { get; set; }
        public double MapY { get; set; }

        public CityLocation(string name, string province, double latitude, double longitude, double mapX = 0, double mapY = 0)
        {
            Name = name;
            Province = province;
            Latitude = latitude;
            Longitude = longitude;
            MapX = mapX;
            MapY = mapY;
        }

        public static readonly IReadOnlyList<CityLocation> IndonesiaPresets = new List<CityLocation>
        {
            new CityLocation("Medan", "Sumatera Utara", 3.5952, 98.6722, 114.6, 144.4),
            new CityLocation("Palembang", "Sumatera Selatan", -2.9761, 104.7754, 237.8, 277.2),
            new CityLocation("Jakarta", "DKI Jakarta", -6.2088, 106.8456, 279.6, 342.7),
            new CityLocation("Bandung", "Jawa Barat", -6.9175, 107.6191, 295.2, 357.1),
            new CityLocation("Semarang", "Jawa Tengah", -6.9667, 110.4167, 351.7, 358.1),
            new CityLocation("Yogyakarta", "DI Yogyakarta", -7.7956, 110.3695, 350.8, 375.0),
            new CityLocation("Surabaya", "Jawa Timur", -7.2575, 112.7521, 398.9, 364.1),
            new CityLocation("Denpasar", "Bali", -8.6705, 115.2126, 448.6, 392.9),
            new CityLocation("Balikpapan", "Kalimantan Timur", -1.2379, 116.8529, 481.7, 242.1),
            new CityLocation("Makassar", "Sulawesi Selatan", -5.1477, 119.4327, 533.8, 321.2),
            new CityLocation("Manado", "Sulawesi Utara", 1.4748, 124.8421, 643.1, 187.3),
            new CityLocation("Jayapura", "Papua", -2.5916, 140.6690, 962.7, 269.5)
        };

        public static CityLocation FindByNameOrDefault(string? cityName)
        {
            if (!string.IsNullOrWhiteSpace(cityName))
            {
                var exact = IndonesiaPresets.FirstOrDefault(c =>
                    c.Name.Equals(cityName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (exact != null) return exact;

                var partial = IndonesiaPresets.FirstOrDefault(c =>
                    c.Name.Contains(cityName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (partial != null) return partial;
            }

            return IndonesiaPresets.First(c => c.Name == "Yogyakarta");
        }

        public override string ToString() => Name;
    }
}
