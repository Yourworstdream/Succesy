# Succesy · Laternenwacht 🏮

> *„Solange die Laterne brennt, findet man den Weg zurück.“*

**Laternenwacht** ist ein Fokuswächter für Windows. Während einer Fokuszeit („Wacht“) hängt am
**oberen Bildschirmrand** ein schmales Banner, das dir live zeigt, **wie lange du dich bereits
ablenkst** – als *Frost*, der über das Licht der Laterne kriecht. Die Texte und Figuren stammen aus
der Welt der Chroniken von Narnia (C. S. Lewis); die Oberfläche folgt dem modernen Gestaltungssystem
„Schneelicht“: warmes Papierweiß, Tinte, ein Laternenorange für Fokus und Lob, Eisblau für Ablenkung.

![Laternenwacht – Fokus](docs/bilder/1-fokus-laufend.png)

![Fokusleiste, Botschaft und Meme](docs/bilder/3-kapsel-botschaft-meme.png)

## Funktionen

* **Fokusleiste oben am Bildschirm** – Restzeit, kumulierte Ablenkungszeit (❄ Frost), Anzahl der
  Verlockungen, Fortschritt; stiehlt beim Anklicken keinen Fokus.
* **Verschiebbar** – Leiste mit der Maus an jede Stelle ziehen; die Position wird gespeichert.
* **Rechtsklick auf die Leiste** – wählen, aus welchem Buch der Chroniken von Narnia die Sprüche
  stammen (Band 1–7 oder alle gemischt), Benachrichtigungen an/aus, Leiste zurück an den Rand.
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
* **Schwimmende Memes** – bei jeder neuen Ablenkung treibt ein Meme schaukelnd quer über den
  Bildschirm („Treibgut im Fluss der Ablenkung“); ein Klick lässt es versinken. Eigene Bilder per
  Rechtsklick ▸ *Memes hinzufügen …* oder im Reiter *Gefährten & Verlockungen*.
* **Mahnrufe per Push‑Benachrichtigung** – ein „Rabenbote“ fliegt unten rechts ein, z. B. nach
  20 Ablenkungen aus *Der König von Narnia*:
  *„Herrscher von Cair Paravel, Ihr gefährdet Euer Königreich mit Eurem Müßiggang!“*
  Jedes der sieben Bücher hat eigene Sprüche (77 insgesamt).
* **Jahreszeiten** als Ergebnis: Frühling (< 10 % Frost), Tauwetter (< 25 %), Winter.
* **Versiegelte Chronik** – jede Wacht wird mit HMAC‑SHA256 versiegelt und verkettet;
  Manipulation, Löschung oder Vertauschung wird erkannt und angezeigt.
* **Sicherheit & Datenschutz** – keine Fenstertitel, keine Tastatureingaben, kein Netzwerk,
  keine Adminrechte, Schlüssel per DPAPI an das Windows‑Konto gebunden.

## Schnellstart

1. Visual Studio 2026 mit Workload **„.NET‑Desktopentwicklung“** installieren.
2. `Laternenwacht.sln` öffnen, `Laternenwacht.App` als Startprojekt festlegen, **F5**.
3. Im Reiter **„Die Wacht“** eine Dauer wählen → **„Laterne entzünden“**.
4. Im Reiter **„Gefährten & Verlockungen“** eigene Programme eintragen (z. B. `discord`, `steam`).

**Als EXE veröffentlichen:** Doppelklick auf **`Veroeffentlichen.cmd`** – Tests laufen, die EXE
(Frontend + Backend in einer Datei) liegt danach unter `publish\win-x64\Laternenwacht.exe`.
Ausführlich inkl. GitHub‑Release: → [docs/Veroeffentlichung-VS2026.md](docs/Veroeffentlichung-VS2026.md)

```powershell
dotnet publish frontend/Laternenwacht.App/Laternenwacht.App.csproj -p:PublishProfile=Win-x64-EinzelneExe
# Ergebnis: publish\win-x64\Laternenwacht.exe
```

## Begriffe im Reich

| Im Reich | Bedeutung |
|---|---|
| Wacht | Fokussitzung |
| Frost | Zeit, in der du abgelenkt warst |
| Gefährten | Programme, die der Arbeit dienen |
| Verlockungen | Programme, die ablenken |
| Chronik | Versiegelte Historie deiner Wachten |
| Siegel | Integritätsprüfung der Chronik |

## Projektstruktur

```
backend/                               BACKEND – eigenständig baubar (Laternenwacht.Backend.sln)
  src/Laternenwacht.Core               Fachlogik: Messung, Bewertung, Sprüche, versiegelte Chronik
  src/Laternenwacht.Platform.Windows   Win32-Messung, DPAPI, Pfade, Protokoll (ohne Oberfläche)
  tests/Laternenwacht.Core.Tests       135 xUnit-Tests
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
