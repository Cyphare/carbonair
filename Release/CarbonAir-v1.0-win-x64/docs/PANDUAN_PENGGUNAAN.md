# Panduan Penggunaan & Konfigurasi Database — CarbonAir MVP v1.0

**Kelompok AB to Z**
- **Ketua Kelompok:** Arnold Gavrael Bonardo Situmorang (24/539797/TK/59872)
- **Anggota 1:** Zakhrova Salsabila (24/534625/TK/59268)
- **Anggota 2:** Johannes De Deo Dimas Aryobimo (24/540351/TK/59948)

---

## 1. Ringkasan Aplikasi & Nilai Tambah Web REST API

**CarbonAir** adalah aplikasi desktop berbasis **Windows Presentation Foundation (WPF .NET 10)** yang memantau kualitas udara (AQI) secara *real-time*, menyediakan peta kualitas udara interaktif di Indonesia, menghitung serta mencatat jejak emisi karbon harian pengguna (Listrik & BBM) ke basis data, dan memberikan notifikasi peringatan bahaya polusi di desktop.

### Fitur Utama & Integrasi Web REST API
Aplikasi ini terintegrasi dengan **Open-Meteo Web REST API** (format JSON melalui HTTPS, tanpa API key berbayar) sebagai layanan pihak ketiga:
1. **Open-Meteo Air Quality API** (`https://air-quality-api.open-meteo.com/v1/air-quality`)
   - Mengambil nilai **US AQI**, konsentrasi polutan (`PM2.5`, `PM10`, `CO`, `NO₂`, `O₃`), serta tren **AQI per jam selama 24 jam terakhir** untuk grafik interaktif Dashboard.
   - Mengambil data **12 stasiun pemantau kota besar di Indonesia secara batch** (Medan, Palembang, Jakarta, Bandung, Semarang, Yogyakarta, Surabaya, Denpasar, Balikpapan, Makassar, Manado, Jayapura) untuk Peta AQI Interaktif.
2. **Open-Meteo Weather API** (`https://api.open-meteo.com/v1/forecast`)
   - Mengambil data **suhu udara (°C)**, **kecepatan angin (km/h)**, dan **kelembapan relatif (%)** secara bersamaan dengan data kualitas udara.
3. **Open-Meteo Geocoding API** (`https://geocoding-api.open-meteo.com/v1/search`)
   - Memungkinkan pengguna mencari kota mana pun (contoh: `Malang`, `Solo`, `Bogor`) untuk dipantau secara langsung di Dashboard.
4. **Mode Offline Cerdas (*Graceful Fallback*)**
   - Apabila perangkat tidak terhubung ke internet, aplikasi tidak mengalami *crash* dan otomatis beralih ke `Offline Mode` sambil tetap menjalankan pencatatan emisi dan database secara normal.

---

## 2. Cara Instalasi & Menjalankan Aplikasi

Paket rilis **CarbonAir MVP v1.0** tersedia dalam bentuk *self-contained single-file executable* (`CarbonAir.exe`) sehingga pengguna **tidak perlu menginstal .NET Runtime atau database server terpisah**.

