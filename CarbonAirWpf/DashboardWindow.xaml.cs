using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CarbonAirWpf.Models;
using CarbonAirWpf.Services;

namespace CarbonAirWpf
{
    public partial class DashboardWindow : Window
    {
        private string _currentUser = "user1";
        private string _selectedEnergyType = "Listrik";
        private readonly EmissionService _emissionService;
        private readonly AqiService _aqiService;

        private readonly SolidColorBrush _activeBg = new SolidColorBrush(Color.FromRgb(0x14, 0x26, 0x30));
        private readonly SolidColorBrush _activeBorder = new SolidColorBrush(Color.FromRgb(0x00, 0xB4, 0xD8));
        private readonly SolidColorBrush _activeText = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF));

        private readonly SolidColorBrush _inactiveBg = Brushes.Transparent;
        private readonly SolidColorBrush _inactiveBorder = Brushes.Transparent;
        private readonly SolidColorBrush _inactiveText = new SolidColorBrush(Color.FromRgb(0x8E, 0x9F, 0xA8));

        public DashboardWindow() : this("user1") { }

        public DashboardWindow(string username)
        {
            InitializeComponent();
            _currentUser = string.IsNullOrWhiteSpace(username) ? "user1" : username.Trim();
            _emissionService = new EmissionService();
            _aqiService = new AqiService();
            LoadUserData();
        }

        // [Tugas 2: Load Data dari Database PostgreSQL]
        private void LoadUserData()
        {
            txtSidebarUsername.Text = _currentUser;
            txtLogUserBadge.Text = _currentUser;
            txtPengaturanUsername.Text = _currentUser;
            txtPengaturanEmail.Text = $"{_currentUser.ToLower()}@carbonair.local";
            txtPengaturanRole.Text = _currentUser.Equals("user1", StringComparison.OrdinalIgnoreCase) ? "Pengguna Utama" : "Pengguna Sekunder";

            // Data grafik tren & indikator AQI (menggunakan AqiService polimorfis)
            bool isUser2 = _currentUser.Equals("user2", StringComparison.OrdinalIgnoreCase);
            int currentAqi = isUser2 ? 85 : 72;
            AqiEvaluator evaluator = _aqiService.EvaluateAqi(currentAqi);

            txtDashboardAqi.Text = evaluator.AqiScore.ToString();
            txtDashboardStatus.Text = evaluator.GetStatusLabel();
            txtDashboardStatus.ToolTip = evaluator.GetAirQualityAdvice();
            txtAqiAdvice.Text = evaluator.GetAirQualityAdvice();

            if (isUser2)
            {
                txtDashboardAqiSub.Text = " Sedang";
                txtDashboardAqiSub.Foreground = new SolidColorBrush(Color.FromRgb(241, 196, 15));
                txtDashboardAngin.Text = "11";
                txtDashboardStatus.Foreground = new SolidColorBrush(Color.FromRgb(241, 196, 15));
                iconAqiAdvice.Fill = new SolidColorBrush(Color.FromRgb(241, 196, 15));
                borderAqiAdvice.BorderBrush = new SolidColorBrush(Color.FromRgb(0x40, 0x36, 0x16));
                pathChartStroke.Data = Geometry.Parse("M 0,145 C 130,145 220,130 320,85 C 410,50 460,20 520,20 C 580,20 620,55 690,75 C 740,90 770,110 800,115");
                pathChartFill.Data = Geometry.Parse("M 0,145 C 130,145 220,130 320,85 C 410,50 460,20 520,20 C 580,20 620,55 690,75 C 740,90 770,110 800,115 L 800,190 L 0,190 Z");
            }
            else
            {
                txtDashboardAqiSub.Text = " Baik";
                txtDashboardAqiSub.Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113));
                txtDashboardAngin.Text = "14";
                txtDashboardStatus.Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113));
                iconAqiAdvice.Fill = new SolidColorBrush(Color.FromRgb(46, 204, 113));
                borderAqiAdvice.BorderBrush = new SolidColorBrush(Color.FromRgb(0x1C, 0x2E, 0x37));
                pathChartStroke.Data = Geometry.Parse("M 0,165 C 140,165 240,150 340,115 C 430,80 470,35 530,35 C 590,35 630,65 690,85 C 740,100 760,115 800,125");
                pathChartFill.Data = Geometry.Parse("M 0,165 C 140,165 240,150 340,115 C 430,80 470,35 530,35 C 590,35 630,65 690,85 C 740,100 760,115 800,125 L 800,190 L 0,190 Z");
            }

            // [Tugas 2: Query PostgreSQL] Mengambil riwayat emisi user dari database
            List<EmissionRecord> logs = _emissionService.GetUserLogs(_currentUser);
            panelDaftarEmisi.Children.Clear();

            double totalCo2 = 0;
            foreach (var log in logs)
            {
                panelDaftarEmisi.Children.Add(CreateEmissionCard(log));
                totalCo2 += log.Co2;
            }

            string totalStr = $"{totalCo2:F1} kg CO₂";
            txtTotalEmisiSummary.Text = $"Total: {totalStr}";
            txtDashboardTotalEmisi.Text = totalStr;
        }

        // Komponen kartu baris riwayat emisi
        private Border CreateEmissionCard(EmissionRecord log)
        {
            Border card = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromRgb(0x0E, 0x17, 0x1C)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x1A, 0x2A, 0x33)),
                BorderThickness = new Thickness(1),
                Height = 64,
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(14, 0, 14, 0)
            };

            Grid grid = new Grid { VerticalAlignment = VerticalAlignment.Center };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            bool isListrik = log.EnergyType.Equals("Listrik", StringComparison.OrdinalIgnoreCase);

            Border iconBox = new Border
            {
                Width = 36,
                Height = 36,
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(isListrik ? Color.FromRgb(0x23, 0x20, 0x14) : Color.FromRgb(0x25, 0x16, 0x16)),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            System.Windows.Shapes.Path path = new System.Windows.Shapes.Path
            {
                Width = 14,
                Height = 14,
                Stretch = Stretch.Uniform,
                Fill = new SolidColorBrush(isListrik ? Color.FromRgb(0xF1, 0xC4, 0x0F) : Color.FromRgb(0xE7, 0x4C, 0x3C)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Data = Geometry.Parse(isListrik 
                    ? "M7 2v11h3v9l7-12h-4l4-8z" 
                    : "M19 5h-2V3H7v18h10v-2h2c1.1 0 2-.9 2-2V7c0-1.1-.9-2-2-2zm-4 14H9V5h6v14zm4-4h-2V7h2v8z")
            };
            iconBox.Child = path;
            Grid.SetColumn(iconBox, 0);

            StackPanel sp = new StackPanel
            {
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            sp.Children.Add(new TextBlock
            {
                Text = log.EnergyType,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            });
            sp.Children.Add(new TextBlock
            {
                Text = $"{log.Date} • {log.FormattedAmount}",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(0x65, 0x79, 0x84)),
                Margin = new Thickness(0, 2, 0, 0)
            });
            Grid.SetColumn(sp, 1);

            TextBlock txtVal = new TextBlock
            {
                Text = log.FormattedCo2,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xD2, 0xFF)),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(txtVal, 2);

            Button btnDel = new Button
            {
                Content = "Hapus",
                Margin = new Thickness(12, 0, 0, 0),
                Padding = new Thickness(8, 3, 8, 3),
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE7, 0x4C, 0x3C)),
                Background = new SolidColorBrush(Color.FromRgb(0x25, 0x16, 0x16)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x22, 0x22)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            btnDel.Click += (s, e) =>
            {
                if (_emissionService.DeleteLog(log.Id))
                {
                    LoadUserData();
                }
            };
            Grid.SetColumn(btnDel, 3);

            grid.Children.Add(iconBox);
            grid.Children.Add(sp);
            grid.Children.Add(txtVal);
            grid.Children.Add(btnDel);
            card.Child = grid;

            return card;
        }

        // Event pemilih jenis energi Listrik
        private void SelectEnergyListrik_Click(object sender, MouseButtonEventArgs e)
        {
            _selectedEnergyType = "Listrik";
            borderEnergyListrik.Background = new SolidColorBrush(Color.FromRgb(0x14, 0x2C, 0x33));
            borderEnergyListrik.BorderBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xB4, 0xD8));
            iconEnergyListrik.Fill = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF));
            txtEnergyListrik.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF));

            borderEnergyBbm.Background = new SolidColorBrush(Color.FromRgb(0x0B, 0x14, 0x18));
            borderEnergyBbm.BorderBrush = new SolidColorBrush(Color.FromRgb(0x1C, 0x2E, 0x37));
            iconEnergyBbm.Fill = new SolidColorBrush(Color.FromRgb(0x65, 0x79, 0x84));
            txtEnergyBbm.Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0x9F, 0xA8));

            lblSatuanKonsumsi.Text = "Jumlah (kWh)";
        }

        // Event pemilih jenis energi BBM
        private void SelectEnergyBbm_Click(object sender, MouseButtonEventArgs e)
        {
            _selectedEnergyType = "BBM";
            borderEnergyBbm.Background = new SolidColorBrush(Color.FromRgb(0x14, 0x2C, 0x33));
            borderEnergyBbm.BorderBrush = new SolidColorBrush(Color.FromRgb(0x00, 0xB4, 0xD8));
            iconEnergyBbm.Fill = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF));
            txtEnergyBbm.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xE5, 0xFF));

            borderEnergyListrik.Background = new SolidColorBrush(Color.FromRgb(0x0B, 0x14, 0x18));
            borderEnergyListrik.BorderBrush = new SolidColorBrush(Color.FromRgb(0x1C, 0x2E, 0x37));
            iconEnergyListrik.Fill = new SolidColorBrush(Color.FromRgb(0x65, 0x79, 0x84));
            txtEnergyListrik.Foreground = new SolidColorBrush(Color.FromRgb(0x8E, 0x9F, 0xA8));

            lblSatuanKonsumsi.Text = "Jumlah (L)";
        }

        // [Tugas 1: Polymorphism & Tugas 2: Insert PostgreSQL]
        private void btnCatatJejak_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(tbJumlahKonsumsi.Text.Trim(), out double amount) || amount <= 0)
            {
                MessageBox.Show("Mohon masukkan angka jumlah konsumsi energi yang valid!", "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string date = tbTanggalKonsumsi.Text.Trim();

            // Eksekusi kalkulasi polimorfis dan simpan ke database PostgreSQL
            bool success = _emissionService.RecordEmission(_currentUser, _selectedEnergyType, amount, date);

            if (success)
            {
                var calc = _emissionService.GetCalculator(_selectedEnergyType);
                double co2 = calc.CalculateCo2(amount);

                MessageBox.Show($"Jejak Karbon Berhasil Dihitung & Disimpan ke PostgreSQL!\n\n" +
                                $"Jenis Energi: {_selectedEnergyType}\n" +
                                $"Jumlah: {amount} {(_selectedEnergyType == "Listrik" ? "kWh" : "L")}\n" +
                                $"Faktor Emisi: {calc.EmissionFactor}\n" +
                                $"Hasil Emisi: +{co2:F2} kg CO₂",
                                "Kalkulasi Polimorfis Sukses", MessageBoxButton.OK, MessageBoxImage.Information);

                LoadUserData();
            }
            else
            {
                MessageBox.Show("Gagal menyimpan catatan emisi ke database PostgreSQL.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnToggleUser_Click(object sender, RoutedEventArgs e)
        {
            _currentUser = _currentUser.Equals("user1", StringComparison.OrdinalIgnoreCase) ? "user2" : "user1";
            LoadUserData();
        }

        private void NavDashboard_Click(object sender, MouseButtonEventArgs e)
        {
            SetActiveTab(0);
        }

        private void NavPetaAqi_Click(object sender, MouseButtonEventArgs e)
        {
            SetActiveTab(1);
        }

        private void NavLogEnergi_Click(object sender, MouseButtonEventArgs e)
        {
            SetActiveTab(2);
        }

        private void NavPengaturan_Click(object sender, MouseButtonEventArgs e)
        {
            SetActiveTab(3);
        }

        public void SetActiveTab(int index)
        {
            panelDashboard.Visibility = (index == 0) ? Visibility.Visible : Visibility.Collapsed;
            panelPetaAqi.Visibility = (index == 1) ? Visibility.Visible : Visibility.Collapsed;
            panelLogEnergi.Visibility = (index == 2) ? Visibility.Visible : Visibility.Collapsed;
            panelPengaturan.Visibility = (index == 3) ? Visibility.Visible : Visibility.Collapsed;

            borderNavDashboard.Background = (index == 0) ? _activeBg : _inactiveBg;
            borderNavDashboard.BorderBrush = (index == 0) ? _activeBorder : _inactiveBorder;
            iconNavDashboard.Fill = (index == 0) ? _activeText : _inactiveText;
            textNavDashboard.Foreground = (index == 0) ? _activeText : _inactiveText;

            borderNavPetaAqi.Background = (index == 1) ? _activeBg : _inactiveBg;
            borderNavPetaAqi.BorderBrush = (index == 1) ? _activeBorder : _inactiveBorder;
            iconNavPetaAqi.Fill = (index == 1) ? _activeText : _inactiveText;
            textNavPetaAqi.Foreground = (index == 1) ? _activeText : _inactiveText;

            borderNavLogEnergi.Background = (index == 2) ? _activeBg : _inactiveBg;
            borderNavLogEnergi.BorderBrush = (index == 2) ? _activeBorder : _inactiveBorder;
            iconNavLogEnergi.Fill = (index == 2) ? _activeText : _inactiveText;
            textNavLogEnergi.Foreground = (index == 2) ? _activeText : _inactiveText;

            borderNavPengaturan.Background = (index == 3) ? _activeBg : _inactiveBg;
            borderNavPengaturan.BorderBrush = (index == 3) ? _activeBorder : _inactiveBorder;
            iconNavPengaturan.Fill = (index == 3) ? _activeText : _inactiveText;
            textNavPengaturan.Foreground = (index == 3) ? _activeText : _inactiveText;
        }

        private void btnNotifikasiPolusi_Click(object sender, RoutedEventArgs e)
        {
            int currentAqi = _currentUser.Equals("user2", StringComparison.OrdinalIgnoreCase) ? 85 : 72;
            NotificationBase notification = _aqiService.CreateNotification(_currentUser, currentAqi);
            string msg = notification.BuildMessage(currentAqi);

            MessageBox.Show(msg, "Notifikasi Desktop Polusi", MessageBoxButton.OK,
                currentAqi <= 75 ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }

        private void btnSimpanPengaturan_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Pengaturan untuk akun '{_currentUser}' berhasil disimpan.", 
                            "Pengaturan Disimpan", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            MainWindow login = new MainWindow();
            login.Show();
            this.Close();
        }
    }
}
