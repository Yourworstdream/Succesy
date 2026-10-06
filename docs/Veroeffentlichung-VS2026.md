# Anleitung: Laternenwacht in Visual Studio 2026 als EXE veröffentlichen

Diese Anleitung führt Schritt für Schritt vom Quellcode zur fertigen, eigenständigen
`Laternenwacht.exe`, die sich ohne Installation auf jedem 64‑Bit‑Windows‑10/11‑Rechner starten lässt.

> **Frontend und Backend sind verbunden.** Die Projektmappe `Laternenwacht.sln` enthält beide Teile;
> die WPF‑App (`frontend/Laternenwacht.App`) referenziert `Laternenwacht.Core` und
> `Laternenwacht.Platform.Windows` aus `backend/`. Beim Veröffentlichen wird alles zusammen in
> **eine** EXE gepackt – es gibt nichts separat zu kopieren oder zu installieren.

## Kurzfassung: in 1 Minute zur EXE

**Doppelklick auf `Veroeffentlichen.cmd`** im Repository‑Ordner. Das Skript führt alle Tests aus,
veröffentlicht beide EXE‑Varianten und öffnet den Explorer mit der fertigen Datei:

| Variante | Datei | Größe | Wann wählen? |
|---|---|---|---|
| **Eigenständig** (Profil `Win-x64-EinzelneExe`) | `publish\win-x64\Laternenwacht.exe` | ~65 MB | Weitergabe an Rechner, auf denen nichts installiert werden soll |
| **Schlank** (Profil `Win-x64-Schlank`) | `publish\win-x64-schlank\Laternenwacht.exe` | ~1,3 MB | Eigener Rechner (Visual Studio 2026 bringt die Laufzeit mit) – **sparsamste Variante** für Festplatte und Arbeitsspeicher |

Nur eine Variante: `.\Veroeffentlichen.ps1 -Variante Schlank` bzw. `-Variante Eigenstaendig`. Voraussetzung ist nur Visual Studio 2026 mit der Workload
„.NET‑Desktopentwicklung“ (Abschnitt 1). Der ausführliche Weg über Visual Studio folgt unten.

---

## 1. Voraussetzungen

| Was | Details |
|---|---|
| Betriebssystem | Windows 10 (1809+) oder Windows 11, 64 Bit |
| IDE | **Visual Studio 2026** (Community, Professional oder Enterprise) |
| Workload | **„.NET-Desktopentwicklung“** (enthält das .NET‑10‑SDK und WPF) |
| Git | In Visual Studio enthalten |

**Workload prüfen/nachinstallieren:**
1. Startmenü → **Visual Studio Installer** öffnen.
2. Bei *Visual Studio 2026* auf **Ändern** klicken.
3. Reiter **Workloads** → Haken bei **.NET-Desktopentwicklung** → **Ändern**.

---

## 2. Repository klonen und öffnen

1. Visual Studio 2026 starten → im Startfenster **„Repository klonen“** wählen.
2. Als Repository‑Adresse `https://github.com/yourworstdream/succesy` eintragen, Zielordner wählen → **Klonen**.
3. Falls die Projektmappe nicht automatisch geöffnet wird:
   **Datei → Öffnen → Projekt/Projektmappe…** → `Laternenwacht.sln` wählen.

Im **Projektmappen‑Explorer** siehst du nun:

```
Projektmappe "Laternenwacht"
├── backend
│   ├── Laternenwacht.Core              ← Fachlogik
│   ├── Laternenwacht.Platform.Windows  ← Windows-Anbindung (Messung, DPAPI)
│   └── Laternenwacht.Core.Tests
└── frontend
    └── Laternenwacht.App               ← WPF-Anwendung (wird zur EXE)
```

---

## 3. Erst testen, dann veröffentlichen

1. **Laternenwacht.App** ist bereits als Startprojekt voreingestellt (fett im Projektmappen‑Explorer).
   Falls nicht: Rechtsklick auf **Laternenwacht.App** → **Als Startprojekt festlegen**.
2. Oben in der Symbolleiste Konfiguration **Debug** wählen und **F5** drücken → die Anwendung startet.
3. **Test → Test-Explorer** öffnen → **Alle Tests ausführen** (grüner Doppelpfeil).
   Alle Tests müssen grün sein, bevor veröffentlicht wird.

---

## 4. Veröffentlichen mit dem mitgelieferten Profil (empfohlen)

Das Repository enthält bereits zwei fertige Veröffentlichungsprofile
(`frontend/Laternenwacht.App/Properties/PublishProfiles/`):
**`Win-x64-EinzelneExe`** (eigenständig) und **`Win-x64-Schlank`** (nutzt die installierte .NET‑10‑Desktop‑Laufzeit).

