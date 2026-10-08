# Laternenwacht – Backend (Übergabe an Gemini)

> Diese Datei ist die Arbeitsanweisung für KI-Assistenten (Gemini CLI liest `GEMINI.md` automatisch).
> Sie beschreibt, was dieses Backend tut, wie man es baut und testet und welche Regeln **nicht**
> gebrochen werden dürfen, weil das WPF-Frontend und bestehende Benutzerdaten davon abhängen.

## 1. Worum geht es?

**Laternenwacht** ist ein Fokuswächter für Windows. Während einer Fokuszeit („Wacht“) misst das
Backend jede Sekunde, welches Programm im Vordergrund ist und ob der Benutzer aktiv ist, und
verbucht die Zeit als **Fokus**, **Ablenkung („Frost“)** oder **Abwesenheit**. Abgeschlossene
Wachten werden in einer **manipulationserkennenden Chronik** gespeichert (HMAC‑SHA256‑Hash‑Kette).
Bei vielen Ablenkungen liefert das Backend humorvolle **Mahnrufe** aus den sieben Büchern der
Chroniken von Narnia.

Jede Wacht wird als **Reise** durch „Der König von Narnia“ erzählt (`Journey`): zehn Stationen in der
Reihenfolge des Buches, vom Laternenpfahl bis Cair Paravel. Die Station hängt **allein** an gemessener /
geplanter Zeit – Ablenkung bremst die Reise nie, sie färbt nur die Geschichte: Jede neue Ablenkungs-Episode
ist ein Stück Türkischer Honig aus der Schachtel der Königin (`FocusSession.DistractionStarts`), und am Ende
malt die Jahreszeit das Abschlussbild für die Chronik. Das Chronik-Format ändert sich dadurch nicht.

Das **Frontend** (WPF, nicht Teil dieser Übergabe) zeigt nur an: Fokusleiste am oberen
Bildschirmrand, Hauptfenster, Benachrichtigungen. Es enthält **keine** Fachlogik.

## 2. Aufbau

```
backend/
├── Laternenwacht.Backend.sln
├── Directory.Build.props      Qualitätsregeln (gelten auch fürs Frontend!)
├── Directory.Packages.props   Zentrale Paketversionen
├── global.json                .NET-10-SDK
├── src/
│   ├── Laternenwacht.Core/              net10.0, plattformunabhängig, keine Pakete
│   │   ├── Abstractions/   IActivityProbe, ISecretProtector
│   │   ├── Model/          SessionRecord, ActivityState, SessionPhase, ChronicleBook, …
│   │   ├── Tracking/       FocusWarden (Fassade), FocusSession (Zustandsautomat),
│   │   │                   ActivityClassifier, Admonitions (Sprüche), ShuffleBag, RealmMood, TimeFormat,
│   │   │                   Journey (die Reise durch „Der König von Narnia“: Stationen, Verlockungen, Abschlussbild)
│   │   ├── Integrity/      SessionJournal (Hash-Kette + Anker), JournalBootstrapper,
│   │   │                   ProtectedKeyStore, AtomicFile, CoreJsonContext
│   │   ├── Media/          MemeCatalog (eigene Meme-Bilder sicher einlesen)
│   │   └── Settings/       FocusSettings, SettingsValidator, SettingsStore, ProcessNames
│   └── Laternenwacht.Platform.Windows/  net10.0-windows, ohne Oberfläche
│       ├── Win32ActivityProbe     Vordergrundprozess + Leerlaufzeit (nur lesend, Name je Fenster zwischengespeichert)
│       ├── MemoryRelief           gibt Arbeitsspeicher zurück, wenn die App in den Hintergrund geht
│       ├── DpapiSecretProtector   Schlüsselschutz per DPAPI (CurrentUser)
│       ├── AppPaths, AppLog       %LOCALAPPDATA%\Laternenwacht, Fehlerprotokoll
│       └── Native/NativeMethods   P/Invoke-Deklarationen
└── tests/Laternenwacht.Core.Tests/      xUnit, 270 Tests, deterministische Uhr
```

## 3. Bauen und testen

```powershell
cd backend
dotnet build Laternenwacht.Backend.sln -c Release   # muss mit 0 Warnungen durchlaufen
dotnet test  Laternenwacht.Backend.sln -c Release   # alle Tests müssen grün sein
```

