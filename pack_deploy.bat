@echo off
setlocal enabledelayedexpansion

set "STOP_SPT=0"
if /I "%~1"=="stop" set "STOP_SPT=1"

for %%I in ("%~dp0.") do set "REPO_DIR=%%~fI"
if not "%REPO_DIR:~-1%"=="\" set "REPO_DIR=%REPO_DIR%\"
pushd "%REPO_DIR%.."
set "GAME_DIR=%cd%\"
popd

if "%STOP_SPT%"=="1" (
  taskkill /IM SPT.Server.exe /F >nul 2>&1
)

set "VERSION="
for /f "usebackq delims=" %%v in ("%REPO_DIR%Sources\Client\VERSION.txt") do (
  set "VERSION=%%v"
  goto :gotver
)
:gotver
if "%VERSION%"=="" (
  echo Failed to read version from Sources\Client\VERSION.txt
  exit /b 1
)

echo Building server...
dotnet build %REPO_DIR%Sources\Server\QuickPrice.Server.csproj -c Release
if errorlevel 1 (
  echo Server build failed.
  exit /b 1
)

echo Building client...
dotnet build %REPO_DIR%Sources\Client\QuickPrice.csproj -c Release /p:TarkovDir=%GAME_DIR%
if errorlevel 1 (
  echo Client build failed.
  exit /b 1
)

set "RELEASE_DIR=%REPO_DIR%Release\QuickPrice-v%VERSION%"
set "CLIENT_TARGET=%RELEASE_DIR%\BepInEx\plugins\QuickPrice"
set "SERVER_TARGET=%RELEASE_DIR%\SPT\user\mods\QuickPrice"
if not exist "%CLIENT_TARGET%" mkdir "%CLIENT_TARGET%"
if not exist "%SERVER_TARGET%" mkdir "%SERVER_TARGET%"

copy /Y "%REPO_DIR%Sources\Client\bin\Release\net471\QuickPrice.dll" "%CLIENT_TARGET%\" >nul
copy /Y "%REPO_DIR%Sources\Server\bin\Release\quickprice.dll" "%SERVER_TARGET%\" >nul
copy /Y "%REPO_DIR%Sources\Server\bin\Release\mod.json" "%SERVER_TARGET%\" >nul
copy /Y "%REPO_DIR%Sources\Server\bin\Release\config.json" "%SERVER_TARGET%\" >nul

(
  echo QuickPrice v%VERSION%
  echo.
  echo Client:
  echo - Copy QuickPrice.dll to BepInEx\plugins\QuickPrice\
  echo.
  echo Server:
  echo - Copy quickprice.dll, mod.json, config.json to SPT\user\mods\QuickPrice\
) > "%RELEASE_DIR%\README.txt"

set "ZIP_PATH=%REPO_DIR%Release\QuickPrice-v%VERSION%.zip"
if exist "%ZIP_PATH%" del /F /Q "%ZIP_PATH%"
PowerShell -NoProfile -Command "Compress-Archive -Path '%RELEASE_DIR%' -DestinationPath '%ZIP_PATH%' -Force"
if errorlevel 1 (
  echo Failed to create zip.
  exit /b 1
)

set "STAGING=%REPO_DIR%Release\_staging"
if exist "%STAGING%" rmdir /S /Q "%STAGING%"
mkdir "%STAGING%"
PowerShell -NoProfile -Command "Expand-Archive -Path '%ZIP_PATH%' -DestinationPath '%STAGING%' -Force"
if errorlevel 1 (
  echo Failed to extract zip.
  exit /b 1
)

set "EXTRACTED=%STAGING%\QuickPrice-v%VERSION%"
xcopy "%EXTRACTED%\*" "%GAME_DIR%\" /E /I /Y >nul
if errorlevel 1 (
  echo Copy to game directory failed. Ensure SPT is not running.
  exit /b 1
)

rmdir /S /Q "%STAGING%"
echo Done. Deployed to %GAME_DIR%
