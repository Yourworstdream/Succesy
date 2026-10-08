# Succesy · Laternenwacht 🏮

> *„Solange die Laterne brennt, findet man den Weg zurück.“*

**Laternenwacht** ist ein Fokuswächter für Windows. Während einer Fokuszeit („Wacht“) hängt am
**oberen Bildschirmrand** ein schmales Banner, das dir live zeigt, **wie lange du dich bereits
ablenkst** – als *Frost*, der über das Licht der Laterne kriecht. Die Texte und Figuren stammen aus
der Welt der Chroniken von Narnia (C. S. Lewis): Jede Wacht ist eine Reise durch *„Der König von
Narnia“* – vom Laternenpfahl bis nach Cair Paravel. Gestaltet als **„Laternendickicht“**: Pergament und
Tinte auf nächtlichem Grund, Laternengold für Fokus, Eisblau nur für Ablenkung, rote Wachssiegel,
goldgerahmte Szenenbilder und die Schriften Cinzel und EB Garamond.

![Laternenwacht – Fokus](docs/bilder/1-fokus-laufend.png)

![Fokusleiste, Botschaft und Meme](docs/bilder/3-kapsel-botschaft-meme.png)

<sub>Hinweis: Die beiden Bildschirmfotos zeigen noch die Vorgängergestaltung „Nachtwald“. Sie werden ersetzt,
sobald Aufnahmen der Laternendickicht‑Oberfläche unter Windows vorliegen.</sub>

## Funktionen

* **Die Reise** – jede Wacht folgt *„Der König von Narnia“* in der Reihenfolge des Buches, in zehn
  Stationen: Laternenpfahl, Schlitten der Königin, leere Höhle, Biberdamm, Hof der Königin, Tauwetter,
  Steinerner Tisch, Morgen am Tisch, Schlacht, Cair Paravel. Die Station hängt nur an gemessener ÷
  geplanter Zeit (Station = 1 + ⌊9 · Anteil⌋, Cair Paravel erst bei 100 %); eine **Rast** hält die Reise an.
  Ein Wegband mit zehn Medaillons und der Laterne zeigt, wie weit du gekommen bist.
* **Türkischer Honig** – jede neue Ablenkung ist ein Stück aus der Schachtel der Königin und bleibt als
  Würfel an der Stelle des Wegbands liegen, an der sie begann. Die Verlockungszeile erzählt aus Edmunds
  Kapiteln (Schlitten, nächtlicher Gang vom Biberdamm, trockenes Brot im Schloss). Wer zurückkommt, wird
  empfangen wie Edmund am Steinernen Tisch – ohne Vorwurf.
* **Wintermesser** – zeigt, wie viel Frost diese Wacht noch als Frühling oder Tauwetter verträgt.
* **Vier Abschlussbilder** – vollendet im Frühling: die Krönung in Cair Paravel; im Tauwetter: der
  Schlitten der Königin bleibt im Matsch stecken; im Winter: der Hof der Steinfiguren mit dem ersten
  goldenen Licht am Tor; abgebrochen: zurück durch den Schrank.
* **Fokusleiste oben am Bildschirm** – Restzeit, kumulierte Ablenkungszeit (❄ Frost), Anzahl der
  Verlockungen, Fortschritt; stiehlt beim Anklicken keinen Fokus.
* **Vier Leistengrößen** – *Groß* (die volle Kapsel), *Mittel* (kompakte Kapsel mit Wegfaden und Knöpfen),
  *Klein* (schmale Pille mit Restzeit und Frost, Steuerung per Rechtsklick) und *Ultradünn* (ein haarfeiner
  Streifen mit Fortschritt, Stationskerben und Honigstücken – angedockt mit freigehaltenem Platz über die ganze
  Bildschirmbreite, sonst 640 px breit; Einzelheiten im Tooltip). Wechsel jederzeit per Rechtsklick ▸ *Größe der Leiste* oder im Reiter *Gefährten & Verlockungen* –
  auch mitten in einer Wacht.
* **Immer im Vordergrund** – die Leiste bleibt über den Arbeitsfenstern: Nach jedem Zeigen und wenn ein anderes
  Programm nach vorn kommt, setzt sie sich wieder an die Spitze (ohne Hooks, nur eine billige Abfrage pro Sekunde,
  solange sie sichtbar ist). Abschaltbar.
* **Platz am oberen Rand freihalten** – oben angedockt meldet sich die Leiste bei Windows wie die Taskleiste als
  Desktop‑Symbolleiste an: Maximierte Fenster beginnen darunter, nichts wird verdeckt. Der Streifen ist genau so
  hoch wie die Leiste (angedockt ohne nach unten fallenden Schatten, damit nichts darüber hinausragt) und wird beim
  Verbergen, Verschieben oder Beenden sofort zurückgegeben; nach einem Neustart des Explorers meldet sich die Leiste
  von selbst neu an. Abschaltbar.
* **Verschiebbar** – Leiste mit der Maus an jede Stelle ziehen; die Position wird gespeichert und bleibt
  vollständig auf dem Bildschirm.