`Laternenwacht.Core` und die Tests laufen auch unter Linux/macOS.
`Laternenwacht.Platform.Windows` lässt sich überall **bauen** (`EnableWindowsTargeting`), läuft aber nur unter Windows.

## 4. Vertrag mit dem Frontend (nicht brechen!)

Das Frontend verwendet genau diese öffentlichen Typen und Mitglieder. Signaturen, Bedeutung und
Ereignis-Reihenfolge müssen erhalten bleiben. Erweiterungen (neue Mitglieder, neue optionale
Parameter mit Standardwert) sind erlaubt.

| Typ | Vom Frontend genutzt |
|---|---|
| `FocusWarden` | Konstruktor `(IActivityProbe, TimeProvider, FocusSettings, string selfProcessName)`, `Start(TimeSpan)`, `Pulse()` (1×/s vom UI-Timer), `Pause()`, `Resume()`, `Abort()`, `ApplySettings(FocusSettings)`, `Current`, `IsActive`, `LastSnapshot`, Ereignis `SessionEnded` (**genau einmal** je Wacht), Ereignis `DistractionStarted` (**einmal je neuer Ablenkungs-Episode**, löst das schwimmende Meme aus) |
| `DistractionStarted` | `ProcessName`, `Episode` |
| `FocusWarden` (Ereignisse) | `ReturnedToWork` (Rückkehr nach ≥ `MinimumAbsenceForWelcome` Ablenkung), `FocusStreakReached` (Fokus-Serie erreicht eine Schwelle aus `Praises.StreakMilestones`, je Serie einmal) |
| `ReturnedToWork` | `ProcessName`, `Absence`, `Episode` |
| `FocusStreakReached` | `Minutes`, `Index` |
| `FocusSession` (Serien) | `CurrentStreak`, `LongestStreak` – nur Ablenkung setzt die Serie zurück, Abwesenheit nicht |
| `SessionRecord.LongestFocusStreak` | neues optionales Feld (alte Einträge: 0) |
| `Homecomings` / `Praises` / `Encouragement` | `Homecomings.For(book, absence, seed)`, `Praises.ForStreak(book, index, seed)`; `Encouragement.Text`, `.Speaker`, `.Book` |
| `FocusWarden.SelfProcessName` | Name der eigenen App (für „als Verlockung markieren“) |
| `KnownDistractions` | `Names` – eingebauter Katalog (Spiele, Launcher, Messenger); greift bei `FocusSettings.UseKnownDistractions` |
| `SettingsEditing` | `MarkAsDistraction(FocusSettings, string)` → neue Einstellungen oder `null` |
| `ShuffleBag<T>` | Konstruktor `(IEnumerable<T>, Random? = null)`, `TryNext(out T)`, `Count` – nie zweimal dasselbe Element hintereinander |
| `MemeCatalog` | `Scan(string directory)`, `AllowedExtensions`, `MaxFileSizeBytes`, `MaxFiles` |
| `FocusSession` | `Phase`, `CurrentState`, `CurrentProcess`, `Focused`, `Distracted`, `Away`, `DistractionCount`, `Remaining`, `Progress`, `FrostRatio`, `IsFinished`, `Measured`, `Planned` |
| `FocusSession.DistractionStarts` | `IReadOnlyList<TimeSpan>`: `Measured` zu Beginn jeder Ablenkungs-Episode (genau `DistractionCount` Einträge; Rast und Abwesenheit erzeugen keinen). Speicher nur bei Episodenbeginn; **nicht** im `SessionRecord` |
| `FocusSession.TopDistractions` | `TopDistractions(int)` liefert eine neue Liste; `TopDistractions(Span<KeyValuePair<string, TimeSpan>>)` füllt einen vorhandenen Puffer in gleicher Reihenfolge ohne Allokation (für die Anzeige im Sekundentakt) |
| `Journey` | `StationCount` (10), `Stations`, `Fraction(measured, planned)`, `StationNumber(measured, planned)` (= 1 + ⌊9 · Anteil⌋, Station 10 erst bei 100 %; exakt in Ticks), `StationAt`, `PositionOf(n)` (= (n−1)/9), `UntilNextStation` (0 an Station 10), `ChapterFor(n)` (1–3 Schlitten, 4 Biberdamm, ab 5 Schloss), `TemptationLine(chapter, processName)`, `SpringFrostAllowance(planned)` / `ThawFrostAllowance(planned)` (Frost bis Tauwetter bzw. Winter), `EndingFor(outcome, mood)`, `Describe(ending)`, `ChronicleTitle(record)`; zusätzlich `EndingFor(record)`, `MoodOf(record)`, `Describe(record)` (Abbruch-Erzählung mit erreichter Station) |
| `JourneyStation` | `Number`, `Name`, `Narration` |
| `TemptationChapter` | `Sledge`, `BeaverDam`, `Castle` |
| `JourneyEnding` / `JourneyEndingText` | `Coronation` (vollendet + Frühling), `Thaw` (vollendet + Tauwetter), `StoneCourtyard` (vollendet + Winter), `Wardrobe` (alles andere); `Label`, `Title`, `Narration` |
| `SessionRecord` | `StartedAtUtc`, `Planned`, `Focused`, `Distracted`, `Measured`, `DistractionCount`, `Outcome`, `TopDistractions` |
| `ActivitySnapshot` | `ProcessName`, `IdleTime` |
| `ActivityState`, `SessionPhase`, `ClassificationMode`, `RealmMood`, `SealStatus`, `ChronicleBook` | Enum-Werte (Namen werden als Text in JSON gespeichert – **nicht umbenennen**) |
| `RealmMoods` | `FromFrost(double)` |
| `TimeFormat` | `Clock(TimeSpan)` |
| `Admonitions` / `Admonition` | `Next(int, TimeSpan, ISet<string>, ChronicleBook, int seed)`; `Admonition.Text`, `.Book`, `.Reply` (`Encouragement?`: nur „Der König von Narnia“ bei den 7 Anzahl-Schwellen – dann spricht in `Text` die Königin, Unterschrift `Admonitions.QueenSignature`, und `Reply` ist die Antwort aus Narnia; sonst `null`) |
| `ChronicleBooks` | `Title(ChronicleBook)`, `Volume(ChronicleBook)`, `Volumes` |
| `FocusSettings` | alle Eigenschaften, `Default`, `HasCustomBarPosition`; Änderungen nur per `with` |
| `SettingsValidator` | `Validate(FocusSettings)` → Liste deutscher Fehlermeldungen |
| `SettingsStore` | Konstruktor `(string path)`, `Load()`, `Save(FocusSettings)`, `LastLoadWarning`, `FilePath` |
| `ProcessNames` | `ParseList(string?)`, `FromImagePath(ReadOnlySpan<char>)` |
| `JournalBootstrapper` / `JournalOpenResult` | `Open(string dir, ISecretProtector, TimeProvider)`; `.Journal`, `.Verification`, `.ArchivedTo` |
| `SessionJournal` | `Append(SessionRecord)`, `Verify()`, `Records` |
| `JournalVerification` | `Status`, `Message` |
| `Win32ActivityProbe`, `DpapiSecretProtector` | parameterlose Konstruktoren |
| `AppPaths` | `DataDirectory`, `SettingsFile`, `LogFile`, `MemeDirectory` |
| `AppLog` | `Error(string context, Exception)`, `Info(string)` |
| `MemoryRelief` | `Release()` (nur bei Zustandswechseln aufrufen, nie periodisch) |

