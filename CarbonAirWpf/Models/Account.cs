namespace CarbonAirWpf.Models
{
    // Encapsulation
    public abstract class Account
    {
        private string _username = string.Empty;
        private string _role = string.Empty;

        public string Username
        {
            get => _username;
            set => _username = string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();
        }

        public string Role
        {
            get => _role;
            set => _role = string.IsNullOrWhiteSpace(value) ? "User" : value.Trim();
        }

        // Polymorphism
        public abstract string GetWelcomeMessage();
    }
}
