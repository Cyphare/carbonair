namespace CarbonAirWpf.Models
{
    // Inheritance
    public class AdminAccount : Account
    {
        public AdminAccount(string username, string role, string email = "", string city = "Yogyakarta")
        {
            Username = username;
            Role = role;
            Email = email;
            City = city;
        }

        public override bool CanManageAllUsers => true;

        // Polymorphism
        public override string GetWelcomeMessage()
        {
            return $"Login Berhasil!\n\nSelamat Datang, {Username}!\nHak Akses: {Role}\nKota Default: {City}\nStatus: Administrator Sistem CarbonAir (Akses Penuh)";
        }
    }
}
