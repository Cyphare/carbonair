-- CarbonAir MVP v1.0 - Skema Database PostgreSQL
-- Jalankan di database target (default: carbonair_db) apabila ingin setup manual.
-- Catatan: Aplikasi CarbonAir juga dapat membuat database dan tabel secara otomatis.

CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    password VARCHAR(100) NOT NULL,
    role VARCHAR(50) NOT NULL,
    email VARCHAR(120) NOT NULL DEFAULT '',
    city VARCHAR(100) NOT NULL DEFAULT 'Yogyakarta'
);

CREATE TABLE IF NOT EXISTS emission_logs (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL,
    energy_type VARCHAR(20) NOT NULL,
    log_date VARCHAR(30) NOT NULL,
    amount NUMERIC(10,2) NOT NULL,
    co2_emission NUMERIC(10,2) NOT NULL
);

INSERT INTO users (username, password, role, email, city) VALUES
    ('user1', 'user1', 'Pengguna Utama', 'user1@carbonair.local', 'Yogyakarta'),
    ('user2', 'user2', 'Pengguna Biasa', 'user2@carbonair.local', 'Jakarta')
ON CONFLICT (username) DO NOTHING;

INSERT INTO emission_logs (username, energy_type, log_date, amount, co2_emission)
SELECT 'user1', 'Listrik', TO_CHAR(CURRENT_DATE - INTERVAL '2 days', 'YYYY-MM-DD'), 24.00, 20.40
WHERE NOT EXISTS (SELECT 1 FROM emission_logs WHERE username = 'user1');

INSERT INTO emission_logs (username, energy_type, log_date, amount, co2_emission)
SELECT 'user1', 'BBM', TO_CHAR(CURRENT_DATE - INTERVAL '1 day', 'YYYY-MM-DD'), 9.00, 20.70
WHERE (SELECT COUNT(*) FROM emission_logs WHERE username = 'user1') < 2;

INSERT INTO emission_logs (username, energy_type, log_date, amount, co2_emission)
SELECT 'user2', 'Listrik', TO_CHAR(CURRENT_DATE, 'YYYY-MM-DD'), 15.00, 12.75
WHERE NOT EXISTS (SELECT 1 FROM emission_logs WHERE username = 'user2');