1. Im Projektmappen‑Explorer **Rechtsklick auf `Laternenwacht.App` → „Veröffentlichen…“**.
2. Visual Studio erkennt das Profil **„Win-x64-EinzelneExe“** automatisch und zeigt die Übersichtsseite.
   *(Oben im Auswahlfeld lässt sich zwischen „Win-x64-EinzelneExe“ und „Win-x64-Schlank“ wechseln.)*
3. Auf **„Veröffentlichen“** klicken.
4. Nach Abschluss erscheint der Hinweis *„Veröffentlichung erfolgreich“*. Über
   **„Zielspeicherort öffnen“** gelangst du direkt zum Ordner:

```
<Repository>\publish\win-x64\Laternenwacht.exe
```

Diese **eine Datei** ist die fertige Anwendung (ca. 60–70 MB, da die .NET‑Laufzeit enthalten ist).
Sie kann z. B. auf einen USB‑Stick kopiert oder per Doppelklick gestartet werden.

Mit dem Profil **„Win-x64-Schlank“** entsteht stattdessen `<Repository>\publish\win-x64-schlank\Laternenwacht.exe`
(ca. 1,3 MB). Sie startet auf jedem Rechner, auf dem die **.NET Desktop Runtime 10 (x64)** installiert ist –
mit Visual Studio 2026 ist das bereits der Fall. Fehlt die Laufzeit, zeigt Windows beim Start einen Hinweis
mit Download‑Link.

---

## 5. Alternativ: Profil selbst anlegen

Falls du das Profil von Grund auf erstellen möchtest (z. B. zu Übungszwecken):

