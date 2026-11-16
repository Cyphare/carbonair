namespace CarbonAirWpf.Models
{
    // Inheritance
    public class AdminAccount : Account
    {
        public AdminAccount(string username, string role)
        {
            Username = username;
            Role = role;
        }

        // Polymorphism
        public override string GetWelcomeMessage()
        {
            return $"Login Berhasil!\n\nSelamat Datang, {Username}!\nHak Akses: {Role}\nStatus: Administrator Sistem CarbonAir (Akses Penuh)";
        }
    }
}
