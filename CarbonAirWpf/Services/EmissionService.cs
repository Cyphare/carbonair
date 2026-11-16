using System;
using System.Collections.Generic;
using CarbonAirWpf.Data;
using CarbonAirWpf.Models;

namespace CarbonAirWpf.Services
{
    public class EmissionService
    {
        private readonly DatabaseHelper _dbHelper;

        public EmissionService()
        {
            _dbHelper = new DatabaseHelper();
        }

        // Polymorphism
        public EmissionCalculator GetCalculator(string energyType)
        {
            if (energyType.Equals("BBM", StringComparison.OrdinalIgnoreCase))
            {
                return new FuelEmissionCalculator();
            }

            return new ElectricityEmissionCalculator();
        }

        // Polymorphism & PostgreSQL
        public bool RecordEmission(string username, string energyType, double amount, string date)
        {
            // Polymorphism
            EmissionCalculator calculator = GetCalculator(energyType);
            double calculatedCo2 = calculator.CalculateCo2(amount);

            // Encapsulation
            var record = new EmissionRecord
            {
                Username = username,
                EnergyType = calculator.EnergyType,
                Amount = amount,
                Date = string.IsNullOrWhiteSpace(date) ? DateTime.Now.ToString("yyyy-MM-dd") : date,
                Co2 = calculatedCo2
            };

            // PostgreSQL
            return _dbHelper.InsertEmissionLogToDb(record);
        }

        // PostgreSQL
        public List<EmissionRecord> GetUserLogs(string username)
        {
            return _dbHelper.GetEmissionLogsFromDb(username);
        }
    }
}
