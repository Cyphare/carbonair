using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using CarbonAirWpf.Data;
using CarbonAirWpf.Models;
using CarbonAirWpf.Services;
using Microsoft.Win32;

namespace CarbonAirWpf
{
    public partial class DashboardWindow : Window
    {
        private readonly Account _account;
        private string _activeLogUser;
        private string _selectedEnergyType = "Listrik";
        private string _selectedLogFilter = "Semua";
        private int? _editingRecordId;

        private AuthService _authService;
        private EmissionService _emissionService;
        private readonly AirQualityApiService _apiService;

        private CityLocation _activeCity;
        private AirQualityData? _currentCityAqi;
        private List<AirQualityData> _mapStations = new();
        private double _currentUserTotalCo2Kg;
        private string _lastAlertedKey = string.Empty;
        private bool _isInitializingControls = true;

        private readonly DispatcherTimer _autoRefreshTimer;

        private readonly SolidColorBrush _activeBg = Brushes.White;
        private readonly SolidColorBrush _activeText = new(Color.FromRgb(0x0F, 0x76, 0x6E));
        private readonly SolidColorBrush _inactiveBg = Brushes.Transparent;
        private readonly SolidColorBrush _inactiveText = new(Color.FromRgb(0x37, 0x41, 0x51));

        public DashboardWindow() : this(new AdminAccount("user1", "Pengguna Utama", "user1@carbonair.local", "Yogyakarta")) { }

        public DashboardWindow(Account account)
        {
            InitializeComponent();

            _account = account;
            _activeLogUser = account.Username;
            _authService = new AuthService();
            _emissionService = new EmissionService();
            _apiService = new AirQualityApiService();

            string defaultCityName = !string.IsNullOrWhiteSpace(account.City)
                ? account.City
                : AppConfigManager.Current.Api.DefaultCity;
            _activeCity = CityLocation.FindByNameOrDefault(defaultCityName);

            _autoRefreshTimer = new DispatcherTimer();
            _autoRefreshTimer.Tick += async (_, _) => await RefreshDashboardAndMapAsync(showAlertToast: true);

            InitializeControls();
            _isInitializingControls = false;

            LoadUserData();
            ConfigureAutoRefreshTimer();

            Loaded += async (_, _) => await RefreshDashboardAndMapAsync(showAlertToast: true);
            Closed += (_, _) => _autoRefreshTimer.Stop();
        }

        private void InitializeControls()
        {
            txtSidebarUsername.Text = _account.Username;
            txtSidebarRole.Text = _account.Role;

            tbTanggalKonsumsi.Text = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            foreach (var city in CityLocation.IndonesiaPresets)
            {
                cbDashboardCity.Items.Add(city.Name);
                cbPengaturanCity.Items.Add(city.Name);
            }
            cbDashboardCity.SelectedItem = _activeCity.Name;
            cbPengaturanCity.SelectedItem = CityLocation.FindByNameOrDefault(_account.City).Name;

            if (_account.CanManageAllUsers)
            {
                panelAdminUserSwitcher.Visibility = Visibility.Visible;
                borderRegularUserBadge.Visibility = Visibility.Collapsed;
                RefreshUserSwitcherList();
            }
            else
            {
                panelAdminUserSwitcher.Visibility = Visibility.Collapsed;
                borderRegularUserBadge.Visibility = Visibility.Visible;
                txtLogUserBadge.Text = _account.Username;
            }

            LoadSettingsIntoUi();
            UpdateLiveCo2Preview();
        }

        private void RefreshUserSwitcherList()
        {
            cbActiveUser.Items.Clear();
            var users = _authService.GetAllUsernames();
            if (users.Count == 0)
            {
                users.Add(_account.Username);
            }
            foreach (string u in users)
            {
                cbActiveUser.Items.Add(u);
            }
            cbActiveUser.SelectedItem = users.Contains(_activeLogUser) ? _activeLogUser : users[0];
        }

