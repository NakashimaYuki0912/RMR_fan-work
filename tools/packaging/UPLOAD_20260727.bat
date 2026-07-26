@echo off
REM Double-click to upload the 2026-07-27 build to Workshop item 3743867841.
REM Opens a normal cmd console so steamcmd can prompt for the password and Steam Guard code.
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0_run_upload_20260727.ps1"
echo.
echo ============================================================
echo  steamcmd exit code: %ERRORLEVEL%
echo  0 = success. Anything else means the upload did NOT go through.
echo ============================================================
pause
exit /b %ERRORLEVEL%
