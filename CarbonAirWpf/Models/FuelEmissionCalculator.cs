namespace CarbonAirWpf.Models
{
    // Inheritance
    public class FuelEmissionCalculator : EmissionCalculator
    {
        public override string EnergyType => "BBM";
        public override double EmissionFactor => 2.30;

        // Polymorphism
        public override double CalculateCo2(double amount)
        {
            return amount * EmissionFactor;
        }
    }
}
