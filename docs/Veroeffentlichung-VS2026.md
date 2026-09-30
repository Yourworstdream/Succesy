# Anleitung: Laternenwacht in Visual Studio 2026 als EXE veröffentlichen

Diese Anleitung führt Schritt für Schritt vom Quellcode zur fertigen, eigenständigen
`Laternenwacht.exe`, die sich ohne Installation auf jedem 64‑Bit‑Windows‑10/11‑Rechner starten lässt.

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
├── src
│   ├── Laternenwacht.App      ← WPF-Anwendung (wird zur EXE)
│   └── Laternenwacht.Core     ← Fachlogik
└── tests
    └── Laternenwacht.Core.Tests
```

---

## 3. Erst testen, dann veröffentlichen

1. Rechtsklick auf **Laternenwacht.App** → **Als Startprojekt festlegen**.
2. Oben in der Symbolleiste Konfiguration **Debug** wählen und **F5** drücken → die Anwendung startet.
3. **Test → Test-Explorer** öffnen → **Alle Tests ausführen** (grüner Doppelpfeil).
   Alle Tests müssen grün sein, bevor veröffentlicht wird.

---

## 4. Veröffentlichen mit dem mitgelieferten Profil (empfohlen)

Das Repository enthält bereits ein fertiges Veröffentlichungsprofil
(`src/Laternenwacht.App/Properties/PublishProfiles/Win-x64-EinzelneExe.pubxml`).

1. Im Projektmappen‑Explorer **Rechtsklick auf `Laternenwacht.App` → „Veröffentlichen…“**.
2. Visual Studio erkennt das Profil **„Win-x64-EinzelneExe“** automatisch und zeigt die Übersichtsseite.
   *(Falls mehrere Profile existieren: oben im Auswahlfeld dieses Profil wählen.)*
3. Auf **„Veröffentlichen“** klicken.
4. Nach Abschluss erscheint der Hinweis *„Veröffentlichung erfolgreich“*. Über
   **„Zielspeicherort öffnen“** gelangst du direkt zum Ordner:

```
<Repository>\publish\win-x64\Laternenwacht.exe
```

Diese **eine Datei** ist die fertige Anwendung (ca. 60–70 MB, da die .NET‑Laufzeit enthalten ist).
Sie kann z. B. auf einen USB‑Stick kopiert oder per Doppelklick gestartet werden.

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

> **Kleinere EXE gewünscht?** Bereitstellungsmodus **„Frameworkabhängig“** wählen. Die EXE ist dann
> nur ~1 MB groß, der Zielrechner benötigt aber die **.NET 10 Desktop Runtime**.

---

## 6. Veröffentlichen über die Kommandozeile

In Visual Studio: **Ansicht → Terminal** (oder die *Developer PowerShell*) und im Repository‑Ordner:

```powershell
dotnet test Laternenwacht.sln -c Release
dotnet publish src/Laternenwacht.App/Laternenwacht.App.csproj -p:PublishProfile=Win-x64-EinzelneExe
```

Ergebnis ebenfalls unter `publish\win-x64\Laternenwacht.exe`.

Zusätzlich baut die GitHub‑Action (`.github/workflows/build.yml`) bei jedem Push die EXE und stellt sie
als Artefakt **„Laternenwacht-win-x64“** im Reiter *Actions* zum Download bereit.

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
