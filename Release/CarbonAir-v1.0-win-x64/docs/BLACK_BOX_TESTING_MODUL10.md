# LAB 10.2 : FUNCTIONAL TESTING (BLACK BOX TESTING)

**Nama Aplikasi:** CarbonAir MVP v1.0  
**Tanggal Pengujian:** 2026-11-22  
**Tester:** Kelompok AB to Z (Arnold Gavrael Bonardo Situmorang, Zakhrova Salsabila, Johannes De Deo Dimas Aryobimo)

---

## A. Halaman Autentikasi, Registrasi & Konfigurasi Database

| No. | Halaman Yang Diuji | Aksi Aktor | Reaksi Sistem — Benar | Reaksi Sistem — Salah | Hasil |
|---|---|---|---|---|---|
| 1. | Halaman Login | Memasukkan username (`user1`) dan password (`user1`) yang valid lalu menekan tombol **Login**. | Menampilkan pesan autentikasi polimorfis Administrator dan membuka halaman **DashboardWindow** sesuai akun `user1`. | Menampilkan pesan error `"Username atau Password salah!"` atau gagal terhubung ke database. | Sesuai ( ✓ )<br>Tidak Sesuai (   ) |
| 2. | Halaman Login (Validasi Kosong & Kredensial Salah) | Mengosongkan input atau memasukkan password salah lalu klik **Login**. | Menampilkan peringatan input wajib diisi atau pesan `"Username atau Password salah!"` tanpa membuka Dashboard. | Aplikasi masuk ke Dashboard atau mengalami *crash*. | Sesuai ( ✓ )<br>Tidak Sesuai (   ) |
| 3. | Halaman Registrasi | Klik **"Daftar di sini"**, mengisi username baru, email, kota default, password & konfirmasi cocok, lalu klik **Daftar Akun**. | Akun baru tersimpan ke database dengan role `Pengguna Biasa`, muncul notifikasi sukses, dan kembali ke form Login. | Gagal menyimpan akun atau tidak menolak username duplikat / password tidak cocok. | Sesuai ( ✓ )<br>Tidak Sesuai (   ) |
| 4. | Konfigurasi Database (`DbConfigWindow`) | Klik **Konfigurasi DB**, memilih provider (`SQLite` atau `PostgreSQL`), lalu klik **Tes Koneksi** dan **Simpan & Terapkan**. | Menampilkan status koneksi berhasil/gagal yang akurat, menyimpan ke `appsettings.json`, dan menginisialisasi tabel DB. | Konfigurasi tidak tersimpan atau aplikasi gagal membaca status DB aktif. | Sesuai ( ✓ )<br>Tidak Sesuai (   ) |

---

## B. Halaman Dashboard & Integrasi Web REST API

| No. | Halaman Yang Diuji | Aksi Aktor | Reaksi Sistem — Benar | Reaksi Sistem — Salah | Hasil |
|---|---|---|---|---|---|
| 5. | Dashboard — Muat Data API Awal | Masuk ke Dashboard setelah Login berhasil. | Memanggil Open-Meteo Air Quality & Weather REST API; menampilkan AQI, polutan utama, suhu, angin, status polusi, grafik 24 jam, detail 5 polutan, serta badge `Live API`. | Data KPI tetap kosong/N/A atau aplikasi membeku/crash saat memuat API. | Sesuai ( ✓ )<br>Tidak Sesuai (   ) |
| 6. | Dashboard — Ganti Kota Preset & Pencarian Kota | Memilih kota pada *dropdown* preset (mis. `Jakarta`) atau mengetik kota (mis. `Bandung`) lalu klik **Cari Kota**. | Memanggil Geocoding/Air Quality API, memperbarui seluruh kartu KPI, grafik tren 24 jam, detail polutan, dan rekomendasi cerdas sesuai kota dipilih. | Data tidak berubah atau pesan pencarian tidak muncul saat kota tidak ditemukan. | Sesuai ( ✓ )<br>Tidak Sesuai (   ) |
| 7. | Dashboard — Refresh API & Fallback Offline | Klik **Refresh API** saat online maupun ketika koneksi internet terputus (*offline*). | Saat online data diperbarui; saat offline menampilkan badge `Offline Mode` dan data fallback tanpa menghentikan aplikasi. | Aplikasi crash / unhandled exception saat koneksi API terputus. | Sesuai ( ✓ )<br>Tidak Sesuai (   ) |

