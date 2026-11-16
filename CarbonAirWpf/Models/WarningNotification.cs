namespace CarbonAirWpf.Models
{
    // Inheritance
    public class WarningNotification : NotificationBase
    {
        public WarningNotification(string recipient)
        {
            Recipient = recipient;
        }

        // Polymorphism
        public override string BuildMessage(int aqiScore)
        {
            return $"{BuildHeader("PERINGATAN POLUSI")}\nHalo {Recipient}, indeks AQI mencapai {aqiScore} (Sedang/Waspada)! Disarankan memakai masker di luar ruangan.";
        }
    }
}
