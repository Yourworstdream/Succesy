// Tests für den Rechenkern. Ausführen: node --test finanzkompass/tests/logik.test.js
'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const L = require('../assets/logik.js');
const B = require('../assets/beispiele.js');

const HEUTE = new Date(2026, 9, 7); // 7. Oktober 2026
const nah = (ist, soll, toleranz = 0.01) =>
  assert.ok(Math.abs(ist - soll) <= toleranz, `erwartet ${soll}, bekommen ${ist}`);

function haushalt(daten) {
  return L.normalisiere(daten, HEUTE);
}

const AUSGEWOGEN = {
  einnahmen: [{ name: 'Gehalt', betrag: 2000 }],
  ausgaben: [
    { name: 'Miete', betrag: 700, kategorie: 'wohnen', beduerfnis: 'existenz', art: 'fix' },
    { name: 'Essen', betrag: 300, kategorie: 'lebensmittel', beduerfnis: 'existenz', art: 'variabel' },
    { name: 'Hobby', betrag: 200, kategorie: 'freizeit', beduerfnis: 'kultur', art: 'variabel' },
    { name: 'Restaurant', betrag: 100, kategorie: 'gastro', beduerfnis: 'luxus', art: 'variabel' },
  ],
  einstellungen: { puffer: 0 },
};

test('zahlAusText liest deutsche und englische Schreibweisen', () => {
  assert.equal(L.zahlAusText('1.234,56'), 1234.56);
  assert.equal(L.zahlAusText('1234.56'), 1234.56);
  assert.equal(L.zahlAusText('12,5 €'), 12.5);
  assert.equal(L.zahlAusText('1.500'), 1500);
  assert.equal(L.zahlAusText('0.500'), 0.5);
  assert.equal(L.zahlAusText('1,234,567'), 1234567);
  assert.equal(L.zahlAusText('−20'), -20);
  assert.equal(L.zahlAusText('  '), null);
  assert.equal(L.zahlAusText('zwölf'), null);
  assert.equal(L.zahlAusText('1.2.3,4,5'), null);
});

test('euro und prozent formatieren auf Deutsch', () => {
  assert.equal(L.euro(1234.5).replace(/\s/g, ' '), '1.234,50 €');
  assert.equal(L.euro(-0.001, true).replace(/\s/g, ' '), '0 €');
  assert.equal(L.prozent(0.153, 1), '15,3 %');
  assert.equal(L.prozent(null), '–');
});

test('Intervalle werden auf den Monat umgerechnet', () => {
  assert.equal(L.monatlich({ betrag: 120, intervall: 'jaehrlich' }), 10);
  assert.equal(L.monatlich({ betrag: 30, intervall: 'quartalsweise' }), 10);
  nah(L.monatlich({ betrag: 12, intervall: 'woechentlich' }), 52);
});

test('normalisiere verwirft Unsinn und setzt sichere Standardwerte', () => {
  const h = L.normalisiere(
    {
      einnahmen: [{ name: '<b>x</b>', betrag: 'viel', intervall: '__proto__' }, null, 5],
      ausgaben: [{ id: 'gleich', kategorie: 'constructor', beduerfnis: 'toString', betrag: -50 }, { id: 'gleich', betrag: 1e12 }],
      ziele: [{ betrag: 100, termin: '2026-13' }],
      buchungen: [{ datum: '2026-02-31x', betrag: 5 }, { datum: '2026-02-03', betrag: 5, typ: 'einnahme', kategorie: 'wohnen' }],
      einstellungen: { rendite: 99, strategie: 'alles' },
    },
    HEUTE
  );
  assert.equal(h.einnahmen.length, 1);
  assert.equal(h.einnahmen[0].betrag, 0);
  assert.equal(h.einnahmen[0].intervall, 'monatlich');
  assert.equal(h.einnahmen[0].name, '<b>x</b>'); // Escaping ist Sache der Oberfläche
  assert.equal(h.ausgaben[0].kategorie, 'sonstiges');
  assert.equal(h.ausgaben[0].beduerfnis, 'kultur');
  assert.equal(h.ausgaben[0].betrag, 0);
  assert.equal(h.ausgaben[1].betrag, 1e7);
  assert.notEqual(h.ausgaben[0].id, h.ausgaben[1].id);
  assert.equal(h.ziele[0].termin, '2027-10');
  assert.equal(h.buchungen.length, 1);
  assert.equal(h.buchungen[0].kategorie, 'einkommen');
  assert.equal(h.einstellungen.rendite, 15);
  assert.equal(h.einstellungen.strategie, 'ausgewogen');
});