### Langkah Instalasi Cepat:
1. Ekstrak arsip `CarbonAir-v1.0-win-x64.zip` ke folder tujuan pilihan Anda (misalnya `D:\CarbonAir\`).
2. Pastikan di dalam folder terdapat `CarbonAir.exe`, `appsettings.json`, `setup.bat`, folder `database/`, dan folder `docs/`.
3. **Jalankan langsung:** Klik dua kali **`CarbonAir.exe`** (atau jalankan **`setup.bat`** lalu pilih menu `[1] Jalankan CarbonAir`).
4. Secara bawaan (*default*), aplikasi langsung menggunakan **SQLite lokal (`carbonair.db`)** yang otomatis dibuat dan diisi data awal saat pertama kali dijalankan.

---

## 3. Panduan Konfigurasi Basis Data (Dual-Mode: SQLite & PostgreSQL)

CarbonAir mendukung **dua mode basis data** yang dapat diganti langsung dari tampilan aplikasi (tanpa mengubah kode program):

### Opsi A — Mode SQLite (Default / Zero-Config)
Cocok untuk penggunaan instan, presentasi, dan pengujian mandiri tanpa instalasi server:
- Tidak memerlukan instalasi PostgreSQL atau service tambahan.
- File basis data `carbonair.db` dibuat otomatis di direktori yang sama dengan `CarbonAir.exe`.
- Tabel `users` dan `emission_logs` beserta 2 akun demo (`user1`, `user2`) dan data sampel emisi langsung tersedia otomatis.

### Opsi B — Mode PostgreSQL (Server)
Cocok apabila ingin menghubungkan aplikasi ke server PostgreSQL:
1. Pastikan layanan **PostgreSQL Server** sudah berjalan di komputer lokal atau jaringan.
2. Buka **`CarbonAir.exe`**, lalu di halaman Login klik tombol **`Konfigurasi DB`** (atau di dalam menu **Pengaturan → Atur Database...**), atau jalankan `setup.bat` pilihan `[2]`.
3. Ubah **Mode Penyimpanan Database** menjadi **`PostgreSQL (Server)`**, lalu isi parameter berikut:
   - **Host Server:** `127.0.0.1` (atau IP server)
   - **Port:** `5432`
   - **Nama Database:** `carbonair_db` *(dibuat otomatis oleh aplikasi jika belum ada!)*
   - **Username:** `postgres`
   - **Password:** password akun PostgreSQL Anda
4. Klik **`Tes Koneksi`** untuk memverifikasi koneksi, lalu klik **`Simpan & Terapkan`**.
5. *(Opsional)* Jika ingin menginisialisasi skema secara manual lewat `psql` / pgAdmin, jalankan skrip SQL di `database/schema_postgresql.sql`.

### Contoh Struktur `appsettings.json`
```json
{
  "Database": {
    "Provider": "SQLite",
    "SqlitePath": "carbonair.db",
    "PostgreSql": {
      "Host": "127.0.0.1",
      "Port": 5432,
      "Database": "carbonair_db",
      "Username": "postgres",
      "Password": ""
    }
  },
  "Api": {
    "DefaultCity": "Yogyakarta",
    "AutoRefreshMinutes": 15,
    "AqiAlertThreshold": 100,
    "EnablePollutionAlert": true
  },
  "Display": {
    "EmissionUnit": "kg"
  }
}
```

---

## 4. Akun Demo Bawaan

| Username | Password | Peran (*Role*) | Hak Akses |
|---|---|---|---|
| `user1` | `user1` | Pengguna Utama (Admin) | Melihat seluruh menu, serta mengelola data log emisi semua pengguna (*user switcher*). |
| `user2` | `user2` | Pengguna Biasa (Regular) | Melihat seluruh menu pemantauan & mengelola log emisi milik akun sendiri. |

> Anda juga dapat membuat akun baru secara mandiri melalui tautan **"Belum punya akun? Daftar di sini"** di halaman Login.

---

## 5. Panduan Penggunaan Setiap Halaman

### A. Halaman Login, Registrasi & Konfigurasi DB
1. Masukkan **Username** dan **Password**, lalu klik **Login** (atau tekan `Enter`).
2. Untuk mendaftar pengguna baru, klik **"Daftar di sini"**, isi Username, Email, Kota Default, dan Password, lalu klik **Daftar Akun**.
3. Klik **`Konfigurasi DB`** di pojok kanan bawah kartu untuk mengganti mode SQLite/PostgreSQL dan menguji koneksi.

### B. Halaman Dashboard
1. **Pilih Kota / Cari Kota:** Gunakan *dropdown* kota preset Indonesia atau ketik nama kota lain di kotak pencarian lalu klik **`Cari Kota`**.
2. **Refresh API:** Klik **`Refresh API`** untuk mengambil data AQI, polutan, cuaca, dan grafik 24 jam terbaru.
3. **Indikator & Grafik:** Pantau **AQI Saat Ini**, **Total Emisi Anda**, **Kecepatan Angin & Cuaca**, **Status Polusi**, **Grafik Tren 24 Jam**, **Detail Konsentrasi Polutan (`PM2.5`, `PM10`, `CO`, `NO₂`, `O₃`)**, dan **Rekomendasi Cerdas CarbonAir**.
4. **Peringatan Polusi:** Jika AQI melebihi ambang batas (default `100`), muncul *banner* peringatan di Dashboard dan jendela notifikasi *toast* di pojok kanan bawah layar.

### C. Halaman Peta AQI Indonesia
1. Menampilkan 12 titik stasiun kota di peta Indonesia dengan warna sesuai standar US EPA (Hijau = Baik, Kuning = Sedang, Oranye = Sensitif, Merah = Tidak Sehat, Ungu = Sangat Tidak Sehat, Merah Tua = Berbahaya).
2. **Klik salah satu titik kota** pada peta untuk melihat detail lengkap AQI, PM2.5, PM10, suhu, angin, kelembapan, dan saran kesehatan di panel bawah.
3. Klik **`Pantau di Dashboard`** untuk langsung memantau kota tersebut di tab Dashboard.

### D. Halaman Log Energi & Emisi
1. **Catat Emisi Baru:** Pilih jenis energi (**Listrik** faktor `0.85 kg CO₂/kWh` atau **BBM** faktor `2.30 kg CO₂/L`), masukkan jumlah konsumsi (estimasi CO₂ tampil otomatis), pastikan tanggal berformat `YYYY-MM-DD`, lalu klik **`+ Catat Jejak Karbon`**.
2. **Ubah & Hapus Catatan:** Pada setiap kartu riwayat emisi, klik **`Edit`** untuk memperbarui data atau klik **`Hapus`** untuk menghapus catatan dari basis data.
3. **Filter & Ekspor CSV:** Gunakan tombol filter **`Semua`**, **`Listrik`**, atau **`BBM`** untuk menyaring riwayat, lalu klik **`Ekspor CSV`** untuk menyimpan laporan ke file `.csv`.
4. **Mode Admin (`user1`):** Dapat memilih pengguna lain dari *dropdown* **"Kelola data user"** di kanan atas.

### E. Halaman Pengaturan
1. **Profil Pengguna & Keamanan:** Perbarui **Email**, **Kota Pemantauan Default**, serta **Ubah Password**.
2. **Preferensi Sistem, Web API & Notifikasi:**
   - Ubah satuan emisi (`kg CO₂` atau `ton CO₂`).
   - Klik **`Tes API`** untuk menguji latensi dan status REST API Open-Meteo.
   - Atur interval *Auto-Refresh* (`Manual`, `5`, `15`, atau `30 Menit`).
   - Aktifkan/nonaktifkan notifikasi desktop, atur ambang batas AQI, atau klik **`Uji Notifikasi`**.
3. **Database & Simpan:** Klik **`Atur Database...`** untuk mengganti DB kapan saja, dan klik **`Simpan Pengaturan`** untuk menyimpan perubahan ke `appsettings.json` dan basis data.
