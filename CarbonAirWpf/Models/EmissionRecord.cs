using System;
using System.Globalization;

namespace CarbonAirWpf.Models
{
    // Encapsulation
    public class EmissionRecord
    {
        private string _energyType = "Listrik";
        private double _amount;
        private double _co2;

        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;

        public string EnergyType
        {
            get => _energyType;
            set => _energyType = string.IsNullOrWhiteSpace(value) ? "Listrik" : value.Trim();
        }

        public double Amount
        {
            get => _amount;
            set => _amount = value < 0 ? 0 : value;
        }

        public double Co2
        {
            get => _co2;
            set => _co2 = value < 0 ? 0 : value;
        }

        public string Unit => EnergyType.Equals("Listrik", StringComparison.OrdinalIgnoreCase) ? "kWh" : "L";

        public string FormattedAmount =>
            string.Format(CultureInfo.InvariantCulture, "{0:0.##} {1}", Amount, Unit);

        public string FormattedCo2 => FormatCo2("kg");

        public string FormatCo2(string unit)
        {
            if (string.Equals(unit, "ton", StringComparison.OrdinalIgnoreCase))
            {
                return string.Format(CultureInfo.InvariantCulture, "+{0:F4} ton CO₂", Co2 / 1000.0);
            }
            return string.Format(CultureInfo.InvariantCulture, "+{0:F1} kg CO₂", Co2);
        }

        public static string FormatTotalCo2(double totalKg, string unit)
        {
            if (string.Equals(unit, "ton", StringComparison.OrdinalIgnoreCase))
            {
                return string.Format(CultureInfo.InvariantCulture, "{0:F4} ton CO₂", totalKg / 1000.0);
            }
            return string.Format(CultureInfo.InvariantCulture, "{0:F1} kg CO₂", totalKg);
        }
    }
}