test('analysiere berechnet Quoten und Bedürfnisgruppen', () => {
  const h = haushalt(Object.assign({}, AUSGEWOGEN, { schulden: [{ name: 'Kredit', rest: 1000, zins: 5, rate: 100 }], ruecklagen: { notgroschen: 2200 } }));
  const a = L.analysiere(h);
  assert.equal(a.einkommen, 2000);
  assert.equal(a.bedarf, 1000);
  assert.equal(a.wuensche, 300);
  assert.equal(a.raten, 100);
  assert.equal(a.pflicht, 1100);
  assert.equal(a.ueberschuss, 600);
  nah(a.sparquote, 0.3);
  nah(a.wohnquote, 0.35);
  nah(a.fixquote, 0.4);
  nah(a.reichweite, 2);
  assert.equal(a.nachKategorie[0].id, 'wohnen');
});

test('Kennzahlen-Ampel und IHK-Note', () => {
  const h = haushalt(Object.assign({}, AUSGEWOGEN, { ruecklagen: { notgroschen: 5000 } }));
  const liste = L.kennzahlen(L.analysiere(h), h.einstellungen);
  const nachId = Object.fromEntries(liste.map((k) => [k.id, k.stufe]));
  assert.equal(nachId.sparquote, 'gut');
  assert.equal(nachId.reichweite, 'gut');
  assert.equal(nachId.pflichtquote, 'gut');
  assert.equal(nachId.wohnquote, 'mittel');
  const note = L.gesamtnote(liste);
  assert.ok(note.punkte >= 92 && note.note === 1, JSON.stringify(note));
  assert.equal(L.gesamtnote(L.kennzahlen(L.analysiere(haushalt({})), h.einstellungen)), null);
});

test('ABC-Analyse teilt nach kumuliertem Anteil ein', () => {
  const posten = [50, 300, 20, 600, 30].map((m, i) => ({ id: 'p' + i, monatlich: m }));
  const abc = L.abcAnalyse(posten);
  assert.deepEqual(
    abc.map((p) => p.monatlich + p.klasse),
    ['600A', '300A', '50B', '30C', '20C']
  );
  nah(abc[abc.length - 1].kumuliert, 1);
});

test('Vergleich mit dem Durchschnittshaushalt summiert auf 100 %', () => {
  const a = L.analysiere(haushalt(AUSGEWOGEN));
  const v = L.vergleichReferenz(a);
  nah(L.summe(v.map((x) => x.referenz)), 1, 1e-9);
  nah(L.summe(v.map((x) => x.anteil)), 1, 1e-9);
  assert.equal(v.find((x) => x.id === 'gastro').stufe, 'normal');
});

test('Zinsrechnung: konformer Monatszins, Rate und Laufzeit passen zueinander', () => {
  nah(Math.pow(1 + L.monatsZins(5), 12), 1.05, 1e-12);
  assert.equal(L.noetigeRate(1200, 0, 12, 0), 100);
  assert.equal(L.noetigeRate(1000, 2000, 12, 3), 0);
  const rate = L.noetigeRate(10000, 1000, 60, 4);
  nah(1000 * Math.pow(1 + L.monatsZins(4), 60) + L.endwertRate(rate, 60, 4), 10000);
  assert.equal(L.monateBisZiel(10000, 1000, rate, 4), 60);
  assert.equal(L.monateBisZiel(1200, 0, 100, 0), 12);
  assert.equal(L.monateBisZiel(1200, 0, 0, 0), null);
  assert.equal(L.monateBisZiel(500, 600, 0, 3), 0);
});

