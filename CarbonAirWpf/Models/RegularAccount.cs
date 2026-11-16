namespace CarbonAirWpf.Models
{
    // Inheritance
    public class RegularAccount : Account
    {
        public RegularAccount(string username, string role)
        {
            Username = username;
            Role = role;
        }

        // Polymorphism
        public override string GetWelcomeMessage()
        {
            return $"Login Berhasil!\n\nSelamat Datang, {Username}!\nHak Akses: {Role}\nStatus: Pengguna Standar CarbonAir (Akses Terbatas)";
        }
    }
}
