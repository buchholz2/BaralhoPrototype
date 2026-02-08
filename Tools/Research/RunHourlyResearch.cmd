@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0HourlyResearch.ps1" -DurationMinutes 15
endlocal
