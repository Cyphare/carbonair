-- CarbonAir MVP v1.0 - Skema Database SQLite (Referensi)
-- Aplikasi membuat file carbonair.db dan tabel berikut secara otomatis saat dijalankan.

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

INSERT OR IGNORE INTO users (username, password, role, email, city) VALUES
    ('user1', 'user1', 'Pengguna Utama', 'user1@carbonair.local', 'Yogyakarta'),
    ('user2', 'user2', 'Pengguna Biasa', 'user2@carbonair.local', 'Jakarta');
