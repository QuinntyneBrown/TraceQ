@echo off
echo ============================================
echo  TraceQ - Starting Backend and Frontend
echo ============================================
echo.

echo Starting Backend (dotnet) on http://localhost:5000 ...
start "TraceQ Backend" cmd /k "cd /d C:\projects\TraceQ\src\TraceQ.Api && dotnet run"

echo Starting Frontend (Angular) on http://localhost:4200 ...
start "TraceQ Frontend" cmd /k "cd /d C:\projects\TraceQ\src\TraceQ.Web && npm start"

echo.
echo Both services are starting in separate windows.
echo   Backend:  http://localhost:5000
echo   Frontend: http://localhost:4200
echo.
