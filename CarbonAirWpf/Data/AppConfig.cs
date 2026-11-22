using System;
using System.IO;
using System.Text.Json;

namespace CarbonAirWpf.Data
{
    public class AppConfig
    {
        public DatabaseConfig Database { get; set; } = new();
        public ApiConfig Api { get; set; } = new();
        public DisplayConfig Display { get; set; } = new();
    }

    public class DatabaseConfig
    {
        public string Provider { get; set; } = "SQLite";
        public string SqlitePath { get; set; } = "carbonair.db";
        public PostgreSqlConfig PostgreSql { get; set; } = new();
    }

    public class PostgreSqlConfig
    {
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 5432;
        public string Database { get; set; } = "carbonair_db";
        public string Username { get; set; } = "postgres";
        public string Password { get; set; } = string.Empty;
    }

    public class ApiConfig
    {
        public string DefaultCity { get; set; } = "Yogyakarta";
        public int AutoRefreshMinutes { get; set; } = 15;
        public int AqiAlertThreshold { get; set; } = 100;
        public bool EnablePollutionAlert { get; set; } = true;
    }

    public class DisplayConfig
    {
        public string EmissionUnit { get; set; } = "kg";
    }

    public static class AppConfigManager
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private static AppConfig? _current;

        public static string DefaultConfigPath =>
            Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        public static AppConfig Current
        {
            get
            {
                if (_current == null)
                {
                    _current = Load();
                }
                return _current;
            }
        }

        public static AppConfig Load(string? customPath = null)
        {
            string path = string.IsNullOrWhiteSpace(customPath) ? DefaultConfigPath : customPath;
            try
            {
                if (!File.Exists(path))
                {
                    var defaults = new AppConfig();
                    Save(defaults, path);
                    if (string.IsNullOrWhiteSpace(customPath))
                    {
                        _current = defaults;
                    }
                    return defaults;
                }

                string json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
                NormalizeConfig(loaded);

                if (string.IsNullOrWhiteSpace(customPath))
                {
                    _current = loaded;
                }
                return loaded;
            }
            catch
            {
                var fallback = new AppConfig();
                if (string.IsNullOrWhiteSpace(customPath))
                {
                    _current = fallback;
                }
                return fallback;
            }
        }

        public static void Save(AppConfig config, string? customPath = null)
        {
            NormalizeConfig(config);
            string path = string.IsNullOrWhiteSpace(customPath) ? DefaultConfigPath : customPath;
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(path, json);

            if (string.IsNullOrWhiteSpace(customPath))
            {
                _current = config;
            }
        }

        public static void Reload(string? customPath = null)
        {
            _current = Load(customPath);
        }

        private static void NormalizeConfig(AppConfig config)
        {
            config.Database ??= new DatabaseConfig();
            config.Database.PostgreSql ??= new PostgreSqlConfig();
            config.Api ??= new ApiConfig();
            config.Display ??= new DisplayConfig();

            config.Database.Provider = string.Equals(config.Database.Provider, "PostgreSQL", StringComparison.OrdinalIgnoreCase)
                ? "PostgreSQL"
                : "SQLite";

            if (string.IsNullOrWhiteSpace(config.Database.SqlitePath))
            {
                config.Database.SqlitePath = "carbonair.db";
            }

            if (config.Database.PostgreSql.Port <= 0)
            {
                config.Database.PostgreSql.Port = 5432;
            }

            if (string.IsNullOrWhiteSpace(config.Api.DefaultCity))
            {
                config.Api.DefaultCity = "Yogyakarta";
            }

            if (config.Api.AqiAlertThreshold <= 0)
            {
                config.Api.AqiAlertThreshold = 100;
            }

            config.Display.EmissionUnit = string.Equals(config.Display.EmissionUnit, "ton", StringComparison.OrdinalIgnoreCase)
                ? "ton"
                : "kg";
        }
    }
}
