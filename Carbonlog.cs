using System;

namespace CarbonAir
{
    internal class CarbonLog
    {
        private int _logId;
        private DateTime _date;
        private double _transportEmissions;
        private double _energyEmissions;
        private double _totalEmissions;

        public int LogId
        {
            get { return _logId; }
            set { _logId = value; }
        }

        public DateTime Date
        {
            get { return _date; }
            set { _date = value; }
        }

        public double TransportEmissions
        {
            get { return _transportEmissions; }
            set { _transportEmissions = value; }
        }

        public double EnergyEmissions
        {
            get { return _energyEmissions; }
            set { _energyEmissions = value; }
        }

        public double TotalEmissions
        {
            get { return _totalEmissions; }
        }

        public double CalculateTotal()
        {
            throw new NotImplementedException();
        }
    }
}