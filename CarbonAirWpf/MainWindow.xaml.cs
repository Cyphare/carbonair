using System;
using System.Windows;
using CarbonAirWpf.Data;
using CarbonAirWpf.Models;
using CarbonAirWpf.Services;

namespace CarbonAirWpf
{
    public partial class MainWindow : Window
    {
        private readonly AuthService _authService;

        public MainWindow()
        {
            InitializeComponent();

            // Inisialisasi skema tabel PostgreSQL
            DatabaseHelper.InitializeDatabase();
            _authService = new AuthService();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = tbUsername.Text.Trim();
            string password = tbPassword.Password.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Silakan masukkan username dan password terlebih dahulu!", 
                                "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // [Tugas 2: Autentikasi PostgreSQL]
                Account? account = _authService.Login(username, password);

                if (account != null)
                {
                    // [Tugas 1: Polymorphism] Memanggil method polimorfis GetWelcomeMessage()
                    string welcomeMessage = account.GetWelcomeMessage();

                    MessageBox.Show(welcomeMessage, "Autentikasi PostgreSQL Berhasil", 
                                    MessageBoxButton.OK, MessageBoxImage.Information);

                    // Membuka DashboardWindow sesuai user yang login
                    DashboardWindow dashboard = new DashboardWindow(account.Username);
                    dashboard.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Username atau Password salah!\nData tidak ditemukan pada database PostgreSQL.", 
                                    "Login Gagal", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan saat menghubungi database PostgreSQL:\n{ex.Message}", 
                                "Koneksi Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
