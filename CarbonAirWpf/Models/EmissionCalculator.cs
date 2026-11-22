namespace CarbonAirWpf.Models
{
    // Abstraction & Polymorphism
    public abstract class EmissionCalculator
    {
        public abstract string EnergyType { get; }
        public abstract double EmissionFactor { get; }
        public abstract string Unit { get; }

        // Polymorphism
        public abstract double CalculateCo2(double amount);
    }
}
