using System;
using CarbonAirWpf.Data;
using CarbonAirWpf.Models;

namespace CarbonAirWpf.Services
{
    public class AuthService
    {
        private readonly DatabaseHelper _dbHelper;

        public AuthService()
        {
            _dbHelper = new DatabaseHelper();
        }

        // PostgreSQL
        public Account? Login(string username, string password)
        {
            string? role = _dbHelper.GetUserRoleFromDb(username, password);

            if (role == null)
            {
                return null;
            }

            // Polymorphism
            return CreateAccount(username, role);
        }

        // Polymorphism
        private Account CreateAccount(string username, string role)
        {
            if (role.Contains("Utama", StringComparison.OrdinalIgnoreCase) ||
                role.Contains("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return new AdminAccount(username, role);
            }

            return new RegularAccount(username, role);
        }
    }
}