## 5. Unverhandelbare Regeln

### Kompatibilität mit vorhandenen Benutzerdaten
1. **Chronik-Format nicht ändern.** Die MAC-Eingabe ist exakt
   `"entry\n{seq}\n{prev}\n{payload}"` bzw. `"anchor\n{seq}\n{mac}"` (UTF-8, HMAC-SHA256, Hex klein).
   Jede Änderung lässt **alle bestehenden Chroniken der Benutzer als „gebrochen“** erscheinen.
   Ist eine Formatänderung unvermeidbar: neues Versionsfeld einführen und alte Einträge weiter prüfen können.
2. **`SessionRecord`/`JournalEntry`/`JournalAnchor`**: keine Felder umbenennen oder entfernen.
3. **`FocusSettings`-Eigenschaften bleiben `{ get; set; }`, nicht `init`.** Grund: Der JSON-Quellgenerator
   belegt fehlende `init`-Eigenschaften mit `default` statt mit dem Initialisierer – ältere
   Einstellungsdateien würden sonst ungültig. Neue Einstellungen brauchen einen sinnvollen Standardwert
   und einen Test wie `SettingsTests.Missing_fields_keep_their_defaults`.

### Sicherheit und Datenschutz
4. Keine Fenstertitel, Fensterinhalte, URLs oder Tastatureingaben erfassen – nur Prozessname und Leerlaufzeit.
5. Keine Netzwerkzugriffe, keine Telemetrie, keine Administratorrechte, keine globalen Hooks.
6. Keine neuen Laufzeitpakete außer Microsoft-eigenen `System.*`-Paketen (Begründung in der Übergabe).
7. JSON nur über die quellgenerierten Kontexte (`CoreJsonContext`, `SettingsJsonContext`) mit
   `UnmappedMemberHandling.Disallow`; keine Reflection-Serialisierung, kein `TypeNameHandling`.
