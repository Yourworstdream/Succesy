<#
.SYNOPSIS
    Baut, testet und veröffentlicht die Laternenwacht (Frontend + Backend) als eine einzelne EXE.

.DESCRIPTION
    1. Führt alle Tests der Gesamtlösung aus (bricht bei Fehlern ab).
    2. Veröffentlicht die WPF-App mit dem Profil "Win-x64-EinzelneExe". Das Backend
       (Laternenwacht.Core, Laternenwacht.Platform.Windows) wird dabei automatisch mit in die EXE gepackt.
    3. Öffnet den Explorer mit der fertigen Datei publish\win-x64\Laternenwacht.exe.

.PARAMETER OhneTests
    Überspringt die Tests (nicht empfohlen).

.EXAMPLE
    .\Veroeffentlichen.ps1
#>
[CmdletBinding()]
param(
    [switch]$OhneTests
)

$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot

function Schritt([string]$text) { Write-Host "`n=== $text ===" -ForegroundColor Yellow }

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'Das .NET SDK wurde nicht gefunden. Bitte Visual Studio 2026 mit der Workload ".NET-Desktopentwicklung" installieren.'
}

if (-not $OhneTests) {
    Schritt 'Tests werden ausgeführt'
    dotnet test Laternenwacht.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Tests fehlgeschlagen – es wurde nichts veröffentlicht.' }
}

Schritt 'Laternenwacht wird als einzelne EXE veröffentlicht'
dotnet publish frontend/Laternenwacht.App/Laternenwacht.App.csproj -p:PublishProfile=Win-x64-EinzelneExe
if ($LASTEXITCODE -ne 0) { throw 'Veröffentlichung fehlgeschlagen.' }

$exe = Join-Path $PSScriptRoot 'publish\win-x64\Laternenwacht.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw "Die EXE wurde nicht gefunden: $exe" }

$groesse = '{0:N1} MB' -f ((Get-Item -LiteralPath $exe).Length / 1MB)
$hash = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash
Write-Host "`nFertig! Die Laterne ist bereit:" -ForegroundColor Green
Write-Host "  $exe ($groesse)"
Write-Host "  SHA-256: $hash"

Start-Process explorer.exe -ArgumentList "/select,`"$exe`""
