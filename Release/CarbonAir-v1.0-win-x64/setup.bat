@echo off
setlocal EnableDelayedExpansion
title CarbonAir MVP v1.0 - Setup & Launcher

set "BASE_DIR=%~dp0"
set "EXE_PATH=%BASE_DIR%CarbonAir.exe"
set "CFG_PATH=%BASE_DIR%appsettings.json"
set "DOC_DIR=%BASE_DIR%docs"

if not exist "%EXE_PATH%" (
    if exist "%BASE_DIR%Release\CarbonAir-v1.0-win-x64\CarbonAir.exe" (
        set "BASE_DIR=%BASE_DIR%Release\CarbonAir-v1.0-win-x64\"
        set "EXE_PATH=!BASE_DIR!CarbonAir.exe"
        set "CFG_PATH=!BASE_DIR!appsettings.json"
        set "DOC_DIR=!BASE_DIR!docs"
    )
)

:menu
cls
echo ============================================================
echo        CARBONAIR MVP v1.0 - SETUP & PELUNCUR APLIKASI
echo               Kelompok AB to Z (Modul 10)
echo ============================================================
echo.
echo   Direktori Aktif : %BASE_DIR%
echo.
echo   [1] Jalankan CarbonAir.exe (Default SQLite Otomatis)
echo   [2] Konfigurasi Database PostgreSQL ke appsettings.json
echo   [3] Kembalikan ke Mode SQLite Lokal (carbonair.db)
echo   [4] Buat Shortcut CarbonAir di Desktop
echo   [5] Buka Worksheet & Panduan Penggunaan (HTML)
echo   [6] Keluar
echo.
set /p "CHOICE=Pilih menu [1-6]: "

if "%CHOICE%"=="1" goto run_app
if "%CHOICE%"=="2" goto config_pg
if "%CHOICE%"=="3" goto config_sqlite
if "%CHOICE%"=="4" goto create_shortcut
if "%CHOICE%"=="5" goto open_docs
if "%CHOICE%"=="6" exit /b 0
goto menu

:run_app
if not exist "%EXE_PATH%" (
    echo [ERROR] CarbonAir.exe tidak ditemukan di %EXE_PATH%.
    pause
    goto menu
)
echo Menjalankan CarbonAir.exe...
start "" "%EXE_PATH%"
goto menu

:config_pg
echo.
echo === Konfigurasi Server PostgreSQL ===
set /p "PG_HOST=Host PostgreSQL [127.0.0.1]: "
if "%PG_HOST%"=="" set "PG_HOST=127.0.0.1"
set /p "PG_PORT=Port PostgreSQL [5432]: "
if "%PG_PORT%"=="" set "PG_PORT=5432"
set /p "PG_DB=Nama Database [carbonair_db]: "
if "%PG_DB%"=="" set "PG_DB=carbonair_db"
set /p "PG_USER=Username PostgreSQL [postgres]: "
if "%PG_USER%"=="" set "PG_USER=postgres"
set /p "PG_PASS=Password PostgreSQL: "

powershell -NoProfile -Command "$cfgPath = '%CFG_PATH%'; if (Test-Path $cfgPath) { $j = Get-Content $cfgPath -Raw | ConvertFrom-Json } else { $j = [pscustomobject]@{ Database = [pscustomobject]@{ Provider='SQLite'; SqlitePath='carbonair.db'; PostgreSql=[pscustomobject]@{} }; Api=[pscustomobject]@{ DefaultCity='Yogyakarta'; AutoRefreshMinutes=15; AqiAlertThreshold=100; EnablePollutionAlert=$true }; Display=[pscustomobject]@{ EmissionUnit='kg' } } }; $j.Database.Provider = 'PostgreSQL'; $j.Database.PostgreSql.Host = '%PG_HOST%'; $j.Database.PostgreSql.Port = [int]'%PG_PORT%'; $j.Database.PostgreSql.Database = '%PG_DB%'; $j.Database.PostgreSql.Username = '%PG_USER%'; $j.Database.PostgreSql.Password = '%PG_PASS%'; $j | ConvertTo-Json -Depth 5 | Set-Content $cfgPath -Encoding UTF8"
echo [OK] Konfigurasi PostgreSQL tersimpan ke %CFG_PATH%.
pause
goto menu

:config_sqlite
powershell -NoProfile -Command "$cfgPath = '%CFG_PATH%'; if (Test-Path $cfgPath) { $j = Get-Content $cfgPath -Raw | ConvertFrom-Json; $j.Database.Provider = 'SQLite'; $j.Database.SqlitePath = 'carbonair.db'; $j | ConvertTo-Json -Depth 5 | Set-Content $cfgPath -Encoding UTF8 }"
echo [OK] Mode database dikembalikan ke SQLite lokal (carbonair.db).
pause
goto menu

:create_shortcut
if not exist "%EXE_PATH%" (
    echo [ERROR] CarbonAir.exe tidak ditemukan.
    pause
    goto menu
)
powershell -NoProfile -Command "$ws = New-Object -ComObject WScript.Shell; $lnk = $ws.CreateShortcut([Environment]::GetFolderPath('Desktop') + '\CarbonAir.lnk'); $lnk.TargetPath = '%EXE_PATH%'; $lnk.WorkingDirectory = '%BASE_DIR%'; $lnk.Description = 'CarbonAir MVP v1.0'; $lnk.Save()"
echo [OK] Shortcut CarbonAir berhasil dibuat di Desktop!
pause
goto menu

:open_docs
if exist "%DOC_DIR%\WORKSHEET_MODUL10_PRINTABLE.html" (
    start "" "%DOC_DIR%\WORKSHEET_MODUL10_PRINTABLE.html"
) else (
    echo [INFO] File worksheet HTML tidak ditemukan di %DOC_DIR%.
    pause
)
goto menu