        private void LoadSettingsIntoUi()
        {
            var cfg = AppConfigManager.Current;
            txtPengaturanUsername.Text = _account.Username;
            txtPengaturanRole.Text = _account.Role;
            tbPengaturanEmail.Text = _account.Email;

            cbEmissionUnit.SelectedIndex = string.Equals(cfg.Display.EmissionUnit, "ton", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            int refreshMinutes = cfg.Api.AutoRefreshMinutes;
            cbAutoRefresh.SelectedIndex = refreshMinutes switch
            {
                0 => 0,
                5 => 1,
                30 => 3,
                _ => 2
            };

            chkEnableAlerts.IsChecked = cfg.Api.EnablePollutionAlert;
            tbAlertThreshold.Text = cfg.Api.AqiAlertThreshold.ToString(CultureInfo.InvariantCulture);

            UpdateDatabaseInfoText();
        }

        private void UpdateDatabaseInfoText()
        {
            var db = AppConfigManager.Current.Database;
            txtSettingsDbInfo.Text = string.Equals(db.Provider, "PostgreSQL", StringComparison.OrdinalIgnoreCase)
                ? $"PostgreSQL ({db.PostgreSql.Host}:{db.PostgreSql.Port}/{db.PostgreSql.Database})"
                : $"SQLite ({db.SqlitePath})";
        }

        private void ConfigureAutoRefreshTimer()
        {
            _autoRefreshTimer.Stop();
            int minutes = AppConfigManager.Current.Api.AutoRefreshMinutes;
            if (minutes > 0)
            {
                _autoRefreshTimer.Interval = TimeSpan.FromMinutes(minutes);
                _autoRefreshTimer.Start();
            }
        }

        private async Task RefreshDashboardAndMapAsync(bool showAlertToast)
        {
            await Task.WhenAll(
                RefreshCityAirQualityAsync(showAlertToast),
                RefreshMapStationsAsync()
            );
        }

        private async Task RefreshCityAirQualityAsync(bool showAlertToast)
        {
            SetRefreshButtonBusy(true);
            try
            {
                AirQualityData data = await _apiService.GetCityAirQualityAsync(_activeCity);
                _currentCityAqi = data;

                ApplyAirQualityToDashboard(data);

                if (showAlertToast)
                {
                    CheckAndShowPollutionAlert(data);
                }
            }
            finally
            {
                SetRefreshButtonBusy(false);
            }
        }

        private void SetRefreshButtonBusy(bool busy)
        {
            btnRefreshApi.IsEnabled = !busy;
            if (btnRefreshApi.Template?.FindName("txtBtnRefreshApi", btnRefreshApi) is TextBlock tb)
            {
                tb.Text = busy ? "Memuat..." : "Refresh";
            }
        }

        private void ApplyAirQualityToDashboard(AirQualityData data)
        {
            Color accent = GetMonochromeTextAccent(data.AqiIndex);
            var accentBrush = new SolidColorBrush(accent);
            var chartAccent = Color.FromRgb(0x0F, 0x76, 0x6E);

            txtDashboardAqi.Text = data.AqiIndex.ToString(CultureInfo.InvariantCulture);
            txtDashboardAqiSub.Text = $" {data.Category}";
            txtDashboardAqiSub.Foreground = accentBrush;
            txtDashboardPollutant.Text = data.MainPollutant;

            txtDashboardAngin.Text = data.WindSpeed.ToString("F1", CultureInfo.InvariantCulture);
            txtDashboardWeatherSub.Text = string.Format(
                CultureInfo.InvariantCulture,
                "{0:F1}°C • {1:F0}%",
                data.Temperature,
                data.Humidity);

            txtDashboardStatus.Text = data.StatusLabel;
            txtDashboardStatus.Foreground = accentBrush;
            txtDashboardStatusSub.Text = data.CityName;

            txtPolPm25.Text = data.Pm25.ToString("F1", CultureInfo.InvariantCulture);
            txtPolPm10.Text = data.Pm10.ToString("F1", CultureInfo.InvariantCulture);
            txtPolCo.Text = data.Co.ToString("F0", CultureInfo.InvariantCulture);
            txtPolNo2.Text = data.No2.ToString("F1", CultureInfo.InvariantCulture);
            txtPolO3.Text = data.Ozone.ToString("F1", CultureInfo.InvariantCulture);

            txtChartTitle.Text = $"Tren AQI 24 Jam — {data.CityName}";
            RenderHourlyChart(data.HourlyAqi, chartAccent);

            txtRecommendation.Text = _emissionService.GenerateRecommendation(data, _currentUserTotalCo2Kg);

            int threshold = AppConfigManager.Current.Api.AqiAlertThreshold;
            if (AppConfigManager.Current.Api.EnablePollutionAlert && data.AqiIndex >= threshold)
            {
                bannerPollutionAlert.Visibility = Visibility.Visible;
                txtBannerAlertTitle.Text = $"Peringatan Polusi di {data.CityName} — AQI {data.AqiIndex} ({data.Category})";
                txtBannerAlertBody.Text = data.HealthAdvice;
            }
            else
            {
                bannerPollutionAlert.Visibility = Visibility.Collapsed;
            }
        }

        private void CheckAndShowPollutionAlert(AirQualityData data)
        {
            var apiCfg = AppConfigManager.Current.Api;
            if (!apiCfg.EnablePollutionAlert || data.AqiIndex < apiCfg.AqiAlertThreshold)
            {
                return;
            }

            string alertKey = $"{data.CityName}:{data.AqiIndex}:{data.FetchedAt:HHmm}";
            if (_lastAlertedKey == alertKey)
            {
                return;
            }
            _lastAlertedKey = alertKey;

            var toast = new PollutionAlertWindow(data);
            toast.Show();
        }

        private void RenderHourlyChart(List<HourlyAqiPoint> points, Color accentColor)
        {
            canvasChartDots.Children.Clear();

            if (points == null || points.Count < 2)
            {
                pathChartStroke.Data = Geometry.Empty;
                pathChartFill.Data = Geometry.Empty;
                txtChartSummary.Text = "Min: -- • Rata-rata: -- • Puncak: --";
                return;
            }

            int minVal = points.Min(p => p.Aqi);
            int maxVal = points.Max(p => p.Aqi);
            double avgVal = points.Average(p => p.Aqi);
            txtChartSummary.Text = string.Format(CultureInfo.InvariantCulture, "Min: {0} • Rata-rata: {1:F0} • Puncak: {2}", minVal, avgVal, maxVal);

            int yMax = Math.Max(120, ((maxVal + 39) / 40) * 40);
            txtYAxisMax.Text = yMax.ToString(CultureInfo.InvariantCulture);
            txtYAxisHigh.Text = (yMax * 3 / 4).ToString(CultureInfo.InvariantCulture);
            txtYAxisMid.Text = (yMax / 2).ToString(CultureInfo.InvariantCulture);
            txtYAxisLow.Text = (yMax / 4).ToString(CultureInfo.InvariantCulture);
            txtYAxisZero.Text = "0";

            const double width = 800.0;
            const double height = 190.0;
            double stepX = width / (points.Count - 1);

            var coords = new List<Point>(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                double x = i * stepX;
                double ratio = Math.Clamp((double)points[i].Aqi / yMax, 0.0, 1.0);
                double y = height * (1.0 - ratio);
                coords.Add(new Point(x, y));
            }

            var strokeFigure = new PathFigure { StartPoint = coords[0], IsClosed = false };
            var fillFigure = new PathFigure { StartPoint = new Point(0, height), IsClosed = true };
            fillFigure.Segments.Add(new LineSegment(coords[0], true));

            for (int i = 1; i < coords.Count; i++)
            {
                var p0 = coords[i - 1];
                var p1 = coords[i];
                double midX = (p0.X + p1.X) / 2.0;
                var seg = new BezierSegment(new Point(midX, p0.Y), new Point(midX, p1.Y), p1, true);
                strokeFigure.Segments.Add(seg);

                var fillSeg = new BezierSegment(new Point(midX, p0.Y), new Point(midX, p1.Y), p1, true);
                fillFigure.Segments.Add(fillSeg);
            }

            fillFigure.Segments.Add(new LineSegment(new Point(width, height), true));

            pathChartStroke.Stroke = new SolidColorBrush(accentColor);
            pathChartStroke.Data = new PathGeometry(new[] { strokeFigure });
            pathChartFill.Fill = new LinearGradientBrush(
                Color.FromArgb(36, accentColor.R, accentColor.G, accentColor.B),
                Color.FromArgb(0, 255, 255, 255),
                new Point(0, 0),
                new Point(0, 1));
            pathChartFill.Data = new PathGeometry(new[] { fillFigure });

            var lastPt = coords[^1];
            var endDot = new Ellipse
            {
                Width = 9,
                Height = 9,
                Fill = new SolidColorBrush(accentColor),
                Stroke = Brushes.White,
                StrokeThickness = 2
            };
            Canvas.SetLeft(endDot, lastPt.X - 4.5);
            Canvas.SetTop(endDot, lastPt.Y - 4.5);
            canvasChartDots.Children.Add(endDot);

            TextBlock[] xLabels = { txtXAxis0, txtXAxis1, txtXAxis2, txtXAxis3, txtXAxis4, txtXAxis5 };
            for (int i = 0; i < xLabels.Length; i++)
            {
                int idx = (int)Math.Round(i * (points.Count - 1) / (double)(xLabels.Length - 1));
                xLabels[i].Text = points[idx].TimeLabel;
            }
        }

        private async Task RefreshMapStationsAsync()
        {
            _mapStations = await _apiService.GetIndonesiaStationsAsync();
            RenderMapPins();

            var defaultStation = _mapStations.FirstOrDefault(s =>
                s.CityName.Equals(_activeCity.Name, StringComparison.OrdinalIgnoreCase)) ?? _mapStations.FirstOrDefault();

            if (defaultStation != null)
            {
                SelectMapStation(defaultStation);
            }
        }

        private void RenderMapPins()
        {
            canvasMapPins.Children.Clear();

            foreach (var station in _mapStations)
            {
                var (bgColor, fgColor) = GetMonochromeAqiColors(station.AqiIndex);
                var bgBrush = new SolidColorBrush(bgColor);
                var fgBrush = new SolidColorBrush(fgColor);

                bool labelAbove = station.CityName is "Jakarta" or "Semarang" or "Surabaya";
                double dx = station.CityName switch
                {
                    "Jakarta" => -7,
                    "Bandung" => 7,
                    "Surabaya" => 4,
                    _ => 0
                };
                double dy = station.CityName switch
                {
                    "Jakarta" => -6,
                    "Bandung" => 6,
                    "Semarang" => -7,
                    "Yogyakarta" => 7,
                    "Surabaya" => -3,
                    _ => 0
                };

                var container = new StackPanel
                {
                    Cursor = Cursors.Hand,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    ToolTip = $"{station.CityName} ({station.Province})\nAQI: {station.AqiIndex} ({station.Category})\nPM2.5: {station.Pm25:F1} µg/m³ | Suhu: {station.Temperature:F1}°C"
                };

                var pinBorder = new Border
                {
                    Width = 26,
                    Height = 26,
                    CornerRadius = new CornerRadius(13),
                    Background = bgBrush,
                    BorderBrush = bgColor.R > 180 ? new SolidColorBrush(Color.FromRgb(0x0F, 0x76, 0x6E)) : Brushes.Transparent,
                    BorderThickness = new Thickness(bgColor.R > 180 ? 1 : 0),
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                pinBorder.Child = new TextBlock
                {
                    Text = station.AqiIndex.ToString(CultureInfo.InvariantCulture),
                    Foreground = fgBrush,
                    FontSize = 10.5,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var labelBg = new Border
                {
                    CornerRadius = new CornerRadius(4),
                    Background = new SolidColorBrush(Color.FromArgb(245, 255, 255, 255)),
                    Padding = new Thickness(5, 1, 5, 1),
                    Margin = labelAbove ? new Thickness(0, 0, 0, 2) : new Thickness(0, 2, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                labelBg.Child = new TextBlock
                {
                    Text = station.CityName,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27)),
                    FontSize = 9.5,
                    FontWeight = FontWeights.SemiBold
                };

                if (labelAbove)
                {
                    container.Children.Add(labelBg);
                    container.Children.Add(pinBorder);
                }
                else
                {
                    container.Children.Add(pinBorder);
                    container.Children.Add(labelBg);
                }

                var captured = station;
                container.MouseLeftButtonDown += (_, _) => SelectMapStation(captured);

                Canvas.SetLeft(container, station.MapX + dx - 24);
                Canvas.SetTop(container, station.MapY + dy - (labelAbove ? 32 : 13));
                canvasMapPins.Children.Add(container);
            }
        }

        private void SelectMapStation(AirQualityData station)
        {
            var (bgColor, fgColor) = GetMonochromeAqiColors(station.AqiIndex);

            borderStationBadge.Background = new SolidColorBrush(bgColor);
            borderStationBadge.BorderBrush = bgColor.R > 180 ? new SolidColorBrush(Color.FromRgb(0x0F, 0x76, 0x6E)) : Brushes.Transparent;
            borderStationBadge.BorderThickness = new Thickness(bgColor.R > 180 ? 1 : 0);
            txtStationAqi.Foreground = new SolidColorBrush(fgColor);
            txtStationAqiLabel.Foreground = new SolidColorBrush(fgColor);
            txtStationAqi.Text = station.AqiIndex.ToString(CultureInfo.InvariantCulture);
            txtStationTitle.Text = station.CityName;
            txtStationProvince.Text = $"({station.Province})";
            txtStationCategory.Text = $"• {station.Category}";
            txtStationCategory.Foreground = new SolidColorBrush(GetMonochromeTextAccent(station.AqiIndex));

            txtStationMetrics.Text = string.Format(
                CultureInfo.InvariantCulture,
                "PM2.5: {0:F1} µg/m³  •  PM10: {1:F1} µg/m³  •  Suhu: {2:F1}°C  •  Angin: {3:F1} km/h  •  Kelembapan: {4:F0}%",
                station.Pm25, station.Pm10, station.Temperature, station.WindSpeed, station.Humidity);

            txtStationAdvice.Text = station.HealthAdvice;
            btnMonitorStation.Tag = station;
        }

        private void LoadUserData()
        {
            string unit = AppConfigManager.Current.Display.EmissionUnit;

            List<EmissionRecord> allUserLogs = _emissionService.GetUserLogs(_activeLogUser, null);
            _currentUserTotalCo2Kg = allUserLogs.Sum(l => l.Co2);

            txtDashboardTotalEmisi.Text = EmissionRecord.FormatTotalCo2(_currentUserTotalCo2Kg, unit);
            txtDashboardLogCount.Text = $"{allUserLogs.Count} catatan";

            List<EmissionRecord> filteredLogs = _selectedLogFilter == "Semua"
                ? allUserLogs
                : _emissionService.GetUserLogs(_activeLogUser, _selectedLogFilter);

            double filteredCo2Kg = filteredLogs.Sum(l => l.Co2);
            txtTotalEmisiSummary.Text = $"Total ({_selectedLogFilter}): {EmissionRecord.FormatTotalCo2(filteredCo2Kg, unit)}";

            panelDaftarEmisi.Children.Clear();
            if (filteredLogs.Count == 0)
            {
                panelDaftarEmisi.Children.Add(new Border
                {
                    Padding = new Thickness(12, 24, 12, 24),
                    Child = new TextBlock
                    {
                        Text = $"Belum ada catatan emisi ({_selectedLogFilter}) untuk pengguna '{_activeLogUser}'.",
                        Foreground = new SolidColorBrush(Color.FromRgb(0x52, 0x52, 0x5B)),
                        FontSize = 12.5,
                        HorizontalAlignment = HorizontalAlignment.Center
                    }
                });
            }
            else
            {
                foreach (var log in filteredLogs)
                {
                    panelDaftarEmisi.Children.Add(CreateEmissionCard(log, unit));
                }
            }

            if (_currentCityAqi != null)
            {
                txtRecommendation.Text = _emissionService.GenerateRecommendation(_currentCityAqi, _currentUserTotalCo2Kg);
            }
        }

        private Border CreateEmissionCard(EmissionRecord log, string unit)
        {
            Border row = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xEA, 0xEC, 0xEF)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(2, 13, 2, 13)
            };

            Grid grid = new Grid { VerticalAlignment = VerticalAlignment.Center };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel sp = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            sp.Children.Add(new TextBlock
            {
                Text = log.EnergyType,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27))
            });
            sp.Children.Add(new TextBlock
            {
                Text = $"{log.Date} • {log.FormattedAmount}",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Color.FromRgb(0x52, 0x52, 0x5B)),
                Margin = new Thickness(0, 2, 0, 0)
            });
            Grid.SetColumn(sp, 0);

