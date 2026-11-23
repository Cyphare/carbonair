using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CarbonAirWpf.Data;
using CarbonAirWpf.Models;

namespace CarbonAirWpf.Services
{
    public class EmissionService
    {
        private readonly DatabaseHelper _dbHelper;

        public EmissionService(DatabaseHelper? dbHelper = null)
        {
            _dbHelper = dbHelper ?? new DatabaseHelper();
        }

        // Polymorphism
        public EmissionCalculator GetCalculator(string energyType)
        {
            if (!string.IsNullOrWhiteSpace(energyType) &&
                energyType.Equals("BBM", StringComparison.OrdinalIgnoreCase))
            {
                return new FuelEmissionCalculator();
            }

            return new ElectricityEmissionCalculator();
        }

        public bool RecordEmission(string username, string energyType, double amount, string date)
        {
            if (string.IsNullOrWhiteSpace(username) || amount <= 0 || double.IsNaN(amount) || double.IsInfinity(amount))
            {
                return false;
            }

            string normalizedDate;
            if (string.IsNullOrWhiteSpace(date))
            {
                normalizedDate = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            else if (DateTime.TryParseExact(date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
            {
                normalizedDate = parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            else
            {
                return false;
            }

            EmissionCalculator calculator = GetCalculator(energyType);
            double calculatedCo2 = calculator.CalculateCo2(amount);

            var record = new EmissionRecord
            {
                Username = username.Trim(),
                EnergyType = calculator.EnergyType,
                Amount = amount,
                Date = normalizedDate,
                Co2 = calculatedCo2
            };

            return _dbHelper.InsertEmissionLogToDb(record) > 0;
        }

        public bool UpdateEmission(int id, string username, string energyType, double amount, string date)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(username) || amount <= 0 || double.IsNaN(amount) || double.IsInfinity(amount))
            {
                return false;
            }

            if (!DateTime.TryParseExact((date ?? string.Empty).Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
            {
                return false;
            }

            EmissionCalculator calculator = GetCalculator(energyType);
            double calculatedCo2 = calculator.CalculateCo2(amount);

            var record = new EmissionRecord
            {
                Id = id,
                Username = username.Trim(),
                EnergyType = calculator.EnergyType,
                Amount = amount,
                Date = parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Co2 = calculatedCo2
            };

            return _dbHelper.UpdateEmissionLogInDb(record);
        }

        public bool DeleteEmission(int id, string username)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            return _dbHelper.DeleteEmissionLogFromDb(id, username);
        }

        public List<EmissionRecord> GetUserLogs(string username, string? energyFilter = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return new List<EmissionRecord>();
            }
            return _dbHelper.GetEmissionLogsFromDb(username, energyFilter);
        }

        public bool ExportLogsToCsv(IEnumerable<EmissionRecord> logs, string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("ID,Username,JenisEnergi,Tanggal,Jumlah,Satuan,EmisiCO2_kg");
                foreach (var log in logs)
                {
                    sb.AppendLine(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0},{1},{2},{3},{4:0.##},{5},{6:F2}",
                        log.Id,
                        EscapeCsv(log.Username),
                        EscapeCsv(log.EnergyType),
                        EscapeCsv(log.Date),
                        log.Amount,
                        EscapeCsv(log.Unit),
                        log.Co2));
                }

                File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GenerateRecommendation(AirQualityData? aqi, double totalCo2Kg)
        {
            var sb = new StringBuilder();

            if (aqi == null)
            {
                sb.Append("Data kualitas udara belum tersedia. Pastikan koneksi internet aktif untuk mengambil data real-time.");
            }
            else if (aqi.AqiIndex <= 50)
            {
                sb.Append($"Udara di {aqi.CityName} tergolong Baik (AQI {aqi.AqiIndex}). Aman untuk berolahraga di luar ruangan atau berjalan kaki/bersepeda.");
            }
            else if (aqi.AqiIndex <= 100)
            {
                sb.Append($"Udara di {aqi.CityName} berstatus Sedang (AQI {aqi.AqiIndex}, polutan utama {aqi.MainPollutant}). Kelompok sensitif dianjurkan membawa masker saat beraktivitas lama di luar.");
            }
            else
            {
                sb.Append($"PERINGATAN POLUSI di {aqi.CityName} (AQI {aqi.AqiIndex} - {aqi.Category}, dominan {aqi.MainPollutant})! Gunakan masker N95/KN95 dan batasi aktivitas fisik di luar ruangan.");
            }

            sb.Append(' ');

            string totalStr = totalCo2Kg.ToString("F1", CultureInfo.InvariantCulture);

            if (totalCo2Kg <= 20)
            {
                sb.Append($"Jejak karbon tercatat Anda ({totalStr} kg CO₂) masih rendah — pertahankan efisiensi listrik & transportasi ramah lingkungan.");
            }
            else if (totalCo2Kg <= 60)
            {
                sb.Append($"Total emisi Anda mencapai {totalStr} kg CO₂. Pertimbangkan mematikan perangkat standby dan menggabungkan rute perjalanan BBM.");
            }
            else
            {
                sb.Append($"Total emisi Anda tinggi ({totalStr} kg CO₂). Prioritaskan transportasi publik, pengurangan konsumsi BBM, dan audit penggunaan AC/listrik harian.");
            }

            return sb.ToString();
        }

        private static string EscapeCsv(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }
    }
}
