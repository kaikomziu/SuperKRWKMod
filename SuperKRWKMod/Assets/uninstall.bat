@echo off
title SuperKRWKMod - Uninstaller
setlocal enabledelayedexpansion
cd /d "%~dp0"
echo Uninstalling SuperKRWKMod...
echo.

set "MOD_DIR=SuperKRWKMod"
set "LEGACY_MOD_DIR=CatnipMod"
set "FILES_TO_REMOVE=%MOD_DIR% %LEGACY_MOD_DIR% winhttp.dll doorstop_config.ini"
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
        echo Bongo Cat is currently running. Closing it to ensure a clean uninstallation...
        taskkill /F /IM %GAME_EXE% >NUL 2>&1
        timeout /t 1 /nobreak >NUL
        echo.
    )
)

echo Removing SuperKRWKMod files from: %GAME_DIR%
echo.

set "ERROR=0"

for /f "tokens=*" %%I in ("%FILES_TO_REMOVE%") do (
    for %%J in (%%I) do (
        set "ITEM=%%~J"
        set "IS_DIR=0"
        
        if /I "%%~J"=="%MOD_DIR%" set "IS_DIR=1"
        if /I "%%~J"=="%LEGACY_MOD_DIR%" set "IS_DIR=1"
        
        if exist "%GAME_DIR%\%%~J" (
            if !IS_DIR! equ 1 (
                rmdir /S /Q "%GAME_DIR%\%%~J" >nul 2>&1
                if exist "%GAME_DIR%\%%~J" (
                    echo [ERROR] Failed to remove !ITEM! folder ^(folder may be in use^)
                    set "ERROR=1"
                ) else (
                    echo [OK] Removed !ITEM! folder
                )
            ) else (
                del /F /Q "%GAME_DIR%\%%~J" >nul 2>&1
                if exist "%GAME_DIR%\%%~J" (
                    echo [ERROR] Failed to remove !ITEM! ^(file may be in use^)
                    set "ERROR=1"
                ) else (
                    echo [OK] Removed !ITEM!
                )
            )
        ) else (
            if !IS_DIR! equ 1 (
                echo [INFO] !ITEM! folder not found ^(may already be removed^)
            ) else (
                echo [INFO] !ITEM! not found ^(may already be removed^)
            )
        )
    )
)

echo.
if %ERROR% equ 0 (
    echo Uninstallation complete.
    echo.
    echo        /\_/\
    echo       ^(o ^. o^)

    start "" "%GAME_DIR%"
) else (
    echo Uninstallation failed!
    echo Please check the errors above and try again.
)
echo.
pause
endlocal