* **Rechtsklick auf die Leiste** – wählen, aus welchem Buch der Chroniken von Narnia die Sprüche
  stammen (Band 1–7 oder alle gemischt), Benachrichtigungen an/aus, Größe der Leiste, Vordergrund,
  Platz freihalten, Leiste zurück an den Rand.
* **Bekannte Verlockungen** wie Hearthstone, Battle.net, Steam, League of Legends, Minecraft oder
  Discord werden ab Werk erkannt; jedes andere Programm per Rechtsklick auf die Leiste ▸
  *„… als Verlockung markieren“*.
* **Ablenkungserkennung** über das Vordergrundprogramm – *Milde Wacht* (Sperrliste) oder
  *Strenge Wacht* (nur Erlaubnisliste zählt als Fokus); Abwesenheit wird separat erfasst.
* **Positives sehen** – die App erkennt, was gut läuft: Lob nach 10, 25, 45, 60 und 90 Minuten
  am Stück, „Willkommen zurück“ nach einer Ablenkung (z. B. *„Kaspian hat lange auf dich gewartet.
  Wenn es nach mir ginge, wären wir ohne dich losgesegelt.“ — Riepiepich*), Würdigung makelloser
  Wachten und neuer Bestleistungen. Lob kommt leise mit grünem Siegel; Mahnrufe lassen sich getrennt
  abschalten.
* **Schwimmende Memes** – bei jeder neuen Ablenkung treibt ein Meme aus der Honigschachtel schaukelnd quer über den
  Bildschirm („Treibgut im Fluss der Ablenkung“); ein Klick lässt es versinken. Eigene Bilder per
  Rechtsklick ▸ *Memes hinzufügen …* oder im Reiter *Gefährten & Verlockungen*.
* **Mahnrufe per Push‑Benachrichtigung** – ein „Rabenbote“ fliegt unten rechts ein. Bei
  *Der König von Narnia* lockt die Königin, und eine Stimme aus Narnia antwortet, z. B. nach
  20 Ablenkungen: *„Zwanzig Stück! Honig gibt es ab jetzt keinen mehr, nur trockenes Brot. Bleibst du trotzdem?“* –
  *„Genau so ging es Edmund. Was sie verspricht, hält sie nicht.“ — Peter*
  Jedes der sieben Bücher hat eigene Sprüche (77 insgesamt).
* **Jahreszeiten** als Ergebnis: Frühling (< 10 % Frost), Tauwetter (< 25 %), Winter.
* **Versiegelte Chronik** – jede Wacht wird mit HMAC‑SHA256 versiegelt und verkettet;
  Manipulation, Löschung oder Vertauschung wird erkannt und angezeigt. Jeder Eintrag trägt den Titel
  seines Bildes (z. B. *„Die Schachtel blieb zu“* oder *„Zurück durch den Schrank · Station 6“*),
  dazu Reise, Frost, Honig und Jahreszeit.
* **Sicherheit & Datenschutz** – keine Fenstertitel, keine Tastatureingaben, kein Netzwerk,
  keine Adminrechte, Schlüssel per DPAPI an das Windows‑Konto gebunden.
* **Ressourcenschonend** – während der Wacht praktisch keine Prozessor‑ und Grafiklast: Szenen sind ruhende
  Bilder, keine Endlos‑Animationen; die Leiste bewegt sich nicht dauerhaft, Schatten sind
  zwischengespeichert, minimiert gibt die App Arbeitsspeicher zurück. Details: Projektdokumentation, Abschnitt 6.3.

## Schnellstart

1. Visual Studio 2026 mit Workload **„.NET‑Desktopentwicklung“** installieren.
2. `Laternenwacht.sln` öffnen, `Laternenwacht.App` als Startprojekt festlegen, **F5**.
3. Im Reiter **„Die Wacht“** eine Dauer wählen → **„Laterne entzünden“**.
4. Im Reiter **„Gefährten & Verlockungen“** eigene Programme eintragen (z. B. `discord`, `steam`).

**Als EXE veröffentlichen:** Doppelklick auf **`Veroeffentlichen.cmd`** – Tests laufen, danach liegen
zwei EXE‑Varianten bereit (Frontend + Backend jeweils in einer Datei):

| Variante | Datei | Größe | Hinweis |
|---|---|---|---|
| Eigenständig | `publish\win-x64\Laternenwacht.exe` | ~67 MB | läuft auf jedem Windows ohne Installation |
| Schlank | `publish\win-x64-schlank\Laternenwacht.exe` | ~4 MB | sparsamste Variante; braucht die „.NET Desktop Runtime 10“ (x64) |

Rund 2,7 MB davon sind eingebettete Schriften und Szenenbilder (siehe *Schriften & Bilder*).

Ausführlich inkl. GitHub‑Release: → [docs/Veroeffentlichung-VS2026.md](docs/Veroeffentlichung-VS2026.md)

```powershell
dotnet publish frontend/Laternenwacht.App/Laternenwacht.App.csproj -p:PublishProfile=Win-x64-EinzelneExe
# Ergebnis: publish\win-x64\Laternenwacht.exe
dotnet publish frontend/Laternenwacht.App/Laternenwacht.App.csproj -p:PublishProfile=Win-x64-Schlank
# Ergebnis: publish\win-x64-schlank\Laternenwacht.exe
```

