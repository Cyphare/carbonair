# CARBONAIR (MVP v1.0)

**CarbonAir** adalah aplikasi desktop berbasis WPF (.NET 10) yang menampilkan data kualitas udara secara *real-time* melalui integrasi **Open-Meteo Web REST API**, menyajikan peta stasiun kualitas udara di 12 kota Indonesia, mencatat konsumsi energi harian (Listrik & BBM) menjadi jejak emisi karbon yang tersimpan di database (**SQLite otomatis / PostgreSQL**), serta dilengkapi notifikasi peringatan bahaya polusi di desktop.

## Kelompok AB to Z
- **Ketua Kelompok:** Arnold Gavrael Bonardo Situmorang – 24/539797/TK/59872
- **Anggota 1:** Zakhrova Salsabila – 24/534625/TK/59268
- **Anggota 2:** Johannes De Deo Dimas Aryobimo – 24/540351/TK/59948

---

## Fitur Utama (Finalisasi Modul 10 — MVP)
1. **Integrasi Web REST API (Nilai Tambah):**
   - **Open-Meteo Air Quality API:** US AQI *real-time*, konsentrasi polutan (`PM2.5`, `PM10`, `CO`, `NO₂`, `O₃`), serta tren AQI 24 jam per jam.
   - **Open-Meteo Weather API:** Suhu udara (°C), kecepatan angin (km/h), dan kelembapan relatif (%).
   - **Open-Meteo Geocoding API:** Pencarian kota kustom secara langsung dari Dashboard.
   - **Batch Monitoring 12 Kota Indonesia:** Peta AQI interaktif lengkap dengan detail stasiun dan tombol *Pantau di Dashboard*.
2. **Dual-Mode Database (Zero-Config SQLite & PostgreSQL):**
   - Langsung jalan tanpa setup server melalui **SQLite lokal (`carbonair.db`)**, atau beralih ke **PostgreSQL Server** via UI `Konfigurasi DB` / `appsettings.json` / `setup.bat`.
3. **Autentikasi, Registrasi & Kontrol Peran (OOP):**
   - `AdminAccount` (`user1`/`user1`) dan `RegularAccount` (`user2`/`user2`) serta fitur pendaftaran akun baru dan ubah profil/password.
4. **CRUD Log Energi, Kalkulasi Polimorfis & Ekspor CSV:**
   - Kalkulator `ElectricityEmissionCalculator` (`0.85 kg CO₂/kWh`) & `FuelEmissionCalculator` (`2.30 kg CO₂/L`), fitur Tambah/Edit/Hapus, filter `Semua`/`Listrik`/`BBM`, ekspor laporan `.csv`, dan konversi satuan `kg`/`ton CO₂`.
5. **Notifikasi Desktop & Rekomendasi Cerdas:**
   - Jendela peringatan *toast* otomatis saat AQI melebihi ambang batas dan saran pengurangan polusi/emisi kontekstual.

---

## Cara Menjalankan & Build dari Source

### Opsi 1: Langsung Pakai Paket Rilis (`.exe`)
Buka folder `Release/CarbonAir-v1.0-win-x64/` (atau ekstrak `Release/CarbonAir-v1.0-win-x64.zip`) lalu klik dua kali **`CarbonAir.exe`** atau jalankan **`setup.bat`**.

### Opsi 2: Build, Test, dan Publish dari Source (.NET 10 SDK)
```powershell
dotnet build CarbonAir.sln -c Release
dotnet test CarbonAir.sln -c Release
dotnet publish CarbonAirWpf/CarbonAirWpf.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o Release/CarbonAir-v1.0-win-x64
```

---

## Dokumentasi & Pengujian Modul 10
- [Panduan Penggunaan & Konfigurasi Database](docs/PANDUAN_PENGGUNAAN.md)
- [Black Box Testing (Format Lab 10.2)](docs/BLACK_BOX_TESTING_MODUL10.md)
- [Worksheet Printable (Siap Cetak PDF)](docs/WORKSHEET_MODUL10_PRINTABLE.html)
- [Skema SQL PostgreSQL](database/schema_postgresql.sql) | [Skema SQL SQLite](database/schema_sqlite.sql)
