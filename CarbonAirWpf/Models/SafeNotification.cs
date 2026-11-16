namespace CarbonAirWpf.Models
{
    // Inheritance
    public class SafeNotification : NotificationBase
    {
        public SafeNotification(string recipient)
        {
            Recipient = recipient;
        }

        // Polymorphism
        public override string BuildMessage(int aqiScore)
        {
            return $"{BuildHeader("INFO AMAN")}\nHalo {Recipient}, indeks AQI saat ini adalah {aqiScore} (Baik). Kondisi udara aman.";
        }
    }
}
