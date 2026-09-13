using System;
using System.Threading.Tasks;

namespace CarbonAir
{
    internal class MainDashboardViewModel
    {
        private AirQualityService _aqService;
        private DatabaseContext _dbContext;
        private AirQuality _currentAQI;
        private CarbonLog _dailyLog;

        public AirQuality CurrentAQI
        {
            get { return _currentAQI; }
            set { _currentAQI = value; }
        }

        public CarbonLog DailyLog
        {
            get { return _dailyLog; }
            set { _dailyLog = value; }
        }

        public MainDashboardViewModel(AirQualityService aqService, DatabaseContext dbContext)
        {
        }

        public async Task LoadDashboardData()
        {
            throw new NotImplementedException();
        }

        public void SaveCarbonLog()
        {
            throw new NotImplementedException();
        }

        public string GenerateRecommendation()
        {
            throw new NotImplementedException();
        }
    }
}