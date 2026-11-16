using CarbonAirWpf.Models;

namespace CarbonAirWpf.Services
{
    // Access Modifier: internal
    internal class AqiService
    {
        // Polymorphism
        public AqiEvaluator EvaluateAqi(int aqiScore)
        {
            if (aqiScore <= 75)
            {
                return new GoodAqiEvaluator(aqiScore);
            }

            return new ModerateAqiEvaluator(aqiScore);
        }
    }
}
