namespace CarbonAirWpf.Models
{
    // Inheritance
    public class ModerateAqiEvaluator : AqiEvaluator
    {
        public ModerateAqiEvaluator(int aqiScore)
        {
            AqiScore = aqiScore;
        }

        // Polymorphism
        public override string GetStatusLabel()
        {
            return "Waspada";
        }

        // Polymorphism
        public override string GetAirQualityAdvice()
        {
            return FormatAdvice("Sedang", "Polusi meningkat, kurangi aktivitas berat di luar ruangan.");
        }
    }
}
