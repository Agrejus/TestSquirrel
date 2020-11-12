@echo off
cd %1
taskkill /IM %2 /F
:loop1
timeout /t 1 /nobreak >nul 2>&1
tasklist | find /i %2 >nul 2>&1
if errorlevel 1 goto cont1
goto loop1
:cont1
Start "" %2