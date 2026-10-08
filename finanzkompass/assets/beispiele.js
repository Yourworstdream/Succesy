/* Finanzkompass – Beispielhaushalte zum Ausprobieren. Alle Zahlen sind ausgedacht, aber realistisch.
 * Termine werden relativ zum heutigen Monat gesetzt, damit die Beispiele nie veralten. */
(function (wurzel, fabrik) {
  'use strict';
  const api = fabrik();
  if (typeof module === 'object' && module.exports) {
    module.exports = api;
  } else {
    wurzel.Beispiele = api;
  }
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  'use strict';

  function inMonaten(heute, anzahl) {
    const d = new Date(heute.getFullYear(), heute.getMonth() + anzahl, 1);
    return d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0');
  }

  // Kurzschreibweise: [Name, Betrag, Kategorie, Bedürfnis, Art, Intervall?, Nutzungen?]
  function ausgaben(liste) {
    return liste.map((x) => ({
      name: x[0],
      betrag: x[1],
      kategorie: x[2],
      beduerfnis: x[3],
      art: x[4],
      intervall: x[5] || 'monatlich',
      nutzungen: x[6] === undefined ? null : x[6],
    }));
  }

  const VORLAGEN = {
    azubi: {
      titel: 'Azubi im 2. Lehrjahr',
      beschreibung: 'WG-Zimmer, Deutschlandticket, Führerschein als Ziel, kleiner Dispo',
      erstellen: (heute) => ({
        einnahmen: [
          { name: 'Ausbildungsvergütung (netto)', betrag: 860, intervall: 'monatlich' },
          { name: 'Kindergeld (von den Eltern)', betrag: 259, intervall: 'monatlich' },
        ],
        ausgaben: ausgaben([
          ['WG-Zimmer warm', 420, 'wohnen', 'existenz', 'fix'],
          ['Lebensmittel', 220, 'lebensmittel', 'existenz', 'variabel'],
          ['Deutschlandticket', 63, 'mobilitaet', 'existenz', 'fix'],
          ['Handyvertrag', 15, 'kommunikation', 'existenz', 'fix'],
          ['Haftpflichtversicherung', 60, 'versicherung', 'existenz', 'fix', 'jaehrlich'],
          ['Drogerie', 25, 'gesundheit', 'existenz', 'variabel'],
          ['Kleidung', 40, 'kleidung', 'kultur', 'variabel'],
          ['Fitnessstudio', 29.9, 'freizeit', 'kultur', 'fix', 'monatlich', 3],
          ['Musikstreaming', 11.99, 'abos', 'kultur', 'fix', 'monatlich', 30],
          ['Videostreaming', 13.99, 'abos', 'luxus', 'fix', 'monatlich', 2],
          ['Feiern & Ausgehen', 80, 'gastro', 'luxus', 'variabel'],
          ['Lieferdienst', 45, 'gastro', 'luxus', 'variabel'],
          ['Gaming & In-App-Käufe', 25, 'freizeit', 'luxus', 'variabel'],
        ]),
        schulden: [{ name: 'Dispokredit', rest: 350, zins: 12.5, rate: 30 }],
        ziele: [
          { name: 'Führerschein Klasse B', betrag: 3200, bereits: 400, termin: inMonaten(heute, 18), prioritaet: 1, anlage: 'auto' },
          { name: 'Sommerurlaub', betrag: 600, bereits: 0, termin: inMonaten(heute, 9), prioritaet: 2, anlage: 'auto' },
        ],
        ruecklagen: { notgroschen: 150, anlagen: 0 },
      }),
    },
    einsteiger: {
      titel: 'Berufseinsteiger, Single',
      beschreibung: 'Eigene Wohnung, Auto auf Kredit, Kreditkartenschulden, erstes Depot',
      erstellen: (heute) => ({
        einnahmen: [
          { name: 'Gehalt (netto)', betrag: 2280, intervall: 'monatlich' },
          { name: 'Weihnachtsgeld (netto)', betrag: 900, intervall: 'jaehrlich' },
        ],
        ausgaben: ausgaben([
          ['Miete warm', 760, 'wohnen', 'existenz', 'fix'],
          ['Strom', 48, 'wohnen', 'existenz', 'fix'],
          ['Internet', 35, 'kommunikation', 'existenz', 'fix'],
          ['Handyvertrag', 20, 'kommunikation', 'existenz', 'fix'],
          ['Lebensmittel', 320, 'lebensmittel', 'existenz', 'variabel'],
          ['Kfz-Versicherung', 540, 'mobilitaet', 'existenz', 'fix', 'jaehrlich'],
          ['Tanken', 120, 'mobilitaet', 'existenz', 'variabel'],
          ['Haftpflicht & Hausrat', 14, 'versicherung', 'existenz', 'fix'],
          ['Berufsunfähigkeitsversicherung', 58, 'versicherung', 'existenz', 'fix'],
          ['Rundfunkbeitrag', 55.08, 'sonstiges', 'existenz', 'fix', 'quartalsweise'],
          ['Drogerie & Apotheke', 35, 'gesundheit', 'existenz', 'variabel'],
          ['Kleidung', 80, 'kleidung', 'kultur', 'variabel'],
          ['Sportverein', 18, 'freizeit', 'kultur', 'fix', 'monatlich', 8],
          ['Hobby (Fotografie)', 60, 'freizeit', 'kultur', 'variabel'],
          ['Videostreaming (2 Dienste)', 23.98, 'abos', 'luxus', 'fix', 'monatlich', 2],
          ['Restaurant & Bar', 150, 'gastro', 'luxus', 'variabel'],
          ['Onlineshopping', 70, 'haushalt', 'luxus', 'variabel'],
        ]),
        schulden: [
          { name: 'Kreditkarte', rest: 1200, zins: 17.9, rate: 50 },
          { name: 'Autokredit', rest: 6800, zins: 5.9, rate: 185 },
        ],
        ziele: [
          { name: 'Urlaub', betrag: 1500, bereits: 200, termin: inMonaten(heute, 10), prioritaet: 2, anlage: 'auto' },
          { name: 'Neues Notebook', betrag: 1200, bereits: 0, termin: inMonaten(heute, 8), prioritaet: 3, anlage: 'auto' },
          { name: 'Eigenkapital Wohnung', betrag: 40000, bereits: 1500, termin: inMonaten(heute, 144), prioritaet: 2, anlage: 'auto' },
        ],
        ruecklagen: { notgroschen: 2100, anlagen: 1500 },
      }),
    },
    familie: {
      titel: 'Familie mit zwei Kindern',
      beschreibung: 'Zwei Einkommen, Kita, Autokredit, Rücklagen vorhanden',
      erstellen: (heute) => ({
        einnahmen: [
          { name: 'Gehalt 1 (netto)', betrag: 2950, intervall: 'monatlich' },
          { name: 'Gehalt 2 (netto, Teilzeit)', betrag: 1350, intervall: 'monatlich' },
          { name: 'Kindergeld (2 Kinder)', betrag: 518, intervall: 'monatlich' },
        ],
        ausgaben: ausgaben([
          ['Miete warm', 1380, 'wohnen', 'existenz', 'fix'],
          ['Strom', 95, 'wohnen', 'existenz', 'fix'],
          ['Lebensmittel', 880, 'lebensmittel', 'existenz', 'variabel'],
          ['Kita-Beitrag', 210, 'kinder', 'existenz', 'fix'],
          ['Kinder: Kleidung & Schule', 120, 'kinder', 'existenz', 'variabel'],
          ['Handy & Internet', 85, 'kommunikation', 'existenz', 'fix'],
          ['Versicherungen', 165, 'versicherung', 'existenz', 'fix'],
          ['Tanken & Wartung', 230, 'mobilitaet', 'existenz', 'variabel'],
          ['Kfz-Versicherung & Steuer', 780, 'mobilitaet', 'existenz', 'fix', 'jaehrlich'],
          ['Drogerie & Apotheke', 90, 'gesundheit', 'existenz', 'variabel'],
          ['Kleidung Erwachsene', 110, 'kleidung', 'kultur', 'variabel'],
          ['Schwimmkurs & Musikschule', 95, 'bildung', 'kultur', 'fix'],
          ['Ausflüge & Freizeit', 160, 'freizeit', 'kultur', 'variabel'],
          ['Streaming & Zeitschriften', 32, 'abos', 'kultur', 'fix', 'monatlich', 20],
          ['Essen gehen & Bestellen', 140, 'gastro', 'luxus', 'variabel'],
          ['Einrichtung & Deko', 60, 'haushalt', 'luxus', 'variabel'],
        ]),
        schulden: [{ name: 'Autokredit', rest: 9500, zins: 4.9, rate: 280 }],
        ziele: [
          { name: 'Familienurlaub', betrag: 3000, bereits: 600, termin: inMonaten(heute, 11), prioritaet: 2, anlage: 'auto' },
          { name: 'Ersatz für das Auto', betrag: 12000, bereits: 2000, termin: inMonaten(heute, 60), prioritaet: 2, anlage: 'auto' },
          { name: 'Ausbildung der Kinder', betrag: 20000, bereits: 1500, termin: inMonaten(heute, 168), prioritaet: 3, anlage: 'auto' },
        ],
        ruecklagen: { notgroschen: 6500, anlagen: 8000 },
      }),
    },
  };

  /** Ein paar Buchungen der letzten Monate, damit das Haushaltsbuch nicht leer startet. */
  function buchungen(daten, heute) {
    const liste = [];
    const fix = daten.ausgaben.filter((a) => a.art === 'fix' && a.intervall === 'monatlich');
    const variabel = daten.ausgaben.filter((a) => a.art === 'variabel' && a.intervall === 'monatlich');
    // Feste Schwankungen statt Zufall: jeder Start sieht gleich aus.
    const schwankung = [0.92, 1.06, 0.98, 1.12, 0.95, 1.03];
    for (let zurueck = 5; zurueck >= 0; zurueck--) {
      const d = new Date(heute.getFullYear(), heute.getMonth() - zurueck, 1);
      const monat = d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0');
      const tageImMonat = zurueck === 0 ? Math.max(1, heute.getDate()) : 28;
      const tag = (t) => monat + '-' + String(Math.min(t, tageImMonat)).padStart(2, '0');
      daten.einnahmen
        .filter((e) => e.intervall === 'monatlich')
        .forEach((e) => liste.push({ datum: tag(1), betrag: e.betrag, typ: 'einnahme', kategorie: 'einkommen', notiz: e.name, quelle: e.id }));
      fix.forEach((a) => liste.push({ datum: tag(2), betrag: a.betrag, typ: 'ausgabe', kategorie: a.kategorie, notiz: a.name, quelle: a.id }));
      daten.schulden.forEach((s) =>
        liste.push({ datum: tag(3), betrag: s.rate, typ: 'ausgabe', kategorie: 'kredit', notiz: 'Rate ' + s.name, quelle: s.id })
      );
      // Im laufenden Monat nur den Anteil bis heute.
      const anteil = zurueck === 0 ? Math.min(1, tageImMonat / 30) : 1;
      variabel.forEach((a, i) => {
        const betrag = Math.round(a.betrag * schwankung[(i + zurueck) % schwankung.length] * anteil * 100) / 100;
        if (betrag > 0) liste.push({ datum: tag(10 + i), betrag, typ: 'ausgabe', kategorie: a.kategorie, notiz: a.name });
      });
    }
    return liste;
  }

  function laden(schluessel, heute) {
    const jetzt = heute || new Date();
    const vorlage = VORLAGEN[schluessel];
    if (!vorlage) return null;
    const daten = vorlage.erstellen(jetzt);
    // Feste IDs, damit Buchungen ihren wiederkehrenden Posten kennen (keine Doppelbuchung).
    ['einnahmen', 'ausgaben', 'schulden', 'ziele'].forEach((liste) => {
      daten[liste].forEach((x, i) => {
        x.id = 'bsp' + liste.charAt(0) + i;
      });
    });
    daten.buchungen = buchungen(daten, jetzt);
    daten.beispiel = vorlage.titel;
    return daten;
  }

  return { VORLAGEN, laden };
});
