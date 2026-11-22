using System;
using System.IO;
using System.Linq;
using CarbonAirWpf.Data;
using CarbonAirWpf.Models;
using CarbonAirWpf.Services;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CarbonAir.Tests
{
    public class DatabaseAndServicesTests : IDisposable
    {
        private readonly string _tempDir;

        public DatabaseAndServicesTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "CarbonAirTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            try
            {
                if (Directory.Exists(_tempDir))
                {
                    Directory.Delete(_tempDir, recursive: true);
                }
            }
            catch
            {
                // Ignore temp cleanup locks
            }
        }

        private DatabaseHelper CreateIsolatedDb(out string dbPath)
        {
            dbPath = Path.Combine(_tempDir, $"test_{Guid.NewGuid():N}.db");
            var cfg = new DatabaseConfig
            {
                Provider = "SQLite",
                SqlitePath = dbPath
            };
            return new DatabaseHelper(cfg);
        }

        [Fact]
        public void InitializeDatabase_CreatesTablesAndSeedsOnce()
        {
            var db = CreateIsolatedDb(out string dbPath);

            var (ok, msg) = db.InitializeDatabase();
            Assert.True(ok, msg);
            Assert.True(File.Exists(dbPath));

            var users = db.GetAllUsernames();
            Assert.Contains("user1", users);
            Assert.Contains("user2", users);

            var user1Logs = db.GetEmissionLogsFromDb("user1");
            Assert.Equal(2, user1Logs.Count);

            // Delete logs, re-run InitializeDatabase, and confirm seeding does not overwrite
            foreach (var log in user1Logs)
            {
                Assert.True(db.DeleteEmissionLogFromDb(log.Id, "user1"));
            }
            var (ok2, _) = db.InitializeDatabase();
            Assert.True(ok2);
            Assert.Empty(db.GetEmissionLogsFromDb("user1"));
        }

        [Fact]
        public void AuthService_LoginRegisterProfileAndPasswordWorkEndToEnd()
        {
            var db = CreateIsolatedDb(out _);
            Assert.True(db.InitializeDatabase().Success);
            var auth = new AuthService(db);

            // Seeded logins
            Account? admin = auth.Login("user1", "user1");
            Account? regular = auth.Login("user2", "user2");
            Assert.NotNull(admin);
            Assert.True(admin!.CanManageAllUsers);
            Assert.NotNull(regular);
            Assert.False(regular!.CanManageAllUsers);
            Assert.Null(auth.Login("user1", "wrong_pass"));

            // Register validation & duplicate guard
            Assert.False(auth.Register("ab", "1234", "a@b.c", "Jakarta").Success);
            Assert.False(auth.Register("user1", "1234", "a@b.c", "Jakarta").Success);

            var regOk = auth.Register("budi99", "rahasia", "budi@mail.id", "Bandung");
            Assert.True(regOk.Success, regOk.Message);

            Account? budi = auth.Login("budi99", "rahasia");
            Assert.NotNull(budi);
            Assert.Equal("Bandung", budi!.City);
            Assert.Equal("budi@mail.id", budi.Email);

            // Profile update
            Assert.True(auth.UpdateProfile("budi99", "newbudi@mail.id", "Surabaya").Success);
            Account? updated = auth.GetUserAccount("budi99");
            Assert.Equal("Surabaya", updated!.City);
            Assert.Equal("newbudi@mail.id", updated.Email);

            // Password change
            Assert.False(auth.ChangePassword("budi99", "salah", "barupass").Success);
            Assert.True(auth.ChangePassword("budi99", "rahasia", "barupass").Success);
            Assert.Null(auth.Login("budi99", "rahasia"));
            Assert.NotNull(auth.Login("budi99", "barupass"));
        }

        [Fact]
        public void EmissionService_CrudFilterCsvAndRecommendationsWork()
        {
            var db = CreateIsolatedDb(out _);
            Assert.True(db.InitializeDatabase().Success);
            var service = new EmissionService(db);

            // Validation failures
            Assert.False(service.RecordEmission("user2", "Listrik", 0, "2026-11-22"));
            Assert.False(service.RecordEmission("user2", "Listrik", -10, "2026-11-22"));
            Assert.False(service.RecordEmission("user2", "Listrik", 10, "22-11-2026"));

            // Insert valid records
            Assert.True(service.RecordEmission("user2", "BBM", 10, "2026-11-22"));
            var allLogs = service.GetUserLogs("user2");
            Assert.Equal(2, allLogs.Count); // 1 seeded Listrik + 1 new BBM

            var bbmLogs = service.GetUserLogs("user2", "BBM");
            Assert.Single(bbmLogs);
            Assert.Equal(23.0, bbmLogs[0].Co2, 2);

            // Update record
            int targetId = bbmLogs[0].Id;
            Assert.True(service.UpdateEmission(targetId, "user2", "Listrik", 20, "2026-11-23"));
            var updated = service.GetUserLogs("user2").First(l => l.Id == targetId);
            Assert.Equal("Listrik", updated.EnergyType);
            Assert.Equal(17.0, updated.Co2, 2);

            // CSV Export
            string csvPath = Path.Combine(_tempDir, "export.csv");
            Assert.True(service.ExportLogsToCsv(service.GetUserLogs("user2"), csvPath));
            string csvContent = File.ReadAllText(csvPath);
            Assert.Contains("ID,Username,JenisEnergi,Tanggal,Jumlah,Satuan,EmisiCO2_kg", csvContent);
            Assert.Contains("user2,Listrik,2026-11-23,20,kWh,17.00", csvContent);

            // Delete record
            Assert.True(service.DeleteEmission(targetId, "user2"));
            Assert.Single(service.GetUserLogs("user2"));

            // Recommendation text
            var highAqi = new AirQualityData { CityName = "Jakarta", AqiIndex = 170, MainPollutant = "PM2.5" };
            highAqi.ApplyClassification();
            string rec = service.GenerateRecommendation(highAqi, 75.0);
            Assert.Contains("PERINGATAN POLUSI", rec);
            Assert.Contains("Jakarta", rec);
        }

        [Fact]
        public void AppConfigManager_SavesAndLoadsRoundTrip()
        {
            string cfgPath = Path.Combine(_tempDir, "appsettings.json");
            var config = new AppConfig
            {
                Database = new DatabaseConfig
                {
                    Provider = "PostgreSQL",
                    SqlitePath = "custom.db",
                    PostgreSql = new PostgreSqlConfig
                    {
                        Host = "10.0.0.5",
                        Port = 5433,
                        Database = "ca_prod",
                        Username = "ca_user",
                        Password = "secret"
                    }
                },
                Api = new ApiConfig
                {
                    DefaultCity = "Bandung",
                    AutoRefreshMinutes = 30,
                    AqiAlertThreshold = 120,
                    EnablePollutionAlert = false
                },
                Display = new DisplayConfig { EmissionUnit = "ton" }
            };

            AppConfigManager.Save(config, cfgPath);
            var loaded = AppConfigManager.Load(cfgPath);

            Assert.Equal("PostgreSQL", loaded.Database.Provider);
            Assert.Equal("10.0.0.5", loaded.Database.PostgreSql.Host);
            Assert.Equal(5433, loaded.Database.PostgreSql.Port);
            Assert.Equal("ca_prod", loaded.Database.PostgreSql.Database);
            Assert.Equal("Bandung", loaded.Api.DefaultCity);
            Assert.Equal(30, loaded.Api.AutoRefreshMinutes);
            Assert.Equal(120, loaded.Api.AqiAlertThreshold);
            Assert.False(loaded.Api.EnablePollutionAlert);
            Assert.Equal("ton", loaded.Display.EmissionUnit);
        }
    }
}
