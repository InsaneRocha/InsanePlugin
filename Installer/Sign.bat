@echo off

REM Check for input
if "%~1"=="" (
    echo Usage: Sign.bat YourApp.exe
    exit /b 1
)

REM Sign the executable
REM Must have benofficial2.pfx added to the user certificate store
signtool sign /sha1 59B8DB9C4DC2531A12D70C4B037BFAE02124F5A2 /fd SHA256 /td SHA256 /tr http://timestamp.digicert.com "%~1"

if %errorlevel% neq 0 (
    echo Signing failed.
    exit /b %errorlevel%
)