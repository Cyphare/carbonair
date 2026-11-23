using System;
using System.Windows;
using System.Windows.Threading;
using CarbonAirWpf.Models;

namespace CarbonAirWpf
{
    public partial class PollutionAlertWindow : Window
    {
        private readonly DispatcherTimer _autoCloseTimer;

        public PollutionAlertWindow(AirQualityData data)
        {
            InitializeComponent();

            txtAlertCity.Text = data.CityName;
            txtAlertAqi.Text = $"AQI {data.AqiIndex} • {data.Category}";
            txtAlertMessage.Text = string.IsNullOrWhiteSpace(data.HealthAdvice)
                ? "Kualitas udara melebihi ambang batas aman. Gunakan masker pelindung saat beraktivitas di luar ruangan."
                : data.HealthAdvice;

            _autoCloseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(8)
            };
            _autoCloseTimer.Tick += (_, _) =>
            {
                _autoCloseTimer.Stop();
                Close();
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Rect workArea = SystemParameters.WorkArea;
            Left = workArea.Right - ActualWidth - 18;
            Top = workArea.Bottom - ActualHeight - 18;
            _autoCloseTimer.Start();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            _autoCloseTimer.Stop();
            Close();
        }
    }
}
