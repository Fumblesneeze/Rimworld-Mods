@echo off
pwsh -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0fake-dotnet.ps1" %*
exit /b %ERRORLEVEL%