---

## C. Halaman Peta Kualitas Udara Indonesia (Peta AQI)

| No. | Halaman Yang Diuji | Aksi Aktor | Reaksi Sistem — Benar | Reaksi Sistem — Salah | Hasil |
|---|---|---|---|---|---|
| 8. | Peta AQI — Tampilan 12 Stasiun Batch API | Membuka tab **Peta AQI** atau menekan **Muat Ulang Peta**. | Menampilkan peta Indonesia bersih beserta 12 pin stasiun berkode warna US EPA dan badge `12 / 12 Online (Open-Meteo API)`. | Pin tidak muncul, menumpuk di satu koordinat, atau menampilkan placeholder `N/A`. | Sesuai ( ✓ )<br>Tidak Sesuai (   ) |
| 9. | Peta AQI — Interaksi Pin & Pantau di Dashboard | Mengklik salah satu pin kota di peta lalu menekan tombol **Pantau di Dashboard**. | Kartu detail menampilkan AQI, provinsi, PM2.5, PM10, cuaca, dan saran kesehatan stasiun tersebut; tombol membawa pengguna ke tab Dashboard untuk kota terpilih. | Kartu detail tidak berubah saat pin diklik atau navigasi gagal berpindah kota. | Sesuai ( ✓ )<br>Tidak Sesuai (   ) |

---

## D. Halaman Log Energi & Emisi Karbon (CRUD, Filter, & Ekspor)

| No. | Halaman Yang Diuji | Aksi Aktor | Reaksi Sistem — Benar | Reaksi Sistem — Salah | Hasil |
|---|---|---|---|---|---|
| 10. | Log Energi — Input Emisi Polimorfis (`Listrik` / `BBM`) | Memilih jenis energi, mengisi jumlah valid (mis. `15`), mengisi tanggal `YYYY-MM-DD`, lalu klik **+ Catat Jejak Karbon**. | Menghitung CO₂ sesuai kalkulator polimorfis (`0.85` untuk Listrik, `2.30` untuk BBM), menyimpan ke database, memperbarui daftar & total emisi Dashboard. | Perhitungan salah, data tidak masuk ke database, atau menerima input angka ≤ 0 / format tanggal salah. | Sesuai ( ✓ )<br>Tidak Sesuai (   ) |
| 11. | Log Energi — Edit & Hapus Catatan Emisi | Klik **Edit** pada kartu catatan, ubah jumlah lalu **Simpan Perubahan**; kemudian uji klik **Hapus** dan konfirmasi. | Perubahan data dan nilai CO₂ baru tersimpan di database; pada aksi Hapus catatan terhapus dari DB dan total emisi berkurang otomatis. | Catatan tidak berubah/terhapus di database atau tombol Edit/Hapus tidak merespons. | Sesuai ( ✓ )<br>Tidak Sesuai (   ) |
| 12. | Log Energi — Filter Jenis & Ekspor CSV | Mengklik filter `Semua`, `Listrik`, `BBM`, lalu menekan tombol **Ekspor CSV** dan memilih lokasi simpan. | Daftar catatan tersaring sesuai jenis energi yang dipilih dan file `.csv` berisi data emisi berhasil diekspor dengan format kolom lengkap. | Filter menampilkan jenis yang salah atau file CSV gagal dibuat/kosong. | Sesuai ( ✓ )<br>Tidak Sesuai (   ) |
| 13. | Log Energi — Kontrol Peran (Admin vs Regular) | Login sebagai `user1` (Admin) vs `user2` (Regular) lalu membuka tab **Log Energi**. | `user1` dapat mengganti target user melalui *dropdown*; `user2` hanya melihat badge namanya sendiri tanpa akses mengubah log user lain. | `user2` dapat mengubah/menghapus catatan milik `user1`. | Sesuai ( ✓ )<br>Tidak Sesuai (   ) |

