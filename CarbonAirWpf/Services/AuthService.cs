using System;
using System.Collections.Generic;
using CarbonAirWpf.Data;
using CarbonAirWpf.Models;

namespace CarbonAirWpf.Services
{
    public class AuthService
    {
        private readonly DatabaseHelper _dbHelper;

        public AuthService(DatabaseHelper? dbHelper = null)
        {
            _dbHelper = dbHelper ?? new DatabaseHelper();
        }

        public Account? Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return null;
            }

            UserDbRecord? record = _dbHelper.GetUserFromDb(username, password);
            if (record == null)
            {
                return null;
            }

            return CreateAccount(record.Username, record.Role, record.Email, record.City);
        }

        public (bool Success, string Message) Register(string username, string password, string email, string city)
        {
            string cleanUser = (username ?? string.Empty).Trim();
            string cleanPass = (password ?? string.Empty).Trim();
            string cleanEmail = (email ?? string.Empty).Trim();
            string cleanCity = string.IsNullOrWhiteSpace(city) ? "Yogyakarta" : city.Trim();

            if (cleanUser.Length < 3)
            {
                return (false, "Username minimal terdiri dari 3 karakter.");
            }
            if (cleanPass.Length < 3)
            {
                return (false, "Password minimal terdiri dari 3 karakter.");
            }
            if (string.IsNullOrWhiteSpace(cleanEmail))
            {
                cleanEmail = $"{cleanUser.ToLowerInvariant()}@carbonair.local";
            }

            if (_dbHelper.UserExists(cleanUser))
            {
                return (false, $"Username '{cleanUser}' sudah terdaftar. Silakan gunakan username lain.");
            }

            bool created = _dbHelper.RegisterUser(cleanUser, cleanPass, "Pengguna Biasa", cleanEmail, cleanCity);
            return created
                ? (true, $"Akun '{cleanUser}' berhasil didaftarkan! Silakan login dengan akun baru Anda.")
                : (false, "Gagal mendaftarkan akun ke database.");
        }

        public (bool Success, string Message) UpdateProfile(string username, string email, string city)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return (false, "Username tidak valid.");
            }

            string cleanEmail = string.IsNullOrWhiteSpace(email)
                ? $"{username.Trim().ToLowerInvariant()}@carbonair.local"
                : email.Trim();
            string cleanCity = string.IsNullOrWhiteSpace(city) ? "Yogyakarta" : city.Trim();

            bool updated = _dbHelper.UpdateUserProfile(username, cleanEmail, cleanCity);
            return updated
                ? (true, "Profil pengguna berhasil diperbarui.")
                : (false, "Gagal memperbarui profil pengguna di database.");
        }

        public (bool Success, string Message) ChangePassword(string username, string oldPassword, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                return (false, "Password lama dan password baru wajib diisi.");
            }
            if (newPassword.Trim().Length < 3)
            {
                return (false, "Password baru minimal terdiri dari 3 karakter.");
            }

            bool changed = _dbHelper.UpdateUserPassword(username, oldPassword, newPassword);
            return changed
                ? (true, "Password berhasil diperbarui.")
                : (false, "Password lama tidak sesuai.");
        }

        public List<string> GetAllUsernames()
        {
            return _dbHelper.GetAllUsernames();
        }

        public Account? GetUserAccount(string username)
        {
            var profile = _dbHelper.GetUserProfile(username);
            if (profile == null)
            {
                return null;
            }
            return CreateAccount(profile.Username, profile.Role, profile.Email, profile.City);
        }

        // Polymorphism
        private static Account CreateAccount(string username, string role, string email, string city)
        {
            if (role.Contains("Utama", StringComparison.OrdinalIgnoreCase) ||
                role.Contains("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return new AdminAccount(username, role, email, city);
            }

            return new RegularAccount(username, role, email, city);
        }
    }
}
