/* Finanzkompass – Rechenkern ohne Oberfläche.
 * Läuft im Browser (window.Logik) und in Node (require), damit alles mit `node --test` prüfbar ist.
 * Alle Beträge in Euro, alle Zinssätze in Prozent pro Jahr, gerechnet wird pro Monat. */
(function (wurzel, fabrik) {
  'use strict';
  const api = fabrik();
  if (typeof module === 'object' && module.exports) {
    module.exports = api;
  } else {
    wurzel.Logik = api;
  }
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  'use strict';

  // ---------------------------------------------------------------- Stammdaten

  const INTERVALLE = {
    woechentlich: { name: 'wöchentlich', faktor: 52 / 12 },
    monatlich: { name: 'monatlich', faktor: 1 },
    quartalsweise: { name: 'vierteljährlich', faktor: 1 / 3 },
    halbjaehrlich: { name: 'halbjährlich', faktor: 1 / 6 },
    jaehrlich: { name: 'jährlich', faktor: 1 / 12 },
  };

  // `referenz` ordnet jede Kategorie einer Gruppe der amtlichen Statistik zu (siehe REFERENZ).
  // Versicherungsbeiträge und Kinderbetreuung zählt die Statistik nicht zu den Konsumausgaben.
  const KATEGORIEN = {
    wohnen: { name: 'Wohnen & Energie', referenz: 'wohnen' },
    lebensmittel: { name: 'Lebensmittel & Getränke', referenz: 'lebensmittel' },
    mobilitaet: { name: 'Mobilität', referenz: 'mobilitaet' },
    kommunikation: { name: 'Handy & Internet', referenz: 'uebrige' },
    versicherung: { name: 'Versicherungen', referenz: null },
    gesundheit: { name: 'Gesundheit & Pflege', referenz: 'gesundheit' },
    kleidung: { name: 'Kleidung & Schuhe', referenz: 'kleidung' },
    haushalt: { name: 'Haushalt & Einrichtung', referenz: 'haushalt' },
    freizeit: { name: 'Freizeit, Sport & Hobby', referenz: 'freizeit' },
    abos: { name: 'Abos & Streaming', referenz: 'freizeit' },
    gastro: { name: 'Restaurant & Lieferdienst', referenz: 'gastro' },
    bildung: { name: 'Bildung', referenz: 'uebrige' },
    kinder: { name: 'Kinder', referenz: null },
    sonstiges: { name: 'Sonstiges', referenz: 'uebrige' },
  };

  // Für das Haushaltsbuch kommen Kreditraten und Einnahmen dazu.
  const BUCHUNGSKATEGORIEN = Object.assign({}, KATEGORIEN, {
    kredit: { name: 'Kreditraten', referenz: null },
    einkommen: { name: 'Einnahmen', referenz: null },
  });

  // Anteile an den Konsumausgaben privater Haushalte in Deutschland 2024
  // (Statistisches Bundesamt, Laufende Wirtschaftsrechnungen 2024, gerundet).
  const REFERENZ = {
    wohnen: { name: 'Wohnen & Energie', anteil: 0.363 },
    lebensmittel: { name: 'Lebensmittel & Getränke', anteil: 0.14 },
    mobilitaet: { name: 'Mobilität', anteil: 0.138 },
    freizeit: { name: 'Freizeit, Sport & Kultur', anteil: 0.095 },
    gastro: { name: 'Gastronomie', anteil: 0.066 },
    haushalt: { name: 'Haushalt & Einrichtung', anteil: 0.045 },
    kleidung: { name: 'Kleidung & Schuhe', anteil: 0.035 },
    gesundheit: { name: 'Gesundheit', anteil: 0.034 },
    uebrige: { name: 'Kommunikation, Versicherungen, Bildung & Sonstiges', anteil: 0.084 },
  };

  // Bedürfnisarten aus der Volkswirtschaftslehre. Existenzbedürfnisse sind der „Bedarf“ der
  // 50/30/20-Regel, Kultur- und Luxusbedürfnisse zählen zu den „Wünschen“.
  const BEDUERFNISSE = {
    existenz: { name: 'Existenzbedürfnis', kurz: 'Muss' },
    kultur: { name: 'Kulturbedürfnis', kurz: 'Wichtig' },
    luxus: { name: 'Luxusbedürfnis', kurz: 'Verzichtbar' },
  };

  const ARTEN = {
    fix: { name: 'Fixkosten' },
    variabel: { name: 'Variabel' },
  };

  const PRIORITAETEN = { 1: 'hoch', 2: 'mittel', 3: 'niedrig' };

  // Ab diesem Zinssatz wird ein Kredit vor dem Auffüllen des Notgroschens getilgt (Dispo, Kreditkarte).
  const GRENZE_TEUER = 8;

  // Unter diesem Monatseinkommen gelten wegen Engel- und Schwabe-Gesetz mildere Grenzen.
  const KLEINES_EINKOMMEN = 1500;

  // Magisches Dreieck der Geldanlage: 1 = schwach, 3 = stark.
  const ANLAGEFORMEN = {
    auto: { name: 'nach Laufzeit wählen' },
    tagesgeld: { name: 'Tagesgeld / Festgeld', sicherheit: 3, liquiditaet: 3, rendite: 1 },
    mischung: { name: 'Mischung Tagesgeld + ETF', sicherheit: 2, liquiditaet: 2, rendite: 2 },
    etf: { name: 'Breit gestreuter ETF', sicherheit: 1, liquiditaet: 2, rendite: 3 },
  };

  const STRATEGIEN = {
    entspannt: { name: 'Entspannt', sparanteil: 0.25 },
    ausgewogen: { name: 'Ausgewogen', sparanteil: 0.4 },
    ehrgeizig: { name: 'Ehrgeizig', sparanteil: 0.6 },
  };

  const STANDARD_EINSTELLUNGEN = {
    rendite: 5,
    tagesgeld: 2,
    inflation: 2,
    notgroschenMonate: 3,
    strategie: 'ausgewogen',
    puffer: 5,
  };

  // Notenschlüssel der IHK-Abschlussprüfung (Punkte → Note).
  const NOTEN = [
    { ab: 92, note: 1, name: 'sehr gut' },
    { ab: 81, note: 2, name: 'gut' },
    { ab: 67, note: 3, name: 'befriedigend' },
    { ab: 50, note: 4, name: 'ausreichend' },
    { ab: 30, note: 5, name: 'mangelhaft' },
    { ab: 0, note: 6, name: 'ungenügend' },
  ];

  // ---------------------------------------------------------------- kleine Helfer

  const summe = (werte) => werte.reduce((s, w) => s + w, 0);
  const hat = (objekt, schluessel) => Object.prototype.hasOwnProperty.call(objekt, schluessel);

  function zahl(wert, min, max, standard) {
    const z = typeof wert === 'number' ? wert : typeof wert === 'string' && wert.trim() !== '' ? Number(wert) : NaN;
    if (!Number.isFinite(z)) return standard;
    return Math.min(max, Math.max(min, z));
  }

  function text(wert, maxLaenge) {
    if (typeof wert !== 'string') return '';
    return wert.replace(/[\u0000-\u001f\u007f]/g, ' ').trim().slice(0, maxLaenge);
  }

  function auswahl(wert, erlaubt, standard) {
    return typeof wert === 'string' && hat(erlaubt, wert) ? wert : standard;
  }

  let zaehler = 0;
  function neueId(praefix) {
    zaehler += 1;
    return praefix + Date.now().toString(36) + zaehler.toString(36) + Math.random().toString(36).slice(2, 6);
  }

  // ---------------------------------------------------------------- Zahlen lesen und schreiben

  /** Liest deutsche und englische Schreibweisen: „1.234,56“, „1234.56“, „12,5 €“, „1.500“. */
  function zahlAusText(eingabe) {
    if (typeof eingabe === 'number') return Number.isFinite(eingabe) ? eingabe : null;
    if (typeof eingabe !== 'string') return null;
    let s = eingabe.replace(/[\s '€]/g, '').replace(/eur$/i, '');
    if (s === '') return null;
    let vorzeichen = 1;
    if (/^[-−]/.test(s)) {
      vorzeichen = -1;
      s = s.slice(1);
    }
    if (!/^[\d.,]+$/.test(s)) return null;
    const komma = s.includes(',');
    const punkt = s.includes('.');
    if (komma && punkt) {
      s = s.lastIndexOf(',') > s.lastIndexOf('.') ? s.replace(/\./g, '').replace(',', '.') : s.replace(/,/g, '');
    } else if (komma) {
      s = (s.match(/,/g) || []).length > 1 ? s.replace(/,/g, '') : s.replace(',', '.');
    } else if (punkt) {
      const teile = s.split('.');
      const tausender = teile.length > 2 || (teile[1].length === 3 && teile[0].length <= 3 && teile[0] !== '0');
      if (tausender) s = s.replace(/\./g, '');
    }
    if (!/^(\d+\.?\d*|\.\d+)$/.test(s)) return null;
    return vorzeichen * parseFloat(s);
  }

  const euroFormat = new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR' });
  const euroGanzFormat = new Intl.NumberFormat('de-DE', { style: 'currency', currency: 'EUR', maximumFractionDigits: 0 });
  const zahlFormat = new Intl.NumberFormat('de-DE', { maximumFractionDigits: 2 });

  function euro(betrag, ganz) {
    const wert = Number.isFinite(betrag) ? betrag : 0;
    const gerundet = ganz ? Math.round(wert) : Math.round(wert * 100) / 100;
    return (ganz ? euroGanzFormat : euroFormat).format(Object.is(gerundet, -0) ? 0 : gerundet);
  }

  function prozent(anteil, stellen) {
    if (anteil === null || !Number.isFinite(anteil)) return '–';
    const s = stellen === undefined ? 0 : stellen;
    return (anteil * 100).toLocaleString('de-DE', { minimumFractionDigits: s, maximumFractionDigits: s }) + ' %';
  }

  function zahlText(wert) {
    return Number.isFinite(wert) ? zahlFormat.format(wert) : '';
  }

  // ---------------------------------------------------------------- Monate

  function monatVon(datum) {
    const d = datum instanceof Date ? datum : new Date();
    return d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0');
  }

  function monatsIndex(monat) {
    const [jahr, m] = monat.split('-').map(Number);
    return jahr * 12 + (m - 1);
  }

  function monatAusIndex(index) {
    const jahr = Math.floor(index / 12);
    return jahr + '-' + String(index - jahr * 12 + 1).padStart(2, '0');
  }

  function monatPlus(monat, anzahl) {
    return monatAusIndex(monatsIndex(monat) + anzahl);
  }

  const MONATSNAMEN = ['Jan.', 'Feb.', 'März', 'Apr.', 'Mai', 'Juni', 'Juli', 'Aug.', 'Sep.', 'Okt.', 'Nov.', 'Dez.'];
  const MONATSNAMEN_LANG = ['Januar', 'Februar', 'März', 'April', 'Mai', 'Juni', 'Juli', 'August', 'September', 'Oktober', 'November', 'Dezember'];

  function monatName(monat, lang) {
    const [jahr, m] = monat.split('-').map(Number);
    return (lang ? MONATSNAMEN_LANG : MONATSNAMEN)[m - 1] + ' ' + jahr;
  }

  function dauerText(monate) {
    if (monate === null || monate === undefined) return 'nie';
    if (monate < 12) return monate === 1 ? '1 Monat' : monate + ' Monate';
    const j = Math.floor(monate / 12);
    const m = monate % 12;
    const jt = j === 1 ? '1 Jahr' : j + ' Jahre';
    return m === 0 ? jt : jt + ', ' + (m === 1 ? '1 Monat' : m + ' Monate');
  }

  // ---------------------------------------------------------------- Datenmodell

  function leererHaushalt() {
    return normalisiere({});
  }

  /** Macht aus beliebigen (z. B. importierten) Daten einen gültigen Haushalt. Unbekanntes fällt weg. */
  function normalisiere(roh, heute) {
    const q = roh && typeof roh === 'object' ? roh : {};
    const vergeben = new Set();
    const id = (wert, praefix) => {
      let neu = typeof wert === 'string' && /^[a-z0-9_-]{1,40}$/i.test(wert) && !vergeben.has(wert) ? wert : neueId(praefix);
      while (vergeben.has(neu)) neu = neueId(praefix);
      vergeben.add(neu);
      return neu;
    };
    const liste = (arr, max, umwandeln) =>
      Array.isArray(arr) ? arr.slice(0, max).filter((x) => x && typeof x === 'object').map(umwandeln).filter(Boolean) : [];
    const standardTermin = monatPlus(monatVon(heute), 12);
    const r = q.ruecklagen && typeof q.ruecklagen === 'object' ? q.ruecklagen : {};
    const e = q.einstellungen && typeof q.einstellungen === 'object' ? q.einstellungen : {};
    const s = STANDARD_EINSTELLUNGEN;

    return {
      version: 1,
      beispiel: text(q.beispiel, 80) || null,
      einnahmen: liste(q.einnahmen, 100, (x) => ({
        id: id(x.id, 'e'),
        name: text(x.name, 60) || 'Einnahme',
        betrag: zahl(x.betrag, 0, 1e7, 0),
        intervall: auswahl(x.intervall, INTERVALLE, 'monatlich'),
      })),
      ausgaben: liste(q.ausgaben, 300, (x) => ({
        id: id(x.id, 'a'),
        name: text(x.name, 60) || 'Ausgabe',
        betrag: zahl(x.betrag, 0, 1e7, 0),
        intervall: auswahl(x.intervall, INTERVALLE, 'monatlich'),
        kategorie: auswahl(x.kategorie, KATEGORIEN, 'sonstiges'),
        beduerfnis: auswahl(x.beduerfnis, BEDUERFNISSE, 'kultur'),
        art: auswahl(x.art, ARTEN, 'variabel'),
        nutzungen: x.nutzungen === null || x.nutzungen === undefined || x.nutzungen === '' ? null : zahl(x.nutzungen, 0, 1000, null),
      })),
      schulden: liste(q.schulden, 50, (x) => ({
        id: id(x.id, 's'),
        name: text(x.name, 60) || 'Kredit',
        rest: zahl(x.rest, 0, 1e8, 0),
        zins: zahl(x.zins, 0, 100, 0),
        rate: zahl(x.rate, 0, 1e7, 0),
      })),
      ziele: liste(q.ziele, 50, (x) => ({
        id: id(x.id, 'z'),
        name: text(x.name, 60) || 'Sparziel',
        betrag: zahl(x.betrag, 0, 1e8, 0),
        bereits: zahl(x.bereits, 0, 1e8, 0),
        termin: typeof x.termin === 'string' && /^\d{4}-(0[1-9]|1[0-2])$/.test(x.termin) ? x.termin : standardTermin,
        prioritaet: Number(auswahl(String(x.prioritaet), PRIORITAETEN, '2')),
        anlage: auswahl(x.anlage, ANLAGEFORMEN, 'auto'),
      })),
      buchungen: liste(q.buchungen, 20000, (x) => {
        if (typeof x.datum !== 'string' || !/^\d{4}-(0[1-9]|1[0-2])-(0[1-9]|[12]\d|3[01])$/.test(x.datum)) return null;
        const typ = x.typ === 'einnahme' ? 'einnahme' : 'ausgabe';
        return {
          id: id(x.id, 'b'),
          datum: x.datum,
          betrag: zahl(x.betrag, 0, 1e7, 0),
          typ,
          kategorie: typ === 'einnahme' ? 'einkommen' : auswahl(x.kategorie, BUCHUNGSKATEGORIEN, 'sonstiges'),
          notiz: text(x.notiz, 80),
          quelle: text(x.quelle, 60) || null,
        };
      }),
      ruecklagen: {
        notgroschen: zahl(r.notgroschen, 0, 1e9, 0),
        anlagen: zahl(r.anlagen, 0, 1e9, 0),
      },
      einstellungen: {
        rendite: zahl(e.rendite, 0, 15, s.rendite),
        tagesgeld: zahl(e.tagesgeld, 0, 10, s.tagesgeld),
        inflation: zahl(e.inflation, 0, 15, s.inflation),
        notgroschenMonate: zahl(e.notgroschenMonate, 1, 12, s.notgroschenMonate),
        strategie: auswahl(e.strategie, STRATEGIEN, s.strategie),
        puffer: zahl(e.puffer, 0, 20, s.puffer),
      },
    };
  }

  function monatlich(posten) {
    return posten.betrag * INTERVALLE[posten.intervall].faktor;
  }

  // ---------------------------------------------------------------- Analyse

  /** Alle Kennzahlen eines Haushalts, jeweils pro Monat. */
  function analysiere(h) {
    const einkommen = summe(h.einnahmen.map(monatlich));
    const posten = h.ausgaben.map((a) => Object.assign({}, a, { monatlich: monatlich(a) }));
    const summeWo = (bedingung) => summe(posten.filter(bedingung).map((p) => p.monatlich));
    const bedarf = summeWo((p) => p.beduerfnis === 'existenz');
    const kultur = summeWo((p) => p.beduerfnis === 'kultur');
    const luxus = summeWo((p) => p.beduerfnis === 'luxus');
    const offeneSchulden = h.schulden.filter((s) => s.rest > 0);
    const raten = summe(offeneSchulden.map((s) => s.rate));
    const fixAusgaben = summeWo((p) => p.art === 'fix');
    const wohnen = summeWo((p) => p.kategorie === 'wohnen');
    const konsum = bedarf + kultur + luxus;
    const pflicht = bedarf + raten;
    const ausgaben = konsum + raten;
    const ueberschuss = einkommen - ausgaben;
    const quote = (wert) => (einkommen > 0 ? wert / einkommen : null);

    const proKategorie = {};
    posten.forEach((p) => {
      proKategorie[p.kategorie] = (proKategorie[p.kategorie] || 0) + p.monatlich;
    });
    const nachKategorie = Object.keys(proKategorie)
      .map((k) => ({ id: k, name: KATEGORIEN[k].name, betrag: proKategorie[k] }))
      .filter((k) => k.betrag > 0)
      .sort((x, y) => y.betrag - x.betrag);

    return {
      einkommen,
      posten,
      bedarf,
      kultur,
      luxus,
      wuensche: kultur + luxus,
      raten,
      pflicht,
      konsum,
      ausgaben,
      fix: fixAusgaben + raten,
      variabel: konsum - fixAusgaben,
      wohnen,
      ueberschuss,
      sparquote: quote(ueberschuss),
      konsumquote: quote(konsum),
      fixquote: quote(fixAusgaben + raten),
      wohnquote: quote(wohnen),
      ratenquote: quote(raten),
      pflichtquote: quote(pflicht),
      wunschquote: quote(kultur + luxus),
      notgroschen: h.ruecklagen.notgroschen,
      reichweite: pflicht > 0 ? h.ruecklagen.notgroschen / pflicht : null,
      schuldenGesamt: summe(offeneSchulden.map((s) => s.rest)),
      nachKategorie,
    };
  }

  /** Ampel für jede Kennzahl, mit Zielwert und Begründung. */
  function kennzahlen(a, einstellungen) {
    const monate = einstellungen.notgroschenMonate;
    const klein = a.einkommen > 0 && a.einkommen < KLEINES_EINKOMMEN;
    const pflichtGrenzen = klein ? [0.6, 0.75] : [0.5, 0.65];
    const wohnGrenzen = klein ? [0.35, 0.45] : [0.3, 0.4];
    const milder = klein ? ' Bei kleinem Einkommen gelten mildere Grenzen (Engel-Gesetz).' : '';
    const liste = [
      {
        id: 'sparquote',
        name: 'Sparquote',
        wert: a.sparquote,
        format: 'prozent',
        gewicht: 30,
        ziel: 'mindestens 15 %, ideal 20 %',
        wissen: 'w-sparquote',
        stufe: (v) => (v >= 0.15 ? 'gut' : v >= 0.05 ? 'mittel' : 'schlecht'),
        texte: {
          gut: 'Du legst einen gesunden Teil deines Einkommens zurück.',
          mittel: 'Du sparst etwas, aber für Rücklagen und Ziele bleibt wenig Spielraum.',
          schlecht: 'Es bleibt kaum etwas übrig. Jede unerwartete Rechnung wird zum Problem.',
        },
      },
      {
        id: 'reichweite',
        name: 'Notgroschen',
        wert: a.reichweite,
        format: 'monate',
        gewicht: 20,
        ziel: monate + ' Monate Pflichtausgaben',
        wissen: 'w-notgroschen',
        stufe: (v) => (v >= monate ? 'gut' : v >= 1 ? 'mittel' : 'schlecht'),
        texte: {
          gut: 'Deine Rücklage trägt dich auch durch einen Jobverlust oder eine große Reparatur.',
          mittel: 'Ein Grundstock ist da. Bau ihn weiter aus, bevor du langfristig anlegst.',
          schlecht: 'Ohne Rücklage landet jede Panne im teuren Dispo.',
        },
      },
      {
        id: 'pflichtquote',
        name: 'Pflichtausgaben',
        wert: a.pflichtquote,
        format: 'prozent',
        gewicht: 15,
        ziel: klein ? 'höchstens 60 % (kleines Einkommen)' : 'höchstens 50 % (50/30/20-Regel)',
        wissen: 'w-503020',
        stufe: (v) => (v <= pflichtGrenzen[0] ? 'gut' : v <= pflichtGrenzen[1] ? 'mittel' : 'schlecht'),
        texte: {
          gut: 'Lebensnotwendiges und Kreditraten lassen dir genug Freiraum.' + milder,
          mittel: 'Ein großer Teil ist fest verplant. Kleine Einsparungen bei großen Posten helfen am meisten.' + milder,
          schlecht: 'Fast alles geht für das Nötigste drauf. Hier hilft nur, große Posten oder das Einkommen zu verändern.',
        },
      },
      {
        id: 'fixquote',
        name: 'Fixkostenquote',
        wert: a.fixquote,
        format: 'prozent',
        gewicht: 10,
        ziel: 'höchstens 50 %',
        wissen: 'w-fixkosten',
        stufe: (v) => (v <= 0.5 ? 'gut' : v <= 0.65 ? 'mittel' : 'schlecht'),
        texte: {
          gut: 'Du bleibst flexibel und kannst auf Veränderungen reagieren.',
          mittel: 'Viele Verträge binden dein Geld. Kündigungsfristen im Blick behalten.',
          schlecht: 'Fixkosten lassen sich kurzfristig kaum senken. Das macht dich verwundbar.',
        },
      },
      {
        id: 'wohnquote',
        name: 'Wohnkostenquote',
        wert: a.wohnquote,
        format: 'prozent',
        gewicht: 10,
        ziel: klein ? 'höchstens 35 % (kleines Einkommen)' : 'höchstens 30 %, ab 40 % überlastet',
        wissen: 'w-wohnkosten',
        stufe: (v) => (v <= wohnGrenzen[0] ? 'gut' : v <= wohnGrenzen[1] ? 'mittel' : 'schlecht'),
        texte: {
          gut: 'Deine Wohnkosten passen zu deinem Einkommen.' + milder,
          mittel: 'Wohnen ist dein größter Brocken. Achte bei Nebenkosten und Energie auf Sparpotenzial.' + milder,
          schlecht: 'Laut EU-Statistik gilt ein Haushalt ab 40 % als durch Wohnkosten überlastet.',
        },
      },
      {
        id: 'ratenquote',
        name: 'Schuldendienstquote',
        wert: a.ratenquote,
        format: 'prozent',
        gewicht: 15,
        ziel: 'höchstens 10 % für Kreditraten',
        wissen: 'w-schulden',
        stufe: (v) => (v <= 0.1 ? 'gut' : v <= 0.2 ? 'mittel' : 'schlecht'),
        texte: {
          gut: a.raten > 0 ? 'Deine Kreditraten sind gut tragbar.' : 'Du bist schuldenfrei.',
          mittel: 'Die Raten schränken dich spürbar ein. Keine neuen Ratenkäufe aufnehmen.',
          schlecht: 'Die Raten sind zu hoch. Sprich frühzeitig mit einer Schuldnerberatung.',
        },
      },
    ];
    return liste.map((k) => {
      const bewertbar = k.wert !== null && Number.isFinite(k.wert);
      const stufe = bewertbar ? k.stufe(k.wert) : null;
      return {
        id: k.id,
        name: k.name,
        wert: k.wert,
        format: k.format,
        gewicht: k.gewicht,
        ziel: k.ziel,
        wissen: k.wissen,
        stufe,
        text: stufe ? k.texte[stufe] : 'Noch nicht berechenbar – trag zuerst Einnahmen und Ausgaben ein.',
      };
    });
  }

  const PUNKTE = { gut: 100, mittel: 55, schlecht: 10 };

  /** Gesamturteil 0–100 Punkte und Schulnote nach IHK-Schlüssel. */
  function gesamtnote(kennzahlListe) {
    const bewertbar = kennzahlListe.filter((k) => k.stufe);
    if (!bewertbar.length) return null;
    const gewichte = summe(bewertbar.map((k) => k.gewicht));
    const punkte = Math.round(summe(bewertbar.map((k) => k.gewicht * PUNKTE[k.stufe])) / gewichte);
    const n = NOTEN.find((x) => punkte >= x.ab);
    return { punkte, note: n.note, name: n.name };
  }

  /** ABC-Analyse: A = Posten bis 80 % der Summe, B = bis 95 %, C = Rest. */
  function abcAnalyse(posten) {
    const sortiert = posten.filter((p) => p.monatlich > 0).sort((x, y) => y.monatlich - x.monatlich);
    const gesamt = summe(sortiert.map((p) => p.monatlich));
    let kumuliert = 0;
    return sortiert.map((p) => {
      const vorher = kumuliert / gesamt;
      kumuliert += p.monatlich;
      const klasse = vorher < 0.8 - 1e-9 ? 'A' : vorher < 0.95 - 1e-9 ? 'B' : 'C';
      return Object.assign({}, p, { anteil: p.monatlich / gesamt, kumuliert: kumuliert / gesamt, klasse });
    });
  }

  /** Alle Ausgaben einschließlich Kreditraten als Posten für die ABC-Analyse. */
  function allePosten(h, a) {
    const raten = h.schulden
      .filter((s) => s.rest > 0 && s.rate > 0)
      .map((s) => ({
        id: s.id,
        name: 'Rate: ' + s.name,
        kategorie: 'kredit',
        beduerfnis: 'existenz',
        art: 'fix',
        monatlich: s.rate,
        istRate: true,
      }));
    return a.posten.concat(raten);
  }

  /** Vergleich der eigenen Ausgabenstruktur mit dem Durchschnittshaushalt (Anteile am Konsum). */
  function vergleichReferenz(a) {
    const betraege = {};
    Object.keys(REFERENZ).forEach((k) => {
      betraege[k] = 0;
    });
    let basis = 0;
    a.posten.forEach((p) => {
      const gruppe = KATEGORIEN[p.kategorie].referenz;
      if (!gruppe) return;
      betraege[gruppe] += p.monatlich;
      basis += p.monatlich;
    });
    // Engel- und Schwabe-Gesetz: Mit kleinem Einkommen steigt der Anteil für Essen und Wohnen.
    const kleinesEinkommen = a.einkommen > 0 && a.einkommen < 2000;
    return Object.keys(REFERENZ).map((k) => {
      const anteil = basis > 0 ? betraege[k] / basis : 0;
      const referenz = REFERENZ[k].anteil;
      let stufe = 'normal';
      if (anteil - referenz >= 0.05 && anteil >= referenz * 1.25) stufe = 'hoch';
      else if (referenz >= 0.03 && anteil < referenz * 0.5) stufe = 'niedrig';
      const erklaerbar = stufe === 'hoch' && kleinesEinkommen && (k === 'wohnen' || k === 'lebensmittel');
      return { id: k, name: REFERENZ[k].name, betrag: betraege[k], anteil, referenz, differenz: anteil - referenz, stufe, erklaerbar };
    });
  }

  // ---------------------------------------------------------------- Zinsrechnung

  /** Konformer Monatszins: zwölfmal angewendet ergibt er genau den Jahreszins. */
  function monatsZins(prozentProJahr) {
    return Math.pow(1 + prozentProJahr / 100, 1 / 12) - 1;
  }

  /** Endwert einer monatlichen Rate (nachschüssig) nach `monate` Monaten. */
  function endwertRate(rate, monate, prozentProJahr) {
    const r = monatsZins(prozentProJahr);
    if (r === 0) return rate * monate;
    return (rate * (Math.pow(1 + r, monate) - 1)) / r;
  }

  /** Was eine regelmäßige Ausgabe gekostet hätte, wäre das Geld stattdessen angelegt worden. */
  function opportunitaetskosten(betragProMonat, prozentProJahr) {
    return [10, 20, 30].map((jahre) => ({
      jahre,
      eingezahlt: betragProMonat * 12 * jahre,
      wert: endwertRate(betragProMonat, jahre * 12, prozentProJahr),
    }));
  }

  /** Monatliche Rate, die von `start` in `monate` Monaten zum `ziel` führt. */
  function noetigeRate(ziel, start, monate, prozentProJahr) {
    const r = monatsZins(prozentProJahr);
    const n = Math.max(1, Math.round(monate));
    const fehlt = ziel - start * Math.pow(1 + r, n);
    if (fehlt <= 0) return 0;
    if (r === 0) return fehlt / n;
    return (fehlt * r) / (Math.pow(1 + r, n) - 1);
  }

  /** Wie viele Monate bis zum Ziel? `null`, wenn es nie (oder erst nach `max` Monaten) klappt. */
  function monateBisZiel(ziel, start, rate, prozentProJahr, max) {
    const grenze = max || 1200;
    if (start >= ziel) return 0;
    const r = monatsZins(prozentProJahr);
    let n;
    if (r === 0) {
      if (rate <= 0) return null;
      n = Math.ceil((ziel - start) / rate - 1e-9);
    } else {
      const nenner = start * r + rate;
      if (nenner <= 0) return null;
      n = Math.ceil(Math.log((ziel * r + rate) / nenner) / Math.log(1 + r) - 1e-9);
    }
    return n <= grenze ? Math.max(1, n) : null;
  }

  /** Sparplan mit Startkapital, Rate, jährlicher Ratenerhöhung (Dynamik) und Inflation. */
  function sparplan(eingabe) {
    const start = zahl(eingabe.start, 0, 1e9, 0);
    const rate = zahl(eingabe.rate, 0, 1e7, 0);
    const jahre = Math.round(zahl(eingabe.jahre, 1, 60, 10));
    const rendite = zahl(eingabe.rendite, -10, 20, 5);
    const dynamik = zahl(eingabe.dynamik, 0, 20, 0);
    const inflation = zahl(eingabe.inflation, 0, 20, 2);
    const r = monatsZins(rendite);
    let kapital = start;
    let eingezahlt = start;
    let aktuelleRate = rate;
    const verlauf = [{ jahr: 0, kapital, eingezahlt, zinsen: 0, real: kapital, rate: aktuelleRate }];
    for (let j = 1; j <= jahre; j++) {
      for (let m = 0; m < 12; m++) {
        kapital = kapital * (1 + r) + aktuelleRate;
        eingezahlt += aktuelleRate;
      }
      verlauf.push({
        jahr: j,
        kapital,
        eingezahlt,
        zinsen: kapital - eingezahlt,
        real: kapital / Math.pow(1 + inflation / 100, j),
        rate: aktuelleRate,
      });
      aktuelleRate *= 1 + dynamik / 100;
    }
    const ende = verlauf[verlauf.length - 1];
    return {
      verlauf,
      endkapital: ende.kapital,
      eingezahlt: ende.eingezahlt,
      zinsen: ende.zinsen,
      real: ende.real,
      verdopplung: rendite > 0 ? 72 / rendite : null,
    };
  }

  /** Umkehrung des Sparplans: Startrate, mit der nach der Laufzeit `ziel` erreicht wird. */
  function rateFuerZiel(ziel, eingabe) {
    if (sparplan(Object.assign({}, eingabe, { rate: 0 })).endkapital >= ziel) return 0;
    let unten = 0;
    let oben = Math.max(ziel, 1);
    for (let i = 0; i < 80; i++) {
      const mitte = (unten + oben) / 2;
      if (sparplan(Object.assign({}, eingabe, { rate: mitte })).endkapital >= ziel) oben = mitte;
      else unten = mitte;
    }
    return oben;
  }

  /** Tilgung eines Kredits mit fester Monatsrate. `monate: null` heißt: die Rate deckt nicht einmal die Zinsen. */
  function tilgung(rest, zinsProJahr, rate) {
    const r = monatsZins(zinsProJahr);
    let offen = rest;
    let zinsen = 0;
    for (let m = 1; m <= 1200; m++) {
      const z = offen * r;
      if (rate <= z + 1e-9) return { monate: null, zinsen: null };
      zinsen += z;
      offen = offen + z - rate;
      if (offen <= 0.005) return { monate: m, zinsen };
    }
    return { monate: null, zinsen: null };
  }

  // ---------------------------------------------------------------- Aufteilung und Fahrplan

  function anlageFuerLaufzeit(monate) {
    if (monate <= 36) return 'tagesgeld';
    if (monate <= 120) return 'mischung';
    return 'etf';
  }

  function zinsFuerAnlage(form, e) {
    if (form === 'tagesgeld') return e.tagesgeld;
    if (form === 'etf') return e.rendite;
    return (e.tagesgeld + e.rendite) / 2;
  }

  /** Monatsfrist eines Ziels: Monat 1 ist der laufende Monat, das Ziel soll im Terminmonat stehen. */
  function frist(ziel, startMonat) {
    return Math.max(1, monatsIndex(ziel.termin) - monatsIndex(startMonat) + 1);
  }

  /** Kürzungsvorschlag: erst Luxus halbieren, dann Kultur um ein Viertel, dann Luxus ganz. */
  function kuerzungsvorschlag(posten, betrag) {
    let offen = betrag;
    const vorschlag = {};
    const luxus = posten.filter((p) => p.beduerfnis === 'luxus').sort((x, y) => y.monatlich - x.monatlich);
    const kultur = posten.filter((p) => p.beduerfnis === 'kultur').sort((x, y) => y.monatlich - x.monatlich);
    const runde = (liste, anteil) => {
      liste.forEach((p) => {
        if (offen <= 0.005) return;
        const bisher = vorschlag[p.id] || 0;
        const moeglich = Math.max(0, p.monatlich * anteil - bisher);
        const x = Math.min(offen, moeglich);
        if (x > 0) {
          vorschlag[p.id] = bisher + x;
          offen -= x;
        }
      });
    };
    runde(luxus, 0.5);
    runde(kultur, 0.25);
    runde(luxus, 1);
    runde(kultur, 0.5);
    // Auf ganze Euro aufrunden: realistische Budgets, und die Summe deckt sicher den Bedarf.
    Object.keys(vorschlag).forEach((id) => {
      const p = posten.find((x) => x.id === id);
      vorschlag[id] = Math.min(p.monatlich, Math.ceil(vorschlag[id] - 0.005));
    });
    offen = Math.max(0, betrag - summe(Object.keys(vorschlag).map((id) => vorschlag[id])));
    return {
      posten: posten
        .filter((p) => vorschlag[p.id])
        .map((p) => ({ id: p.id, name: p.name, vorher: p.monatlich, kuerzung: vorschlag[p.id], nachher: p.monatlich - vorschlag[p.id] }))
        .sort((x, y) => y.kuerzung - x.kuerzung),
      ungedeckt: Math.max(0, offen),
    };
  }

  /**
   * Teilt das Monatseinkommen realistisch auf und simuliert den Weg zu Rücklage, Schuldenfreiheit und Zielen.
   * Reihenfolge (Finanzpyramide): Pflicht → Puffer → Wünsche (gedeckelt) → Sparen;
   * die Sparrate fließt nach dem Wasserfall in Notgroschen, teure Schulden, Ziele und Vermögensaufbau.
   */
  function planen(h, heute) {
    const a = analysiere(h);
    const e = h.einstellungen;
    const N = a.einkommen;
    const startMonat = monatVon(heute);
    const plan = {
      analyse: a,
      startMonat,
      status: 'ok',
      strategie: e.strategie,
      sparanteil: STRATEGIEN[e.strategie].sparanteil,
      toepfe: [],
      sparen: 0,
      puffer: 0,
      wunschBudget: 0,
      kuerzung: 0,
      kuerzungen: { posten: [], ungedeckt: 0 },
      fehlbetrag: 0,
      frei: 0,
      simulation: null,
    };
    if (N <= 0) {
      plan.status = 'leer';
      return plan;
    }
    const spielraum = N - a.pflicht;
    if (spielraum <= 0) {
      plan.status = 'defizit';
      plan.fehlbetrag = -spielraum + a.wuensche;
      plan.kuerzung = a.wuensche;
      plan.toepfe = [
        { id: 'pflicht', name: 'Pflichtausgaben', betrag: a.bedarf },
        { id: 'raten', name: 'Kreditraten', betrag: a.raten },
      ];
      return plan;
    }
    const puffer = Math.min((N * e.puffer) / 100, spielraum);
    const frei = spielraum - puffer;
    plan.frei = frei;
    let sparen = frei * plan.sparanteil;
    let wunschBudget = frei - sparen;
    // Kleine Überschreitungen (bis 5 %, mindestens 10 €) sind keine Kürzung wert.
    const toleranz = Math.max(10, wunschBudget * 0.05);
    if (a.wuensche <= wunschBudget + toleranz) {
      // Wer schon weniger für Wünsche ausgibt, soll nicht mehr ausgeben – der Rest wird gespart.
      wunschBudget = a.wuensche;
      sparen = frei - a.wuensche;
    } else {
      plan.kuerzung = a.wuensche - wunschBudget;
      plan.kuerzungen = kuerzungsvorschlag(a.posten, plan.kuerzung);
    }
    // Daueraufträge in 5-€-Schritten; der Rundungsrest bleibt als Puffer auf dem Girokonto.
    const sparenRund = Math.floor(sparen / 5 + 1e-9) * 5;
    plan.sparen = sparenRund;
    plan.puffer = puffer + (sparen - sparenRund);
    plan.wunschBudget = wunschBudget;
    plan.toepfe = [
      { id: 'pflicht', name: 'Pflichtausgaben', betrag: a.bedarf },
      { id: 'raten', name: 'Kreditraten', betrag: a.raten },
      { id: 'wuensche', name: 'Wünsche', betrag: wunschBudget },
      { id: 'puffer', name: 'Puffer', betrag: plan.puffer },
      { id: 'sparen', name: 'Sparen & Tilgen', betrag: sparenRund },
    ];
    plan.simulation = simuliere(h, a, sparenRund, startMonat);
    return plan;
  }

  const STUFEN = {
    grundstock: 'Notgroschen-Grundstock',
    schulden: 'Teure Schulden tilgen',
    notgroschen: 'Notgroschen auffüllen',
    ziele: 'Sparziele',
    tilgen: 'Kredite schneller tilgen',
    vermoegen: 'Vermögensaufbau',
  };
  const STUFEN_REIHENFOLGE = ['grundstock', 'schulden', 'notgroschen', 'ziele', 'tilgen', 'vermoegen'];

  /** Monat für Monat: Verzinsung, reguläre Raten, dann Wasserfall der Sparrate. */
  function simuliere(h, a, sparrate, startMonat, monateMax) {
    const e = h.einstellungen;
    const laufzeit = monateMax || 360;
    const rTagesgeld = monatsZins(e.tagesgeld);
    const rRendite = monatsZins(e.rendite);
    const grundstock = a.pflicht;
    const ngZiel = a.pflicht * e.notgroschenMonate;
    let ng = h.ruecklagen.notgroschen;
    let depot = h.ruecklagen.anlagen;
    let frei = 0;

    const schulden = h.schulden
      .filter((s) => s.rest > 0)
      .map((s) => ({
        id: s.id,
        name: s.name,
        zins: s.zins,
        rate: s.rate,
        rest: s.rest,
        r: monatsZins(s.zins),
        offen: s.rest,
        zinsen: 0,
        getilgt: null,
        teuer: s.zins >= GRENZE_TEUER,
        lohnend: s.zins > e.rendite,
        waechst: false,
      }));
    const ziele = h.ziele
      .filter((z) => z.betrag > 0)
      .map((z) => {
        const f = frist(z, startMonat);
        const form = z.anlage === 'auto' ? anlageFuerLaufzeit(f) : z.anlage;
        const zinsPa = zinsFuerAnlage(form, e);
        return {
          id: z.id,
          name: z.name,
          betrag: z.betrag,
          prioritaet: z.prioritaet,
          termin: z.termin,
          stand: z.bereits,
          frist: f,
          form,
          zinsPa,
          r: monatsZins(zinsPa),
          erreicht: z.bereits >= z.betrag ? 0 : null,
          ersteRate: 0,
        };
      });

    const zielStand = () => summe(ziele.filter((z) => z.erreicht === null).map((z) => z.stand));
    const schuldStand = () => summe(schulden.map((s) => s.offen));
    const verlauf = [{ monat: 0, vermoegen: ng + depot + zielStand() - schuldStand(), anlagen: ng + depot + zielStand() }];
    const monate = [];
    let notgroschenVoll = ng >= ngZiel ? 0 : null;

    for (let t = 1; t <= laufzeit; t++) {
      ng *= 1 + rTagesgeld;
      depot *= 1 + rRendite;
      ziele.forEach((z) => {
        if (z.erreicht === null) z.stand *= 1 + z.r;
      });

      let freiAbNaechstemMonat = 0;
      schulden.forEach((s) => {
        if (s.getilgt !== null) return;
        const zins = s.offen * s.r;
        s.zinsen += zins;
        s.offen += zins;
        if (s.rate <= zins + 1e-9) s.waechst = true;
        s.offen -= Math.min(s.rate, s.offen);
        if (s.offen <= 0.005) {
          s.offen = 0;
          s.getilgt = t;
          freiAbNaechstemMonat += s.rate;
        }
      });

      let budget = sparrate + frei;
      const verteilung = {};
      const stufen = new Set();
      const gib = (schluessel, betrag, stufe) => {
        if (betrag <= 1e-9) return;
        verteilung[schluessel] = (verteilung[schluessel] || 0) + betrag;
        budget -= betrag;
        if (betrag >= 1) stufen.add(stufe);
      };

      // 1. Grundstock: ein Monat Pflichtausgaben
      if (ng < grundstock) {
        const x = Math.min(budget, grundstock - ng);
        ng += x;
        gib('notgroschen', x, 'grundstock');
      }
      // 2. Teure Schulden (Dispo, Kreditkarte), höchster Zins zuerst – Lawinenmethode
      schulden
        .filter((s) => s.getilgt === null && s.teuer)
        .sort((x, y) => y.zins - x.zins)
        .forEach((s) => {
          const x = Math.min(budget, s.offen);
          s.offen -= x;
          gib('schuld:' + s.id, x, 'schulden');
          if (s.offen <= 0.005) {
            s.offen = 0;
            s.getilgt = t;
            freiAbNaechstemMonat += s.rate;
          }
        });
      // 3. Notgroschen auffüllen (höchstens die Hälfte), parallel Sparziele nach Priorität
      if (ng < ngZiel && budget > 0) {
        const x = Math.min(ngZiel - ng, budget * 0.5);
        ng += x;
        gib('notgroschen', x, 'notgroschen');
      }
      [1, 2, 3].forEach((prio) => {
        const offen = ziele.filter((z) => z.erreicht === null && z.prioritaet === prio);
        if (!offen.length || budget <= 1e-9) return;
        const bedarf = offen.map((z) => {
          const restMonate = z.frist - t + 1;
          return restMonate <= 1 ? Math.max(0, z.betrag - z.stand) : noetigeRate(z.betrag, z.stand, restMonate, z.zinsPa);
        });
        const gesamt = summe(bedarf);
        const faktor = gesamt <= budget ? 1 : budget / gesamt;
        offen.forEach((z, i) => {
          const x = bedarf[i] * faktor;
          z.stand += x;
          if (t === 1) z.ersteRate = x;
          gib('ziel:' + z.id, x, 'ziele');
          if (z.stand >= z.betrag - 0.005) z.erreicht = t;
        });
      });
      if (ng < ngZiel && budget > 0) {
        const x = Math.min(ngZiel - ng, budget);
        ng += x;
        gib('notgroschen', x, 'notgroschen');
      }
      if (notgroschenVoll === null && ng >= ngZiel - 0.005) notgroschenVoll = t;
      // 4. Kredite, deren Zins über der erwarteten Rendite liegt: Sondertilgung schlägt Anlegen
      schulden
        .filter((s) => s.getilgt === null && !s.teuer && s.lohnend)
        .sort((x, y) => y.zins - x.zins)
        .forEach((s) => {
          const x = Math.min(budget, s.offen);
          s.offen -= x;
          gib('schuld:' + s.id, x, 'tilgen');
          if (s.offen <= 0.005) {
            s.offen = 0;
            s.getilgt = t;
            freiAbNaechstemMonat += s.rate;
          }
        });
      // 5. Was übrig bleibt, wird langfristig angelegt
      if (budget > 0.005) {
        depot += budget;
        gib('vermoegen', budget, 'vermoegen');
      }

      frei += freiAbNaechstemMonat;
      monate.push({ verteilung, stufen: STUFEN_REIHENFOLGE.filter((s) => stufen.has(s)) });
      verlauf.push({ monat: t, vermoegen: ng + depot + zielStand() - schuldStand(), anlagen: ng + depot + zielStand() });
    }

    return {
      verlauf,
      ersterMonat: monate.length ? monate[0].verteilung : {},
      letzterMonat: monate.length ? monate[monate.length - 1].verteilung : {},
      phasen: phasenBilden(monate, startMonat),
      ziele: ziele.map((z) => ({
        id: z.id,
        name: z.name,
        betrag: z.betrag,
        termin: z.termin,
        frist: z.frist,
        form: z.form,
        zinsPa: z.zinsPa,
        erreicht: z.erreicht,
        erreichtMonat: z.erreicht === null ? null : monatPlus(startMonat, Math.max(0, z.erreicht - 1)),
        puenktlich: z.erreicht !== null && z.erreicht <= z.frist,
        ersteRate: z.ersteRate,
        noetig: noetigeRate(z.betrag, h.ziele.find((x) => x.id === z.id).bereits, z.frist, z.zinsPa),
      })),
      schulden: schulden.map((s) => {
        const ohne = tilgung(s.rest, s.zins, s.rate);
        return {
          id: s.id,
          name: s.name,
          zins: s.zins,
          teuer: s.teuer,
          lohnend: s.lohnend,
          waechst: s.waechst && s.getilgt === null,
          getilgt: s.getilgt,
          getilgtMonat: s.getilgt === null ? null : monatPlus(startMonat, s.getilgt - 1),
          zinsen: s.zinsen,
          ohneMonate: ohne.monate,
          ohneZinsen: ohne.zinsen,
        };
      }),
      notgroschenZiel: ngZiel,
      notgroschenVoll,
      notgroschenVollMonat: notgroschenVoll ? monatPlus(startMonat, notgroschenVoll - 1) : null,
    };
  }

  /** Fasst aufeinanderfolgende Monate mit denselben Stufen zu Phasen zusammen. */
  function phasenBilden(monate, startMonat) {
    const phasen = [];
    monate.forEach((m, i) => {
      const schluessel = m.stufen.join('|');
      const letzte = phasen[phasen.length - 1];
      if (letzte && letzte.schluessel === schluessel) {
        letzte.bis = i + 1;
        Object.keys(m.verteilung).forEach((k) => {
          letzte.summen[k] = (letzte.summen[k] || 0) + m.verteilung[k];
        });
      } else {
        phasen.push({ schluessel, stufen: m.stufen, von: i + 1, bis: i + 1, summen: Object.assign({}, m.verteilung) });
      }
    });
    return phasen.map((p) => {
      const dauer = p.bis - p.von + 1;
      const durchschnitt = {};
      Object.keys(p.summen).forEach((k) => {
        durchschnitt[k] = p.summen[k] / dauer;
      });
      return {
        stufen: p.stufen,
        titel: p.stufen.map((s) => STUFEN[s]).join(' · ') || 'Nichts zu verteilen',
        von: p.von,
        bis: p.bis,
        vonMonat: monatPlus(startMonat, p.von - 1),
        bisMonat: monatPlus(startMonat, p.bis - 1),
        dauer,
        durchschnitt,
      };
    });
  }

  // ---------------------------------------------------------------- Bewertung einzelner Ausgaben

  const VERTRAGSKATEGORIEN = ['versicherung', 'kommunikation'];
  const ENERGIE = /strom|gas|heiz|energie|internet|dsl/i;

  function istVertrag(p) {
    return p.art === 'fix' && (VERTRAGSKATEGORIEN.includes(p.kategorie) || (p.kategorie === 'wohnen' && ENERGIE.test(p.name)));
  }

  function seltenGenutzt(p) {
    return p.nutzungen !== null && p.nutzungen !== undefined && p.nutzungen <= 2 && p.monatlich >= 5;
  }

  /** Urteil je Ausgabe nach Bedürfnisart, ABC-Klasse, Durchschnittsvergleich und Grenznutzen. */
  function bewertePosten(h, plan) {
    const a = plan.analyse;
    const e = h.einstellungen;
    const abc = {};
    abcAnalyse(allePosten(h, a)).forEach((p) => {
      abc[p.id] = p.klasse;
    });
    const vergleich = {};
    vergleichReferenz(a).forEach((v) => {
      vergleich[v.id] = v;
    });
    const kuerzung = {};
    plan.kuerzungen.posten.forEach((k) => {
      kuerzung[k.id] = k;
    });
    const angespannt = a.ueberschuss < 0 || (a.sparquote !== null && a.sparquote < 0.1) || (a.wunschquote !== null && a.wunschquote > 0.3);

    return a.posten
      .slice()
      .sort((x, y) => y.monatlich - x.monatlich)
      .map((p) => {
        const klasse = abc[p.id] || 'C';
        const ref = vergleich[KATEGORIEN[p.kategorie].referenz];
        const zehnJahre = endwertRate(p.monatlich, 120, e.rendite);
        let urteil;
        const gruende = [];
        if (p.beduerfnis === 'existenz') {
          urteil = 'notwendig';
          gruende.push('Existenzbedürfnis, lässt sich kaum vermeiden.');
          if (klasse === 'A' && ref && ref.stufe === 'hoch' && !ref.erklaerbar) {
            urteil = 'pruefen';
            gruende.push('Großer Posten in einer Kategorie über dem Durchschnitt: Minimalprinzip anwenden, geht es günstiger?');
          } else if (istVertrag(p)) {
            gruende.push('Vertrag: einmal im Jahr Tarife vergleichen (Minimalprinzip).');
          } else if (p.art === 'variabel' && klasse === 'A') {
            gruende.push('Großer variabler Posten: mit festem Wochenbudget und Einkaufsliste planen (Maximalprinzip).');
          }
        } else if (p.beduerfnis === 'kultur') {
          urteil = angespannt && klasse !== 'C' ? 'pruefen' : 'angemessen';
          gruende.push(
            urteil === 'pruefen'
              ? 'Kulturbedürfnis in angespannter Lage: Höhe überprüfen, nicht unbedingt streichen.'
              : 'Kulturbedürfnis in angemessener Höhe.'
          );
        } else {
          urteil = angespannt ? 'kuerzen' : 'bewusst';
          gruende.push(
            angespannt
              ? 'Verzichtbar und deine Sparquote ist zu niedrig. In 10 Jahren angelegt wären das ' + euro(zehnJahre, true) + '.'
              : 'Verzichtbar, aber dein Budget gibt es her. Opportunitätskosten in 10 Jahren: ' + euro(zehnJahre, true) + '.'
          );
        }
        if (seltenGenutzt(p)) {
          const proNutzung = p.nutzungen > 0 ? p.monatlich / p.nutzungen : null;
          if (urteil !== 'kuerzen') urteil = 'pruefen';
          gruende.push(
            proNutzung === null
              ? 'Wird gar nicht genutzt: Kündigen spart den vollen Betrag.'
              : 'Nur ' + zahlText(p.nutzungen) + '× im Monat genutzt, also ' + euro(proNutzung) + ' pro Nutzung (geringer Grenznutzen).'
          );
        }
        if (kuerzung[p.id]) gruende.push('Vorschlag: um ' + euro(kuerzung[p.id].kuerzung, true) + ' im Monat senken.');
        return {
          id: p.id,
          name: p.name,
          kategorie: p.kategorie,
          beduerfnis: p.beduerfnis,
          art: p.art,
          monatlich: p.monatlich,
          klasse,
          urteil,
          begruendung: gruende.join(' '),
        };
      });
  }

  // ---------------------------------------------------------------- Empfehlungen

  /** Konkrete nächste Schritte, nach Dringlichkeit sortiert. */
  function empfehlungen(h, plan) {
    const a = plan.analyse;
    const e = h.einstellungen;
    const liste = [];
    if (a.einkommen <= 0) return liste;
    if (a.ueberschuss < 0) {
      liste.push({
        stufe: 'dringend',
        titel: 'Du gibst ' + euro(-a.ueberschuss, true) + ' mehr aus, als du einnimmst',
        text: 'Ein dauerhaftes Minus endet im Dispo, der meist über 10 % Zinsen kostet. Kürze zuerst verzichtbare Ausgaben, prüfe dann die größten Pflichtposten.',
        potenzial: -a.ueberschuss,
        wissen: 'w-503020',
      });
    }
    if (plan.status === 'defizit') {
      liste.push({
        stufe: 'dringend',
        titel: 'Pflichtausgaben übersteigen dein Einkommen',
        text: 'Prüfe, ob dir Wohngeld, Berufsausbildungsbeihilfe (BAB), BAföG oder Kinderzuschlag zustehen. Eine Schuldnerberatung (z. B. Verbraucherzentrale, Caritas, Diakonie) hilft kostenlos.',
        potenzial: null,
        wissen: 'w-finanzpyramide',
      });
    }
    h.schulden
      .filter((s) => s.rest > 0 && (s.zins >= GRENZE_TEUER || s.zins > e.rendite))
      .sort((x, y) => y.zins - x.zins)
      .forEach((s) => {
        const teuer = s.zins >= GRENZE_TEUER;
        liste.push({
          stufe: teuer ? 'dringend' : 'tipp',
          titel: s.name + ' kostet ' + zahlText(s.zins) + ' % Zinsen',
          text: teuer
            ? 'Jeder Euro Sondertilgung bringt dir sicher ' + zahlText(s.zins) + ' %. Das schlägt jede Geldanlage. Tilge den teuersten Kredit zuerst (Lawinenmethode).'
            : 'Der Zins liegt über der erwarteten Rendite von ' + zahlText(e.rendite) + ' %. Sobald der Notgroschen steht, lohnt sich Sondertilgung mehr als Anlegen.',
          potenzial: null,
          wissen: 'w-schulden',
        });
      });
    if (a.reichweite !== null && a.reichweite < 1) {
      liste.push({
        stufe: 'dringend',
        titel: 'Baue einen Notgroschen auf',
        text: 'Ziel für den Anfang: ' + euro(a.pflicht, true) + ' (ein Monat Pflichtausgaben) auf einem Tagesgeldkonto. Danach weiter auf ' + zahlText(e.notgroschenMonate) + ' Monate.',
        potenzial: null,
        wissen: 'w-notgroschen',
      });
    } else if (a.reichweite !== null && a.reichweite < e.notgroschenMonate) {
      liste.push({
        stufe: 'wichtig',
        titel: 'Notgroschen auf ' + zahlText(e.notgroschenMonate) + ' Monate auffüllen',
        text: 'Es fehlen noch ' + euro(a.pflicht * e.notgroschenMonate - a.notgroschen, true) + '. Der Fahrplan unter „Aufteilung“ zeigt, wann du das schaffst.',
        potenzial: null,
        wissen: 'w-notgroschen',
      });
    }
    if (plan.kuerzung > 0.5 && plan.status !== 'defizit') {
      const top = plan.kuerzungen.posten.slice(0, 3).map((k) => k.name + ' (−' + euro(k.kuerzung, true) + ')');
      liste.push({
        stufe: 'wichtig',
        titel: 'Wünsche um ' + euro(plan.kuerzung, true) + ' im Monat senken',
        text: 'Damit erreichst du die Sparrate deiner Strategie. Am meisten bringt: ' + top.join(', ') + '. Mach es in zwei, drei Schritten, das hält länger.',
        potenzial: plan.kuerzung,
        wissen: 'w-503020',
      });
    }
    a.posten.filter(seltenGenutzt).forEach((p) => {
      liste.push({
        stufe: 'tipp',
        titel: p.name + ' kaum genutzt',
        text:
          (p.nutzungen > 0 ? euro(p.monatlich / p.nutzungen) + ' pro Nutzung. ' : 'Gar nicht genutzt. ') +
          'Der Nutzen jeder weiteren Nutzung ist klein, die Kosten bleiben (Grenznutzen). Kündigen oder pausieren?',
        potenzial: p.monatlich,
        wissen: 'w-grenznutzen',
      });
    });
    const vertraege = a.posten.filter(istVertrag);
    const vertragSumme = summe(vertraege.map((p) => p.monatlich));
    if (vertragSumme >= 30) {
      liste.push({
        stufe: 'tipp',
        titel: 'Verträge vergleichen: ' + euro(vertragSumme, true) + ' im Monat',
        text: 'Versicherungen, Handy, Internet und Energie: gleicher Nutzen für weniger Geld (Minimalprinzip). Schon 10 % weniger sparen ' + euro(vertragSumme * 1.2, true) + ' im Jahr.',
        potenzial: vertragSumme * 0.1,
        wissen: 'w-prinzip',
      });
    }
    vergleichReferenz(a)
      .filter((v) => v.stufe === 'hoch' && !v.erklaerbar)
      .forEach((v) => {
        liste.push({
          stufe: 'tipp',
          titel: v.name + ': ' + prozent(v.anteil) + ' statt ' + prozent(v.referenz),
          text: 'Du gibst hier deutlich mehr aus als ein durchschnittlicher Haushalt. Das kann bewusst so sein, ist aber ein guter Ort zum Nachrechnen.',
          potenzial: null,
          wissen: 'w-engel',
        });
      });
    if (a.ueberschuss > 0 && a.sparquote < 0.1 && plan.kuerzung <= 0.5) {
      liste.push({
        stufe: 'wichtig',
        titel: 'Zahl dich zuerst selbst',
        text: 'Richte zum Zahltag einen Dauerauftrag aufs Sparkonto ein. Was nicht auf dem Girokonto liegt, wird nicht nebenbei ausgegeben.',
        potenzial: null,
        wissen: 'w-pyf',
      });
    }
    if (!liste.some((x) => x.stufe !== 'tipp') && a.ueberschuss > 0) {
      liste.push({
        stufe: 'tipp',
        titel: 'Langfristig investieren',
        text: 'Deine Basis steht. Ein breit gestreuter ETF-Sparplan nutzt den Zinseszins am besten, wenn du 10 Jahre und länger Zeit hast.',
        potenzial: null,
        wissen: 'w-zinseszins',
      });
    }
    const rang = { dringend: 0, wichtig: 1, tipp: 2 };
    return liste.sort((x, y) => rang[x.stufe] - rang[y.stufe] || (y.potenzial || 0) - (x.potenzial || 0));
  }

  // ---------------------------------------------------------------- Haushaltsbuch

  /** Geplanter Monatsbetrag je Kategorie (aus den wiederkehrenden Posten). */
  function planProKategorie(h) {
    const plan = {};
    h.ausgaben.forEach((p) => {
      plan[p.kategorie] = (plan[p.kategorie] || 0) + monatlich(p);
    });
    const raten = summe(h.schulden.filter((s) => s.rest > 0).map((s) => s.rate));
    if (raten > 0) plan.kredit = raten;
    return plan;
  }

  function buchungenImMonat(h, monat) {
    return h.buchungen.filter((b) => b.datum.slice(0, 7) === monat);
  }

  /** Soll-Ist-Vergleich eines Monats. */
  function sollIst(h, monat) {
    const buchungen = buchungenImMonat(h, monat);
    const ist = {};
    let einnahmen = 0;
    let ausgaben = 0;
    buchungen.forEach((b) => {
      if (b.typ === 'einnahme') {
        einnahmen += b.betrag;
      } else {
        ausgaben += b.betrag;
        ist[b.kategorie] = (ist[b.kategorie] || 0) + b.betrag;
      }
    });
    const plan = planProKategorie(h);
    const ids = Array.from(new Set(Object.keys(plan).concat(Object.keys(ist))));
    const kategorien = ids
      .map((k) => {
        const p = plan[k] || 0;
        const i = ist[k] || 0;
        return { id: k, name: BUCHUNGSKATEGORIEN[k].name, plan: p, ist: i, differenz: i - p, quote: p > 0 ? i / p : null };
      })
      .sort((x, y) => Math.max(y.plan, y.ist) - Math.max(x.plan, x.ist));
    return {
      monat,
      anzahl: buchungen.length,
      einnahmen,
      ausgaben,
      saldo: einnahmen - ausgaben,
      planEinnahmen: summe(h.einnahmen.map(monatlich)),
      planAusgaben: summe(Object.keys(plan).map((k) => plan[k])),
      kategorien,
    };
  }

  /** Einnahmen und Ausgaben der letzten `anzahl` Monate bis einschließlich `bisMonat`. */
  function monatsverlauf(h, bisMonat, anzahl) {
    const n = anzahl || 12;
    const start = monatsIndex(bisMonat) - n + 1;
    const werte = [];
    for (let i = 0; i < n; i++) werte.push({ monat: monatAusIndex(start + i), einnahmen: 0, ausgaben: 0 });
    h.buchungen.forEach((b) => {
      const idx = monatsIndex(b.datum.slice(0, 7)) - start;
      if (idx < 0 || idx >= n) return;
      if (b.typ === 'einnahme') werte[idx].einnahmen += b.betrag;
      else werte[idx].ausgaben += b.betrag;
    });
    return werte;
  }

  /** Buchungen für alle monatlichen Fixposten, die im Monat noch fehlen. */
  function wiederkehrendeBuchungen(h, monat) {
    const vorhanden = new Set(buchungenImMonat(h, monat).filter((b) => b.quelle).map((b) => b.quelle));
    const datum = monat + '-01';
    const neu = [];
    h.einnahmen
      .filter((x) => x.intervall === 'monatlich' && x.betrag > 0 && !vorhanden.has(x.id))
      .forEach((x) => neu.push({ datum, betrag: x.betrag, typ: 'einnahme', kategorie: 'einkommen', notiz: x.name, quelle: x.id }));
    h.ausgaben
      .filter((x) => x.art === 'fix' && x.intervall === 'monatlich' && x.betrag > 0 && !vorhanden.has(x.id))
      .forEach((x) => neu.push({ datum, betrag: x.betrag, typ: 'ausgabe', kategorie: x.kategorie, notiz: x.name, quelle: x.id }));
    h.schulden
      .filter((x) => x.rest > 0 && x.rate > 0 && !vorhanden.has(x.id))
      .forEach((x) => neu.push({ datum, betrag: x.rate, typ: 'ausgabe', kategorie: 'kredit', notiz: 'Rate ' + x.name, quelle: x.id }));
    return neu;
  }

  return {
    INTERVALLE,
    KATEGORIEN,
    BUCHUNGSKATEGORIEN,
    GRENZE_TEUER,
    REFERENZ,
    BEDUERFNISSE,
    ARTEN,
    PRIORITAETEN,
    ANLAGEFORMEN,
    STRATEGIEN,
    STANDARD_EINSTELLUNGEN,
    STUFEN,
    summe,
    zahlAusText,
    euro,
    prozent,
    zahlText,
    monatVon,
    monatsIndex,
    monatPlus,
    monatName,
    dauerText,
    neueId,
    leererHaushalt,
    normalisiere,
    monatlich,
    analysiere,
    kennzahlen,
    gesamtnote,
    abcAnalyse,
    allePosten,
    vergleichReferenz,
    monatsZins,
    endwertRate,
    opportunitaetskosten,
    noetigeRate,
    monateBisZiel,
    sparplan,
    rateFuerZiel,
    tilgung,
    anlageFuerLaufzeit,
    zinsFuerAnlage,
    kuerzungsvorschlag,
    planen,
    bewertePosten,
    empfehlungen,
    planProKategorie,
    sollIst,
    monatsverlauf,
    wiederkehrendeBuchungen,
  };
});