## Begriffe im Reich

| Im Reich | Bedeutung |
|---|---|
| Wacht | Fokussitzung |
| Frost | Zeit, in der du abgelenkt warst |
| Station | Wegpunkt der Reise (1–10), bestimmt allein durch gemessene ÷ geplante Zeit |
| Türkischer Honig | Ein Stück je neuer Ablenkung – die Verlockung der Königin |
| Wintermesser | Anzeige, wie viel Frost bis Tauwetter bzw. Winter noch bleibt |
| Rast | Pause; die Reise steht still |
| Gefährten | Programme, die der Arbeit dienen |
| Verlockungen | Programme, die ablenken |
| Chronik | Versiegelte Historie deiner Wachten |
| Siegel | Integritätsprüfung der Chronik |

## Projektstruktur

```
backend/                               BACKEND – eigenständig baubar (Laternenwacht.Backend.sln)
  src/Laternenwacht.Core               Fachlogik: Messung, Bewertung, Sprüche, versiegelte Chronik
  src/Laternenwacht.Platform.Windows   Win32-Messung, DPAPI, Pfade, Protokoll (ohne Oberfläche)
  tests/Laternenwacht.Core.Tests       281 xUnit-Tests
  GEMINI.md                            Übergabe-Anweisung & Schnittstellenvertrag für KI-Assistenten
frontend/                              FRONTEND – nur Darstellung
  Laternenwacht.App                    WPF: Fokusleiste, Hauptfenster, Rabenbote, Gestaltung
docs/                      Projektdokumentation & Veröffentlichungsanleitung
```

## Dokumentation

* [Projektdokumentation](docs/Projektdokumentation.md) – Analyse, Entwurf, Sicherheitskonzept, Tests, Fazit
* [Veröffentlichung in Visual Studio 2026](docs/Veroeffentlichung-VS2026.md)

## Memes

* **Eingebaut:** Jede Bilddatei in `frontend/Laternenwacht.App/Assets/Memes/` wird beim Bauen
  automatisch in die EXE eingebettet – einfach Bilder dort ablegen und neu veröffentlichen.
* **Eigene:** In der App über *Memes hinzufügen …* (kopiert nach `%LOCALAPPDATA%\Laternenwacht\Memes`).
* Erlaubt: JPG, PNG, BMP, GIF (erstes Bild), je bis 10 MB, höchstens 200 eigene Memes.
* Hinweis: Die mitgelieferten Memes stammen aus dem Internet und sind nur für den privaten Gebrauch
  gedacht. Vor einer öffentlichen Weitergabe der EXE bitte entfernen oder durch eigene Bilder ersetzen.

## Schriften & Bilder

* **Schriften:** *Cinzel*, *Cinzel Decorative* und *EB Garamond* liegen als statische TTF‑Dateien in
  `frontend/Laternenwacht.App/Assets/Fonts/` und werden in die EXE eingebettet – es muss nichts
  installiert werden. Sie stehen unter der **SIL Open Font License 1.1**; die Lizenztexte
  (`OFL-Cinzel.txt`, `OFL-EBGaramond.txt`) liegen daneben und gehören bei einer Weitergabe dazu.
* **Szenenbilder:** Die sechs Bilder der Reise (Laternenpfahl, Schrank, Schlitten, Tauwetter,
  Cair Paravel, Steinhof) sind **eigene Zeichnungen** – keine Motive aus Büchern oder Filmen. Sie liegen
  als JPEG (960 × 1200) in `frontend/Laternenwacht.App/Assets/Szenen/`.
* **Quellen:** Die bearbeitbaren Vorlagen liegen als SVG in [`docs/szenen/`](docs/szenen) (viewBox
  480 × 600). Nach einer Änderung das SVG in doppelter Größe (960 × 1200, JPEG‑Qualität ~90) neu
  rendern und die JPEG‑Datei in `Assets/Szenen/` ersetzen – die Dateinamen bleiben gleich
  (`szene-laterne` → `laterne.jpg`, `szene-schrank` → `schrank.jpg`, `szene-schlitten` → `schlitten.jpg`,
  `szene-tauwetter` → `tauwetter.jpg`, `szene-cair` → `cair-paravel.jpg`, `szene-hof` → `steinhof.jpg`).

## Backend allein weitergeben

Der Ordner `backend/` ist in sich geschlossen. Zum Übergeben (z. B. an Gemini) einfach den Ordner
zippen – `backend/GEMINI.md` erklärt Aufbau, Regeln und die Schnittstelle zum Frontend.

```powershell
cd backend
dotnet test Laternenwacht.Backend.sln
```

## Build & Test (Kommandozeile)

```powershell
dotnet build Laternenwacht.sln -c Release
dotnet test  Laternenwacht.sln -c Release
```

---

<sub>Nicht‑kommerzielle Hommage an die Atmosphäre der Chroniken von C. S. Lewis; keine Verbindung zu
den Rechteinhabern. Alle Texte sind eigene Formulierungen. Lizenz: siehe [LICENSE](LICENSE).</sub>