8. Siegel immer mit `CryptographicOperations.FixedTimeEquals` vergleichen.
9. Dateien atomar schreiben (`AtomicFile`), Größenlimits beim Lesen beibehalten.
   Fremde Bilddateien (Meme-Ordner) nur über `MemeCatalog` einlesen: oberste Ebene, keine Reparse-Points,
   nur Bildendungen, Größen- und Anzahlgrenze.
10. Zeitmessung ausschließlich über `TimeProvider.GetTimestamp()` (monoton) – nie `DateTime.Now` für Dauern.
11. Fehler beim Lesen beschädigter Dateien dürfen nie zum Absturz führen (sichere Standardwerte bzw. Archivierung).

### Code-Qualität
12. `TreatWarningsAsErrors` und `AnalysisLevel latest-recommended` bleiben an. Warnungen beheben, nicht unterdrücken
    (Ausnahme nur mit `SuppressMessage` und stichhaltiger `Justification`).
13. Nullable-Referenztypen aktiviert; file-scoped Namespaces; Stil wie im vorhandenen Code.
14. **Sprache:** Kommentare, XML-Doku, Fehlermeldungen und Sprüche auf **Deutsch**; Bezeichner auf Englisch.
15. Jede Verhaltensänderung bekommt Tests. Zeitabhängiges mit `ManualTimeProvider` (siehe `TestDoubles.cs`) testen,
    nie mit `Thread.Sleep`.
16. **Ressourcen schonen:** `FocusWarden.Pulse` läuft jede Sekunde, die ganze Wacht lang. Dort keine teuren
    Systemabfragen (z. B. keine Prozessliste, kein `Process`-Objekt pro Takt), keine Dateizugriffe, möglichst
    keine Speicheranforderungen. `Win32ActivityProbe` fragt den Prozessnamen nur beim Fensterwechsel neu ab.
17. Sprüche (`Admonitions`, `Homecomings`, `Praises`, `Journey`) sind **eigene Formulierungen** – keine wörtlichen
    Zitate aus den Büchern, Übersetzungen oder Filmen (`OwnWordingTests` hält bekannte buchnahe Wendungen fern).
    Jedes Buch braucht genau einen Spruch je Schwelle (aktuell 7 Anzahl- + 4 Frost-Schwellen = 11).

## 6. Bekannte Grenzen (bewusst so)

* Die Chronik ist manipulations**erkennend**, nicht manipulations**sicher**: Wer als derselbe Windows-Benutzer
  eigenen Code ausführt, kann über DPAPI gültige Siegel erzeugen. Ein Rollback auf eine komplette
  ältere Kopie (Chronik **und** Anker) bleibt unentdeckt.
* Ablenkung innerhalb eines erlaubten Programms (z. B. Videoportal im Browser) wird nicht erkannt – Datenschutz geht vor.

## 7. Rückgabe der Arbeit

* Nur Dateien unter `backend/` ändern.
* Am Ende `dotnet build` (0 Warnungen) und `dotnet test` (alles grün) ausführen und das Ergebnis nennen.
* Jede Änderung am Vertrag aus Abschnitt 4 ausdrücklich auflisten (alt → neu), damit das Frontend angepasst werden kann.
