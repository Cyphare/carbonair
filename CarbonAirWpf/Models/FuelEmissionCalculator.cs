namespace CarbonAirWpf.Models
{
    // Inheritance
    public class FuelEmissionCalculator : EmissionCalculator
    {
        public override string EnergyType => "BBM";
        public override double EmissionFactor => 2.30;
        public override string Unit => "L";

        // Polymorphism
        public override double CalculateCo2(double amount)
        {
            return amount < 0 ? 0 : amount * EmissionFactor;
        }
    }
}
