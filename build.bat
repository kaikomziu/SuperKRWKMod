@echo off

echo ========================================
echo   SuperKRWKMod Build Script
echo ========================================
echo.

REM Try to find MSBuild in common Visual Studio installation paths
set "MSBUILD_PATH="

REM Check Visual Studio 2026 (version 18)
if exist "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
    goto :found_msbuild
)
if exist "C:\Program Files\Microsoft Visual Studio\18\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\18\Professional\MSBuild\Current\Bin\MSBuild.exe"
    goto :found_msbuild
)
if exist "C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    goto :found_msbuild
)
if exist "C:\Program Files\Microsoft Visual Studio\18\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\18\MSBuild\Current\Bin\MSBuild.exe"
    goto :found_msbuild
)

REM Check Visual Studio 2025
if exist "C:\Program Files\Microsoft Visual Studio\2025\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2025\Community\MSBuild\Current\Bin\MSBuild.exe"
    goto :found_msbuild
)
if exist "C:\Program Files\Microsoft Visual Studio\2025\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2025\Professional\MSBuild\Current\Bin\MSBuild.exe"
    goto :found_msbuild
)
if exist "C:\Program Files\Microsoft Visual Studio\2025\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2025\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    goto :found_msbuild
)

REM Check Visual Studio 2022
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
    goto :found_msbuild
)
if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
    goto :found_msbuild
)
if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    goto :found_msbuild
)

REM Check Visual Studio 2019
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
    goto :found_msbuild
)
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
    goto :found_msbuild
)
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    goto :found_msbuild
)

REM Check Build Tools
if exist "C:\Program Files\Microsoft Visual Studio\18\BuildTools\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\18\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
    goto :found_msbuild
)
if exist "C:\Program Files\Microsoft Visual Studio\2025\BuildTools\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2025\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
    goto :found_msbuild
)
if exist "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
    goto :found_msbuild
)
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
    goto :found_msbuild
)

REM Check .NET Framework MSBuild (older installations)
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe" (
    set "MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"
    goto :found_msbuild
)

REM If still not found, try using MSBuild from PATH
where msbuild.exe >nul 2>&1
if %ERRORLEVEL% == 0 (
    set "MSBUILD_PATH=msbuild.exe"
    goto :found_msbuild
)

echo ERROR: Could not find MSBuild.exe
echo.
echo Please install one of the following:
echo   - Visual Studio 2019 or later (Community, Professional, or Enterprise)
echo   - Visual Studio Build Tools 2019 or later
echo.
echo Download links:
echo   - Visual Studio 2026 Community (free, full IDE): https://visualstudio.microsoft.com/downloads/
echo   - Build Tools for Visual Studio 2026 (minimal installation, no IDE): https://aka.ms/vs/stable/vs_BuildTools.exe
echo.
echo Or ensure MSBuild is in your system PATH.
echo.
pause
exit /b 1

:found_msbuild
echo Found MSBuild at: %MSBUILD_PATH%
echo.
echo Building Release configuration...
echo.

REM Build the project in Release configuration
"%MSBUILD_PATH%" "SuperKRWKMod\SuperKRWKMod.csproj" /p:Configuration=Release /p:Platform=AnyCPU /t:Rebuild /v:minimal

if %ERRORLEVEL% == 0 (
    echo.
    echo ========================================
    echo   Build completed successfully!
    echo ========================================
    echo.
    echo Output location: SuperKRWKMod\bin\Release\
    echo Zip file: SuperKRWKMod\bin\SuperKRWKMod.zip
    echo.
    echo        /\_/\
    echo       ^(o ^. o^)

    start "" explorer "SuperKRWKMod\bin"
) else (
    echo.
    echo ========================================
    echo   Build failed!
    echo ========================================
    echo.
    echo Please check the error messages above.
    echo.
    pause
    exit /b 1
)

pause

