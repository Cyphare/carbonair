using System;

namespace CarbonAir
{
    internal class AirQuality
    {
        private int _aqiIndex;
        private string _category;
        private string _mainPollutant;
        private double _temperature;

        public int AQIIndex
        {
            get { return _aqiIndex; }
            set { _aqiIndex = value; }
        }

        public string Category
        {
            get { return _category; }
            set { _category = value; }
        }

        public string MainPollutant
        {
            get { return _mainPollutant; }
            set { _mainPollutant = value; }
        }

        public double Temperature
        {
            get { return _temperature; }
            set { _temperature = value; }
        }
    }
}