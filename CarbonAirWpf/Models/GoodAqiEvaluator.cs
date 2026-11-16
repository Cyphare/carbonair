namespace CarbonAirWpf.Models
{
    // Inheritance
    public class GoodAqiEvaluator : AqiEvaluator
    {
        public GoodAqiEvaluator(int aqiScore)
        {
            AqiScore = aqiScore;
        }

        // Polymorphism
        public override string GetStatusLabel()
        {
            return "Aman";
        }

        // Polymorphism
        public override string GetAirQualityAdvice()
        {
            return FormatAdvice("Baik", "Udara bersih, aman untuk aktivitas luar ruangan.");
        }
    }
}
