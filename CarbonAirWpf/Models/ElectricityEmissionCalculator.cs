namespace CarbonAirWpf.Models
{
    // Inheritance
    public class ElectricityEmissionCalculator : EmissionCalculator
    {
        public override string EnergyType => "Listrik";
        public override double EmissionFactor => 0.85;
        public override string Unit => "kWh";

        // Polymorphism
        public override double CalculateCo2(double amount)
        {
            return amount < 0 ? 0 : amount * EmissionFactor;
        }
    }
}
