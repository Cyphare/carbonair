namespace CarbonAirWpf.Models
{
    // Encapsulation
    public abstract class NotificationBase
    {
        // Access Modifier: private
        private string _recipient = "user1";

        // Access Modifier: protected
        protected string ChannelName = "CarbonAir Desktop Alert";

        // Access Modifier: public
        public string Recipient
        {
            get => _recipient;
            set => _recipient = string.IsNullOrWhiteSpace(value) ? "user1" : value.Trim();
        }

        // Access Modifier: protected
        protected string BuildHeader(string level)
        {
            return $"[{ChannelName} - {level}]";
        }

        // Polymorphism
        public abstract string BuildMessage(int aqiScore);
    }
}
