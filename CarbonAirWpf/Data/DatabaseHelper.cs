using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO;
using CarbonAirWpf.Models;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace CarbonAirWpf.Data
{
    public record UserDbRecord(string Username, string Role, string Email, string City);

    /// <summary>
    /// Dual-mode database helper supporting zero-config SQLite (default) and PostgreSQL server.
    /// </summary>
    public class DatabaseHelper
    {
        private readonly DatabaseConfig? _overrideConfig;

        public DatabaseHelper(DatabaseConfig? config = null)
        {
            _overrideConfig = config;
        }

        private DatabaseConfig ActiveConfig => _overrideConfig ?? AppConfigManager.Current.Database;

        public bool IsPostgreSql =>
            string.Equals(ActiveConfig.Provider, "PostgreSQL", StringComparison.OrdinalIgnoreCase);

        public string ResolvedSqlitePath
        {
            get
            {
                string raw = string.IsNullOrWhiteSpace(ActiveConfig.SqlitePath)
                    ? "carbonair.db"
                    : ActiveConfig.SqlitePath.Trim();

                return Path.IsPathRooted(raw)
                    ? raw
                    : Path.Combine(AppContext.BaseDirectory, raw);
            }
        }

        private string BuildPostgreSqlConnectionString(string? databaseOverride = null)
        {
            var pg = ActiveConfig.PostgreSql;
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = string.IsNullOrWhiteSpace(pg.Host) ? "127.0.0.1" : pg.Host.Trim(),
                Port = pg.Port > 0 ? pg.Port : 5432,
                Database = databaseOverride ?? (string.IsNullOrWhiteSpace(pg.Database) ? "carbonair_db" : pg.Database.Trim()),
                Username = string.IsNullOrWhiteSpace(pg.Username) ? "postgres" : pg.Username.Trim(),
                Password = pg.Password ?? string.Empty,
                Timeout = 5,
                CommandTimeout = 10
            };
            return builder.ConnectionString;
        }

        private DbConnection CreateConnection()
        {
            if (IsPostgreSql)
            {
                return new NpgsqlConnection(BuildPostgreSqlConnectionString());
            }

            string dbPath = ResolvedSqlitePath;
            string? dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            return new SqliteConnection($"Data Source={dbPath}");
        }

        public static (bool Success, string Message) InitializeDefaultDatabase()
        {
            return new DatabaseHelper().InitializeDatabase();
        }

        public (bool Success, string Message) TestConnection()
        {
            try
            {
                if (IsPostgreSql)
                {
                    try
                    {
                        using var conn = new NpgsqlConnection(BuildPostgreSqlConnectionString());
                        conn.Open();
                        return (true, $"Koneksi PostgreSQL berhasil ({ActiveConfig.PostgreSql.Host}:{ActiveConfig.PostgreSql.Port}/{ActiveConfig.PostgreSql.Database}).");
                    }
                    catch (PostgresException pgEx) when (pgEx.SqlState == "3D000")
                    {
                        using var maintConn = new NpgsqlConnection(BuildPostgreSqlConnectionString("postgres"));
                        maintConn.Open();
                        return (true, $"Server PostgreSQL aktif! Database '{ActiveConfig.PostgreSql.Database}' belum ada dan akan dibuat otomatis saat disimpan.");
                    }
                }
                else
                {
                    using var conn = CreateConnection();
                    conn.Open();
                    return (true, $"Koneksi SQLite siap digunakan ({ResolvedSqlitePath}).");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Gagal terhubung ke database ({ActiveConfig.Provider}): {ex.Message}");
            }
        }

        public (bool Success, string Message) InitializeDatabase()
        {
            try
            {
                if (IsPostgreSql)
                {
                    EnsurePostgreSqlDatabaseExists();
                }

                using var conn = CreateConnection();
                conn.Open();

                if (IsPostgreSql)
                {
                    string ddlPg = @"
                        CREATE TABLE IF NOT EXISTS users (
                            id SERIAL PRIMARY KEY,
                            username VARCHAR(50) UNIQUE NOT NULL,
                            password VARCHAR(100) NOT NULL,
                            role VARCHAR(50) NOT NULL,
                            email VARCHAR(120) NOT NULL DEFAULT '',
                            city VARCHAR(100) NOT NULL DEFAULT 'Yogyakarta'
                        );

                        ALTER TABLE users ADD COLUMN IF NOT EXISTS email VARCHAR(120) NOT NULL DEFAULT '';
                        ALTER TABLE users ADD COLUMN IF NOT EXISTS city VARCHAR(100) NOT NULL DEFAULT 'Yogyakarta';

                        CREATE TABLE IF NOT EXISTS emission_logs (
                            id SERIAL PRIMARY KEY,
                            username VARCHAR(50) NOT NULL,
                            energy_type VARCHAR(20) NOT NULL,
                            log_date VARCHAR(30) NOT NULL,
                            amount NUMERIC(10,2) NOT NULL,
                            co2_emission NUMERIC(10,2) NOT NULL
                        );
                    ";
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = ddlPg;
                    cmd.ExecuteNonQuery();
                }
                else
                {
                    string ddlSqlite = @"
                        CREATE TABLE IF NOT EXISTS users (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            username TEXT UNIQUE NOT NULL,
                            password TEXT NOT NULL,
                            role TEXT NOT NULL,
                            email TEXT NOT NULL DEFAULT '',
                            city TEXT NOT NULL DEFAULT 'Yogyakarta'
                        );

                        CREATE TABLE IF NOT EXISTS emission_logs (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,
                            username TEXT NOT NULL,
                            energy_type TEXT NOT NULL,
                            log_date TEXT NOT NULL,
                            amount REAL NOT NULL,
                            co2_emission REAL NOT NULL
                        );
                    ";
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = ddlSqlite;
                    cmd.ExecuteNonQuery();

                    EnsureSqliteColumn(conn, "users", "email", "TEXT NOT NULL DEFAULT ''");
                    EnsureSqliteColumn(conn, "users", "city", "TEXT NOT NULL DEFAULT 'Yogyakarta'");
                }

                SeedInitialDataIfEmpty(conn);
                return (true, $"Inisialisasi database {ActiveConfig.Provider} berhasil.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Database Init Error]: {ex.Message}");
                return (false, $"Gagal inisialisasi database ({ActiveConfig.Provider}): {ex.Message}");
            }
        }

        private void EnsurePostgreSqlDatabaseExists()
        {
            string targetDb = string.IsNullOrWhiteSpace(ActiveConfig.PostgreSql.Database)
                ? "carbonair_db"
                : ActiveConfig.PostgreSql.Database.Trim();

            using var maintConn = new NpgsqlConnection(BuildPostgreSqlConnectionString("postgres"));
            maintConn.Open();

            using var checkCmd = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @db", maintConn);
            checkCmd.Parameters.AddWithValue("db", targetDb);
            object? exists = checkCmd.ExecuteScalar();

            if (exists == null)
            {
                string safeIdent = targetDb.Replace("\"", "\"\"");
                using var createCmd = new NpgsqlCommand($"CREATE DATABASE \"{safeIdent}\"", maintConn);
                createCmd.ExecuteNonQuery();
            }
        }

        private static void EnsureSqliteColumn(DbConnection conn, string table, string column, string definition)
        {
            bool found = false;
            using (var pragmaCmd = conn.CreateCommand())
            {
                pragmaCmd.CommandText = $"PRAGMA table_info({table});";
                using var reader = pragmaCmd.ExecuteReader();
                while (reader.Read())
                {
                    if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                using var alterCmd = conn.CreateCommand();
                alterCmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition};";
                alterCmd.ExecuteNonQuery();
            }
        }

        private static void SeedInitialDataIfEmpty(DbConnection conn)
        {
            using var countCmd = conn.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM users";
            long userCount = Convert.ToInt64(countCmd.ExecuteScalar(), CultureInfo.InvariantCulture);

            if (userCount > 0)
            {
                return;
            }

            InsertSeedUser(conn, "user1", "user1", "Pengguna Utama", "user1@carbonair.local", "Yogyakarta");
            InsertSeedUser(conn, "user2", "user2", "Pengguna Biasa", "user2@carbonair.local", "Jakarta");

            string today = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string yesterday = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string twoDaysAgo = DateTime.Today.AddDays(-2).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            InsertSeedLog(conn, "user1", "Listrik", twoDaysAgo, 24.0, 20.4);
            InsertSeedLog(conn, "user1", "BBM", yesterday, 9.0, 20.7);
            InsertSeedLog(conn, "user2", "Listrik", today, 15.0, 12.75);
        }

        private static void InsertSeedUser(DbConnection conn, string username, string password, string role, string email, string city)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO users (username, password, role, email, city) VALUES (@u, @p, @r, @e, @c)";
            AddParam(cmd, "u", username);
            AddParam(cmd, "p", password);
            AddParam(cmd, "r", role);
            AddParam(cmd, "e", email);
            AddParam(cmd, "c", city);
            cmd.ExecuteNonQuery();
        }

        private static void InsertSeedLog(DbConnection conn, string username, string energyType, string logDate, double amount, double co2)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO emission_logs (username, energy_type, log_date, amount, co2_emission) VALUES (@u, @t, @d, @a, @c)";
            AddParam(cmd, "u", username);
            AddParam(cmd, "t", energyType);
            AddParam(cmd, "d", logDate);
            AddParam(cmd, "a", (decimal)amount);
            AddParam(cmd, "c", (decimal)co2);
            cmd.ExecuteNonQuery();
        }

        public UserDbRecord? GetUserFromDb(string username, string password)
        {
            using var conn = CreateConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT username, role, email, city FROM users WHERE username = @u AND password = @p LIMIT 1";
            AddParam(cmd, "u", username.Trim());
            AddParam(cmd, "p", password.Trim());

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new UserDbRecord(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    reader.IsDBNull(3) || string.IsNullOrWhiteSpace(reader.GetString(3)) ? "Yogyakarta" : reader.GetString(3)
                );
            }

            return null;
        }

        public UserDbRecord? GetUserProfile(string username)
        {
            using var conn = CreateConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT username, role, email, city FROM users WHERE username = @u LIMIT 1";
            AddParam(cmd, "u", username.Trim());

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new UserDbRecord(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    reader.IsDBNull(3) || string.IsNullOrWhiteSpace(reader.GetString(3)) ? "Yogyakarta" : reader.GetString(3)
                );
            }

            return null;
        }

        public bool UserExists(string username)
        {
            using var conn = CreateConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM users WHERE LOWER(username) = LOWER(@u)";
            AddParam(cmd, "u", username.Trim());
            long count = Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
            return count > 0;
        }

        public bool RegisterUser(string username, string password, string role, string email, string city)
        {
            if (UserExists(username))
            {
                return false;
            }

            using var conn = CreateConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO users (username, password, role, email, city) VALUES (@u, @p, @r, @e, @c)";
            AddParam(cmd, "u", username.Trim());
            AddParam(cmd, "p", password.Trim());
            AddParam(cmd, "r", string.IsNullOrWhiteSpace(role) ? "Pengguna Biasa" : role.Trim());
            AddParam(cmd, "e", email.Trim());
            AddParam(cmd, "c", string.IsNullOrWhiteSpace(city) ? "Yogyakarta" : city.Trim());

            return cmd.ExecuteNonQuery() > 0;
        }

        public bool UpdateUserProfile(string username, string email, string city)
        {
            using var conn = CreateConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE users SET email = @e, city = @c WHERE username = @u";
            AddParam(cmd, "e", email.Trim());
            AddParam(cmd, "c", string.IsNullOrWhiteSpace(city) ? "Yogyakarta" : city.Trim());
            AddParam(cmd, "u", username.Trim());

            return cmd.ExecuteNonQuery() > 0;
        }

        public bool UpdateUserPassword(string username, string oldPassword, string newPassword)
        {
            using var conn = CreateConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE users SET password = @np WHERE username = @u AND password = @op";
            AddParam(cmd, "np", newPassword.Trim());
            AddParam(cmd, "u", username.Trim());
            AddParam(cmd, "op", oldPassword.Trim());

            return cmd.ExecuteNonQuery() > 0;
        }

        public List<string> GetAllUsernames()
        {
            var usernames = new List<string>();
            using var conn = CreateConnection();
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT username FROM users ORDER BY id ASC";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                usernames.Add(reader.GetString(0));
            }
            return usernames;
        }

        public List<EmissionRecord> GetEmissionLogsFromDb(string username, string? energyType = null)
        {
            var logs = new List<EmissionRecord>();
            try
            {
                using var conn = CreateConnection();
                conn.Open();

                using var cmd = conn.CreateCommand();
                bool filterEnergy = !string.IsNullOrWhiteSpace(energyType) &&
                                    !string.Equals(energyType, "Semua", StringComparison.OrdinalIgnoreCase);

                cmd.CommandText = filterEnergy
                    ? "SELECT id, username, energy_type, log_date, amount, co2_emission FROM emission_logs WHERE username = @u AND LOWER(energy_type) = LOWER(@t) ORDER BY log_date DESC, id DESC"
                    : "SELECT id, username, energy_type, log_date, amount, co2_emission FROM emission_logs WHERE username = @u ORDER BY log_date DESC, id DESC";

                AddParam(cmd, "u", username.Trim());
                if (filterEnergy)
                {
                    AddParam(cmd, "t", energyType!.Trim());
                }

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    logs.Add(new EmissionRecord
                    {
                        Id = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        EnergyType = reader.GetString(2),
                        Date = reader.GetString(3),
                        Amount = Convert.ToDouble(reader.GetValue(4), CultureInfo.InvariantCulture),
                        Co2 = Convert.ToDouble(reader.GetValue(5), CultureInfo.InvariantCulture)
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetEmissionLogs Error]: {ex.Message}");
            }
            return logs;
        }

        public int InsertEmissionLogToDb(EmissionRecord record)
        {
            try
            {
                using var conn = CreateConnection();
                conn.Open();

                using var cmd = conn.CreateCommand();
                if (IsPostgreSql)
                {
                    cmd.CommandText = @"INSERT INTO emission_logs (username, energy_type, log_date, amount, co2_emission)
                                        VALUES (@u, @t, @d, @a, @c) RETURNING id;";
                }
                else
                {
                    cmd.CommandText = @"INSERT INTO emission_logs (username, energy_type, log_date, amount, co2_emission)
                                        VALUES (@u, @t, @d, @a, @c);
                                        SELECT last_insert_rowid();";
                }

                AddParam(cmd, "u", record.Username);
                AddParam(cmd, "t", record.EnergyType);
                AddParam(cmd, "d", record.Date);
                AddParam(cmd, "a", (decimal)record.Amount);
                AddParam(cmd, "c", (decimal)record.Co2);

                object? idObj = cmd.ExecuteScalar();
                int newId = Convert.ToInt32(idObj, CultureInfo.InvariantCulture);
                record.Id = newId;
                return newId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InsertEmissionLog Error]: {ex.Message}");
                return 0;
            }
        }

        public bool UpdateEmissionLogInDb(EmissionRecord record)
        {
            try
            {
                using var conn = CreateConnection();
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"UPDATE emission_logs
                                    SET energy_type = @t, log_date = @d, amount = @a, co2_emission = @c
                                    WHERE id = @id AND username = @u;";
                AddParam(cmd, "t", record.EnergyType);
                AddParam(cmd, "d", record.Date);
                AddParam(cmd, "a", (decimal)record.Amount);
                AddParam(cmd, "c", (decimal)record.Co2);
                AddParam(cmd, "id", record.Id);
                AddParam(cmd, "u", record.Username);

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateEmissionLog Error]: {ex.Message}");
                return false;
            }
        }

        public bool DeleteEmissionLogFromDb(int id, string username)
        {
            try
            {
                using var conn = CreateConnection();
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM emission_logs WHERE id = @id AND username = @u;";
                AddParam(cmd, "id", id);
                AddParam(cmd, "u", username.Trim());

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DeleteEmissionLog Error]: {ex.Message}");
                return false;
            }
        }

        private static void AddParam(DbCommand cmd, string name, object value)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = name;
            param.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(param);
        }
    }
}
