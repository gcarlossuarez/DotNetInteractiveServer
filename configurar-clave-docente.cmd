@echo off
cd /d "%~dp0"
echo Cierra el frontend antes de configurar la clave si la compilacion indica archivos en uso.
dotnet build "tools\FrontendHost\FrontendHost.csproj" --nologo -v quiet
if errorlevel 1 (
 pause
 exit /b 1
)
dotnet "tools\FrontendHost\bin\Debug\net10.0\FrontendHost.dll" --configure-teacher-key
pause
