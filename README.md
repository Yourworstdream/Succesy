# Succesy · Laternenwacht 🏮

> *„Solange die Laterne brennt, findet man den Weg zurück.“*

**Laternenwacht** ist ein Fokuswächter für Windows. Während einer Fokuszeit („Wacht“) hängt am
**oberen Bildschirmrand** ein schmales Banner, das dir live zeigt, **wie lange du dich bereits
ablenkst** – als *Frost*, der über eine Laterne im verschneiten Wald kriecht. Gestaltet im Geist
klassischer Fantasy‑Chroniken à la C. S. Lewis: Laternenpfahl, ewiger Winter, wiederkehrender Frühling.

![Symbol](src/Laternenwacht.App/Assets/laterne.png)

## Funktionen

* **Fokusleiste oben am Bildschirm** – Restzeit, kumulierte Ablenkungszeit (❄ Frost), Anzahl der
  Verlockungen, Fortschritt; stiehlt beim Anklicken keinen Fokus.
* **Ablenkungserkennung** über das Vordergrundprogramm – *Milde Wacht* (Sperrliste) oder
  *Strenge Wacht* (nur Erlaubnisliste zählt als Fokus); Abwesenheit wird separat erfasst.
* **Mahnrufe** mit Augenzwinkern, z. B. nach 20 Ablenkungen:
  *„Herrscher von Cair Paravel, Ihr gefährdet Euer Königreich mit Eurem Müßiggang!“*
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

**Als EXE veröffentlichen:** → [docs/Veroeffentlichung-VS2026.md](docs/Veroeffentlichung-VS2026.md)

```powershell
dotnet publish src/Laternenwacht.App/Laternenwacht.App.csproj -p:PublishProfile=Win-x64-EinzelneExe
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
src/Laternenwacht.Core     Fachlogik (Messung, Bewertung, versiegelte Chronik) – plattformunabhängig
src/Laternenwacht.App      WPF-Oberfläche, Win32-/DPAPI-Anbindung
tests/…Core.Tests          71 xUnit-Tests
docs/                      Projektdokumentation & Veröffentlichungsanleitung
```

## Dokumentation

* [Projektdokumentation](docs/Projektdokumentation.md) – Analyse, Entwurf, Sicherheitskonzept, Tests, Fazit
* [Veröffentlichung in Visual Studio 2026](docs/Veroeffentlichung-VS2026.md)

## Build & Test (Kommandozeile)

```powershell
dotnet build Laternenwacht.sln -c Release
dotnet test  Laternenwacht.sln -c Release
```

---

<sub>Nicht‑kommerzielle Hommage an die Atmosphäre der Chroniken von C. S. Lewis; keine Verbindung zu
den Rechteinhabern. Alle Texte sind eigene Formulierungen. Lizenz: siehe [LICENSE](LICENSE).</sub>
