@echo off
rem Doppelklick: baut, testet und veroeffentlicht die Laternenwacht als einzelne EXE.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Veroeffentlichen.ps1" %*
if errorlevel 1 (echo. & echo Veroeffentlichung fehlgeschlagen - siehe Meldungen oben.)
pause
