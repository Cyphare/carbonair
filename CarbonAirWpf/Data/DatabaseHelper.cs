using System;
using System.Collections.Generic;
using Npgsql;
using CarbonAirWpf.Models;

namespace CarbonAirWpf.Data
{
    // PostgreSQL
    public class DatabaseHelper
    {
        private const string ConnectionString = 
            "Host=127.0.0.1;Port=5432;Database=carbonair_db;Username=postgres;Password=B!mo110306";

        // PostgreSQL: DDL
        public static void InitializeDatabase()
        {
            try
            {
                using var conn = new NpgsqlConnection(ConnectionString);
                conn.Open();

                string ddl = @"
                    CREATE TABLE IF NOT EXISTS users (
                        id SERIAL PRIMARY KEY,
                        username VARCHAR(50) UNIQUE NOT NULL,
                        password VARCHAR(50) NOT NULL,
                        role VARCHAR(50) NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS emission_logs (
                        id SERIAL PRIMARY KEY,
                        username VARCHAR(50) NOT NULL,
                        energy_type VARCHAR(20) NOT NULL,
                        log_date VARCHAR(30) NOT NULL,
                        amount NUMERIC(10,2) NOT NULL,
                        co2_emission NUMERIC(10,2) NOT NULL
                    );

                    INSERT INTO users (username, password, role) VALUES 
                    ('user1', 'user1', 'Pengguna Utama'),
                    ('user2', 'user2', 'Pengguna Biasa')
                    ON CONFLICT (username) DO UPDATE SET password = EXCLUDED.password, role = EXCLUDED.role;
                ";

                using var cmd = new NpgsqlCommand(ddl, conn);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Database Init Error]: {ex.Message}");
            }
        }

        // PostgreSQL: Query Login
        public string? GetUserRoleFromDb(string username, string password)
        {
            try
            {
                using var conn = new NpgsqlConnection(ConnectionString);
                conn.Open();

                string query = "SELECT role FROM users WHERE username = @u AND password = @p LIMIT 1";
                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("u", username.Trim());
                cmd.Parameters.AddWithValue("p", password.Trim());

                object? result = cmd.ExecuteScalar();
                return result?.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Gagal terhubung ke database PostgreSQL: {ex.Message}", ex);
            }
        }

        // PostgreSQL: Query Select Logs
        public List<EmissionRecord> GetEmissionLogsFromDb(string username)
        {
            var logs = new List<EmissionRecord>();
            try
            {
                using var conn = new NpgsqlConnection(ConnectionString);
                conn.Open();

                string query = "SELECT id, username, energy_type, log_date, amount, co2_emission FROM emission_logs WHERE username = @u ORDER BY id DESC";
                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("u", username.Trim());

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    logs.Add(new EmissionRecord
                    {
                        Id = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        EnergyType = reader.GetString(2),
                        Date = reader.GetString(3),
                        Amount = Convert.ToDouble(reader.GetDecimal(4)),
                        Co2 = Convert.ToDouble(reader.GetDecimal(5))
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetEmissionLogs Error]: {ex.Message}");
            }
            return logs;
        }

        // PostgreSQL: Query Insert Log
        public bool InsertEmissionLogToDb(EmissionRecord record)
        {
            try
            {
                using var conn = new NpgsqlConnection(ConnectionString);
                conn.Open();

                string query = @"INSERT INTO emission_logs (username, energy_type, log_date, amount, co2_emission) 
                                 VALUES (@u, @t, @d, @a, @c)";
                using var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("u", record.Username);
                cmd.Parameters.AddWithValue("t", record.EnergyType);
                cmd.Parameters.AddWithValue("d", record.Date);
                cmd.Parameters.AddWithValue("a", (decimal)record.Amount);
                cmd.Parameters.AddWithValue("c", (decimal)record.Co2);

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[InsertEmissionLog Error]: {ex.Message}");
                return false;
            }
        }
    }
}