test('Sparplan rechnet mit Zinseszins, Dynamik und Inflation', () => {
  const ohneZins = L.sparplan({ start: 1000, rate: 100, jahre: 10, rendite: 0, inflation: 0 });
  assert.equal(ohneZins.endkapital, 13000);
  assert.equal(ohneZins.zinsen, 0);
  const mitZins = L.sparplan({ start: 0, rate: 100, jahre: 20, rendite: 5, inflation: 2 });
  nah(mitZins.endkapital, L.endwertRate(100, 240, 5));
  assert.ok(mitZins.zinsen > mitZins.eingezahlt * 0.5);
  nah(mitZins.real, mitZins.endkapital / Math.pow(1.02, 20));
  assert.equal(mitZins.verdopplung, 14.4);
  const dynamik = L.sparplan({ rate: 100, jahre: 2, rendite: 0, dynamik: 10, inflation: 0 });
  nah(dynamik.eingezahlt, 1200 + 1320);
  const rate = L.rateFuerZiel(50000, { start: 0, jahre: 15, rendite: 5, dynamik: 2 });
  nah(L.sparplan({ rate, jahre: 15, rendite: 5, dynamik: 2 }).endkapital, 50000, 0.05);
});

test('Tilgung erkennt Raten, die nicht einmal die Zinsen decken', () => {
  const t = L.tilgung(1200, 0, 100);
  assert.equal(t.monate, 12);
  assert.equal(t.zinsen, 0);
  assert.equal(L.tilgung(10000, 12, 50).monate, null);
  assert.ok(L.tilgung(5000, 6, 200).zinsen > 0);
});

test('Opportunitätskosten wachsen mit der Zeit', () => {
  const o = L.opportunitaetskosten(50, 5);
  assert.deepEqual(o.map((x) => x.jahre), [10, 20, 30]);
  assert.equal(o[0].eingezahlt, 6000);
  assert.ok(o[2].wert > o[1].wert && o[1].wert > o[0].wert && o[0].wert > 6000);
});

test('planen: die Töpfe ergeben zusammen genau das Einkommen', () => {
  for (const schluessel of Object.keys(B.VORLAGEN)) {
    const h = haushalt(B.laden(schluessel, HEUTE));
    const plan = L.planen(h, HEUTE);
    assert.equal(plan.status, 'ok', schluessel);
    nah(L.summe(plan.toepfe.map((t) => t.betrag)), plan.analyse.einkommen, 1e-6);
    assert.equal(plan.sparen % 5, 0);
    nah(L.summe(Object.values(plan.simulation.ersterMonat)), plan.sparen, 1e-6);
  }
});

test('planen: wer wenig für Wünsche ausgibt, spart den Rest', () => {
  const h = haushalt(Object.assign({}, AUSGEWOGEN, { einstellungen: { puffer: 0, strategie: 'entspannt' } }));
  const plan = L.planen(h, HEUTE);
  assert.equal(plan.kuerzung, 0);
  assert.equal(plan.wunschBudget, 300);
  assert.equal(plan.sparen, 700);
});

test('planen: zu hohe Wünsche werden mit konkreten Vorschlägen gedeckelt', () => {
  const daten = JSON.parse(JSON.stringify(AUSGEWOGEN));
  daten.ausgaben.push({ id: 'lux', name: 'Shopping', betrag: 500, kategorie: 'kleidung', beduerfnis: 'luxus' });
  const h = haushalt(daten);
  const plan = L.planen(h, HEUTE);
  // frei = 2000 − 1000 = 1000, ausgewogen: 400 sparen, 600 Wünsche; vorhanden sind 800
  assert.equal(plan.sparen, 400);
  nah(plan.wunschBudget, 600);
  nah(plan.kuerzung, 200);
  assert.equal(plan.kuerzungen.posten[0].id, 'lux');
  nah(L.summe(plan.kuerzungen.posten.map((k) => k.kuerzung)), 200);
});

test('planen: Defizit, wenn schon die Pflichtausgaben nicht gedeckt sind', () => {
  const h = haushalt({
    einnahmen: [{ betrag: 900 }],
    ausgaben: [{ betrag: 950, beduerfnis: 'existenz', kategorie: 'wohnen' }, { betrag: 50, beduerfnis: 'luxus' }],
  });
  const plan = L.planen(h, HEUTE);
  assert.equal(plan.status, 'defizit');
  assert.equal(plan.fehlbetrag, 100);
  assert.equal(L.empfehlungen(h, plan)[0].stufe, 'dringend');
});

