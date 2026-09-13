@echo off
setlocal
cd /d "%~dp0"
set "EXAM_CONTESTS=%~1"
if not defined EXAM_CONTESTS set "EXAM_CONTESTS=%USERPROFILE%\Downloads\DotNetInteractiveServer\Contests"
where dotnet >nul 2>nul
if errorlevel 1 (
 echo Se necesita .NET 10 SDK. Python no es necesario.
 pause
 exit /b 1
)
echo Preparando frontend con .NET 10 SDK...
dotnet build "tools\FrontendHost\FrontendHost.csproj" --nologo -v quiet
if errorlevel 1 (
 echo No se pudo compilar. Comprueba que este instalado el SDK de .NET 10.
 pause
 exit /b 1
)
echo Abre http://localhost:1101 . Mantiene DotNetInteractive en el puerto 1100.
echo Casos: %EXAM_CONTESTS%
echo Cierra esta ventana para detener solo el frontend.
dotnet "tools\FrontendHost\bin\Debug\net10.0\FrontendHost.dll" --frontend-root "%CD%" --contests "%EXAM_CONTESTS%"
pause
