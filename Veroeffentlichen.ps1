<#
.SYNOPSIS
    Baut, testet und veröffentlicht die Laternenwacht (Frontend + Backend) als eine einzelne EXE.

.DESCRIPTION
    1. Führt alle Tests der Gesamtlösung aus (bricht bei Fehlern ab).
    2. Veröffentlicht die WPF-App als einzelne EXE. Das Backend (Laternenwacht.Core,
       Laternenwacht.Platform.Windows) wird dabei automatisch mit in die EXE gepackt.
         - Eigenständig (Profil "Win-x64-EinzelneExe"): ~67 MB, bringt die .NET-Laufzeit mit,
           läuft auf jedem Windows ohne Installation  -> publish\win-x64\Laternenwacht.exe
         - Schlank (Profil "Win-x64-Schlank"): ~4 MB, nutzt die installierte .NET-10-Desktop-Laufzeit
           und ist die sparsamste Variante           -> publish\win-x64-schlank\Laternenwacht.exe
    3. Öffnet den Explorer mit der fertigen Datei.

.PARAMETER Variante
    Beide (Standard), Eigenstaendig oder Schlank.

.PARAMETER OhneTests
    Überspringt die Tests (nicht empfohlen).

.EXAMPLE
    .\Veroeffentlichen.ps1
.EXAMPLE
    .\Veroeffentlichen.ps1 -Variante Schlank
#>
[CmdletBinding()]
param(
    [ValidateSet('Beide', 'Eigenstaendig', 'Schlank')]
    [string]$Variante = 'Beide',
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

$varianten = @(
    @{ Name = 'Eigenstaendig'; Profil = 'Win-x64-EinzelneExe'; Ordner = 'publish\win-x64' },
    @{ Name = 'Schlank';       Profil = 'Win-x64-Schlank';     Ordner = 'publish\win-x64-schlank' }
) | Where-Object { $Variante -eq 'Beide' -or $_.Name -eq $Variante }

$fertig = @()
foreach ($v in $varianten) {
    Schritt "Laternenwacht wird veröffentlicht: $($v.Name) ($($v.Profil))"
    dotnet publish frontend/Laternenwacht.App/Laternenwacht.App.csproj "-p:PublishProfile=$($v.Profil)"
    if ($LASTEXITCODE -ne 0) { throw "Veröffentlichung fehlgeschlagen: $($v.Name)" }

    $exe = Join-Path $PSScriptRoot "$($v.Ordner)\Laternenwacht.exe"
    if (-not (Test-Path -LiteralPath $exe)) { throw "Die EXE wurde nicht gefunden: $exe" }
    $fertig += $exe
}

Write-Host "`nFertig! Die Laterne ist bereit:" -ForegroundColor Green
foreach ($exe in $fertig) {
    $groesse = '{0:N1} MB' -f ((Get-Item -LiteralPath $exe).Length / 1MB)
    $hash = (Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash
    Write-Host "  $exe ($groesse)"
    Write-Host "  SHA-256: $hash"
}
if ($Variante -ne 'Eigenstaendig') {
    Write-Host "`nHinweis: Die schlanke EXE braucht die '.NET Desktop Runtime 10' (x64). Fehlt sie, bietet Windows beim Start den Download an." -ForegroundColor Cyan
}

Start-Process explorer.exe -ArgumentList "/select,`"$($fertig[-1])`""
