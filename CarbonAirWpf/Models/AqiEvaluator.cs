namespace CarbonAirWpf.Models
{
    // Encapsulation
    public abstract class AqiEvaluator
    {
        // Access Modifier: private
        private int _aqiScore;

        // Access Modifier: protected
        protected string AppSource = "CarbonAir Monitor";

        // Access Modifier: public
        public int AqiScore
        {
            get => _aqiScore;
            set => _aqiScore = value < 0 ? 0 : value;
        }

        // Access Modifier: protected
        protected string FormatAdvice(string category, string message)
        {
            return $"[{AppSource}] AQI {AqiScore} ({category}): {message}";
        }

        // Polymorphism
        public abstract string GetStatusLabel();
        public abstract string GetAirQualityAdvice();
    }
}
