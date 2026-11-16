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

        // Polymorphism
        public NotificationBase CreateNotification(string recipient, int aqiScore)
        {
            if (aqiScore <= 75)
            {
                return new SafeNotification(recipient);
            }

            return new WarningNotification(recipient);
        }
    }
}