1. Rechtsklick auf **Laternenwacht.App** → **Veröffentlichen…** → **Neues Profil hinzufügen** (bzw. „+ Neu“).
2. **Ziel:** **Ordner** → **Weiter**.
3. **Spezifisches Ziel:** **Ordner** → **Weiter**.
4. **Ordnerspeicherort:** z. B. `..\..\publish\win-x64\` → **Fertig stellen**.
5. Auf der Profilseite **„Alle Einstellungen anzeigen“** (Stiftsymbol bei *Einstellungen*) öffnen und setzen:

| Einstellung | Wert | Warum |
|---|---|---|
| Konfiguration | **Release** | Optimierter Code |
| Zielframework | **net10.0-windows** | Wie im Projekt festgelegt |
| Bereitstellungsmodus | **Eigenständig** | Zielrechner braucht keine .NET‑Installation |
| Zielruntime | **win-x64** | 64‑Bit‑Windows |
| Dateiveröffentlichungsoptionen → **Einzelne Datei erstellen** | ✔ | Ergebnis ist genau eine EXE |
| Dateiveröffentlichungsoptionen → **ReadyToRun-Kompilierung aktivieren** | ✔ | Schnellerer Start |
| Dateiveröffentlichungsoptionen → **Nicht verwendete Assemblys kürzen** | ✘ | WPF unterstützt kein Trimming |
| Alle vorhandenen Dateien vor der Veröffentlichung löschen | ✔ | Keine Altlasten im Ausgabeordner |

6. **Speichern** → **Veröffentlichen**.

> **Kleinere EXE gewünscht?** Bereitstellungsmodus **„Frameworkabhängig“** wählen (so ist das Profil
> „Win-x64-Schlank“ eingestellt). Die EXE ist dann nur ~1,3 MB groß, der Zielrechner benötigt aber die
> **.NET 10 Desktop Runtime**.

---

## 6. Veröffentlichen über die Kommandozeile

In Visual Studio: **Ansicht → Terminal** (oder die *Developer PowerShell*) und im Repository‑Ordner:

```powershell
dotnet test Laternenwacht.sln -c Release
dotnet publish frontend/Laternenwacht.App/Laternenwacht.App.csproj -p:PublishProfile=Win-x64-EinzelneExe
dotnet publish frontend/Laternenwacht.App/Laternenwacht.App.csproj -p:PublishProfile=Win-x64-Schlank
```

Ergebnis unter `publish\win-x64\Laternenwacht.exe` bzw. `publish\win-x64-schlank\Laternenwacht.exe`.

Noch einfacher: `.\Veroeffentlichen.ps1` (bzw. Doppelklick auf `Veroeffentlichen.cmd`) erledigt Tests,
Veröffentlichung und zeigt Größe und SHA‑256‑Prüfsumme beider EXE an.

Zusätzlich baut die GitHub‑Action (`.github/workflows/build.yml`) bei jedem Push die EXE und stellt sie
als Artefakt **„Laternenwacht-win-x64“** im Reiter *Actions* zum Download bereit.

---

## 6a. Auf GitHub als Release veröffentlichen (zum Herunterladen für andere)

Ein Versions‑Tag erzeugt automatisch ein GitHub‑Release mit der fertigen EXE und ihrer Prüfsumme
(`.github/workflows/release.yml`):

**In Visual Studio 2026:**
1. **Git → Git‑Repository verwalten** (bzw. Fenster *Git‑Repository*) öffnen.
2. Den gewünschten Commit auf `main` auswählen → Rechtsklick → **Neues Tag…**.
3. Tag‑Name z. B. **`v1.6.0`** eingeben → **Tag erstellen**.
4. **Git → Push** und dabei **Tags mit übertragen** (im Push‑Menü „Alle Tags pushen“).

**Oder im Terminal:**
```powershell
git tag v1.6.0
git push origin v1.6.0
```

Nach wenigen Minuten erscheint unter **GitHub → Releases** die Seite *Laternenwacht v1.6.0* mit
`Laternenwacht-v1.6.0-win-x64.exe` (eigenständig), `Laternenwacht-v1.6.0-win-x64-schlank.exe` und den
zugehörigen `…exe.sha256`‑Prüfsummen. Die Versionsnummer der EXE wird dabei aus dem
Tag übernommen.

> Vor einer **öffentlichen** Veröffentlichung die mitgelieferten Internet‑Memes aus
> `frontend/Laternenwacht.App/Assets/Memes/` entfernen oder durch eigene Bilder ersetzen
> (Urheberrecht) – siehe README.

---

## 7. Erster Start auf einem anderen Rechner

* **Windows SmartScreen** meldet ggf. *„Der Computer wurde durch Windows geschützt“*, weil die EXE
  nicht digital signiert ist. → **Weitere Informationen → Trotzdem ausführen**.
* **Keine Administratorrechte nötig** – die Anwendung läuft mit Benutzerrechten (`asInvoker`).
* Daten liegen unter `%LOCALAPPDATA%\Laternenwacht\`
  (`einstellungen.json`, `chronik.jsonl`, `chronik.anker.json`, `siegel.key`, `laternenwacht.log`).

### Optional: EXE signieren (für die Verteilung im Unternehmen)

Mit einem Code‑Signing‑Zertifikat entfällt die SmartScreen‑Warnung auf Dauer:

```powershell
signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 /a publish\win-x64\Laternenwacht.exe
```

(`signtool` ist Teil des Windows SDK, das mit Visual Studio installiert werden kann.)

---

## 8. Häufige Probleme

| Meldung / Symptom | Ursache | Lösung |
|---|---|---|
| `NETSDK1045: Das aktuelle .NET SDK unterstützt .NET 10.0 nicht` | Veraltetes Visual Studio/SDK | Visual Studio Installer → **Aktualisieren** |
| `A compatible .NET SDK was not found` (global.json) | Kein .NET‑10‑SDK installiert | Workload *.NET-Desktopentwicklung* installieren |
| Veröffentlichung schlägt fehl: *Datei wird verwendet* | Laternenwacht läuft noch | Anwendung beenden, erneut veröffentlichen |
| Build‑Fehler durch eine Warnung | Projekt behandelt Warnungen als Fehler (Qualitätsanspruch) | Warnung beheben – nicht abschalten |
| „Die Laternenwacht brennt bereits“ | Es darf nur eine Instanz laufen | Vorhandenes Fenster verwenden |
| F5: *Ein Projekt mit dem Ausgabetyp „Klassenbibliothek“ kann nicht direkt gestartet werden* | Ein Backend‑Projekt ist Startprojekt | Rechtsklick auf **Laternenwacht.App** → **Als Startprojekt festlegen** |
| *Das Argument "%~dp0Veroeffentlichen.ps1" für den -File-Parameter ist nicht vorhanden* | Der **Inhalt** der .cmd wurde in die Eingabeaufforderung kopiert (z. B. in `C:\Windows\System32`) | Die **Datei** ausführen: im Explorer im Repository‑Ordner doppelklicken, oder `cd /d <Repository-Ordner>` und dann `Veroeffentlichen.cmd` |
| `Veroeffentlichen.cmd` schließt sofort / meldet *Ausführung von Skripts ist deaktiviert* | PowerShell‑Richtlinie | Das `.cmd` startet PowerShell bereits mit `-ExecutionPolicy Bypass` nur für dieses Skript; sonst im Terminal `powershell -ExecutionPolicy Bypass -File .\Veroeffentlichen.ps1` |
