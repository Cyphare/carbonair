using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CarbonAirWpf.Data;

namespace CarbonAirWpf
{
    public partial class DbConfigWindow : Window
    {
        public DbConfigWindow()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            var db = AppConfigManager.Current.Database;
            cbProvider.SelectedIndex = string.Equals(db.Provider, "PostgreSQL", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            tbSqlitePath.Text = db.SqlitePath;

            tbPgHost.Text = db.PostgreSql.Host;
            tbPgPort.Text = db.PostgreSql.Port.ToString();
            tbPgDatabase.Text = db.PostgreSql.Database;
            tbPgUsername.Text = db.PostgreSql.Username;
            tbPgPassword.Password = db.PostgreSql.Password;

            ToggleProviderPanels();
        }

        private void cbProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (panelSqlite != null && panelPostgres != null)
            {
                ToggleProviderPanels();
            }
        }

        private void ToggleProviderPanels()
        {
            bool isPg = cbProvider.SelectedIndex == 1;
            panelSqlite.Visibility = isPg ? Visibility.Collapsed : Visibility.Visible;
            panelPostgres.Visibility = isPg ? Visibility.Visible : Visibility.Collapsed;
        }

        private DatabaseConfig BuildConfigFromInputs()
        {
            int port = int.TryParse(tbPgPort.Text.Trim(), out int p) && p > 0 ? p : 5432;
            return new DatabaseConfig
            {
                Provider = cbProvider.SelectedIndex == 1 ? "PostgreSQL" : "SQLite",
                SqlitePath = string.IsNullOrWhiteSpace(tbSqlitePath.Text) ? "carbonair.db" : tbSqlitePath.Text.Trim(),
                PostgreSql = new PostgreSqlConfig
                {
                    Host = string.IsNullOrWhiteSpace(tbPgHost.Text) ? "127.0.0.1" : tbPgHost.Text.Trim(),
                    Port = port,
                    Database = string.IsNullOrWhiteSpace(tbPgDatabase.Text) ? "carbonair_db" : tbPgDatabase.Text.Trim(),
                    Username = string.IsNullOrWhiteSpace(tbPgUsername.Text) ? "postgres" : tbPgUsername.Text.Trim(),
                    Password = tbPgPassword.Password ?? string.Empty
                }
            };
        }

        private void ShowStatus(bool ok, string message)
        {
            borderDbTestStatus.Visibility = Visibility.Visible;
            borderDbTestStatus.Background = new SolidColorBrush(Color.FromRgb(0xF4, 0xF6, 0xF8));
            borderDbTestStatus.BorderBrush = new SolidColorBrush(ok
                ? Color.FromRgb(0x0F, 0x76, 0x6E)
                : Color.FromRgb(0x11, 0x18, 0x27));
            txtDbTestStatus.Foreground = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27));
            txtDbTestStatus.Text = message;
        }

        private void btnTestDb_Click(object sender, RoutedEventArgs e)
        {
            var candidate = BuildConfigFromInputs();
            var helper = new DatabaseHelper(candidate);
            var (success, message) = helper.TestConnection();
            ShowStatus(success, message);
        }

        private void btnSaveDb_Click(object sender, RoutedEventArgs e)
        {
            var candidate = BuildConfigFromInputs();
            var helper = new DatabaseHelper(candidate);
            var (success, message) = helper.InitializeDatabase();

            if (!success)
            {
                ShowStatus(false, message);
                MessageBox.Show(message, "Gagal Menerapkan Konfigurasi Database", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var fullConfig = AppConfigManager.Current;
            fullConfig.Database = candidate;
            AppConfigManager.Save(fullConfig);

            MessageBox.Show($"Konfigurasi database berhasil disimpan dan diinisialisasi!\nMode Aktif: {candidate.Provider}",
                "Database Siap", MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }

        private void btnCancelDb_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
