using System;
using System.Collections.Generic;

namespace CarbonAirWpf.Models
{
    public record HourlyAqiPoint(string TimeLabel, int Aqi);

    public record AqiClassification(string Category, string StatusLabel, string ColorHex, string HealthAdvice);

    public class AirQualityData
    {
        public string CityName { get; set; } = "Yogyakarta";
        public string Province { get; set; } = "DI Yogyakarta";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double MapX { get; set; }
        public double MapY { get; set; }

        public int AqiIndex { get; set; }
        public string Category { get; set; } = "Baik";
        public string StatusLabel { get; set; } = "Aman";
        public string ColorHex { get; set; } = "#2ECC71";
        public string HealthAdvice { get; set; } = string.Empty;
        public string MainPollutant { get; set; } = "PM2.5";

        public double Pm25 { get; set; }
        public double Pm10 { get; set; }
        public double Co { get; set; }
        public double No2 { get; set; }
        public double Ozone { get; set; }

        public double Temperature { get; set; }
        public double WindSpeed { get; set; }
        public double Humidity { get; set; }

        public List<HourlyAqiPoint> HourlyAqi { get; set; } = new();
        public DateTime FetchedAt { get; set; } = DateTime.Now;
        public bool IsFromLiveApi { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public void ApplyClassification()
        {
            var c = ClassifyAqi(AqiIndex);
            Category = c.Category;
            StatusLabel = c.StatusLabel;
            ColorHex = c.ColorHex;
            HealthAdvice = c.HealthAdvice;
        }

        public static AqiClassification ClassifyAqi(int aqi)
        {
            if (aqi <= 50)
            {
                return new AqiClassification(
                    "Baik",
                    "Aman",
                    "#2ECC71",
                    "Kualitas udara sangat baik dan aman untuk seluruh aktivitas luar ruangan.");
            }
            if (aqi <= 100)
            {
                return new AqiClassification(
                    "Sedang",
                    "Cukup Aman",
                    "#F1C40F",
                    "Kualitas udara cukup baik. Kelompok sensitif sebaiknya mengurangi aktivitas berat di luar ruangan.");
            }
            if (aqi <= 150)
            {
                return new AqiClassification(
                    "Tidak Sehat (Sensitif)",
                    "Waspada",
                    "#E67E22",
                    "Waspada! Anak-anak, lansia, dan penderita gangguan pernapasan disarankan membatasi aktivitas luar ruangan.");
            }
            if (aqi <= 200)
            {
                return new AqiClassification(
                    "Tidak Sehat",
                    "Bahaya",
                    "#E74C3C",
                    "Bahaya! Kurangi aktivitas luar ruangan dan gunakan masker pelindung (N95/KN95) saat keluar rumah.");
            }
            if (aqi <= 300)
            {
                return new AqiClassification(
                    "Sangat Tidak Sehat",
                    "Bahaya Tinggi",
                    "#9B59B6",
                    "Bahaya Tinggi! Hindari aktivitas luar ruangan, tutup jendela, dan gunakan penyaring udara.");
            }

            return new AqiClassification(
                "Berbahaya",
                "Darurat",
                "#C0392B",
                "Kondisi Darurat Polusi! Tetap berada di dalam ruangan dan ikuti imbauan kesehatan setempat.");
        }
    }
}
