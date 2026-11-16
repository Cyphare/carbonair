namespace CarbonAirWpf.Models
{
    // Encapsulation & Base Class
    public abstract class EmissionCalculator
    {
        public abstract string EnergyType { get; }
        public abstract double EmissionFactor { get; }

        // Polymorphism
        public abstract double CalculateCo2(double amount);
    }
}