            TextBlock txtVal = new TextBlock
            {
                Text = log.FormatCo2(unit),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 18, 0)
            };
            Grid.SetColumn(txtVal, 1);

            StackPanel actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            Button btnEdit = CreateSmallActionButton("Edit", Color.FromRgb(0x0F, 0x76, 0x6E));
            btnEdit.Margin = new Thickness(0, 0, 12, 0);
            btnEdit.Click += (_, _) => BeginEditRecord(log);

            Button btnDelete = CreateSmallActionButton("Hapus", Color.FromRgb(0x52, 0x52, 0x5B));
            btnDelete.Click += (_, _) => DeleteRecord(log);

            actions.Children.Add(btnEdit);
            actions.Children.Add(btnDelete);
            Grid.SetColumn(actions, 2);

            grid.Children.Add(sp);
            grid.Children.Add(txtVal);
            grid.Children.Add(actions);
            row.Child = grid;

            return row;
        }

        private static Button CreateSmallActionButton(string label, Color fg)
        {
            var btn = new Button
            {
                Cursor = Cursors.Hand,
                Height = 26,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };

            var factory = new FrameworkElementFactory(typeof(Border));
            factory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            factory.SetValue(Border.PaddingProperty, new Thickness(2, 0, 2, 0));

            var textFactory = new FrameworkElementFactory(typeof(TextBlock));
            textFactory.SetValue(TextBlock.TextProperty, label);
            textFactory.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(fg));
            textFactory.SetValue(TextBlock.FontSizeProperty, 12.0);
            textFactory.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
            textFactory.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

            factory.AppendChild(textFactory);
            btn.Template = new ControlTemplate(typeof(Button)) { VisualTree = factory };
            return btn;
        }

        private void BeginEditRecord(EmissionRecord log)
        {
            _editingRecordId = log.Id;
            if (log.EnergyType.Equals("BBM", StringComparison.OrdinalIgnoreCase))
            {
                SetEnergyTypeUi("BBM");
            }
            else
            {
                SetEnergyTypeUi("Listrik");
            }

            tbJumlahKonsumsi.Text = log.Amount.ToString("0.##", CultureInfo.InvariantCulture);
            tbTanggalKonsumsi.Text = log.Date;

            txtFormModeTitle.Text = $"Edit Catatan (#{log.Id})";
            if (btnCatatJejak.Template?.FindName("txtBtnCatatJejak", btnCatatJejak) is TextBlock tb)
            {
                tb.Text = "Simpan Perubahan";
            }
            btnCancelEdit.Visibility = Visibility.Visible;
        }

        private void CancelEditMode()
        {
            _editingRecordId = null;
            txtFormModeTitle.Text = "Input Konsumsi";
            if (btnCatatJejak.Template?.FindName("txtBtnCatatJejak", btnCatatJejak) is TextBlock tb)
            {
                tb.Text = "+ Catat Jejak";
            }
            btnCancelEdit.Visibility = Visibility.Collapsed;
            tbTanggalKonsumsi.Text = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private void DeleteRecord(EmissionRecord log)
        {
            var confirm = MessageBox.Show(
                $"Yakin ingin menghapus catatan {log.EnergyType} ({log.FormattedAmount}) pada tanggal {log.Date}?",
                "Konfirmasi Hapus", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            if (_emissionService.DeleteEmission(log.Id, _activeLogUser))
            {
                if (_editingRecordId == log.Id)
                {
                    CancelEditMode();
                }
                LoadUserData();
            }
            else
            {
                MessageBox.Show("Gagal menghapus catatan emisi dari database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectEnergyListrik_Click(object sender, MouseButtonEventArgs e) => SetEnergyTypeUi("Listrik");

        private void SelectEnergyBbm_Click(object sender, MouseButtonEventArgs e) => SetEnergyTypeUi("BBM");

        private void SetEnergyTypeUi(string energyType)
        {
            _selectedEnergyType = energyType;
            bool isListrik = energyType.Equals("Listrik", StringComparison.OrdinalIgnoreCase);

            var activeBg = new SolidColorBrush(Color.FromRgb(0x0F, 0x76, 0x6E));
            var inactiveBg = new SolidColorBrush(Color.FromRgb(0xE2, 0xE7, 0xEC));
            var inactiveFg = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27));

            borderEnergyListrik.Background = isListrik ? activeBg : inactiveBg;
            txtEnergyListrik.Foreground = isListrik ? Brushes.White : inactiveFg;
            txtEnergyListrik.FontWeight = isListrik ? FontWeights.SemiBold : FontWeights.Normal;

            borderEnergyBbm.Background = !isListrik ? activeBg : inactiveBg;
            txtEnergyBbm.Foreground = !isListrik ? Brushes.White : inactiveFg;
            txtEnergyBbm.FontWeight = !isListrik ? FontWeights.SemiBold : FontWeights.Normal;

            lblSatuanKonsumsi.Text = isListrik ? "Jumlah (kWh)" : "Jumlah (L)";
            UpdateLiveCo2Preview();
        }

        private void tbJumlahKonsumsi_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtLiveCo2Preview != null)
            {
                UpdateLiveCo2Preview();
            }
        }

        private void UpdateLiveCo2Preview()
        {
            var calc = _emissionService.GetCalculator(_selectedEnergyType);
            if (TryParseAmount(tbJumlahKonsumsi.Text, out double amount) && amount > 0)
            {
                double co2 = calc.CalculateCo2(amount);
                txtLiveCo2Preview.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    "Estimasi: +{0:F2} kg CO₂",
                    co2);
            }
            else
            {
                txtLiveCo2Preview.Text = string.Format(
                    CultureInfo.InvariantCulture,
                    "Faktor {0}: {1:F2} kg/{2}",
                    calc.EnergyType, calc.EmissionFactor, calc.Unit);
            }
        }

        private static bool TryParseAmount(string raw, out double amount)
        {
            string normalized = (raw ?? string.Empty).Trim().Replace(',', '.');
            return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out amount);
        }

        private void btnCatatJejak_Click(object sender, RoutedEventArgs e)
        {
            if (!TryParseAmount(tbJumlahKonsumsi.Text, out double amount) || amount <= 0)
            {
                MessageBox.Show("Mohon masukkan angka jumlah konsumsi energi yang valid (> 0)!",
                                "Peringatan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string date = tbTanggalKonsumsi.Text.Trim();
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                MessageBox.Show("Format tanggal harus YYYY-MM-DD (contoh: 2026-11-22)!",
                                "Format Tanggal Tidak Valid", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isEditing = _editingRecordId.HasValue;
            bool success = isEditing
                ? _emissionService.UpdateEmission(_editingRecordId!.Value, _activeLogUser, _selectedEnergyType, amount, date)
                : _emissionService.RecordEmission(_activeLogUser, _selectedEnergyType, amount, date);

            if (success)
            {
                var calc = _emissionService.GetCalculator(_selectedEnergyType);
                double co2 = calc.CalculateCo2(amount);

                MessageBox.Show(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}\n\nPengguna: {1}\nJenis Energi: {2}\nJumlah: {3:0.##} {4}\nFaktor Emisi: {5:F2} kg/{4}\nHasil Emisi: +{6:F2} kg CO₂",
                        isEditing ? "Perubahan Jejak Karbon Disimpan!" : "Jejak Karbon Berhasil Dihitung & Disimpan!",
                        _activeLogUser,
                        calc.EnergyType,
                        amount,
                        calc.Unit,
                        calc.EmissionFactor,
                        co2),
                    isEditing ? "Update Emisi Sukses" : "Kalkulasi Polimorfis Sukses",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                CancelEditMode();
                LoadUserData();
            }
            else
            {
                MessageBox.Show("Gagal menyimpan catatan emisi ke database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancelEdit_Click(object sender, RoutedEventArgs e) => CancelEditMode();

        private void cbActiveUser_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializingControls || cbActiveUser.SelectedItem == null) return;
            _activeLogUser = cbActiveUser.SelectedItem.ToString() ?? _account.Username;
            CancelEditMode();
            LoadUserData();
        }

        private void FilterAll_Click(object sender, MouseButtonEventArgs e) => SetLogFilter("Semua");
        private void FilterListrik_Click(object sender, MouseButtonEventArgs e) => SetLogFilter("Listrik");
        private void FilterBbm_Click(object sender, MouseButtonEventArgs e) => SetLogFilter("BBM");

        private void SetLogFilter(string filter)
        {
            _selectedLogFilter = filter;
            UpdateFilterPill(borderFilterAll, txtFilterAll, filter == "Semua");
            UpdateFilterPill(borderFilterListrik, txtFilterListrik, filter == "Listrik");
            UpdateFilterPill(borderFilterBbm, txtFilterBbm, filter == "BBM");
            LoadUserData();
        }

        private static void UpdateFilterPill(Border border, TextBlock text, bool active)
        {
            border.BorderBrush = active
                ? new SolidColorBrush(Color.FromRgb(0x0F, 0x76, 0x6E))
                : Brushes.Transparent;
            text.Foreground = active
                ? new SolidColorBrush(Color.FromRgb(0x0F, 0x76, 0x6E))
                : new SolidColorBrush(Color.FromRgb(0x52, 0x52, 0x5B));
            text.FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal;
        }

        private void btnExportCsv_Click(object sender, RoutedEventArgs e)
        {
            var logs = _emissionService.GetUserLogs(_activeLogUser, _selectedLogFilter == "Semua" ? null : _selectedLogFilter);
            if (logs.Count == 0)
            {
                MessageBox.Show("Tidak ada data catatan emisi untuk diekspor.", "Informasi", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new SaveFileDialog
            {
                Filter = "CSV File (*.csv)|*.csv",
                FileName = $"CarbonAir_Emisi_{_activeLogUser}_{DateTime.Now:yyyyMMdd}.csv",
                Title = "Ekspor Riwayat Emisi ke CSV"
            };

            if (dlg.ShowDialog() == true)
            {
                bool ok = _emissionService.ExportLogsToCsv(logs, dlg.FileName);
                MessageBox.Show(
                    ok ? $"Berhasil mengekspor {logs.Count} catatan emisi ke:\n{dlg.FileName}" : "Gagal mengekspor file CSV.",
                    ok ? "Ekspor Sukses" : "Error",
                    MessageBoxButton.OK,
                    ok ? MessageBoxImage.Information : MessageBoxImage.Error);
            }
        }

        private async void cbDashboardCity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializingControls || cbDashboardCity.SelectedItem == null) return;
            string cityName = cbDashboardCity.SelectedItem.ToString() ?? "Yogyakarta";
            _activeCity = CityLocation.FindByNameOrDefault(cityName);
            await RefreshCityAirQualityAsync(showAlertToast: true);
        }

        private async void tbCitySearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await ExecuteCitySearchAsync();
            }
        }

        private async void btnSearchCity_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteCitySearchAsync();
        }

        private async Task ExecuteCitySearchAsync()
        {
            string query = tbCitySearch.Text.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                MessageBox.Show("Masukkan nama kota yang ingin dicari (contoh: Malang, Solo, Bogor).",
                                "Pencarian Kota", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            btnSearchCity.IsEnabled = false;
            try
            {
                CityLocation? found = await _apiService.SearchCityAsync(query);

                if (found == null)
                {
                    MessageBox.Show($"Kota '{query}' tidak ditemukan melalui Open-Meteo Geocoding API atau koneksi sedang offline.",
                                    "Kota Tidak Ditemukan", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _activeCity = found;
                await RefreshCityAirQualityAsync(showAlertToast: true);
            }
            finally
            {
                btnSearchCity.IsEnabled = true;
            }
        }

        private async void btnRefreshApi_Click(object sender, RoutedEventArgs e)
        {
            await RefreshCityAirQualityAsync(showAlertToast: true);
        }

        private async void btnRefreshMap_Click(object sender, RoutedEventArgs e)
        {
            await RefreshMapStationsAsync();
        }

        private async void btnMonitorStation_Click(object sender, RoutedEventArgs e)
        {
            if (btnMonitorStation.Tag is AirQualityData station)
            {
                _activeCity = CityLocation.FindByNameOrDefault(station.CityName);
                _isInitializingControls = true;
                cbDashboardCity.SelectedItem = _activeCity.Name;
                _isInitializingControls = false;

                SetActiveTab(0);
                await RefreshCityAirQualityAsync(showAlertToast: true);
            }
        }

        private void btnBannerAdvice_Click(object sender, RoutedEventArgs e)
        {
            if (_currentCityAqi != null)
            {
                new PollutionAlertWindow(_currentCityAqi).Show();
            }
        }

        private async void btnTestApi_Click(object sender, RoutedEventArgs e)
        {
            btnTestApi.IsEnabled = false;
            try
            {
                var (success, message) = await _apiService.TestConnectionAsync();
                MessageBox.Show(message, success ? "Tes Koneksi API Berhasil" : "Tes Koneksi API Gagal",
                                MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            finally
            {
                btnTestApi.IsEnabled = true;
            }
        }

        private void btnTestAlert_Click(object sender, RoutedEventArgs e)
        {
            var sample = _currentCityAqi ?? new AirQualityData
            {
                CityName = _activeCity.Name,
                AqiIndex = 158
            };

            var preview = new AirQualityData
            {
                CityName = sample.CityName,
                AqiIndex = Math.Max(sample.AqiIndex, 155)
            };
            preview.ApplyClassification();
            new PollutionAlertWindow(preview).Show();
        }

        private void btnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            string oldPass = tbOldPassword.Password;
            string newPass = tbNewPassword.Password;
            string confirm = tbConfirmNewPassword.Password;

            if (newPass != confirm)
            {
                MessageBox.Show("Password baru dan konfirmasi password tidak cocok!", "Peringatan",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var (success, message) = _authService.ChangePassword(_account.Username, oldPass, newPass);
            MessageBox.Show(message, success ? "Password Diperbarui" : "Gagal Ubah Password",
                            MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);

            if (success)
            {
                tbOldPassword.Password = string.Empty;
                tbNewPassword.Password = string.Empty;
                tbConfirmNewPassword.Password = string.Empty;
            }
        }

        private void btnSettingsDbConfig_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new DbConfigWindow { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                _authService = new AuthService();
                _emissionService = new EmissionService();
                UpdateDatabaseInfoText();
                if (_account.CanManageAllUsers)
                {
                    RefreshUserSwitcherList();
                }
                LoadUserData();
            }
        }

        private void btnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(tbAlertThreshold.Text.Trim(), out int threshold) || threshold <= 0)
            {
                MessageBox.Show("Ambang batas AQI harus berupa angka bulat positif (contoh: 100).",
                                "Validasi Pengaturan", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string email = tbPengaturanEmail.Text.Trim();
            string city = cbPengaturanCity.SelectedItem?.ToString() ?? "Yogyakarta";

            var (profOk, profMsg) = _authService.UpdateProfile(_account.Username, email, city);
            if (!profOk)
            {
                MessageBox.Show(profMsg, "Error Profil", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _account.Email = email;
            _account.City = city;

            var cfg = AppConfigManager.Current;
            cfg.Display.EmissionUnit = cbEmissionUnit.SelectedIndex == 1 ? "ton" : "kg";
            cfg.Api.DefaultCity = city;
            cfg.Api.AutoRefreshMinutes = cbAutoRefresh.SelectedIndex switch
            {
                0 => 0,
                1 => 5,
                3 => 30,
                _ => 15
            };
            cfg.Api.EnablePollutionAlert = chkEnableAlerts.IsChecked == true;
            cfg.Api.AqiAlertThreshold = threshold;

            AppConfigManager.Save(cfg);
            ConfigureAutoRefreshTimer();
            LoadUserData();
            if (_currentCityAqi != null)
            {
                ApplyAirQualityToDashboard(_currentCityAqi);
            }

            MessageBox.Show("Seluruh pengaturan profil, API, notifikasi, dan satuan emisi berhasil disimpan!",
                            "Pengaturan Tersimpan", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NavDashboard_Click(object sender, MouseButtonEventArgs e) => SetActiveTab(0);
        private void NavPetaAqi_Click(object sender, MouseButtonEventArgs e) => SetActiveTab(1);
        private void NavLogEnergi_Click(object sender, MouseButtonEventArgs e) => SetActiveTab(2);
        private void NavPengaturan_Click(object sender, MouseButtonEventArgs e) => SetActiveTab(3);

        public void SetActiveTab(int index)
        {
            panelDashboard.Visibility = (index == 0) ? Visibility.Visible : Visibility.Collapsed;
            panelPetaAqi.Visibility = (index == 1) ? Visibility.Visible : Visibility.Collapsed;
            panelLogEnergi.Visibility = (index == 2) ? Visibility.Visible : Visibility.Collapsed;
            panelPengaturan.Visibility = (index == 3) ? Visibility.Visible : Visibility.Collapsed;

            borderNavDashboard.Background = (index == 0) ? _activeBg : _inactiveBg;
            barNavDashboard.Visibility = (index == 0) ? Visibility.Visible : Visibility.Collapsed;
            iconNavDashboard.Fill = (index == 0) ? _activeText : _inactiveText;
            textNavDashboard.Foreground = (index == 0) ? _activeText : _inactiveText;
            textNavDashboard.FontWeight = (index == 0) ? FontWeights.SemiBold : FontWeights.Normal;

            borderNavPetaAqi.Background = (index == 1) ? _activeBg : _inactiveBg;
            barNavPetaAqi.Visibility = (index == 1) ? Visibility.Visible : Visibility.Collapsed;
            iconNavPetaAqi.Fill = (index == 1) ? _activeText : _inactiveText;
            textNavPetaAqi.Foreground = (index == 1) ? _activeText : _inactiveText;
            textNavPetaAqi.FontWeight = (index == 1) ? FontWeights.SemiBold : FontWeights.Normal;

            borderNavLogEnergi.Background = (index == 2) ? _activeBg : _inactiveBg;
            barNavLogEnergi.Visibility = (index == 2) ? Visibility.Visible : Visibility.Collapsed;
            iconNavLogEnergi.Fill = (index == 2) ? _activeText : _inactiveText;
            textNavLogEnergi.Foreground = (index == 2) ? _activeText : _inactiveText;
            textNavLogEnergi.FontWeight = (index == 2) ? FontWeights.SemiBold : FontWeights.Normal;

            borderNavPengaturan.Background = (index == 3) ? _activeBg : _inactiveBg;
            barNavPengaturan.Visibility = (index == 3) ? Visibility.Visible : Visibility.Collapsed;
            iconNavPengaturan.Fill = (index == 3) ? _activeText : _inactiveText;
            textNavPengaturan.Foreground = (index == 3) ? _activeText : _inactiveText;
            textNavPengaturan.FontWeight = (index == 3) ? FontWeights.SemiBold : FontWeights.Normal;
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            _autoRefreshTimer.Stop();
            MainWindow login = new MainWindow();
            login.Show();
            Close();
        }

        private static (Color Bg, Color Fg) GetMonochromeAqiColors(int aqi)
        {
            if (aqi <= 50) return (Color.FromRgb(0xD1, 0xEC, 0xE7), Color.FromRgb(0x11, 0x5E, 0x59));
            if (aqi <= 100) return (Color.FromRgb(0x0F, 0x76, 0x6E), Colors.White);
            if (aqi <= 150) return (Color.FromRgb(0x11, 0x5E, 0x59), Colors.White);
            if (aqi <= 200) return (Color.FromRgb(0x13, 0x4E, 0x4A), Colors.White);
            return (Color.FromRgb(0x11, 0x18, 0x27), Colors.White);
        }

        private static Color GetMonochromeTextAccent(int aqi)
        {
            if (aqi <= 150) return Color.FromRgb(0x0F, 0x76, 0x6E);
            if (aqi <= 200) return Color.FromRgb(0x13, 0x4E, 0x4A);
            return Color.FromRgb(0x11, 0x18, 0x27);
        }
    }
}