test('Simulation: erst Grundstock, dann teure Schulden, freie Raten fließen weiter', () => {
  const h = haushalt({
    einnahmen: [{ betrag: 2000 }],
    ausgaben: [{ betrag: 1000, beduerfnis: 'existenz', kategorie: 'wohnen', art: 'fix' }],
    schulden: [
      { id: 'teuer', name: 'Kreditkarte', rest: 1000, zins: 18, rate: 50 },
      { id: 'billig', name: 'Auto', rest: 5000, zins: 3, rate: 200 },
    ],
    ruecklagen: { notgroschen: 1000 },
    einstellungen: { puffer: 0, strategie: 'ausgewogen' },
  });
  const plan = L.planen(h, HEUTE);
  const sim = plan.simulation;
  // Grundstock = 1 Monat Pflicht (1000 + 250) → 250 fehlen, Rest in die Kreditkarte
  nah(sim.ersterMonat.notgroschen, 250 - 1000 * L.monatsZins(2), 0.01);
  assert.ok(sim.ersterMonat['schuld:teuer'] > 0);
  assert.equal(sim.ersterMonat['schuld:billig'], undefined);
  assert.equal(sim.phasen[0].stufen[0], 'grundstock');
  const teuer = sim.schulden.find((s) => s.id === 'teuer');
  const billig = sim.schulden.find((s) => s.id === 'billig');
  assert.ok(teuer.getilgt <= 4, 'Kreditkarte schnell getilgt');
  assert.ok(teuer.zinsen < teuer.ohneZinsen);
  assert.equal(billig.getilgt, billig.ohneMonate);
  assert.ok(sim.notgroschenVoll !== null);
  const letzte = sim.phasen[sim.phasen.length - 1];
  assert.deepEqual(letzte.stufen, ['vermoegen']);
  // Nach der Tilgung beider Kredite fließen Sparrate und beide frei gewordenen Raten ins Vermögen
  nah(sim.letzterMonat.vermoegen, plan.sparen + 250, 0.01);
});

test('Simulation: Sparziele nach Priorität und Termin', () => {
  const h = haushalt({
    einnahmen: [{ betrag: 2500 }],
    ausgaben: [{ betrag: 1000, beduerfnis: 'existenz', art: 'fix' }],
    ziele: [
      { id: 'wichtig', name: 'Führerschein', betrag: 2400, termin: '2027-09', prioritaet: 1, anlage: 'tagesgeld' },
      { id: 'egal', name: 'Konsole', betrag: 50000, termin: '2027-03', prioritaet: 3, anlage: 'tagesgeld' },
    ],
    ruecklagen: { notgroschen: 3000 },
    einstellungen: { puffer: 0, tagesgeld: 0 },
  });
  const sim = L.planen(h, HEUTE).simulation;
  const wichtig = sim.ziele.find((z) => z.id === 'wichtig');
  const egal = sim.ziele.find((z) => z.id === 'egal');
  assert.equal(wichtig.frist, 12);
  assert.ok(wichtig.puenktlich);
  nah(wichtig.ersteRate, 200);
  assert.equal(egal.puenktlich, false);
  assert.ok(egal.erreicht === null || egal.erreicht > egal.frist);
});

test('Empfehlungen nennen teure Schulden und selten genutzte Abos', () => {
  const h = haushalt(B.laden('azubi', HEUTE));
  const plan = L.planen(h, HEUTE);
  const titel = L.empfehlungen(h, plan).map((e) => e.titel);
  assert.ok(titel.some((t) => t.startsWith('Dispokredit kostet')), titel.join(' | '));
  assert.ok(titel.some((t) => t.includes('kaum genutzt')), titel.join(' | '));
  const urteile = L.bewertePosten(h, plan);
  assert.equal(urteile.length, h.ausgaben.length);
  assert.ok(urteile.some((u) => u.urteil === 'kuerzen'));
});

