@echo off
title SuperKRWKMod - Installer

cd /d "%~dp0"
echo Installing SuperKRWKMod...
echo.

set "SOURCE_DIR=BongoCat"

set "GAME_RELATIVE_DIR=\steamapps\common\BongoCat"
set "GAME_EXE=BongoCat.exe"

set "STEAM_PATH="
set "GAME_DIR="

for /f "tokens=2*" %%A in ('reg query "HKCU\Software\Valve\Steam" /v SteamPath 2^>nul') do set "STEAM_PATH=%%B"
if not defined STEAM_PATH (
    for /f "tokens=2*" %%A in ('reg query "HKLM\SOFTWARE\WOW6432Node\Valve\Steam" /v InstallPath 2^>nul') do set "STEAM_PATH=%%B"
)
if not defined STEAM_PATH (
    if exist "C:\Program Files (x86)\Steam\steam.exe" set "STEAM_PATH=C:\Program Files (x86)\Steam"
)
if not defined STEAM_PATH (
    if exist "C:\Program Files\Steam\steam.exe" set "STEAM_PATH=C:\Program Files\Steam"
)
if defined STEAM_PATH (
    if exist "%STEAM_PATH%%GAME_RELATIVE_DIR%\%GAME_EXE%" (
        set "GAME_DIR=%STEAM_PATH%%GAME_RELATIVE_DIR%"
    )
)

if not defined GAME_DIR (
    if exist "C:\Program Files (x86)\Steam%GAME_RELATIVE_DIR%\%GAME_EXE%" (
        set "GAME_DIR=C:\Program Files (x86)\Steam%GAME_RELATIVE_DIR%"
    )
)

if not defined GAME_DIR (
    echo [ERROR] Bongo Cat game directory not found.
    echo.
    pause
    exit /b 1
)

if exist "%GAME_DIR%\winhttp.dll" (
    tasklist /FI "IMAGENAME eq %GAME_EXE%" /NH 2>NUL | findstr /R "%GAME_EXE%" >NUL
    if not errorlevel 1 (
        echo Bongo Cat is currently running. Closing it to ensure a clean re-installation...
        taskkill /F /IM %GAME_EXE% >NUL 2>&1
        timeout /t 1 /nobreak >NUL
        echo.
    )
)

echo Copying SuperKRWKMod files to: %GAME_DIR%
echo.

set "ERROR=0"

if exist "%SOURCE_DIR%" (
    xcopy /E /I /Y "%SOURCE_DIR%" "%GAME_DIR%" >nul 2>&1
    if errorlevel 1 (
        echo [ERROR] Failed to copy mod files
        set "ERROR=1"
    ) else (
        echo [OK] Mod files copied successfully
    )
    REM Remove the folder of the previous name (Catnip Mod); its settings are migrated on first launch.
    if exist "%GAME_DIR%\CatnipMod\catnip.ini" if not exist "%GAME_DIR%\SuperKRWKMod\superkrwk.ini" (
        copy /Y "%GAME_DIR%\CatnipMod\catnip.ini" "%GAME_DIR%\SuperKRWKMod\superkrwk.ini" >nul 2>&1
    )
    if exist "%GAME_DIR%\CatnipMod" (
        rmdir /S /Q "%GAME_DIR%\CatnipMod" >nul 2>&1
        echo [OK] Removed old CatnipMod folder
    )
) else (
    echo [ERROR] %SOURCE_DIR% folder not found!
    set "ERROR=1"
)

echo.
if %ERROR% equ 0 (
    echo Installation complete.
    echo Launch Bongo Cat through Steam to verify the mod is working.
    echo.
    echo        /\_/\
    echo       ^(o ^. o^)
    
    start "" "%GAME_DIR%"
) else (
    echo Installation failed!
    echo Please check the errors above and try again.
)
echo.
pause
endlocal