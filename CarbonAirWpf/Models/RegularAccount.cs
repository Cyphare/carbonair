namespace CarbonAirWpf.Models
{
    // Inheritance
    public class RegularAccount : Account
    {
        public RegularAccount(string username, string role, string email = "", string city = "Yogyakarta")
        {
            Username = username;
            Role = role;
            Email = email;
            City = city;
        }

        public override bool CanManageAllUsers => false;

        // Polymorphism
        public override string GetWelcomeMessage()
        {
            return $"Login Berhasil!\n\nSelamat Datang, {Username}!\nHak Akses: {Role}\nKota Default: {City}\nStatus: Pengguna Standar CarbonAir (Akses Terbatas)";
        }
    }
}