test('Haushaltsbuch: Soll-Ist, Verlauf und wiederkehrende Buchungen ohne Doppelte', () => {
  const h = haushalt(B.laden('einsteiger', HEUTE));
  const monat = L.monatVon(HEUTE);
  const si = L.sollIst(h, monat);
  assert.ok(si.anzahl > 0);
  assert.ok(si.kategorien.some((k) => k.id === 'kredit'));
  const verlauf = L.monatsverlauf(h, monat, 12);
  assert.equal(verlauf.length, 12);
  assert.equal(verlauf[11].monat, monat);
  assert.ok(verlauf[11].einnahmen > 0 && verlauf[5].ausgaben === 0);
  // Für den laufenden Monat sind alle Fixposten schon gebucht
  assert.equal(L.wiederkehrendeBuchungen(h, monat).length, 0);
  const naechster = L.monatPlus(monat, 1);
  const neu = L.wiederkehrendeBuchungen(h, naechster);
  assert.ok(neu.length > 0);
  assert.ok(neu.every((b) => b.datum === naechster + '-01'));
});

test('Monatshelfer', () => {
  assert.equal(L.monatPlus('2026-11', 3), '2027-02');
  assert.equal(L.monatName('2026-10'), 'Okt. 2026');
  assert.equal(L.dauerText(14), '1 Jahr, 2 Monate');
  assert.equal(L.dauerText(null), 'nie');
});

test('Simulation: Kredit über der Rendite erst nach vollem Notgroschen schneller tilgen', () => {
  const h = haushalt({
    einnahmen: [{ betrag: 3000 }],
    ausgaben: [{ betrag: 1000, beduerfnis: 'existenz', art: 'fix' }],
    schulden: [{ id: 'auto', name: 'Autokredit', rest: 6000, zins: 6, rate: 200 }],
    ruecklagen: { notgroschen: 1200 },
    einstellungen: { puffer: 0, rendite: 5 },
  });
  const sim = L.planen(h, HEUTE).simulation;
  assert.equal(sim.ersterMonat['schuld:auto'], undefined);
  const phase = sim.phasen.find((p) => p.stufen.includes('tilgen'));
  assert.ok(phase, sim.phasen.map((p) => p.titel).join(' / '));
  assert.ok(phase.von >= sim.notgroschenVoll);
  const auto = sim.schulden[0];
  assert.ok(auto.getilgt < auto.ohneMonate && auto.zinsen < auto.ohneZinsen);
});

test('planen: kleine Überschreitung der Wünsche löst keine Kürzung aus', () => {
  const daten = JSON.parse(JSON.stringify(AUSGEWOGEN));
  daten.ausgaben.push({ name: 'Kino', betrag: 305, beduerfnis: 'kultur' }); // Wünsche 605 statt 600
  const plan = L.planen(haushalt(daten), HEUTE);
  assert.equal(plan.kuerzung, 0);
  nah(plan.wunschBudget, 605);
});

test('Kennzahlen: mildere Grenzen bei kleinem Einkommen (Engel-Gesetz)', () => {
  const h = haushalt({
    einnahmen: [{ betrag: 1000 }],
    ausgaben: [
      { betrag: 380, kategorie: 'wohnen', beduerfnis: 'existenz' },
      { betrag: 250, kategorie: 'lebensmittel', beduerfnis: 'existenz' },
    ],
  });
  const k = Object.fromEntries(L.kennzahlen(L.analysiere(h), h.einstellungen).map((x) => [x.id, x.stufe]));
  assert.equal(k.pflichtquote, 'mittel');
  assert.equal(k.wohnquote, 'mittel');
});

test('Vergleich lässt Versicherungen und Kinderbetreuung außen vor', () => {
  const h = haushalt({
    einnahmen: [{ betrag: 3000 }],
    ausgaben: [
      { betrag: 1000, kategorie: 'wohnen', beduerfnis: 'existenz' },
      { betrag: 500, kategorie: 'versicherung', beduerfnis: 'existenz' },
      { betrag: 300, kategorie: 'kinder', beduerfnis: 'existenz' },
    ],
  });
  const wohnen = L.vergleichReferenz(L.analysiere(h)).find((v) => v.id === 'wohnen');
  assert.equal(wohnen.anteil, 1);
});
