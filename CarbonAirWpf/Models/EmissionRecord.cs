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

        public string FormattedAmount => EnergyType == "Listrik" ? $"{Amount:F0} kWh" : $"{Amount:F0} L";
        public string FormattedCo2 => $"+{Co2:F1} kg CO₂";
    }
}
