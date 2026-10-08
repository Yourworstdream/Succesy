# Projektdokumentation – „Laternenwacht“

**Ein Fokuswächter mit manipulationsgeschützter Sitzungschronik**

| | |
|---|---|
| Ausbildungsberuf | Fachinformatiker/-in für Anwendungsentwicklung |
| Projekt | Laternenwacht – Desktopanwendung zur Messung von Ablenkung während Fokuszeiten |
| Technologie | C# 14, .NET 10, WPF, xUnit |
| Repository | `yourworstdream/succesy` |
| Stand | Oktober 2026, Version 1.6.0 |

---

## Inhaltsverzeichnis

1. [Einleitung](#1-einleitung)
2. [Projektplanung](#2-projektplanung)
3. [Analysephase](#3-analysephase)
4. [Entwurfsphase](#4-entwurfsphase)
5. [Sicherheits- und Integritätskonzept](#5-sicherheits--und-integritätskonzept)
6. [Implementierungsphase](#6-implementierungsphase)
7. [Qualitätssicherung](#7-qualitätssicherung)
8. [Abnahme und Einführung](#8-abnahme-und-einführung)
9. [Fazit und Ausblick](#9-fazit-und-ausblick)
10. [Anhang](#10-anhang)

---

## 1. Einleitung

### 1.1 Projektumfeld

Konzentriertes Arbeiten in festen Zeitblöcken („Fokuszeiten“, etwa nach der Pomodoro‑Technik) ist
in Ausbildung, Studium und Softwareentwicklung verbreitet. Gängige Timer zählen jedoch nur die
*geplante* Zeit herunter. Wie viel dieser Zeit tatsächlich durch Ablenkungen (Messenger, Spiele,
Streaming) verloren geht, bleibt unsichtbar.

### 1.2 Projektziel

Es wird eine Windows‑Desktopanwendung entwickelt, die

* während einer Fokuszeit **am oberen Bildschirmrand dauerhaft anzeigt, wie lange sich der Benutzer
  bereits ablenkt** („Frost“),
* abgeschlossene Sitzungen in einer **manipulationserkennenden Chronik** speichert (Integrität),
* dabei **Datenschutz und Sicherheit** konsequent berücksichtigt (keine Fenstertitel, keine
  Netzwerkkommunikation, keine erhöhten Rechte),
* und durch eine **Fantasy‑Gestaltung** – angelehnt an die Atmosphäre der Chroniken von C. S. Lewis
  (Laternenpfahl im Schneewald, ewiger Winter, wiederkehrender Frühling) – motiviert statt zu tadeln.

### 1.3 Projektbegründung

Sichtbarkeit verändert Verhalten: Eine laufend sichtbare Ablenkungszeit wirkt als unmittelbares
Feedback. Die Gamification über Jahreszeiten und humorvolle „Mahnrufe“ senkt die Hemmschwelle,
das Werkzeug dauerhaft zu nutzen. Die versiegelte Chronik schafft eine verlässliche Datengrundlage,
etwa für Lerntagebücher oder Selbstreflexion in der Ausbildung.

### 1.4 Projektabgrenzung

Nicht Bestandteil des Projekts sind:

* Blockieren von Programmen oder Webseiten (die Anwendung beobachtet, sie bevormundet nicht),
* Auswertung einzelner Browser‑Tabs (dafür wären Fenstertitel nötig – bewusst ausgeschlossen),
* Synchronisation zwischen Geräten, Mehrbenutzerbetrieb, Lokalisierung in weitere Sprachen.

---

## 2. Projektplanung

### 2.1 Projektphasen und Zeitplanung

| Phase | Tätigkeiten | Soll (h) |
|---|---|---:|
| Analyse | Ist‑Analyse, Anforderungen, Wirtschaftlichkeit | 6 |
| Entwurf | Architektur, Zustandsautomat, Datenmodell, Sicherheitskonzept, UI‑Konzept | 12 |
| Implementierung | Kernlogik, Integritätsjournal, Win32‑Anbindung, WPF‑Oberfläche | 34 |
| Qualitätssicherung | Unit‑Tests, manuelle Tests, Code‑Review, statische Analyse | 12 |
| Dokumentation | Projektdokumentation, Benutzer‑ und Veröffentlichungsanleitung | 8 |
| **Summe** | | **72** |

### 2.2 Ressourcen

* **Hardware:** Arbeitsplatzrechner mit Windows 11.
* **Software:** Visual Studio 2026, .NET 10 SDK, Git/GitHub, GitHub Actions (CI).
* **Bibliotheken:** ausschließlich .NET‑Bordmittel; für Tests xUnit. Bewusst **keine** weiteren
  Laufzeitabhängigkeiten (kleinere Angriffsfläche, keine Lizenz‑ oder Lieferkettenrisiken).

### 2.3 Entwicklungsprozess

Iterativ‑inkrementelles Vorgehen: Zuerst die plattformunabhängige, testgetriebene Fachlogik
(`Laternenwacht.Core`), anschließend die Windows‑Oberfläche. Jede Iteration endet mit grünem Build
(Warnungen werden als Fehler behandelt) und grünen Tests in der CI.

---

## 3. Analysephase

### 3.1 Ist‑Analyse

Bisher wurden einfache Timer‑Apps genutzt. Ablenkungen wurden – wenn überhaupt – nachträglich
geschätzt. Schätzungen sind erfahrungsgemäß stark geschönt; eine objektive Messung fehlte.

### 3.2 Wirtschaftlichkeitsbetrachtung (vereinfacht)

Angenommen, ein Auszubildender verliert täglich 30 Minuten Fokuszeit unbemerkt. Reduziert die
Sichtbarkeit diesen Verlust nur um ein Drittel, ergeben sich bei 220 Arbeitstagen
**≈ 55 Stunden** gewonnene Arbeitszeit pro Jahr und Person. Dem stehen einmalig 72 Stunden
Entwicklungsaufwand gegenüber; ab etwa zwei Nutzern amortisiert sich das Projekt im ersten Jahr.
Lizenzkosten entstehen nicht.

### 3.3 Anforderungen

**Funktionale Anforderungen**

| Nr. | Anforderung | Priorität |
|---|---|---|
| F1 | Fokussitzung („Wacht“) mit wählbarer Dauer (1–480 min, Schnellwahl 15/25/50/90) starten | Muss |
| F2 | Stets sichtbare Leiste am **oberen Bildschirmrand** mit Restzeit und **kumulierter Ablenkungszeit** | Muss |
| F3 | Erkennung von Ablenkung anhand des Vordergrundprogramms (Erlaubnis‑ oder Sperrliste) | Muss |
| F4 | Erkennung von Abwesenheit über Leerlaufzeit, getrennt ausgewiesen | Muss |
| F5 | Pausieren, Fortsetzen, Abbrechen einer Wacht | Muss |
| F6 | Speicherung abgeschlossener Wachten in einer Chronik mit Integritätsprüfung | Muss |
| F7 | Anzeige des Siegelzustands (unversehrt / gebrochen) | Muss |
| F8 | Konfigurierbare Listen, Dauer, Leerlaufschwelle mit Eingabevalidierung | Muss |
| F9 | Humorvolle Mahnrufe bei Erreichen von Ablenkungsschwellen | Soll |
| F10 | Statistik: Gesamtfokus, Gesamtfrost, Frühlingsquote | Soll |
| F11 | Mahnrufe als **Push‑Benachrichtigung** („Rabenbote“), abschaltbar | Soll |
| F12 | Fokusleiste **frei verschiebbar**, Position wird gespeichert; Rückkehr an den oberen Rand | Soll |
| F16 | Fokusleiste in **vier Größen** (Ultradünn, Klein, Mittel, Groß), sofort umschaltbar; bleibt **im Vordergrund** und hält angedockt auf Wunsch den **Platz am oberen Rand frei** (maximierte Fenster beginnen darunter) | Soll |
| F13 | **Rechtsklick‑Menü** der Leiste: Auswahl, aus welchem der sieben Bücher der Chroniken die Mahnrufe stammen (oder gemischt) | Soll |
| F15 | **Positive Verstärkung:** Lob für Fokus‑Serien (10/25/45/60/90 min), „Willkommen zurück“ nach einer Ablenkung, Würdigung makelloser Wachten und neuer Bestleistungen; getrennt von den Mahnrufen abschaltbar | Soll |
| F14 | Bei jeder neuen Ablenkung **treibt ein Meme** in einem Pop‑up quer über den Bildschirm; eigene Memes hinzufügbar, abschaltbar | Kann |

**Nichtfunktionale Anforderungen**

| Nr. | Anforderung |
|---|---|
| N1 | **Datenschutz:** nur Prozessname und Leerlaufzeit; keine Fenstertitel, Inhalte, Tastatureingaben (Datensparsamkeit, Art. 5 DSGVO) |
| N2 | **Sicherheit:** keine Administratorrechte, keine Netzwerkzugriffe, keine Fremdbibliotheken zur Laufzeit |
| N3 | **Integrität:** Veränderung, Löschung, Vertauschung oder Abschneiden von Chronikeinträgen wird erkannt |
| N4 | **Robustheit:** beschädigte Dateien führen nie zum Absturz |
| N5 | **Messgenauigkeit:** Verstellen der Systemuhr beeinflusst die Messung nicht |
| N6 | **Ergonomie:** Leiste stiehlt keinen Fokus, ist DPI‑fest, per Tastatur/Screenreader bedienbar |
| N7 | **Wartbarkeit:** Schichtenarchitektur, MVVM, automatisierte Tests für die gesamte Fachlogik |
| N8 | **Auslieferung:** eine einzelne EXE – eigenständig (ohne Installation) oder schlank (nutzt die installierte .NET‑Laufzeit) |
| N9 | **Ressourcenschonung:** Während einer Wacht (Hauptfenster im Hintergrund) praktisch keine Prozessor‑ und Grafiklast, geringer Arbeitsspeicher; keine Bewegung im Augenwinkel |

---

## 4. Entwurfsphase

### 4.1 Architektur

Die Lösung folgt einer Schichtenarchitektur mit strikter Abhängigkeitsrichtung
(Oberfläche → Fachlogik; niemals umgekehrt). Plattformspezifisches ist hinter Schnittstellen
verborgen (*Dependency Inversion*), wodurch die Fachlogik ohne Windows testbar ist.

```mermaid
flowchart TB
    subgraph FE["FRONTEND · frontend/Laternenwacht.App (WPF, net10.0-windows)"]
        V[Views<br/>MainWindow · WatchPage · ChroniclePage · FocusBarWindow · RavenToastWindow]
        VM[ViewModels<br/>Shell · Bar · Session · Chronicle · Settings]
        L[Services<br/>Lore · NotificationService · WindowStyles]
        V --> VM --> L
    end
    subgraph BE["BACKEND · backend/"]
        subgraph Core["Laternenwacht.Core (net10.0, plattformunabhängig)"]
            T[Tracking<br/>FocusWarden · FocusSession · ActivityClassifier · Admonitions]
            I[Integrity<br/>SessionJournal · JournalBootstrapper · ProtectedKeyStore · AtomicFile]
            C[Settings<br/>FocusSettings · SettingsValidator · SettingsStore · ProcessNames]
            A[Abstractions<br/>IActivityProbe · ISecretProtector]
        end
        subgraph P["Laternenwacht.Platform.Windows (net10.0-windows, ohne UI)"]
            S[Win32ActivityProbe · DpapiSecretProtector · AppPaths · AppLog]
        end
    end
    VM --> T & I & C
    VM --> S
    S -. implementiert .-> A
    T --> A
    I --> A
    Tests[Laternenwacht.Core.Tests<br/>xUnit] --> Core
```

**Begründete Technologieentscheidungen**

| Entscheidung | Alternativen | Begründung |
|---|---|---|
| WPF | WinUI 3, WinForms, Electron | Reif, transparente randlose Fenster, Vektorgrafik/Animation, keine Zusatzlaufzeit, in VS 2026 voll unterstützt |
| Eigenes MVVM‑Minimum | CommunityToolkit.Mvvm | Keine Laufzeitabhängigkeit (N2); Umfang gering |
| `TimeProvider` (.NET) | `DateTime.Now`, `Stopwatch` direkt | Monotone Zeit (N5) **und** testbar durch eigene Uhr |
| JSON Lines + HMAC‑Kette | SQLite, Klartext‑JSON | Anhängen ohne Umschreiben, zeilenweise prüfbar, keine Datenbankabhängigkeit |
| Quellgenerierte JSON‑Serialisierung | Reflection‑basiert | Keine Reflection, kein Polymorphismus, unbekannte Felder werden abgelehnt |

### 4.2 Zustandsautomat einer Wacht

```mermaid
stateDiagram-v2
    [*] --> Running : Start(dauer)
    Running --> Paused : Pause()
    Paused --> Running : Resume()
    Running --> Completed : gemessene Zeit ≥ geplante Zeit
    Running --> Aborted : Abort()
    Paused --> Aborted : Abort()
    Completed --> [*] : Eintrag in Chronik
    Aborted --> [*] : Eintrag in Chronik
```

Innerhalb von `Running` wechselt der **Aktivitätszustand** bei jeder Messung (1 Hz):

| Zustand | Bedingung | Darstellung |
|---|---|---|
| `Focused` | erlaubtes/neutrales Programm, Benutzer aktiv | Laterne brennt, goldener Rahmen |
| `Distracted` | Verlockung (Sperrliste) bzw. Nicht‑Gefährte (strenger Modus) | Frost kriecht über die Leiste |
| `Away` | Leerlauf ≥ Schwelle **oder** Messlücke > 30 s (Standby) | „Die Laterne wacht allein“ |

**Verbuchungsregel:** Die Zeit zwischen zwei Messungen wird dem *zuvor* gemessenen Zustand
zugerechnet und auf die Restzeit begrenzt. So endet eine Wacht exakt nach der geplanten Dauer,
und `Fokus + Frost + Abwesenheit = gemessene Zeit` gilt stets (Invariante, per Test abgesichert).

### 4.3 Klassifikation

```mermaid
flowchart LR
    S[Momentaufnahme] --> I{Leerlauf ≥ Schwelle?}
    I -- ja --> Away
    I -- nein --> U{Prozess bekannt?}
    U -- nein --> Focused
    U -- ja --> G{Eigene App oder Gefährte?}
    G -- ja --> Focused
    G -- nein --> V{Verlockung?}
    V -- ja --> Distracted
    V -- nein --> M{Modus}
    M -- Milde Wacht --> Focused
    M -- Strenge Wacht --> Distracted
```

Unbekannte Vordergrundfenster (Sperrbildschirm, Zugriff verweigert) werden bewusst **nicht**
bestraft – eine Fehlmessung soll nie zu Lasten des Benutzers gehen.

### 4.4 Datenmodell

```mermaid
classDiagram
    class SessionRecord {
        Guid Id
        DateTimeOffset StartedAtUtc
        DateTimeOffset EndedAtUtc
        TimeSpan Planned
        TimeSpan Focused
        TimeSpan Distracted
        TimeSpan Away
        int DistractionCount
        SessionPhase Outcome
        List~DistractionEntry~ TopDistractions
    }
    class JournalEntry {
        long Seq
        string Prev
        string Payload
        string Mac
    }
    class JournalAnchor {
        long Seq
        string Mac
        string AnchorMac
    }
    JournalEntry --> SessionRecord : Payload (JSON)
    JournalAnchor --> JournalEntry : verweist auf letzten
```

Ablage unter `%LOCALAPPDATA%\Laternenwacht\`:

| Datei | Inhalt |
|---|---|
| `einstellungen.json` | Einstellungen (eingerücktes JSON, menschenlesbar) |
| `chronik.jsonl` | Eine versiegelte Zeile je Wacht |
| `chronik.anker.json` | Versiegelter Verweis auf den letzten Eintrag |
| `siegel.key` | 256‑Bit‑HMAC‑Schlüssel, DPAPI‑verschlüsselt |
| `archiv\gebrochen-<Zeit>\` | Unverändert archivierte, gebrochene Chronik (Beweissicherung) |
| `laternenwacht.log` | Technisches Fehlerprotokoll (ohne Aktivitätsdaten, max. 1 MB) |

### 4.5 Oberflächenkonzept

**Gestaltungsleitbild:** „Das Reich hinter dem Schrank“ – eine Winternacht mit einem einzelnen
Laternenpfahl. Die Metaphern sind durchgängig und selbsterklärend:

| Fachbegriff | Im Reich |
|---|---|
| Fokussitzung | **Wacht** |
| Ablenkungszeit | **Frost** |
| Erlaubte Programme | **Gefährten** (in den Einstellungen: „Arbeitsprogramme“) |
| Ablenkende Programme | **Verlockungen** (in den Einstellungen: „Ablenkungen“) |
| Sitzungshistorie | **Chronik** |
| Integritätsprüfung | **Siegel** |
| Ergebnisbewertung | **Jahreszeit**: Frühling (< 10 % Frost), Tauwetter (< 25 %), Winter |
| Hinweis bei vielen Ablenkungen | **Mahnruf** (z. B. bei 20 Verlockungen die Königin: *„Zwanzig Stück! Honig gibt es ab jetzt keinen mehr, nur trockenes Brot. Bleibst du trotzdem?“*, darauf Peter: *„Genau so ging es Edmund. Was sie verspricht, hält sie nicht.“*) |

**Gestaltungssystem „Nachtwald“ (Version 1.5, Vorgänger).** Die Oberfläche hat zuvor zwei Iterationen
durchlaufen, die beide im Nutzertest scheiterten – ein lehrreicher Teil des Projekts:

1. *Fantasy‑klassisch* (Nachtblau, Gold, Pergament, Serifen, Ornamente): wirkte altmodisch und „generiert“.
2. *„Schneelicht“* (Cremeweiß, Tinte, Orange, schlichte weiße Karten): wirkte beliebig – Cremeweiß mit Orange
   ist zudem die Markenfarbwelt eines bekannten KI‑Assistenten, die App sah dadurch „nach KI“ aus.

Die dritte Fassung leitet Farbe und Form konsequent aus dem Motiv ab – **ein Laternenlicht in einem
verschneiten Wald bei Nacht** – statt aus einem Baukasten:

| Rolle | Farbe | Verwendung |
|---|---|---|
| Nacht | `#08100E`–`#0D1915` | Fensterhintergrund: Tannen‑Schwarzgrün statt Schwarz oder Navy, mit warmem Lichtschimmer und feinem Korn |
| Glas | 4–7 % Weiß, Kante 9–14 % | Flächen mit feiner Lichtkante statt flacher Karten |
| Ivory · Sage · Moss | `#EDE8DD` · `#A3AEA6` · `#6E7C74` | Text in drei Stufen, leicht grünstichig passend zum Wald |
| **Kerzenlicht** | `#E2C48D` | einziger warmer Akzent: Fokus, Fortschritt, Lob – mit sanftem Lichtschein |
| **Frost** | `#9FD3EA` | ausschließlich Ablenkung – kaltes Licht |

* **Illustration statt Kasten:** Im Zentrum der Fokus‑Seite steht eine Szene (Sternenhimmel, drei Ebenen
  Tannen, verschneiter Boden, Laternenpfahl). Der Fortschritt läuft als **Lichtring um die Laterne**;
  bei Ablenkung wird das Licht kalt und eisblau. Fallender Schnee und ein leicht atmender Lichtschein
  geben Leben – beides entfällt, wenn Windows „Animationen anzeigen“ ausgeschaltet ist.
  Die Tannenreihen sind per Skript generiert (unregelmäßig wie echte Waldkanten) und als Vektorpfade eingebettet.
* **Eigene Fensterleiste** (`WindowChrome`) statt der weißen Standard‑Titelleiste; Größenänderung,
  Andocken und Maximieren bleiben erhalten.
* **Typografie:** Überschriften in *Sitka* (moderne Serifenschrift, Bestandteil von Windows), Zahlen in
  *Segoe UI Variable Display* im leichten Schnitt (groß, ruhig, tabellarisch), Fließtext in *Segoe UI Variable Text*.
* **Details:** leuchtende Marke am aktiven Navigationseintrag, Schalter und Hauptschaltfläche in Champagner,
  dunkle Auswahllisten, Kontextmenüs und Tooltips, schmale Bildlaufleisten.
* **Positiv gestaltet:** Die rechte Spalte zeigt neben Kennzahlen und Verteilung ein **„Nächstes Ziel“**
  (z. B. „25 Minuten am Stück im Licht – noch 4:29“).
* **Barrierefreiheit:** sichtbarer Tastaturfokus (Kerzenlicht‑Rahmen), `AutomationProperties.Name` an
  Steuerelementen, reduzierte Bewegung respektiert, Kontrast Ivory auf Nacht > 14 : 1.

**Gestaltungssystem „Laternendickicht“ (aktuell, vierte Iteration).** Der Nachtwald trug die Stimmung,
erzählte aber nichts: Die Szene war Dekoration, und ihre Endlos‑Animationen kosteten Rechenzeit (siehe 6.3).
Die vierte Fassung macht aus jeder Wacht eine **Geschichte** – eine Reise durch *„Der König von Narnia“* –
und gestaltet die Oberfläche wie ein **aufgeschlagenes Buch im nächtlichen Laternenwald**: Pergamentkarten
mit Tinte tragen die Geschichte, der Nachtgrund bleibt still im Hintergrund. Die Farbrollen stehen im Kopf
von `Themes/Laternendickicht.xaml`:

| Rolle | Farbe | Verwendung |
|---|---|---|
| Nacht | `#081116` (Grund), Schein oben `#0B181E`–`#16303A` | Fensterhintergrund; Glasflächen Nachtblau `#0D1A1F` mit Goldkante |
| Ivory | `#F1E6CC` | Text auf Nacht |
| Pergament | `#F6EEDA` → `#ECDFBF` → `#DCC69A`, Rand `#A07A36` | Karten mit Innenlinie und vier Eckzierden |
| Tinte | `#2A1E14` (gedämpft `#6B5434`, weich `#4A3A28`, Goldtinte `#8A5A12`) | Text auf Pergament |
| **Laternengold** | `#E7B75F` | einziges warmes Licht: Fokus, Fortschritt, Lob, Bilderrahmen |
| **Eis** | `#9FD0EA` / `#4F92B5` | ausschließlich Ablenkung – der Winter der Königin |
| **Wachsrot** | `#9E2B1F` | Siegel (gewählte Dauer, versiegelte Chronik), Narnia |
| Frühling · Rast · Honig | `#B9D58E` · `#E08A4A` · `#F4EBDD` | Jahreszeit, Pause, Stücke Türkischer Honig |

* **Schriften:** Überschriften in *Cinzel* (römische Versalien), Zierzeilen in *Cinzel Decorative*,
  Fließtext und Zahlen in *EB Garamond*. Alle drei liegen als statische TTF‑Dateien in `Assets/Fonts/`
  und stehen unter der SIL Open Font License 1.1 (Lizenztexte daneben); fehlen sie, greifen Sitka bzw. Georgia.
* **Die Reise:** Zehn Stationen in der Reihenfolge des Buches – Laternenpfahl, Schlitten der Königin, leere
  Höhle, Biberdamm, Hof der Königin, Tauwetter, Steinerner Tisch, Morgen am Tisch, Schlacht, Cair Paravel.
  Die Station hängt allein am Anteil gemessener ÷ geplanter Zeit: **Station = 1 + ⌊9 · Anteil⌋**, Cair Paravel
  erst bei 100 %; eine Rast hält die Reise an (`Journey` im Backend). Ein Wegband aus zehn Medaillons mit der
  Laterne zeigt den Stand; jede neue Ablenkung legt ein Stück **Türkischen Honig** an die Stelle, an der sie
  begann. Die Verlockungszeile erzählt aus Edmunds Kapiteln (Stationen 1–3 der Schlitten, Station 4 der
  nächtliche Gang vom Biberdamm, ab Station 5 das trockene Brot im Schloss); die Rückkehr wird ohne Vorwurf
  empfangen, wie Edmund am Steinernen Tisch.
* **Szenenbilder statt Animation:** Sechs eigene Zeichnungen (Laternenpfahl, Schrank, Schlitten, Tauwetter,
  Cair Paravel, Steinhof) liegen als JPEG 960 × 1200 in `Assets/Szenen/`, die bearbeitbaren SVG‑Quellen in
  `docs/szenen/`. Sie stehen ruhend in einem goldenen Bogenrahmen; Ablenkung, Rast und Abwesenheit legen nur
  einen Schleier darüber (Eis, warmer Schein, Abdunkelung).
* **Vier Abschlussbilder:** vollendet im Frühling die Krönung in Cair Paravel, im Tauwetter der im Matsch
  steckende Schlitten, im Winter der Hof der Steinfiguren mit dem ersten goldenen Licht am Tor; abgebrochen
  geht es zurück durch den Schrank. Das **Wintermesser** zeigt während der Wacht, wie viel Frost noch als
  Frühling bzw. Tauwetter gilt.
* **Chronik als Verzeichnis der Bilder:** Spalten WACHT (Datum und Bildtitel, z. B. *„Die Schachtel blieb zu“*),
  REISE (kleines Wegband), FROST, HONIG, JAHRESZEIT und SIEGEL; die Kennzahl **KRONEN** zählt die Krönungen.
* **Rabenbote:** Bei *„Der König von Narnia“* spricht die Königin den Mahnruf, darunter antwortet eine Stimme
  aus Narnia – die Königin hat nie das letzte Wort.
* **Barrierefreiheit:** Wachssiegel und Buchrücken sind per Tastatur bedienbar und benannt, Einträge der Chronik
  sind per Tab erreichbar und tragen Bildtitel und Details auch für Screenreader; Text auf Pergament hält
  mindestens 4,5 : 1.

![Fokus – laufende Wacht](bilder/1-fokus-laufend.png)

*Abb.: Fokus‑Seite während einer Wacht in der Vorgängergestaltung „Nachtwald“ (Entwurfsvorschau als
HTML‑Nachbau). Mitte: Szene mit Lichtring und Restzeit; rechts: Kennzahlen, Verteilung und nächstes Ziel.
Die drei Abbildungen dieses Abschnitts werden durch Aufnahmen der Laternendickicht‑Oberfläche ersetzt,
sobald diese unter Windows vorliegen.*

![Fokus – bereit](bilder/2-fokus-bereit.png)

*Abb.: Fokus‑Seite vor dem Start mit Schnellstart und eigener Dauer (Vorgängergestaltung „Nachtwald“).*

![Fokusleiste, Botschaft und Meme](bilder/3-kapsel-botschaft-meme.png)

*Abb.: Fokusleiste aus dunklem Waldglas – oben im Fokus, darunter abgelenkt (eisblauer Schimmer).
Rechts eine Botschaft mit Initial‑Avatar und Serifen‑Zitat, links ein treibendes Meme (Vorgängergestaltung „Nachtwald“).*

**Fokusleiste (Kapsel)**

* Kapsel aus dunklem Waldglas, damit sie auf hellen wie dunklen Hintergründen trägt.
  Laternen‑Medaillon mit Fortschrittsring, große Zahl = Restzeit, Unterzeile = Kurzstatus
  (z. B. „Am Schlitten · Hearthstone“ bei Ablenkung), **Wegfaden** mit den zehn Stationen, der Laterne und
  je Ablenkung einem Stück Türkischem Honig, Chip = Frostzeit und Honigstücke. In der Rast glimmt die Kapsel
  warm, im Wegfaden steht statt der Laterne ein Feuer.
* **Vier Größen** (Einstellung `BarSize`, Rechtsklick ▸ *Größe der Leiste* oder Reiter *Gefährten & Verlockungen*,
  wirkt sofort):

  | Größe | Maße | Inhalt |
  |---|---|---|
  | Groß | Kapsel 776 × 70 | wie oben beschrieben (Standard, die ursprüngliche Leiste) |
  | Mittel | Kapsel 560 × 50 | Medaillon 36 px mit Ring, Restzeit (Cinzel 22 px), Kurzstatus 13 px (gekürzt), Wegfaden 110 px, kompakter Frost‑Chip, Knöpfe 34 px |
  | Klein | Pille 300 × 34 | Mini‑Ring (22 px) mit Zustandszeichen, Restzeit 16 px, Frost‑Chip; keine Knöpfe – Steuerung im Kontextmenü; Tooltip mit Kurzstatus, Station und nächster Station |
  | Ultradünn | Streifen 6 px (Fenster 8 px) | angedockt mit freigehaltenem Platz über die ganze Breite des Arbeitsbereichs, sonst (frei verschoben, ohne Reservierung oder wenn sie scheitert) 640 px; dunkle Spur, Füllung bis zum Fortschritt (Gold, bei Ablenkung Eisblau auf deutlich hellerer, eisgetönter Spur mit 2‑px‑Eissaum, in der Rast Bernstein), Kerben an den Stationen bei (n − 1)/9, winzige Honigstücke; Tooltip mit Restzeit, Status, Frost, Honig und Station |

  Alle Größen teilen denselben Datenkontext (`BarViewModel`). Gezeigt wird nur die Vorlage der gewählten Größe
  (`ContentTemplate` per Datentrigger) – die anderen werden gar nicht erst erzeugt. Das Fenster passt sich der
  Vorlage an (`SizeToContent`). Die ultradünne Leiste zeichnet in drei `DrawingLayers`‑Ebenen
  (`ThinJourneyStrip`); der sekündliche Fortschritt verstellt nur eine Skalierung, und auch das nur bei einem neuen
  ganzen Pixel. Ihr 2‑px‑Griffrand ist zu 1/255 gedeckt, weil ein geschichtetes Fenster völlig transparente Pixel
  zur Maus durchlässt.
* **Immer im Vordergrund** (`BarAlwaysOnTop`): Warum die Leiste früher unter Arbeitsfenster geraten konnte: WPF setzt
  `HWND_TOPMOST` nur beim Erzeugen des Fensters und bei einer Änderung von `Topmost`. Unter den „stets oben“‑Fenstern liegt
  aber immer das zuletzt aktivierte oder gezeigte vorn, und die Leiste wird nie aktiviert (`WS_EX_NOACTIVATE`) – sie kam von
  selbst nie wieder nach vorn. Beim Verbergen und Zeigen zu jeder Wacht, bei „Desktop anzeigen“, Vollbildwechseln und
  Bildschirmänderungen konnte Windows sie hinter andere Fenster schieben, ohne dass WPF davon erfuhr. Abhilfe ohne Hooks:
  `SetWindowPos(HWND_TOPMOST, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER)` beim Erzeugen, nach jedem
  Zeigen, nach Bildschirm‑ und DPI‑Wechseln und – solange die Leiste sichtbar ist – in einem ruhigen Sekundentakt nur dann,
  wenn `GetForegroundWindow()` ein anderes, gewöhnliches Fenster meldet oder das Merkmal `WS_EX_TOPMOST` verloren ging.
  Fenster, die selbst „stets oben“ sind (Startmenü, Alt+Tab, Taskansicht, Kontextmenüs), dürfen vorn bleiben, solange sie
  im Vordergrund sind. Verborgen läuft nichts. Ist die Option aus, gilt `HWND_NOTOPMOST`, und nichts wird erneut gesetzt.
* **Platz am oberen Rand freihalten** (`BarReservesSpace`, `AppBarDocking`): Ist die Leiste oben angedockt (nicht frei
  verschoben) und während einer Wacht sichtbar, meldet sie sich per `SHAppBarMessage` als Desktop‑Symbolleiste an
  (`ABM_NEW` mit eigener Rückrufnachricht aus `RegisterWindowMessage`, `ABM_QUERYPOS`/`ABM_SETPOS` mit `ABE_TOP` auf dem
  Hauptbildschirm). Reserviert wird die ganze Fensterhöhe in Gerätepixeln (DPI des Bildschirms): Groß 92, Mittel 67,
  Klein 46, Ultradünn 8 geräteunabhängige Pixel. Damit das Fenster nicht höher ist als der Streifen, ziehen die Vorlagen
  angedockt mit Reservierung (`FocusBarWindow.IsReservingTop`) den unteren Rand auf den Saum ein und zeichnen den
  Schatten ohne Versatz und mit kleinem Radius – sonst läge ein Schattenstreifen über der Titel‑ bzw. Tab‑Leiste
  maximierter Fenster und finge dort Klicks ab (ein geschichtetes Fenster nimmt jeden nicht völlig transparenten Pixel
  als Treffer). Maximierte Fenster beginnen darunter. Die Lage wird erst nach dem Layout bestimmt
  (`DispatcherPriority.Loaded`, gebündelt) und nach jeder echten Größenänderung des HWND (`WM_WINDOWPOSCHANGED` ohne
  `SWP_NOSIZE`) erneut – so zählt beim Andocken und Eingrenzen immer die tatsächliche Breite. Startet der Explorer neu,
  verwirft er alle Anmeldungen; auf die Rundmeldung `TaskbarCreated` meldet sich die Leiste neu an und setzt „stets oben“
  erneut. Eine andere Größe oder DPI wird neu ausgehandelt (`ABM_QUERYPOS`/`ABM_SETPOS` mit neuer Höhe); `ABN_POSCHANGED`
  (z. B. verschobene Taskleiste) lässt nachfragen und nur bei einem anderen Ergebnis neu setzen – so entstehen keine
  Rundmeldungs‑Schleifen. `ABN_FULLSCREENAPP` holt die Leiste nach vorn bzw. stellt sie nach dem Vollbild wieder an ihren
  Platz: Sie weicht Vollbildprogrammen bewusst nicht aus, denn ein Video im Vollbild ist genau die Ablenkung, die sie zeigen
  soll („Immer im Vordergrund“ ausschalten, wenn sie z. B. bei Präsentationen stören würde). `ABM_REMOVE` beim Verbergen,
  beim Verschieben an eine freie Stelle, beim Abschalten der Option, beim Schließen und Beenden sowie bei unbehandelten
  Ausnahmen (`AppDomain.UnhandledException`); bei einem harten Absturz räumt die Shell verwaiste Leisten selbst auf.
* **Verschiebbar:** Ziehen mit der linken Maustaste; die Position wird gespeichert. Beim Anzeigen wird die Leiste
  vollständig in den Arbeitsbereich des nächstgelegenen Bildschirms geschoben (etwa nach dem Wechsel auf eine breitere
  Größe oder wenn der Zweitbildschirm fehlt); die gespeicherte Position bleibt dabei erhalten. Rechtsklick ▸
  *Leiste zurück an den oberen Rand* setzt sie zurück.
* **Rechtsklick‑Menü:** Programm im Vordergrund als Ablenkung markieren, Buch der Chroniken wählen
  (Band 1–7 oder gemischt), Lob/Mahnrufe/Memes schalten, *Größe der Leiste* (Ultradünn/Klein/Mittel/Groß),
  *Immer im Vordergrund*, *Platz am oberen Rand freihalten*, Leiste zurücksetzen, Wacht steuern.
* `WS_EX_NOACTIVATE`: Klicks stehlen **nicht** den Tastaturfokus – sonst würde die Leiste selbst
  die Messung verfälschen. `WS_EX_TOOLWINDOW`: kein Eintrag in Alt+Tab/Taskleiste.
* Bei Ablenkung wechseln Ring, Rand, Wegfaden und Chip von Laternengold zu Eisblau, die Kapsel schimmert kalt und im Medaillon steht eine Schneeflocke.

---

## 5. Sicherheits- und Integritätskonzept

### 5.1 Schutzziele und Bedrohungsmodell

| Schutzziel | Bedrohung (STRIDE) | Beispiel |
|---|---|---|
| Integrität | **T**ampering | Benutzer schönt Frostzeiten in `chronik.jsonl` |
| Integrität | **R**epudiation | Einzelne schlechte Wachten werden gelöscht |
| Vertraulichkeit | **I**nformation Disclosure | Fremde lesen Aktivitätsdaten mit |
| Verfügbarkeit | **D**enial of Service | Beschädigte/riesige Dateien bringen die App zum Absturz |
| Rechte | **E**levation of Privilege | Anwendung fordert unnötig Adminrechte |

**Angreifermodell:** Der typische „Angreifer“ ist der Benutzer selbst oder ein Mitbenutzer, der
Dateien mit einem Texteditor verändert. Ein lokaler Administrator mit Debugger ist ausdrücklich
**nicht** im Schutzumfang (siehe 5.4).

### 5.2 Maßnahmen

| # | Maßnahme | Umsetzung | Wirkt gegen |
|---|---|---|---|
| M1 | **HMAC‑SHA256‑Siegel** je Eintrag über `Seq`, `Prev` und exakten Payload‑Text | `SessionJournal.ComputeEntryMac` | Veränderung |
| M2 | **Hash‑Kette** (`Prev` = Siegel des Vorgängers) und fortlaufende `Seq` | `SessionJournal.VerifyCore` | Löschen, Einfügen, Vertauschen |
| M3 | **Versiegelter Anker** auf den letzten Eintrag | `chronik.anker.json` | Abschneiden am Ende, Löschen der ganzen Chronik |
| M4 | **Domänentrennung** der MAC‑Eingaben (`entry\n…`, `anchor\n…`) | Präfixe | Verwechslung von Anker‑ und Eintragssiegeln |
| M5 | **Konstantzeit‑Vergleich** der Siegel | `CryptographicOperations.FixedTimeEquals` | Timing‑Seitenkanäle |
| M6 | **Schlüsselschutz mit DPAPI** (CurrentUser + anwendungsspezifische Entropie) | `DpapiSecretProtector` | Auslesen/Kopieren des Schlüssels durch andere Konten |
| M7 | **Kryptografisch zufälliger 256‑Bit‑Schlüssel** | `RandomNumberGenerator` | Erraten |
| M8 | **Beweissicherung statt Überschreiben**: gebrochene Chronik wird unverändert archiviert, danach neue Kette | `JournalBootstrapper` | Vertuschung, Datenverlust |
| M9 | **Atomares Schreiben** (Temp‑Datei + Umbenennen, `Flush(true)`) | `AtomicFile` | Halbe Dateien bei Absturz/Stromausfall |
| M10 | **Absturztoleranz des Ankers** (hinkt er genau einen gültig versiegelten Eintrag hinterher, wird er nachgezogen) | `VerifyCore` | Fehlalarm nach Absturz |
| M11 | **Strikte Eingabevalidierung**: Whitelist‑Regex für Prozessnamen, Wertebereiche, Listenlängen | `ProcessNames`, `SettingsValidator` | Pfad‑/Steuerzeicheninjektion, unsinnige Werte |
| M12 | **Sichere Deserialisierung**: quellgeneriert, kein Polymorphismus, `UnmappedMemberHandling.Disallow`, `required`‑Felder | `CoreJsonContext` | Deserialisierungsangriffe, untergeschobene Felder |
| M13 | **Größenlimits** (Einstellungen 256 KB, Zeile 16 KB, Chronik 20 MB, Anker 4 KB) | Store/Journal | Speicher‑DoS |
| M14 | **Monotone Zeitmessung** (`TimeProvider.GetTimestamp`) | `FocusSession` | Uhrmanipulation |
| M15 | **Least Privilege**: `asInvoker`, keine Hooks, nur lesende Win32‑Abfragen | `app.manifest`, `NativeMethods` | Rechteausweitung |
| M16 | **DLL‑Suchpfad auf System32 beschränkt** | `DefaultDllImportSearchPaths` | DLL‑Hijacking |
| M17 | **Datensparsamkeit**: keine Fenstertitel, keine Eingaben, Protokoll ohne Aktivitätsdaten | `Win32ActivityProbe`, `AppLog` | Offenlegung privater Inhalte |
| M18 | **Keine Netzwerkzugriffe, keine Laufzeit‑Fremdpakete** | Architektur | Datenabfluss, Lieferkettenangriffe |
| M19 | **Einzelinstanz** über benannten Mutex | `App.OnStartup` | Konkurrierende Schreibzugriffe |
| M20 | **Statische Analyse** (`AnalysisLevel latest-recommended`, Warnungen = Fehler) | `Directory.Build.props` | Fehlerklassen wie fehlendes Dispose, unsichere APIs |

### 5.3 Ablauf der Integritätsprüfung beim Start

```mermaid
sequenceDiagram
    participant App
    participant Boot as JournalBootstrapper
    participant Key as ProtectedKeyStore
    participant J as SessionJournal
    App->>Boot: Open(datenverzeichnis)
    Boot->>Key: GetOrCreateKey()
    alt Schlüssel nicht entschlüsselbar
        Boot->>Boot: Chronik + Schlüssel archivieren, neuen Schlüssel erzeugen
    end
    Boot->>J: Verify()
    J->>J: je Zeile: Seq, Prev, HMAC prüfen
    J->>J: Anker prüfen (Siegel, Seq, MAC)
    alt Siegel gebrochen
        Boot->>Boot: Chronik unverändert archivieren (inkl. Schlüssel)
        Boot->>J: neue, leere Kette
    end
    Boot-->>App: Journal + Prüfergebnis + Archivpfad
    App->>App: Siegel‑Plakette und Hinweis anzeigen
```

### 5.4 Grenzen (kritische Reflexion)

* Der Schlüssel gehört dem Benutzerkonto. Wer als **derselbe Benutzer** gezielt Code ausführt
  (z. B. ein eigenes Programm, das DPAPI aufruft), kann gültige Siegel erzeugen. Ein vollständiger
  Schutz wäre nur mit einer vertrauenswürdigen Drittpartei (Server‑Zeitstempel, TPM‑Attestierung)
  möglich – außerhalb des Projektumfangs und im Widerspruch zu N2 (keine Netzwerkzugriffe).
* **Rollback** auf eine vollständige ältere Kopie (Chronik *und* Anker) ist nicht erkennbar.
* Die Klassifikation basiert auf Prozessnamen; eine Ablenkung *innerhalb* eines erlaubten
  Programms (z. B. Videoportal im Browser) wird im milden Modus nicht erkannt – bewusst zugunsten
  des Datenschutzes (N1).

Die Chronik ist damit **manipulationserkennend** („tamper‑evident“), nicht **manipulationssicher**
(„tamper‑proof“). Für den Einsatzzweck – ehrliche Selbstbeobachtung – ist das angemessen.

---

## 6. Implementierungsphase

### 6.1 Projektstruktur

```
Succesy/
├── Laternenwacht.sln                Gesamtlösung (Backend + Frontend)
├── global.json                      SDK-Festlegung (.NET 10)
├── backend/                         BACKEND – eigenständig baubar und übergebbar
│   ├── Laternenwacht.Backend.sln
│   ├── GEMINI.md                    Arbeitsanweisung/Vertrag für KI-Assistenten
│   ├── Directory.Build.props        Qualitätsregeln (vom Frontend importiert)
│   ├── Directory.Packages.props     Zentrale Paketversionen
│   ├── src/
│   │   ├── Laternenwacht.Core/              Fachlogik (plattformunabhängig)
│   │   │   ├── Abstractions/   IActivityProbe, ISecretProtector
│   │   │   ├── Integrity/      SessionJournal, JournalBootstrapper, ProtectedKeyStore, AtomicFile
│   │   │   ├── Model/          SessionRecord, ActivityState, SessionPhase, ChronicleBook …
│   │   │   ├── Settings/       FocusSettings, SettingsValidator, SettingsStore, ProcessNames
│   │   │   └── Tracking/       FocusWarden, FocusSession, ActivityClassifier, Admonitions …
│   │   └── Laternenwacht.Platform.Windows/  Win32-Messung, DPAPI, Pfade, Protokoll (ohne UI)
│   └── tests/Laternenwacht.Core.Tests/
├── frontend/                        FRONTEND – nur Darstellung
│   ├── Directory.Build.props        importiert die Regeln des Backends
│   └── Laternenwacht.App/           WPF-Anwendung
│       ├── Assets/                  Anwendungssymbol
│       ├── Properties/PublishProfiles/   Veröffentlichungsprofile (eigenständig / schlank)
│       ├── Services/                Erzähltexte (Lore), Rabenbote, Fensterstile
│       ├── Themes/Laternendickicht.xaml  Gestaltungssystem
│       ├── ViewModels/              MVVM
│       └── Views/                   Hauptfenster, Seiten, Wegband, Szenenrahmen, Fokusleiste (vier Größen,
│                                    ThinJourneyStrip, AppBarDocking), Rabenbote, Meme, RenderCache (Ressourcenschonung)
├── docs/                            Diese Dokumentation, Veröffentlichungsanleitung
└── .github/workflows/build.yml      CI: Build, Test, EXE-Artefakt
```

**Trennung von Frontend und Backend:** Das Backend kennt das Frontend nicht und lässt sich ohne
den Rest des Repositorys bauen und testen (`backend/Laternenwacht.Backend.sln`). Das Frontend
referenziert ausschließlich die beiden Backend-Projekte. Die öffentliche Schnittstelle, die das
Frontend nutzt, ist in `backend/GEMINI.md` (Abschnitt „Vertrag mit dem Frontend“) festgehalten;
dadurch kann das Backend an andere Entwickler oder KI-Assistenten übergeben werden, ohne dass
versehentlich das Frontend bricht.

### 6.2 Ausgewählte Implementierungsdetails

**Zeitverbuchung ohne Drift** (`FocusSession.Accumulate`): Statt pro Tick pauschal „+1 s“ zu
zählen, wird die tatsächlich vergangene monotone Zeit gemessen. Verzögerte Timer‑Ticks (z. B. bei
hoher Last) führen so nicht zu Ungenauigkeiten; Messlücken > 30 s (Standby) werden als
Abwesenheit gewertet.

```csharp
var delta = _time.GetElapsedTime(_lastTimestamp, now);
var credited = delta < remaining ? delta : remaining;          // nie über die geplante Dauer
var state = delta > MaxTickGap ? ActivityState.Away : CurrentState;
```

**Versiegeln eines Eintrags** (`SessionJournal.Append`): Der Payload wird *einmal* serialisiert und
als exakter Text gespeichert. Dadurch ist die Prüfung byte‑genau reproduzierbar, unabhängig von
künftigen Änderungen an Serialisierungsoptionen.

```csharp
var payload = JsonSerializer.Serialize(record, CoreJsonContext.Default.SessionRecord);
var entry = new JournalEntry(seq, _lastMac, payload, ComputeEntryMac(seq, _lastMac, payload));
```

**Mahnrufe** (`Admonitions.Next`): Schwellen nach Anzahl der Verlockungen (3, 5, 10, 15, **20**, 30, 50)
und nach Frostminuten (5, 10, 20, 45). Jeder Spruch erscheint höchstens einmal je Wacht;
übersprungene Schwellen werden nicht nachgereicht, damit keine Spruchkaskade entsteht.
Jedes der sieben Bücher besitzt einen eigenen Satz von elf Sprüchen (eine je Schwelle). Im Modus
„Alle Chroniken“ wählt ein je Wacht zufälliger Startwert für jede Schwelle ein anderes Buch.

**Rabenbote** (`RavenToastWindow`, `NotificationService`): Eigene Push‑Benachrichtigung statt
Windows‑Toast. Begründung: Windows‑Toasts erfordern für nicht paketierte Anwendungen eine
registrierte App‑ID mit Startmenü‑Verknüpfung (zusätzliche Installationsschritte und
Registry‑Schreibzugriffe), außerdem passt die Darstellung nicht zur Gestaltung. Der Rabenbote
aktiviert sich nie (`WS_EX_NOACTIVATE`), verschwindet nach 10 s, pausiert bei Mausberührung und
ersetzt eine noch sichtbare ältere Botschaft.

**Positive Verstärkung** (Backend: `FocusSession.CurrentStreak/LongestStreak`, `FocusWarden.ReturnedToWork`,
`FocusWarden.FocusStreakReached`, `Homecomings`, `Praises`): Die App erkennt nicht nur Ablenkung,
sondern vor allem, was gut läuft. Eine Fokus‑Serie wächst nur im Fokus und wird ausschließlich durch
eine Ablenkung zurückgesetzt – kurze Abwesenheit (Nachdenken, Telefonat) bestraft also nicht. Lob kommt
leise (ohne Ton) und mit grünem Siegel, damit es die Konzentration nicht selbst unterbricht; je Serie
wird jede Schwelle nur einmal gewürdigt. Die längste Serie wird als optionales Feld `LongestFocusStreak`
in der Chronik gespeichert – bestehende Einträge bleiben gültig versiegelt, weil das Siegel über den
gespeicherten Text gebildet wird.

**Schwimmende Memes** (`MemeService`, `MemeFloatWindow`, Backend: `FocusWarden.DistractionStarted`,
`ShuffleBag<T>`, `MemeCatalog`): Das Backend meldet jede *neue* Ablenkungs‑Episode genau einmal.
Das Frontend wählt per „Wundertüte“ ein Meme (nie dasselbe zweimal hintereinander) und lässt ein
kleines, nicht aktivierendes Fenster zeitbasiert über den Arbeitsbereich treiben (Sinuswelle +
leichtes Schaukeln). Bewusst kein bildschirmgroßes transparentes Fenster – das wäre bei hohen
Auflösungen teuer. Es treibt höchstens ein Meme gleichzeitig, damit schnelles Hin‑ und Herwechseln
den Bildschirm nicht flutet. Eigene Bilder werden defensiv eingelesen (nur Bildendungen, oberste
Ordnerebene, keine Verknüpfungen, max. 10 MB, max. 200 Dateien) und mit begrenzter Auflösung
dekodiert; unlesbare Dateien werden übersprungen und protokolliert. Eingebaute Memes wurden
verkleinert und ohne Metadaten (EXIF, ggf. GPS) gespeichert.

**Sofort wirkende Einstellungen** (`SettingsViewModel.ApplyQuickChange`): Buchwahl, Benachrichtigungen,
Leistenposition, Leistengröße, „Immer im Vordergrund“ und „Platz am oberen Rand freihalten“ werden ohne „Speichern“
übernommen – ungespeicherte Eingaben im Formular bleiben dabei unberührt. `App` reagiert in `SettingsSaved` nur mit
`FocusBarWindow.ApplyPosition` und `ApplyBarSettings`; eine laufende Wacht bleibt unberührt.

**Fokusleiste ohne Fokusraub** (`WindowStyles.MakeNonActivatingToolWindow`): Erweiterte Fensterstile
`WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW` werden nach Erzeugung des nativen Fensters gesetzt und per
`SetWindowPos(SWP_FRAMECHANGED)` wirksam gemacht. `WindowStyles.SetTopmost` setzt die Z‑Reihenfolge
(`HWND_TOPMOST`/`HWND_NOTOPMOST`) ohne Verschieben oder Aktivieren.

### 6.3 Ressourcenschonung (Leistungsoptimierung, Version 1.6)

Eine Fokushilfe läuft stundenlang nebenher. Sie darf weder den Lüfter anwerfen noch den Akku leeren
(Anforderung N9). Für Version 1.6 wurde daher systematisch vorgegangen: Zuerst wurde ermittelt,
**wann** die Anwendung überhaupt Arbeit verrichtet (Kostentreiber). Danach wurde jeder Treiber beseitigt
oder auf den Moment beschränkt, in dem tatsächlich jemand hinsieht.

**Analyse der Kostentreiber (Version 1.5)**

| Kostentreiber | Warum teuer |
|---|---|
| Endlos‑Animationen der Nachtwald‑Szene (26 Schneeflocken mit je 2 Animationen, Atmen, Funkeln) | WPF berechnet laufende Animationen auch für **minimierte oder verdeckte** Fenster weiter, bis zu 60‑mal je Sekunde, die ganze Wacht lang. |
| Flackernde Flamme in der Fokusleiste | Die Leiste ist ein transparentes (geschichtetes) Fenster. Jede sichtbare Änderung lässt es neu zeichnen, einschließlich zweier großer Schatteneffekte. Das Ergebnis wird anschließend von der Grafikkarte in den Hauptspeicher zurückkopiert. |
| Schatteneffekte (`DropShadowEffect`) auf Elementen mit sekündlich wechselndem Inhalt | Effekte sind Pixel‑Shader über die ganze Fläche. Jede Änderung im Inneren erzwingt ihre Neuberechnung. |
| Meme‑Bewegung über `CompositionTarget.Rendering` | Solange ein Handler angemeldet ist, zeichnet WPF ununterbrochen mit voller Bildwiederholrate. |
| Restzeit‑Balken der Botschaft als `ProgressBar` | Jeder Animationsschritt löst einen Layout‑Durchlauf aus. |
| Sekündliche Prozessabfrage über `Process.GetProcessById(…).ProcessName` | Erzeugt jede Sekunde ein neues `Process`‑Objekt samt Systemhandle. Je nach .NET‑Version fordert sie dafür sogar eine Momentaufnahme **aller** laufenden Prozesse an. |
| Sekundentakt im ViewModel | Rund 25 Änderungsmeldungen je Sekunde, auch für unveränderte Werte, plus eine Neuabfrage aller Befehle (`CommandManager.InvalidateRequerySuggested`). |
| Chronik‑Liste ohne Virtualisierung | Für jeden Eintrag werden sämtliche Elemente erzeugt. Der Aufwand wächst mit jeder Wacht. |

**Maßnahmen**

| Maßnahme | Umsetzung | Wirkung |
|---|---|---|
| Bewegung nur, wenn jemand hinsieht | `AmbientMotion` startet Szenen‑Animationen nur, wenn das Element sichtbar und sein Fenster aktiv und nicht minimiert ist. Andernfalls werden sie mit `Storyboard.Pause` angehalten. Die Windows‑Einstellung „Animationen anzeigen“ wird beachtet. | Während einer Wacht (man arbeitet in anderen Programmen) ruht die Szene vollständig. Seit dem Gestaltungssystem „Laternendickicht“ gibt es keine Endlos‑Animationen mehr: Die Szenen sind ruhende, eingefrorene Bilder, `AmbientMotion` und die animierte Nachtwald‑Szene sind entfallen. |
| Weniger, gebündelte Animationen | Der Schnee besteht aus 3 Tiefenebenen mit je *einer* Geometrie statt aus 26 Einzelelementen. Jede Ebene enthält ihre Flocken doppelt, um eine Szenenhöhe versetzt, damit die Endlosschleife nahtlos schließt. | 6 statt 52 Animationsuhren, weniger Objekte. |
| Gedrosselte Bildraten | Global gelten 30 statt 60 Bilder je Sekunde (`Timeline.DesiredFrameRate`), für den Schnee 24, das Atmen 15 und den Restzeit‑Balken 20. | Halbe bis Viertel‑Last, solange etwas animiert wird. |
| Ruhige Fokusleiste | Die Flamme flackert nicht mehr dauerhaft (Bewegung im Augenwinkel lenkt zudem ab). Schatten und Kapselkörper liegen in einer zwischengespeicherten Ebene (`RenderCache` → `BitmapCache` in Bildschirmauflösung). | Pro Sekunde wird nur noch der geänderte Text neu gezeichnet. |
| Lichtschein ohne Effekt | `ProgressRing.Glow` zeichnet zwei breitere, blasse Striche statt eines `DropShadowEffect`. | Kein Pixel‑Shader bei jeder Fortschrittsänderung. |
| Meme mit eigenem Takt | `DispatcherTimer` mit 30 Hz statt `CompositionTarget.Rendering`. Karte und Schatten werden als Bitmap zwischengespeichert, die Neigung auf ¼° gerundet. | Das Verschieben des Fensters kostet fast nichts. Neu gezeichnet wird nur bei sichtbarer Änderung. |
| Restzeit‑Balken als Transformation | `ScaleTransform` statt `ProgressBar.Value`. | Kein Layout‑Durchlauf pro Bild. |
| Sparsame Prozessabfrage | Das Ergebnis wird je Vordergrundfenster zwischengespeichert, denn ein Fenster gehört zeitlebens demselben Prozess. Neue Abfragen laufen über `OpenProcess` (minimales Leserecht) und `QueryFullProcessImageName`; nur in Sonderfällen wird auf `Process` zurückgegriffen. | Im Normalfall keine Systemabfrage im Sekundentakt, sondern nur beim Fensterwechsel. |
| Nur echte Änderungen melden | Alle Anzeigewerte sind gespeichert und melden sich nur bei Änderung. Befehle werden nur beim Zustandswechsel neu bewertet. | Typisch 10–12 statt rund 25 Meldungen je Sekunde, keine sekündliche Befehlsabfrage. |
| Virtualisierte Chronik | `VirtualizingStackPanel` mit Recycling. | Der Speicherbedarf hängt nicht mehr von der Anzahl der Wachten ab. |
| Speicher zurückgeben | `MemoryRelief`: Ist das Hauptfenster 3 s minimiert, folgen ein kompaktierender GC‑Lauf und das Leeren des Arbeitssatzes. | Geringerer Arbeitsspeicher, solange die Anwendung im Hintergrund wacht. |
| Laufzeiteinstellungen | `ConcurrentGarbageCollection=false` (kein GC‑Hintergrundthread), `TieredPGO=false` (keine Profilierungs‑Instrumentierung), ReadyToRun (vorübersetzt). | Weniger Threads, weniger JIT‑Arbeit beim und nach dem Start. |
| Schlanke Auslieferung | Profil `Win-x64-Schlank`: ~4 MB statt ~67 MB. Es nutzt die installierte .NET‑10‑Desktop‑Laufzeit. | Weniger Festplattenplatz. Die Laufzeit liegt nur einmal auf dem Rechner und wird von allen .NET‑Programmen gemeinsam genutzt. |

**Abwägungen und Grenzen**

* *Trimming* (Entfernen ungenutzten Codes) unterstützt WPF nicht. Es bleibt deaktiviert.
* Die **eigenständige** EXE bleibt komprimiert. Unkomprimiert wäre sie ~148 MB statt ~67 MB groß, und ein
  Vorteil beim Arbeitsspeicher ließ sich nicht sicher belegen. Die sparsamste Variante ist die schlanke EXE.
* Das Leeren des Arbeitssatzes senkt den angezeigten Arbeitsspeicher sofort; benötigte Seiten lädt Windows
  bei Bedarf nach. Deshalb geschieht es nur einmal beim Minimieren, nie periodisch.
* Bitmap‑Zwischenspeicher wirken nur bei Hardware‑Darstellung. Bei Software‑Darstellung (z. B. Remotedesktop)
  zeichnet WPF wie zuvor, nur ohne den Vorteil.
* Der **Datenschutz** bleibt unverändert: Vom Programmpfad wird nur der Dateiname verwendet, nichts davon gespeichert
  (`ProcessNames.FromImagePath`, mit Unit‑Tests belegt).

**Nachweis.** Die Wirkung lässt sich an zählbaren Größen festmachen (Codeanalyse; Zustand: Wacht läuft,
Hauptfenster im Hintergrund):

| Kennzahl | Version 1.5 | Version 1.6 |
|---|---|---|
| Laufende Animationsuhren | 56 (52 Schnee, 3 Atmen, 1 Flamme) | 0 |
| Neuzeichnungen der Fokusleiste | bis 60 je Sekunde (Flamme) | 1 je Sekunde (Zahlenwechsel) |
| Neu berechnete Schatteneffekte in der Leiste | Kapsel‑ und Ringschatten bei jedem Bild | keine (Schatten aus dem Zwischenspeicher, Ringschein ohne Effekt) |
| Prozessabfragen beim System | 1 je Sekunde (`Process`‑Objekt mit Handle) | 0, solange dasselbe Fenster vorne ist; beim Fensterwechsel eine Abfrage |
| Änderungsmeldungen im ViewModel | rund 25 je Sekunde + Befehlsabfrage | nur geänderte Werte (typisch 10–12) |

Die Laufzeitwerte werden auf dem Zielsystem gemessen: Im Task‑Manager unter *Details* die Spalten
„CPU‑Zeit“, „Arbeitsspeicher (privater Arbeitssatz)“ und „GPU“ einblenden, eine 25‑Minuten‑Wacht mit
Version 1.5 und 1.6 laufen lassen und vergleichen. Genauer geht es mit
`dotnet-counters monitor -n Laternenwacht` (CPU, GC‑Heap, Arbeitssatz). Siehe auch die Testfälle T24–T28.

---

## 7. Qualitätssicherung

### 7.1 Automatisierte Tests

281 Unit‑Tests (xUnit) für die Fachlogik, u. a.:

| Testklasse | Geprüft wird |
|---|---|
| `FocusSessionTests` | Verbuchung auf Vorzustand, Episodenzählung, exaktes Ende, Pausen, Standby‑Lücken, **Uhrmanipulation**, Abbruch |
| `ActivityClassifierTests` | Beide Modi, Groß‑/Kleinschreibung, `.exe`, Leerlauf, unbekannter Vordergrund |
| `FocusWardenTests` | Ereignis „Wacht beendet“ genau einmal, keine Doppelstarts, Einstellungen wirken sofort |
| `SessionJournalTests` | **Veränderung, Löschung, Vertauschung, Abschneiden, gefälschter Anker, falscher Schlüssel, Müllzeilen**, Absturz‑Reparatur |
| `JournalBootstrapperTests` | Erststart, Schlüssel nie im Klartext, Archivierung gebrochener Chroniken, defekter Schlüssel |
| `SettingsTests` | Wertebereiche, Überschneidungen, Normalisierung, **verdächtige Namen** (Pfade, Nullbytes), Round‑Trip, **beschädigte Dateien**, Prozessname aus dem Programmpfad, **Leistengröße und „Platz freihalten“** (Standardwerte, fehlende Felder, Round‑Trip als Text, stabile Namen, unbekannte Werte) |
| `HomecomingTests`, `PraiseTests` | Abwesenheitsstufen, Riepiepich‑Gruß, vollständige Spruchsätze je Buch und Schwelle |
| `KnownDistractionTests` | Hearthstone & Co. ab Werk erkannt, Gefährten haben Vorrang, Katalog abschaltbar, Markieren ohne Duplikate |
| `ShuffleBagTests` | Jedes Element einmal je Durchgang, nie zweimal hintereinander, Sonderfälle leer/einzeln |
| `MemeCatalogTests` | Nur Bildendungen, sortiert, leere Dateien und Unterordner ignoriert, Anzahlgrenze |
| `AdmonitionTests` | Schwellen, Einmaligkeit, kein Nachreichen, **Cair‑Paravel‑Mahnruf bei 20**, vollständige und eindeutige Spruchsätze je Buch, gewähltes Buch wird genutzt, gemischter Modus |
| `FormattingTests` | Zeitformat, Jahreszeiten‑Grenzen |

Zeitabhängige Tests laufen mit einer **manuell vorgestellten Uhr** (`ManualTimeProvider`) und sind
damit deterministisch und schnell (< 1 s gesamt).

### 7.2 Statische Analyse und CI

* `TreatWarningsAsErrors`, `Nullable enable`, `AnalysisLevel latest-recommended`, Code‑Stil im Build.
* GitHub Actions (`windows-latest`): Restore → Build (Release) → Test → Publish → EXE als Artefakt.

### 7.3 Manueller Testplan (Auszug)

| Nr. | Schritt | Erwartung |
|---|---|---|
| T1 | Wacht mit 1 min starten, nur in Visual Studio arbeiten | Leiste gold, Frost 00:00, nach 1 min „Der Frühling ist gekommen!“ |
| T2 | Während der Wacht Discord in den Vordergrund holen | Rahmen eisblau, Frost zählt hoch, Detail „Eine Verlockung ruft: discord“ |
| T3 | Buch „Der König von Narnia“ wählen, 20‑mal zwischen Editor und Verlockung wechseln | Rabenbote unten rechts: die Königin („Zwanzig Stück! …“) mit Peters Antwort |
| T4 | Auf die Leiste klicken | Vorheriges Fenster behält den Tastaturfokus |
| T5 | 2 min keine Eingabe | „Die Laterne wacht allein“, Zeit unter „abwesend“ |
| T6 | Systemuhr während der Wacht um 1 h verstellen | Restzeit unverändert |
| T7 | `chronik.jsonl` im Editor ändern, App neu starten | Plakette „Das Siegel ist gebrochen!“, Archivhinweis, neue Chronik |
| T8 | Zweite Instanz starten | Hinweis „Die Laternenwacht brennt bereits“ |
| T9 | Ungültigen Namen `C:\x.exe` als Verlockung speichern | Fehlermeldung, nichts gespeichert |
| T10 | Anzeige mit 150 % / 200 % Skalierung | Leiste und Symbole scharf, oben zentriert |
| T11 | Leiste mit der Maus verschieben, App neu starten | Leiste erscheint an der neuen Stelle, alle Ecken rund |
| T12 | Rechtsklick ▸ *Leiste zurück an den oberen Rand* | Leiste dockt oben zentriert an |
| T29 | Wacht starten, ein Arbeitsfenster maximieren und mehrfach zwischen Programmen wechseln (auch Win+D, Alt+Tab, zweite Wacht) | Leiste bleibt sichtbar über den Arbeitsfenstern; Startmenü und Alt+Tab liegen kurz darüber |
| T30 | „Platz am oberen Rand freihalten“ an, Fenster maximieren | Fenster beginnt direkt unter der Leiste; nach dem Ende der Wacht (Leiste verschwindet) füllt es wieder den ganzen Bildschirm |
| T31 | Rechtsklick ▸ *Größe der Leiste* ▸ Ultradünn / Klein / Mittel / Groß während einer Wacht | Leiste wechselt sofort, bleibt oben zentriert (ultradünn mit freigehaltenem Platz über die ganze Breite); reservierter Streifen passt sich der Höhe an, unter der Leiste lässt sich die Tab‑ bzw. Titelleiste maximierter Fenster auch in der Mitte anklicken |
| T32 | Ultradünne Leiste nach unten ziehen, Rechtsklick, Tooltip | Streifen wird 640 px breit, Platz oben wird freigegeben; Kontextmenü und Tooltip (Restzeit, Status, Frost, Honig, Station) funktionieren |
| T33 | Leiste an den rechten Rand ziehen, Größe „Groß“ wählen | Leiste bleibt vollständig auf dem Bildschirm |
| T13 | Rechtsklick ▸ *Sprüche aus dem Buch* ▸ *Der silberne Sessel* | Häkchen wandert, nächster Mahnruf stammt aus diesem Buch |
| T14 | Benachrichtigungen per Rechtsklick abschalten, Schwelle erreichen | Kein Rabenbote; Mahnruf nur im Hauptfenster |
| T15 | Auf den Rabenboten klicken, während in einem Editor getippt wird | Botschaft verschwindet, Editor behält den Fokus |
| T16 | Während der Wacht zu einer Verlockung wechseln | Ein Meme treibt schaukelnd über den Bildschirm, Beschriftung „Verlockung Nr. 1: …“ |
| T17 | Auf das treibende Meme klicken | Es versinkt; das aktive Programm behält den Fokus |
| T21 | 11 Minuten ohne Ablenkung arbeiten | Leise grüne Botschaft „10 Minuten am Stück im Licht“, Leiste: „Seit 11:00 ununterbrochen im Licht – stark!“ |
| T22 | 15 Minuten in Hearthstone, dann zurück in den Editor (Buch: *Die Reise auf der Morgenröte*) | „Willkommen zurück im Licht“ – „Kaspian hat lange auf dich gewartet …“ — Riepiepich; treibendes Meme versinkt |
| T23 | Wacht ohne eine einzige Ablenkung vollenden | Würdigung „Makellose Wacht“, ggf. „Neue Bestleistung“ |
| T19 | Wacht starten, Hearthstone in den Vordergrund holen | Frost zählt hoch, „Eine Verlockung ruft: Hearthstone“ |
| T20 | Unbekanntes Spiel im Vordergrund, Rechtsklick auf die Leiste ▸ *„… als Verlockung markieren“* | Ab sofort Frost; Eintrag erscheint in der Liste der Verlockungen |
| T18 | *Memes hinzufügen …*, eine PNG‑ und eine TXT‑Datei wählen | PNG wird übernommen, TXT übersprungen und gemeldet |
| T24 | Wacht starten, in einem anderen Programm arbeiten (Hauptfenster bleibt offen, aber im Hintergrund) | Task‑Manager: Laternenwacht bei ≈ 0 % CPU und GPU; Schnee steht still und fällt weiter, sobald das Fenster vorne ist |
| T25 | Hauptfenster minimieren, 5 s warten | Privater Arbeitssatz sinkt deutlich; Leiste zählt unverändert weiter |
| T26 | Leiste auf einen Bildschirm mit anderer Skalierung (z. B. 100 % → 150 %) ziehen | Kapselrand, Schatten und Schrift bleiben scharf |
| T27 | Windows ▸ Barrierefreiheit ▸ *Animationseffekte* aus, App neu starten | Szene steht still, alle Funktionen unverändert |
| T28 | Schlanke EXE auf einem Rechner ohne .NET‑10‑Desktop‑Laufzeit starten | Windows meldet die fehlende Laufzeit und bietet den Download an; mit Laufzeit startet die App normal |

---

## 8. Abnahme und Einführung

Die Auslieferung erfolgt als **einzelne EXE** in zwei Varianten: **eigenständig** (~67 MB, keine
Installation und keine .NET‑Laufzeit auf dem Zielsystem nötig) oder **schlank** (~4 MB, nutzt die
installierte .NET‑10‑Desktop‑Laufzeit und ist die ressourcenschonendste Variante). Die Schritte in Visual Studio 2026 beschreibt
[`Veroeffentlichung-VS2026.md`](Veroeffentlichung-VS2026.md). Einstellungen und Chronik liegen im
Benutzerprofil; eine Deinstallation besteht aus dem Löschen der EXE und des Ordners
`%LOCALAPPDATA%\Laternenwacht`.

Frontend und Backend werden dabei über Projektverweise zu einer Anwendung verbunden und gemeinsam
in die EXE gepackt. Für die Auslieferung stehen drei Wege bereit: das Skript `Veroeffentlichen.cmd`
(Tests + Veröffentlichung per Doppelklick), das Veröffentlichungsprofil in Visual Studio und – für die
Weitergabe an andere – ein GitHub‑Release, das ein Versions‑Tag (`v1.6.0`) automatisch mit beiden EXE‑Varianten
und ihren SHA‑256‑Prüfsummen erzeugt.

---

## 9. Fazit und Ausblick

### 9.1 Soll‑Ist‑Vergleich

Alle Muss‑ und Soll‑Anforderungen (F1–F13, N1–N8) wurden umgesetzt. Die Fachlogik ist vollständig
von der Oberfläche entkoppelt und automatisiert getestet.

### 9.2 Lessons Learned

* Monotone Zeitquellen und eine injizierbare Uhr (`TimeProvider`) machen Zeitlogik sowohl korrekt
  als auch testbar – ein früher Architekturentscheid, der sich mehrfach bezahlt machte.
* Integrität braucht mehr als eine Prüfsumme: Erst Kette **und** Anker decken alle Manipulationsarten ab.
* Eine konsequente Metaphernwelt ersetzt Erklärtexte – die Bedienung wird intuitiv.
* Leistung entsteht vor allem durch Weglassen: Die größten Einsparungen kamen nicht von schnellerem Code,
  sondern davon, Arbeit nur dann zu verrichten, wenn jemand hinsieht. In WPF sind transparente Fenster und
  Effekte teuer – was ständig sichtbar ist, muss ruhig sein.

### 9.3 Ausblick

* Signierte Releases und automatische Updates (z. B. über MSIX/WinGet).
* Optionaler externer Zeitstempeldienst (RFC 3161) für stärkere Integrität.
* Wochen‑ und Monatsauswertung als Diagramm; Export (CSV/PDF) für Lerntagebücher.
* Andocken an den oberen Rand eines beliebigen Bildschirms (derzeit Hauptbildschirm; frei verschoben
  funktioniert die Leiste bereits auf allen Bildschirmen).
* Optional echte Windows‑Benachrichtigungen (Info‑Center) bei MSIX‑Paketierung.

---

## 10. Anhang

* **A1** Benutzer‑Kurzanleitung: siehe [`README.md`](../README.md)
* **A2** Veröffentlichung: [`Veroeffentlichung-VS2026.md`](Veroeffentlichung-VS2026.md)
* **A3** Quellcode: Backend `backend/src/`, Tests `backend/tests/`, Frontend `frontend/`
* **A4** Übergabe‑Anweisung für das Backend: [`backend/GEMINI.md`](../backend/GEMINI.md)

> **Hinweis zur Gestaltung:** Die Laternenwacht ist eine nicht‑kommerzielle Hommage an die
> Atmosphäre der Chroniken von C. S. Lewis. Sie steht in keiner Verbindung zu den Rechteinhabern;
> alle Texte sind eigene Formulierungen.
