@echo off
REM Script para hacer push automático de los últimos cambios a GitHub

git add .
for /f "tokens=1-4 delims=/ " %%a in ("%date%") do set DATE=%%d-%%b-%%c
for /f "tokens=1-2 delims=: " %%a in ("%time%") do set TIME=%%a%%b
set MSG=Auto UPDATE: %DATE%_%TIME%
git commit -m "%MSG%"
git pull --rebase origin main
git push origin main

echo.
echo Push completed. Press any key to exit.
pause >nul
