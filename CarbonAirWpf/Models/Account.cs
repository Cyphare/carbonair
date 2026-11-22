namespace CarbonAirWpf.Models
{
    // Encapsulation & Abstraction
    public abstract class Account
    {
        private string _username = string.Empty;
        private string _role = string.Empty;
        private string _email = string.Empty;
        private string _city = "Yogyakarta";

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

        public string Email
        {
            get => _email;
            set => _email = string.IsNullOrWhiteSpace(value)
                ? $"{_username.ToLowerInvariant()}@carbonair.local"
                : value.Trim();
        }

        public string City
        {
            get => _city;
            set => _city = string.IsNullOrWhiteSpace(value) ? "Yogyakarta" : value.Trim();
        }

        public abstract bool CanManageAllUsers { get; }

        // Polymorphism
        public abstract string GetWelcomeMessage();
    }
}