---

## E. Halaman Pengaturan, Notifikasi Desktop, & Logout

| No. | Halaman Yang Diuji | Aksi Aktor | Reaksi Sistem — Benar | Reaksi Sistem — Salah | Hasil |
|---|---|---|---|---|---|
| 14. | Pengaturan — Simpan Profil, Satuan, & Auto-Refresh | Mengubah email, kota default, satuan (`kg CO₂` / `ton CO₂`), interval *auto-refresh*, ambang AQI, lalu klik **Simpan Pengaturan**. | Data profil tersimpan di DB, preferensi tersimpan di `appsettings.json`, dan satuan emisi langsung berubah pada Dashboard serta Log Energi. | Perubahan hilang setelah disimpan atau tampilan angka emisi tidak berubah. | Sesuai ( ✓ )<br>Tidak Sesuai (   ) |
| 15. | Pengaturan — Ubah Password & Tes Koneksi API | Mengisi password lama/baru lalu klik **Ubah Password**, serta mengklik **Tes API**. | Password berhasil diperbarui di database jika password lama cocok; **Tes API** menampilkan status konektivitas dan latensi Open-Meteo dalam ms. | Mengizinkan ubah password dengan password lama yang salah atau **Tes API** tidak menampilkan hasil. | Sesuai ( ✓ )<br>Tidak Sesuai (   ) |
| 16. | Notifikasi Bahaya Polusi Desktop | Mengklik **Uji Notifikasi** di Pengaturan atau memantau kota dengan `AQI ≥ ambang batas`. | Jendela *toast* notifikasi muncul di pojok kanan bawah layar berisi nama kota, AQI, kategori, saran kesehatan, dan menutup otomatis setelah 8 detik. | Notifikasi tidak tampil atau memblokir interaksi jendela utama. | Sesuai ( ✓ )<br>Tidak Sesuai (   ) |
| 17. | Logout | Mengklik tombol **Keluar** di sidebar kiri bawah. | Timer latar belakang berhenti, jendela Dashboard tertutup, dan aplikasi kembali ke halaman **MainWindow** (Login). | Tidak keluar ke halaman Login atau jendela Dashboard tetap tertinggal. | Sesuai ( ✓ )<br>Tidak Sesuai (   ) |

---

## Ringkasan Pengujian Otomatis (xUnit — White Box & Integration)

Selain pengujian fungsional *Black Box* di atas, proyek ini juga dilengkapi dengan **19 skenario pengujian otomatis (`CarbonAir.Tests`)** yang dijalankan melalui `dotnet test CarbonAir.sln -c Release`:
- **Unit Test Model & Polimorfisme (`ModelsAndCalculatorTests`):** Kalkulator Listrik/BBM, Hierarki `AdminAccount`/`RegularAccount`, Enkapsulasi `EmissionRecord`, Klasifikasi 6 tingkat US EPA `AirQualityData`, serta validasi 12 koordinat kota preset Indonesia.
- **Integration Test Database & Layanan (`DatabaseAndServicesTests`):** Inisialisasi skema SQLite terisolasi, *seeding* idempoten, Login/Registrasi/Profil/Password, CRUD Log Emisi, Ekspor CSV, Rekomendasi Cerdas, dan persistensi `appsettings.json`.
- **Integration Test Web REST API (`AirQualityApiServiceTests`):** Pengambilan data real-time Open-Meteo Yogyakarta, batch 12 kota Indonesia, Geocoding pencarian kota `Bandung`, uji latensi koneksi, dan simulasi *fallback offline*.
- **Hasil Eksekusi:** **19 / 19 Lulus (0 Gagal).**
