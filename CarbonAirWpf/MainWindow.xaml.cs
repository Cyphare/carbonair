using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CarbonAirWpf.Data;
using CarbonAirWpf.Models;
using CarbonAirWpf.Services;

namespace CarbonAirWpf
{
    public partial class MainWindow : Window
    {
        private DatabaseHelper _dbHelper;
        private AuthService _authService;

        public MainWindow()
        {
            InitializeComponent();

            AppConfigManager.Load();
            foreach (var city in CityLocation.IndonesiaPresets)
            {
                cbRegCity.Items.Add(city.Name);
            }
            cbRegCity.SelectedItem = "Yogyakarta";

            _dbHelper = new DatabaseHelper();
            _authService = new AuthService(_dbHelper);
            InitializeAndUpdateDbStatus();
        }

        private void InitializeAndUpdateDbStatus()
        {
            var (success, message) = _dbHelper.InitializeDatabase();
            string provider = AppConfigManager.Current.Database.Provider;

            if (success)
            {
                dotDbStatus.Fill = new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71));
                txtDbStatusPill.Text = $"DB: {provider} (Siap)";
                txtDbStatusPill.Foreground = new SolidColorBrush(Color.FromRgb(0xA0, 0xB2, 0xBC));
            }
            else
            {
                dotDbStatus.Fill = new SolidColorBrush(Color.FromRgb(0xE7, 0x4C, 0x3C));
                txtDbStatusPill.Text = $"DB: {provider} (Error)";
                txtDbStatusPill.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B));
                txtDbStatusPill.ToolTip = message;
            }
        }

        private void InputField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnLogin_Click(btnLogin, new RoutedEventArgs());
            }
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
                Account? account = _authService.Login(username, password);

                if (account != null)
                {
                    string welcomeMessage = account.GetWelcomeMessage();
                    MessageBox.Show(welcomeMessage, $"Autentikasi {AppConfigManager.Current.Database.Provider} Berhasil",
                                    MessageBoxButton.OK, MessageBoxImage.Information);

                    DashboardWindow dashboard = new DashboardWindow(account);
                    dashboard.Show();
                    Close();
                }
                else
                {
                    MessageBox.Show("Username atau Password salah!\nPeriksa kembali kredensial Anda atau daftar akun baru.",
                                    "Login Gagal", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan saat menghubungi database {AppConfigManager.Current.Database.Provider}:\n{ex.Message}",
                                "Koneksi Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            string username = tbRegUsername.Text.Trim();
            string email = tbRegEmail.Text.Trim();
            string city = cbRegCity.SelectedItem?.ToString() ?? "Yogyakarta";
            string pass = tbRegPassword.Password.Trim();
            string confirm = tbRegConfirmPassword.Password.Trim();

            if (pass != confirm)
            {
                MessageBox.Show("Password dan konfirmasi password tidak cocok!", "Peringatan",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var (success, message) = _authService.Register(username, pass, email, city);
                if (success)
                {
                    MessageBox.Show(message, "Registrasi Berhasil", MessageBoxButton.OK, MessageBoxImage.Information);
                    tbUsername.Text = username;
                    tbPassword.Password = string.Empty;
                    SwitchToLoginView();
                }
                else
                {
                    MessageBox.Show(message, "Registrasi Gagal", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal mendaftarkan akun:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SwitchToRegister_Click(object sender, MouseButtonEventArgs e)
        {
            panelLogin.Visibility = Visibility.Collapsed;
            panelRegister.Visibility = Visibility.Visible;
            txtAuthSubtitle.Text = "Buat akun CarbonAir baru";
        }

        private void SwitchToLogin_Click(object sender, MouseButtonEventArgs e)
        {
            SwitchToLoginView();
        }

        private void SwitchToLoginView()
        {
            panelRegister.Visibility = Visibility.Collapsed;
            panelLogin.Visibility = Visibility.Visible;
            txtAuthSubtitle.Text = "Masuk ke akun Anda";
        }

        private void btnOpenDbConfig_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new DbConfigWindow { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                _dbHelper = new DatabaseHelper();
                _authService = new AuthService(_dbHelper);
                InitializeAndUpdateDbStatus();
            }
        }
    }
}
