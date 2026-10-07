/* Finanzkompass – Oberfläche: Zustand, Ansichten, Formulare.
 * Gerechnet wird ausschließlich in logik.js; diese Datei zeigt nur an und nimmt Eingaben entgegen.
 * Alle Texte aus Eingaben laufen durch html`…` und werden dabei maskiert. */
(function () {
  'use strict';

  const L = window.Logik;
  const BSP = window.Beispiele;
  const D = window.Diagramme;
  const SCHLUESSEL = 'finanzkompass.v1';
  const SCHLUESSEL_UI = 'finanzkompass.oberflaeche';

  // ---------------------------------------------------------------- sicheres HTML

  class Roh {
    constructor(s) {
      this.s = s;
    }
  }
  const ZEICHEN = { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' };
  const esc = (s) => String(s).replace(/[&<>"']/g, (z) => ZEICHEN[z]);

  function teil(w) {
    if (w === null || w === undefined || w === false || w === true) return '';
    if (Array.isArray(w)) return w.map(teil).join('');
    if (w instanceof Roh) return w.s;
    return esc(w);
  }

  function html(t, ...werte) {
    let s = t[0];
    for (let i = 0; i < werte.length; i++) s += teil(werte[i]) + t[i + 1];
    return new Roh(s);
  }

  const $ = (sel, wurzel) => (wurzel || document).querySelector(sel);
  const $$ = (sel, wurzel) => Array.from((wurzel || document).querySelectorAll(sel));

  // ---------------------------------------------------------------- Formate und Farben

  const euro = (b) => L.euro(b);
  const euroGanz = (b) => L.euro(b, true);
  const prozent = (q, s) => L.prozent(q, s);
  const zahlFeld = (z) => (Number.isFinite(z) ? L.zahlText(z) : '');

  const FARBE = {
    pflicht: 'var(--s1)',
    raten: 'var(--s2)',
    wuensche: 'var(--s3)',
    puffer: 'var(--s4)',
    sparen: 'var(--s5)',
    grau: 'var(--grau)',
  };

  const STUFE = {
    gut: { name: 'Gut', symbol: '✓' },
    mittel: { name: 'Ausbaufähig', symbol: '!' },
    schlecht: { name: 'Kritisch', symbol: '✕' },
    neutral: { name: 'Info', symbol: 'i' },
  };

  const URTEIL = {
    notwendig: { name: 'Notwendig', stufe: 'neutral' },
    angemessen: { name: 'Angemessen', stufe: 'gut' },
    bewusst: { name: 'Bewusst genießen', stufe: 'gut' },
    pruefen: { name: 'Prüfen', stufe: 'mittel' },
    kuerzen: { name: 'Kürzen', stufe: 'schlecht' },
  };

  const DRINGLICHKEIT = {
    dringend: { name: 'Dringend', stufe: 'schlecht' },
    wichtig: { name: 'Wichtig', stufe: 'mittel' },
    tipp: { name: 'Tipp', stufe: 'neutral' },
  };

  const BEDUERFNIS_TEXT = {
    existenz: 'Lebensnotwendig: Wohnen, Grundnahrung, Energie, Weg zur Arbeit, Pflichtversicherungen.',
    kultur: 'Gehört zu einem normalen Leben dazu, die Höhe ist aber wählbar: Hobby, Kleidung über das Nötige, Verein, Bücher.',
    luxus: 'Angenehm, aber verzichtbar: zweiter Streamingdienst, Lieferdienst, Markenartikel, Spontankäufe.',
  };

  function chip(stufe, text) {
    const s = STUFE[stufe] || STUFE.neutral;
    return html`<span class="chip chip--${stufe || 'neutral'}"><span class="chip-symbol" aria-hidden="true">${s.symbol}</span>${text || s.name}</span>`;
  }

  // ---------------------------------------------------------------- Zustand und Speicher

  let haushalt = null;
  let speicherbar = true;
  const ui = {
    ansicht: 'uebersicht',
    monat: L.monatVon(new Date()),
    horizont: 10,
    rechner: { modus: 'endwert', start: 1000, rate: 100, ziel: 50000, jahre: 20, rendite: 5, dynamik: 0, inflation: 2 },
    buchung: { typ: 'ausgabe', kategorie: 'lebensmittel' },
  };

  function laden() {
    let roh = null;
    try {
      roh = localStorage.getItem(SCHLUESSEL);
    } catch (e) {
      speicherbar = false;
    }
    if (roh) {
      try {
        return L.normalisiere(JSON.parse(roh));
      } catch (e) {
        try {
          localStorage.setItem(SCHLUESSEL + '.defekt', roh);
        } catch (_) {
          /* nichts zu retten */
        }
      }
    }
    return L.normalisiere(BSP.laden('azubi'));
  }

  function speichern() {
    try {
      localStorage.setItem(SCHLUESSEL, JSON.stringify(haushalt));
      speicherbar = true;
    } catch (e) {
      speicherbar = false;
    }
    speicherstatus();
  }

  function uiLaden() {
    try {
      const r = JSON.parse(localStorage.getItem(SCHLUESSEL_UI) || 'null');
      if (r && typeof r === 'object') {
        if ([5, 10, 20, 30].includes(r.horizont)) ui.horizont = r.horizont;
        if (r.rechner && typeof r.rechner === 'object') {
          Object.keys(ui.rechner).forEach((k) => {
            if (k === 'modus') ui.rechner.modus = r.rechner.modus === 'rate' ? 'rate' : 'endwert';
            else if (Number.isFinite(r.rechner[k])) ui.rechner[k] = r.rechner[k];
          });
        }
      }
    } catch (e) {
      /* ohne gemerkte Ansicht weiter */
    }
  }

  function uiSpeichern() {
    try {
      localStorage.setItem(SCHLUESSEL_UI, JSON.stringify({ horizont: ui.horizont, rechner: ui.rechner }));
    } catch (e) {
      /* nicht schlimm */
    }
  }

  function speicherstatus() {
    const el = $('#speicherstatus');
    if (!el) return;
    el.textContent = speicherbar ? 'Gespeichert in diesem Browser' : 'Nicht gespeichert: Browser-Speicher gesperrt';
    el.classList.toggle('speicherstatus--warnung', !speicherbar);
  }

  /** Ändert den Haushalt, speichert, zeichnet neu und bietet bei Bedarf „Rückgängig“ an. */
  function aendern(veraenderung, meldung) {
    const vorher = JSON.stringify(haushalt);
    veraenderung(haushalt);
    haushalt = L.normalisiere(haushalt);
    speichern();
    rendern();
    if (meldung) toast(meldung, vorher);
  }

  // ---------------------------------------------------------------- Rückmeldung

  let toastTimer = null;
  let toastVorher = null;

  function toast(text, vorher) {
    const el = $('#toast');
    toastVorher = vorher || null;
    el.innerHTML = html`<span>${text}</span>${vorher ? html`<button type="button" class="knopf knopf--klein knopf--hell" data-aktion="rueckgaengig">Rückgängig</button>` : ''}`.s;
    el.hidden = false;
    clearTimeout(toastTimer);
    toastTimer = setTimeout(() => {
      el.hidden = true;
      toastVorher = null;
    }, 7000);
  }

  // ---------------------------------------------------------------- Navigation und Zeichnen

  const REITER = ['uebersicht', 'haushalt', 'bewertung', 'aufteilung', 'sparplaene', 'buch', 'wissen'];
  const ANSICHTEN = {
    uebersicht: ansichtUebersicht,
    haushalt: ansichtHaushalt,
    bewertung: ansichtBewertung,
    aufteilung: ansichtAufteilung,
    sparplaene: ansichtSparplaene,
    buch: ansichtBuch,
  };

  function route() {
    const h = decodeURIComponent(location.hash.slice(1));
    if (REITER.includes(h)) return { ansicht: h };
    if (/^w-[a-z0-9-]+$/.test(h) && document.getElementById(h)) return { ansicht: 'wissen', anker: h };
    return { ansicht: 'uebersicht' };
  }

  function navigieren() {
    const r = route();
    const gewechselt = r.ansicht !== ui.ansicht;
    ui.ansicht = r.ansicht;
    rendern();
    if (r.anker) {
      const d = document.getElementById(r.anker);
      d.open = true;
      d.scrollIntoView({ block: 'start' });
      d.querySelector('summary').focus({ preventScroll: true });
    } else if (gewechselt) {
      window.scrollTo(0, 0);
    }
  }

  let diagrammAuftraege = [];

  function diagramm(art, daten) {
    diagrammAuftraege.push({ art, daten });
    return html`<div class="diagramm" data-diagramm="${diagrammAuftraege.length - 1}"></div>`;
  }

  function diagrammeZeichnen() {
    $$('[data-diagramm]').forEach((el) => {
      if (!el.offsetParent) return;
      const auftrag = diagrammAuftraege[Number(el.dataset.diagramm)];
      if (auftrag) D[auftrag.art](el, auftrag.daten);
    });
  }

  function fokusMerken() {
    const a = document.activeElement;
    if (!a || a === document.body || $('#dialog').contains(a)) return null;
    if (a.id) return '#' + CSS.escape(a.id);
    if (a.dataset && a.dataset.aktion) {
      let sel = '[data-aktion="' + CSS.escape(a.dataset.aktion) + '"]';
      if (a.dataset.id) sel += '[data-id="' + CSS.escape(a.dataset.id) + '"]';
      if (a.dataset.wert) sel += '[data-wert="' + CSS.escape(a.dataset.wert) + '"]';
      return sel;
    }
    return null;
  }

  function rendern() {
    const fokus = fokusMerken();
    $$('[data-reiter]').forEach((a) => {
      if (a.dataset.reiter === ui.ansicht) a.setAttribute('aria-current', 'page');
      else a.removeAttribute('aria-current');
    });
    $$('[data-ansicht]').forEach((s) => {
      s.hidden = s.dataset.ansicht !== ui.ansicht;
      // Verborgene Ansichten leeren: keine doppelten IDs, kein veralteter Inhalt
      if (s.hidden && ANSICHTEN[s.dataset.ansicht]) s.textContent = '';
    });
    $('#beispiel-hinweis').innerHTML = beispielHinweis().s;
    const erzeugen = ANSICHTEN[ui.ansicht];
    if (erzeugen) {
      diagrammAuftraege = [];
      $('#ansicht-' + ui.ansicht).innerHTML = erzeugen().s;
      diagrammeZeichnen();
    }
    D.tip.verbergen();
    if (fokus) {
      const el = $(fokus);
      if (el && el !== document.activeElement) el.focus({ preventScroll: true });
    }
  }

  // ---------------------------------------------------------------- Bausteine

  function kopf(titel, unterzeile, aktionen) {
    return html`<header class="ansicht-kopf">
      <div><h1>${titel}</h1>${unterzeile ? html`<p>${unterzeile}</p>` : ''}</div>
      ${aktionen ? html`<div class="ansicht-aktionen">${aktionen}</div>` : ''}
    </header>`;
  }

  function karte(titel, inhalt, o) {
    const opt = o || {};
    return html`<section class="karte ${opt.klasse || ''}" ${opt.id ? html`id="${opt.id}"` : ''}>
      <div class="karte-kopf">
        <div><h2>${titel}</h2>${opt.unter ? html`<p class="karte-unter">${opt.unter}</p>` : ''}</div>
        ${opt.aktion || ''}
      </div>
      ${inhalt}
    </section>`;
  }

  function wissen(anker, text) {
    return html`<a class="wissen-link" href="#${anker}">${text || 'Wie wird das bewertet?'}</a>`;
  }

  function knopf(text, aktion, o) {
    const opt = o || {};
    return html`<button type="button" class="knopf ${opt.klasse || ''}" data-aktion="${aktion}" ${opt.id ? html`data-id="${opt.id}"` : ''} ${opt.wert !== undefined ? html`data-wert="${opt.wert}"` : ''} ${opt.label ? html`aria-label="${opt.label}"` : ''}>${text}</button>`;
  }

  function kachel(label, wert, unter, extra) {
    return html`<div class="kachel">
      <span class="kachel-label">${label}</span>
      <span class="kachel-wert">${wert}</span>
      <span class="kachel-unter">${unter}${extra ? html` ${extra}` : ''}</span>
    </div>`;
  }

  function tipAttribute(titel, wert, name, farbe) {
    return html`tabindex="0" data-tip-titel="${titel}" data-tip-wert="${wert}" data-tip-name="${name || ''}" data-tip-farbe="${farbe || ''}"`;
  }

  /** 100-%-Stapelbalken; die Werte stehen zusätzlich in der Legende darunter. */
  function stapel(segmente, label) {
    const sichtbar = segmente.filter((s) => s.anteil > 0.0005);
    return html`<div class="stapel" role="img" aria-label="${label}: ${sichtbar.map((s) => s.name + ' ' + prozent(s.anteil)).join(', ')}">
      ${sichtbar.map(
        (s) => html`<span class="stapel-teil" style="flex-grow:${s.anteil.toFixed(5)};background:${s.farbe}" ${tipAttribute(s.name, euroGanz(s.betrag), prozent(s.anteil, 1) + ' ' + (s.bezug || 'vom Einkommen'), s.farbe)}></span>`
      )}
    </div>`;
  }

  function legende(segmente) {
    return html`<ul class="legende">
      ${segmente.map(
        (s) => html`<li><span class="legende-farbe" style="background:${s.farbe}"></span><span class="legende-name">${s.name}</span><span class="legende-wert">${euroGanz(s.betrag)}${s.anteil !== undefined ? html` · ${prozent(s.anteil)}` : ''}</span></li>`
      )}
    </ul>`;
  }

  /** Waagerechte Balkenliste in einer Farbe; `soll` zeichnet eine Zielmarke ein. */
  function balkenliste(zeilen, o) {
    const opt = o || {};
    const max = opt.max || Math.max.apply(null, zeilen.map((z) => Math.max(z.wert, z.soll || 0)).concat([1e-9]));
    return html`<div class="balkenliste">
      ${zeilen.map(
        (z) => html`<div class="balkenzeile" ${tipAttribute(z.name, z.tipWert || z.text, z.tipName || '', z.farbe || 'var(--s1)')}>
          <span class="balken-name">${z.name}${z.unter ? html`<small>${z.unter}</small>` : ''}</span>
          <span class="balken-spur">
            <span class="balken" style="width:${Math.min(100, (Math.max(0, z.wert) / max) * 100).toFixed(2)}%;background:${z.farbe || 'var(--s1)'}"></span>
            ${z.soll !== undefined ? html`<span class="balken-soll" style="left:${Math.min(100, (z.soll / max) * 100).toFixed(2)}%" title="${opt.sollName || 'Ziel'}"></span>` : ''}
          </span>
          <span class="balken-wert">${z.text}${z.chip || ''}</span>
        </div>`
      )}
    </div>`;
  }

  function meter(anteil, stufe, label) {
    const breite = Math.max(0, Math.min(1, anteil || 0)) * 100;
    return html`<div class="meter meter--${stufe || 'akzent'}" role="meter" aria-valuemin="0" aria-valuemax="100" aria-valuenow="${Math.round(breite)}" aria-label="${label || 'Fortschritt'}"><span style="width:${breite.toFixed(1)}%"></span></div>`;
  }

  function tabellenAnsicht(titel, kopfzeile, zeilen) {
    return html`<details class="tabellen-ansicht">
      <summary>${titel || 'Werte als Tabelle'}</summary>
      <div class="tabelle-rahmen"><table class="tabelle">
        <thead><tr>${kopfzeile.map((k, i) => html`<th ${i ? html`class="zahl"` : ''}>${k}</th>`)}</tr></thead>
        <tbody>${zeilen.map((z) => html`<tr>${z.map((w, i) => html`<td ${i ? html`class="zahl"` : ''}>${w}</td>`)}</tr>`)}</tbody>
      </table></div>
    </details>`;
  }

  function leer(text, aktion) {
    return html`<div class="leer"><p>${text}</p>${aktion || ''}</div>`;
  }

  function anlageName(form) {
    return L.ANLAGEFORMEN[form].name;
  }

  // ---------------------------------------------------------------- Beispiel-Hinweis

  function beispielHinweis() {
    if (!haushalt.beispiel) return html``;
    return html`<div class="hinweis-beispiel" role="status">
      <p><strong>Beispieldaten: ${haushalt.beispiel}.</strong> Das sind ausgedachte Zahlen zum Ausprobieren, nicht deine.</p>
      <div class="hinweis-aktionen">
        ${knopf('Mit eigenen Zahlen starten', 'neu-beginnen', { klasse: 'knopf--primaer knopf--klein' })}
        ${knopf('Beispiel als Vorlage behalten', 'beispiel-behalten', { klasse: 'knopf--klein' })}
        ${knopf('Anderes Beispiel', 'daten-dialog', { klasse: 'knopf--klein knopf--leise' })}
      </div>
    </div>`;
  }

  // ================================================================ Übersicht

  function ansichtUebersicht() {
    if (!haushalt.einnahmen.length && !haushalt.ausgaben.length) return startseite();
    const plan = L.planen(haushalt);
    const a = plan.analyse;
    const e = haushalt.einstellungen;
    const kz = L.kennzahlen(a, e);
    const nach = Object.fromEntries(kz.map((k) => [k.id, k]));
    const note = L.gesamtnote(kz);
    const emp = L.empfehlungen(haushalt, plan);
    return html`
      ${kopf('Übersicht', 'Deine Finanzen auf einen Blick, alle Beträge pro Monat.')}
      <div class="raster raster--oben">
        ${notenKarte(note)}
        <div class="kacheln">
          ${kachel('Einnahmen', euroGanz(a.einkommen), 'netto pro Monat')}
          ${kachel('Ausgaben', euroGanz(a.ausgaben), 'inklusive Kreditraten')}
          ${kachel(a.ueberschuss >= 0 ? 'Übrig' : 'Fehlbetrag', euroGanz(a.ueberschuss), 'Sparquote ' + prozent(a.sparquote), nach.sparquote.stufe ? chip(nach.sparquote.stufe) : '')}
          ${kachel('Notgroschen', a.reichweite === null ? euroGanz(a.notgroschen) : L.zahlText(Math.floor(a.reichweite * 10) / 10) + ' Monate', euroGanz(a.notgroschen) + ' Rücklage', nach.reichweite.stufe ? chip(nach.reichweite.stufe) : '')}
        </div>
      </div>
      ${karte('Wohin dein Geld fließt', geldfluss(a), { unter: 'Dein Ist-Zustand im Vergleich zur 50/30/20-Regel' })}
      <div class="raster raster--zwei">
        ${karte('Ausgaben nach Kategorie', kategorienBalken(a), { unter: 'Konsumausgaben ohne Kreditraten' })}
        ${karte('Was du jetzt tun solltest', emp.length ? empfehlungsliste(emp.slice(0, 3)) : leer('Keine offenen Punkte. Gut gemacht!'), {
          aktion: emp.length > 3 ? html`<a class="knopf knopf--klein knopf--leise" href="#bewertung">Alle ${emp.length} ansehen</a>` : '',
        })}
      </div>
      <div class="raster raster--zwei">
        ${karte('Sparziele', zieleKurz(plan), { aktion: html`<a class="knopf knopf--klein knopf--leise" href="#sparplaene">Sparpläne</a>` })}
        ${karte('Dieser Monat', monatKurz(), { aktion: html`<a class="knopf knopf--klein knopf--leise" href="#buch">Haushaltsbuch</a>` })}
      </div>`;
  }

  function startseite() {
    return html`
      ${kopf('Willkommen beim Finanzkompass', 'Trag deine Einnahmen und regelmäßigen Ausgaben ein. Die App bewertet sie, teilt dein Geld realistisch auf und erstellt Sparpläne.')}
      <div class="raster raster--zwei">
        ${karte('Mit eigenen Zahlen beginnen', html`<p>Am besten nimmst du deine Kontoauszüge der letzten drei Monate zur Hand. Erst Einnahmen, dann feste Kosten wie Miete und Verträge, dann variable Ausgaben wie Lebensmittel.</p>
          <div class="knopfreihe">${knopf('Erste Einnahme eintragen', 'neu-einnahme', { klasse: 'knopf--primaer' })}${knopf('Erste Ausgabe eintragen', 'neu-ausgabe')}</div>`)}
        ${karte('Erst einmal ausprobieren', beispielAuswahl())}
      </div>`;
  }

  function beispielAuswahl() {
    return html`<div class="beispiele">
      ${Object.keys(BSP.VORLAGEN).map(
        (k) => html`<button type="button" class="beispiel" data-aktion="beispiel" data-id="${k}">
          <strong>${BSP.VORLAGEN[k].titel}</strong><span>${BSP.VORLAGEN[k].beschreibung}</span>
        </button>`
      )}
    </div>`;
  }

  function notenKarte(note) {
    if (!note) {
      return html`<div class="note-karte">${leer('Sobald Einnahmen und Ausgaben eingetragen sind, bekommst du hier eine Note.')}</div>`;
    }
    const umfang = 2 * Math.PI * 42;
    const stufe = note.note <= 2 ? 'gut' : note.note <= 4 ? 'mittel' : 'schlecht';
    return html`<div class="note-karte">
      <svg class="note-ring" viewBox="0 0 100 100" aria-hidden="true">
        <circle cx="50" cy="50" r="42" class="note-spur"></circle>
        <circle cx="50" cy="50" r="42" class="note-wert note-wert--${stufe}" stroke-dasharray="${((note.punkte / 100) * umfang).toFixed(1)} ${umfang.toFixed(1)}" transform="rotate(-90 50 50)"></circle>
      </svg>
      <div class="note-text">
        <span class="note-label">Finanznote</span>
        <span class="note-zahl">${note.note}</span>
        <span class="note-name">${note.name} · ${note.punkte} von 100 Punkten</span>
        ${wissen('w-note', 'So entsteht die Note')}
      </div>
    </div>`;
  }

  function geldfluss(a) {
    const basis = Math.max(a.einkommen, a.ausgaben) || 1;
    const ist = [
      { id: 'pflicht', name: 'Pflichtausgaben', betrag: a.bedarf },
      { id: 'raten', name: 'Kreditraten', betrag: a.raten },
      { id: 'wuensche', name: 'Wünsche', betrag: a.wuensche },
      { id: 'sparen', name: 'Übrig zum Sparen', betrag: Math.max(0, a.ueberschuss) },
    ]
      .filter((s) => s.betrag > 0)
      .map((s) => Object.assign(s, { anteil: s.betrag / basis, farbe: FARBE[s.id] }));
    const soll = [
      { id: 'pflicht', name: 'Bedarf', anteil: 0.5 },
      { id: 'wuensche', name: 'Wünsche', anteil: 0.3 },
      { id: 'sparen', name: 'Sparen', anteil: 0.2 },
    ].map((s) => Object.assign(s, { betrag: s.anteil * a.einkommen, farbe: FARBE[s.id] }));
    return html`
      <div class="stapel-gruppe">
        <div class="stapel-zeile"><span class="stapel-label">Du</span>${stapel(ist, 'Ist-Verteilung')}</div>
        <div class="stapel-zeile"><span class="stapel-label">50/30/20</span>${stapel(soll, 'Richtwert')}</div>
      </div>
      ${a.ueberschuss < 0 ? html`<p class="warnzeile">${chip('schlecht', 'Minus')} Deine Ausgaben übersteigen dein Einkommen um ${euroGanz(-a.ueberschuss)} im Monat.</p>` : ''}
      ${legende(ist.map((s) => Object.assign({}, s, { anteil: a.einkommen > 0 ? s.betrag / a.einkommen : undefined })))}
      <p class="erklaerung">Faustregel: höchstens 50 % für Pflichtausgaben und Kreditraten, 30 % für Wünsche, mindestens 20 % sparen. ${wissen('w-503020', 'Mehr zur 50/30/20-Regel')}</p>`;
  }

  function kategorienBalken(a) {
    if (!a.nachKategorie.length) return leer('Noch keine Ausgaben eingetragen.', knopf('Ausgabe eintragen', 'neu-ausgabe', { klasse: 'knopf--primaer' }));
    const oben = a.nachKategorie.slice(0, 7);
    const rest = a.nachKategorie.slice(7);
    if (rest.length) oben.push({ id: 'weitere', name: 'Weitere ' + rest.length + ' Kategorien', betrag: L.summe(rest.map((r) => r.betrag)) });
    return balkenliste(
      oben.map((k) => ({
        name: k.name,
        wert: k.betrag,
        text: euroGanz(k.betrag),
        tipWert: euroGanz(k.betrag),
        tipName: prozent(k.betrag / a.konsum, 1) + ' deiner Konsumausgaben',
      }))
    );
  }

  function empfehlungsliste(liste) {
    return html`<ol class="empfehlungen">
      ${liste.map(
        (e) => html`<li class="empfehlung">
          <div class="empfehlung-kopf">${chip(DRINGLICHKEIT[e.stufe].stufe, DRINGLICHKEIT[e.stufe].name)}<strong>${e.titel}</strong></div>
          <p>${e.text}</p>
          <p class="empfehlung-fuss">${e.potenzial ? html`<span class="potenzial">bis zu ${euroGanz(e.potenzial)} im Monat</span>` : ''}${wissen(e.wissen, 'Hintergrund')}</p>
        </li>`
      )}
    </ol>`;
  }

  function zieleKurz(plan) {
    if (!haushalt.ziele.length) return leer('Noch keine Sparziele.', knopf('Sparziel anlegen', 'neu-ziel', { klasse: 'knopf--primaer' }));
    const sim = plan.simulation;
    return html`<ul class="ziel-kurzliste">
      ${haushalt.ziele.map((z) => {
        const s = sim && sim.ziele.find((x) => x.id === z.id);
        return html`<li>
          <div class="ziel-kurz-kopf"><strong>${z.name}</strong><span>${euroGanz(z.bereits)} von ${euroGanz(z.betrag)}</span></div>
          ${meter(z.betrag ? z.bereits / z.betrag : 0, 'akzent', z.name)}
          <p class="ziel-kurz-fuss">Geplant ${L.monatName(z.termin)}${s ? (s.erreicht === null ? html` · ${chip('schlecht', 'mit dieser Sparrate nicht erreichbar')}` : html` · voraussichtlich ${L.monatName(s.erreichtMonat || plan.startMonat)} ${chip(s.puenktlich ? 'gut' : 'mittel', s.puenktlich ? 'pünktlich' : 'später')}`) : ''}</p>
        </li>`;
      })}
    </ul>`;
  }

  function monatKurz() {
    const si = L.sollIst(haushalt, L.monatVon(new Date()));
    if (!si.anzahl) return leer('In diesem Monat ist noch nichts gebucht.', html`<a class="knopf knopf--primaer" href="#buch">Erste Buchung erfassen</a>`);
    const quote = si.planAusgaben > 0 ? si.ausgaben / si.planAusgaben : null;
    const stufe = quote === null ? 'akzent' : quote > 1 ? 'schlecht' : quote > 0.9 ? 'mittel' : 'akzent';
    return html`
      <div class="monat-kurz">
        <div class="monat-kurz-zeile"><span>Ausgegeben</span><strong>${euroGanz(si.ausgaben)}</strong><span class="leise">von ${euroGanz(si.planAusgaben)} geplant</span></div>
        ${meter(quote, stufe, 'Ausgaben im Vergleich zum Plan')}
        <div class="monat-kurz-zeile"><span>Eingenommen</span><strong>${euroGanz(si.einnahmen)}</strong><span class="leise">Saldo ${euroGanz(si.saldo)}</span></div>
      </div>
      <p class="erklaerung">${si.anzahl} Buchungen im ${L.monatName(si.monat, true)}. Der Monat läuft noch, deshalb ist der Vergleich vorläufig.</p>`;
  }

  // ================================================================ Einnahmen & Ausgaben

  function ansichtHaushalt() {
    const a = L.analysiere(haushalt);
    return html`
      ${kopf('Einnahmen & Ausgaben', 'Trag regelmäßige Beträge so ein, wie sie anfallen. Jährliche oder vierteljährliche Kosten rechnet die App auf den Monat um.')}
      ${karte('Einnahmen', tabelleEinnahmen(a), { aktion: knopf('+ Einnahme', 'neu-einnahme', { klasse: 'knopf--primaer knopf--klein' }), unter: 'Netto, also das, was auf dem Konto ankommt' })}
      ${karte('Ausgaben', tabelleAusgaben(a), { aktion: knopf('+ Ausgabe', 'neu-ausgabe', { klasse: 'knopf--primaer knopf--klein' }), unter: 'Ohne Kreditraten, die stehen unten bei den Krediten' })}
      <div class="raster raster--zwei">
        ${karte('Kredite & Schulden', tabelleSchulden(), { aktion: knopf('+ Kredit', 'neu-schuld', { klasse: 'knopf--primaer knopf--klein' }), unter: 'Dispo, Kreditkarte, Ratenkäufe, Autokredit' })}
        ${karte('Rücklagen', ruecklagenFormular(), { unter: 'Was du schon angespart hast' })}
      </div>`;
  }

  function aktionsknoepfe(art, id, name) {
    return html`<div class="zeilen-aktionen">
      <button type="button" class="knopf knopf--symbol" data-aktion="bearbeiten-${art}" data-id="${id}" aria-label="${name} bearbeiten" title="Bearbeiten">✎</button>
      <button type="button" class="knopf knopf--symbol" data-aktion="loeschen-${art}" data-id="${id}" aria-label="${name} löschen" title="Löschen">✕</button>
    </div>`;
  }

  function tabelleEinnahmen(a) {
    if (!haushalt.einnahmen.length) return leer('Noch keine Einnahmen. Lohn, Ausbildungsvergütung, Kindergeld, BAföG, Nebenjob …');
    return html`<div class="tabelle-rahmen"><table class="tabelle">
      <thead><tr><th>Bezeichnung</th><th class="zahl">Betrag</th><th class="zahl">pro Monat</th><th><span class="sr-only">Aktionen</span></th></tr></thead>
      <tbody>${haushalt.einnahmen.map(
        (x) => html`<tr>
          <td>${x.name}</td>
          <td class="zahl">${euro(x.betrag)}<small>${L.INTERVALLE[x.intervall].name}</small></td>
          <td class="zahl">${euro(L.monatlich(x))}</td>
          <td>${aktionsknoepfe('einnahme', x.id, x.name)}</td>
        </tr>`
      )}</tbody>
      <tfoot><tr><th>Summe</th><td></td><td class="zahl">${euro(a.einkommen)}</td><td></td></tr></tfoot>
    </table></div>`;
  }

  function tabelleAusgaben(a) {
    if (!haushalt.ausgaben.length) return leer('Noch keine Ausgaben. Fang mit den großen festen Posten an: Miete, Strom, Handy, Versicherungen.');
    const sortiert = a.posten.slice().sort((x, y) => y.monatlich - x.monatlich);
    return html`<div class="tabelle-rahmen"><table class="tabelle">
      <thead><tr><th>Bezeichnung</th><th class="zahl">Betrag</th><th class="zahl">pro Monat</th><th>Bedürfnis</th><th class="nur-breit">Art</th><th><span class="sr-only">Aktionen</span></th></tr></thead>
      <tbody>${sortiert.map(
        (x) => html`<tr>
          <td>${x.name}<small>${L.KATEGORIEN[x.kategorie].name}</small></td>
          <td class="zahl">${euro(x.betrag)}<small>${L.INTERVALLE[x.intervall].name}</small></td>
          <td class="zahl">${euro(x.monatlich)}</td>
          <td><span class="marke-beduerfnis marke-beduerfnis--${x.beduerfnis}" title="${L.BEDUERFNISSE[x.beduerfnis].name}">${L.BEDUERFNISSE[x.beduerfnis].kurz}</span></td>
          <td class="nur-breit">${L.ARTEN[x.art].name}</td>
          <td>${aktionsknoepfe('ausgabe', x.id, x.name)}</td>
        </tr>`
      )}</tbody>
      <tfoot><tr><th>Summe</th><td></td><td class="zahl">${euro(a.konsum)}</td><td colspan="3" class="leise">davon Fixkosten ${euroGanz(a.fix - a.raten)}</td></tr></tfoot>
    </table></div>`;
  }

  function tabelleSchulden() {
    if (!haushalt.schulden.length) return leer('Keine Kredite eingetragen. Schuldenfrei ist die beste Ausgangslage.');
    return html`<div class="tabelle-rahmen"><table class="tabelle">
      <thead><tr><th>Kredit</th><th class="zahl">Rest</th><th class="zahl">Zins</th><th class="zahl">Rate</th><th class="zahl">Laufzeit</th><th><span class="sr-only">Aktionen</span></th></tr></thead>
      <tbody>${haushalt.schulden.map((s) => {
        const t = L.tilgung(s.rest, s.zins, s.rate);
        return html`<tr>
          <td>${s.name}${s.zins >= L.GRENZE_TEUER ? html`<small>${chip('schlecht', 'teuer')}</small>` : ''}</td>
          <td class="zahl">${euroGanz(s.rest)}</td>
          <td class="zahl">${L.zahlText(s.zins)} %</td>
          <td class="zahl">${euro(s.rate)}</td>
          <td class="zahl">${s.rest <= 0 ? 'getilgt' : t.monate === null ? html`${chip('schlecht', 'Rate zu klein')}` : html`${L.dauerText(t.monate)}<small>${euroGanz(t.zinsen)} Zinsen</small>`}</td>
          <td>${aktionsknoepfe('schuld', s.id, s.name)}</td>
        </tr>`;
      })}</tbody>
    </table></div>`;
  }

  function ruecklagenFormular() {
    const r = haushalt.ruecklagen;
    return html`<form class="formular formular--eng" data-formular="ruecklagen">
      <label class="feld"><span>Notgroschen (Tagesgeld, Sparkonto)</span>
        <input id="r-notgroschen" name="notgroschen" inputmode="decimal" autocomplete="off" value="${zahlFeld(r.notgroschen)}"></label>
      <label class="feld"><span>Geldanlagen (Depot, ETF, Bausparvertrag)</span>
        <input id="r-anlagen" name="anlagen" inputmode="decimal" autocomplete="off" value="${zahlFeld(r.anlagen)}"></label>
      <p class="formfehler" role="alert" hidden></p>
      <div class="knopfreihe"><button type="submit" class="knopf knopf--primaer knopf--klein">Rücklagen speichern</button></div>
    </form>`;
  }

  // ================================================================ Bewertung

  function ansichtBewertung() {
    const plan = L.planen(haushalt);
    const a = plan.analyse;
    if (a.einkommen <= 0 || !a.posten.length) {
      return html`${kopf('Bewertung', 'Hier werden deine Ausgaben nach ökonomischen Grundsätzen bewertet.')}${leer('Für eine Bewertung brauche ich mindestens eine Einnahme und eine Ausgabe.', html`<a class="knopf knopf--primaer" href="#haushalt">Zu Einnahmen & Ausgaben</a>`)}`;
    }
    const e = haushalt.einstellungen;
    const kz = L.kennzahlen(a, e);
    const abschnitte = [
      ['b-kennzahlen', 'Kennzahlen'],
      ['b-503020', '50/30/20'],
      ['b-beduerfnisse', 'Bedürfnisse'],
      ['b-abc', 'ABC-Analyse'],
      ['b-vergleich', 'Durchschnitt'],
      ['b-opportunitaet', 'Opportunitätskosten'],
      ['b-nutzung', 'Kosten pro Nutzung'],
      ['b-urteile', 'Einzelurteile'],
      ['b-empfehlungen', 'Empfehlungen'],
    ];
    return html`
      ${kopf('Bewertung', 'Deine Ausgaben, geprüft mit Werkzeugen aus Volks- und Betriebswirtschaft. Jede Methode beleuchtet eine andere Seite.')}
      <nav class="sprungleiste" aria-label="Abschnitte der Bewertung">
        ${abschnitte.map(([id, name]) => html`<button type="button" class="knopf knopf--klein knopf--leise" data-aktion="springen" data-id="${id}">${name}</button>`)}
      </nav>
      ${karte('Kennzahlen mit Ampel', kennzahlRaster(kz), { id: 'b-kennzahlen', unter: 'Sechs Kennzahlen, die Banken und Schuldnerberatungen ähnlich verwenden' })}
      ${karte('50/30/20-Regel', regel503020(a), { id: 'b-503020', unter: 'Bedarf, Wünsche und Sparen im Verhältnis zum Nettoeinkommen' })}
      ${karte('Bedürfnisarten', beduerfnisse(a), { id: 'b-beduerfnisse', unter: 'Existenz-, Kultur- und Luxusbedürfnisse' })}
      ${karte('ABC-Analyse', abc(a), { id: 'b-abc', unter: 'Welche wenigen Posten machen den Großteil aus? (Pareto-Prinzip)' })}
      ${karte('Vergleich mit dem Durchschnittshaushalt', vergleich(a), { id: 'b-vergleich', unter: 'Anteile an den Konsumausgaben, Deutschland 2024' })}
      ${karte('Opportunitätskosten', opportunitaet(a, e), { id: 'b-opportunitaet', unter: 'Was deine Wünsche kosten, wenn man den entgangenen Zinseszins mitrechnet' })}
      ${karte('Kosten pro Nutzung', kostenProNutzung(a), { id: 'b-nutzung', unter: 'Abos und Mitgliedschaften nach Grenznutzen' })}
      ${karte('Jede Ausgabe einzeln', einzelurteile(plan), { id: 'b-urteile', unter: 'Urteil aus Bedürfnisart, ABC-Klasse, Durchschnitt und Nutzung' })}
      ${karte('Empfehlungen', (() => {
        const emp = L.empfehlungen(haushalt, plan);
        return emp.length ? empfehlungsliste(emp) : leer('Keine offenen Punkte.');
      })(), { id: 'b-empfehlungen', unter: 'Nach Dringlichkeit sortiert' })}`;
  }

  function wertText(k) {
    if (k.wert === null || !Number.isFinite(k.wert)) return '–';
    if (k.format === 'monate') return L.zahlText(Math.floor(k.wert * 10) / 10) + ' Monate';
    return prozent(k.wert);
  }

  function kennzahlRaster(kz) {
    return html`<div class="kennzahl-raster">
      ${kz.map(
        (k) => html`<article class="kennzahl">
          <div class="kennzahl-kopf"><h3>${k.name}</h3>${k.stufe ? chip(k.stufe) : chip('neutral', 'offen')}</div>
          <p class="kennzahl-wert">${wertText(k)}</p>
          <p class="kennzahl-ziel">Ziel: ${k.ziel}</p>
          <p class="kennzahl-text">${k.text}</p>
          ${wissen(k.wissen, 'Was steckt dahinter?')}
        </article>`
      )}
    </div>`;
  }

  function regel503020(a) {
    const zeilen = [
      { name: 'Bedarf', unter: 'Pflichtausgaben + Kreditraten', ist: a.pflichtquote, soll: 0.5, farbe: FARBE.pflicht, max: true },
      { name: 'Wünsche', unter: 'Kultur- und Luxusbedürfnisse', ist: a.wunschquote, soll: 0.3, farbe: FARBE.wuensche, max: true },
      { name: 'Sparen', unter: 'was übrig bleibt', ist: Math.max(0, a.sparquote), soll: 0.2, farbe: FARBE.sparen, max: false },
    ];
    return html`
      ${balkenliste(
        zeilen.map((z) => {
          const ok = z.max ? z.ist <= z.soll + 0.005 : z.ist >= z.soll - 0.005;
          return {
            name: z.name,
            unter: z.unter,
            wert: z.ist,
            soll: z.soll,
            farbe: z.farbe,
            text: prozent(z.ist) + ' · Ziel ' + (z.max ? 'höchstens ' : 'mindestens ') + prozent(z.soll),
            chip: chip(ok ? 'gut' : 'mittel', ok ? 'passt' : z.max ? 'zu hoch' : 'zu niedrig'),
            tipWert: prozent(z.ist, 1),
            tipName: 'Ziel ' + prozent(z.soll),
          };
        }),
        { max: 1, sollName: 'Richtwert' }
      )}
      <p class="erklaerung">Der Strich markiert den Richtwert. Die Regel ist eine Faustformel: Bei kleinem Einkommen ist ein höherer Bedarfsanteil normal, dann zählt vor allem, dass überhaupt etwas übrig bleibt. ${wissen('w-503020', 'Mehr zur Regel')}</p>`;
  }

  function beduerfnisse(a) {
    const gruppen = ['existenz', 'kultur', 'luxus'].map((b) => {
      const betrag = b === 'existenz' ? a.bedarf : b === 'kultur' ? a.kultur : a.luxus;
      return { id: b, betrag, anteil: a.konsum > 0 ? betrag / a.konsum : 0 };
    });
    return html`
      ${balkenliste(
        gruppen.map((g) => ({
          name: L.BEDUERFNISSE[g.id].name,
          unter: L.BEDUERFNISSE[g.id].kurz,
          wert: g.betrag,
          text: euroGanz(g.betrag) + ' · ' + prozent(g.anteil),
          tipWert: euroGanz(g.betrag),
          tipName: prozent(g.anteil, 1) + ' deines Konsums',
        }))
      )}
      <dl class="begriffe">
        ${gruppen.map((g) => html`<div><dt>${L.BEDUERFNISSE[g.id].name}</dt><dd>${BEDUERFNIS_TEXT[g.id]}</dd></div>`)}
      </dl>
      <p class="erklaerung">Ein Bedürfnis wird erst zum Bedarf, wenn Kaufkraft dahintersteht. Sparen heißt deshalb vor allem: bei Luxusbedürfnissen bewusst entscheiden. ${wissen('w-beduerfnisse', 'Bedürfnisarten erklärt')}</p>`;
  }

  function abc(a) {
    const posten = L.abcAnalyse(L.allePosten(haushalt, a));
    const anzahl = { A: 0, B: 0, C: 0 };
    const summen = { A: 0, B: 0, C: 0 };
    posten.forEach((p) => {
      anzahl[p.klasse] += 1;
      summen[p.klasse] += p.anteil;
    });
    const klasseChip = (k) => chip(k === 'A' ? 'mittel' : 'neutral', 'Klasse ' + k);
    return html`
      <p class="lead">${anzahl.A} von ${posten.length} Posten verursachen ${prozent(summen.A)} deiner Ausgaben. Dort ist der Hebel am größten; bei C-Posten lohnt sich der Aufwand kaum.</p>
      ${diagramm('pareto', {
        posten: posten.map((p) => ({ name: p.name, anteil: p.anteil, kumuliert: p.kumuliert, klasse: p.klasse, wertText: euroGanz(p.monatlich) })),
        farbeSaeule: FARBE.pflicht,
        farbeLinie: FARBE.raten,
        beschreibung: 'Pareto-Diagramm: Anteil jedes Postens und kumulierter Anteil',
      })}
      <ul class="legende legende--klein">
        <li><span class="legende-farbe" style="background:${FARBE.pflicht}"></span><span class="legende-name">Anteil des Postens</span></li>
        <li><span class="legende-farbe legende-farbe--linie" style="background:${FARBE.raten}"></span><span class="legende-name">kumulierter Anteil</span></li>
      </ul>
      <div class="tabelle-rahmen"><table class="tabelle">
        <thead><tr><th class="zahl">Nr.</th><th>Posten</th><th class="zahl">pro Monat</th><th class="zahl">Anteil</th><th class="zahl nur-breit">kumuliert</th><th>Klasse</th></tr></thead>
        <tbody>${posten.map(
          (p, i) => html`<tr>
            <td class="zahl">${i + 1}</td><td>${p.name}</td><td class="zahl">${euro(p.monatlich)}</td>
            <td class="zahl">${prozent(p.anteil, 1)}</td><td class="zahl nur-breit">${prozent(p.kumuliert, 1)}</td><td>${klasseChip(p.klasse)}</td>
          </tr>`
        )}</tbody>
      </table></div>
      <p class="erklaerung">A-Posten machen zusammen bis 80 % der Summe aus, B-Posten bis 95 %, der Rest ist C. Die Methode stammt aus der Materialwirtschaft. ${wissen('w-abc', 'ABC-Analyse erklärt')}</p>`;
  }

  function vergleich(a) {
    const zeilen = L.vergleichReferenz(a);
    const ausgelassen = a.posten.some((p) => !L.KATEGORIEN[p.kategorie].referenz);
    const stufeChip = (v) =>
      v.stufe === 'hoch' ? chip(v.erklaerbar ? 'neutral' : 'mittel', v.erklaerbar ? 'hoch, aber erklärbar' : 'deutlich höher') : v.stufe === 'niedrig' ? chip('neutral', 'niedriger') : chip('gut', 'üblich');
    return html`
      ${balkenliste(
        zeilen.map((v) => ({
          name: v.name,
          wert: v.anteil,
          soll: v.referenz,
          text: prozent(v.anteil) + ' · Ø ' + prozent(v.referenz),
          chip: stufeChip(v),
          tipWert: prozent(v.anteil, 1) + ' (' + euroGanz(v.betrag) + ')',
          tipName: 'Durchschnitt ' + prozent(v.referenz, 1),
        })),
        { max: Math.max(0.4, Math.max.apply(null, zeilen.map((v) => Math.max(v.anteil, v.referenz)))), sollName: 'Durchschnittshaushalt' }
      )}
      <p class="erklaerung">Balken: dein Anteil, Strich: Durchschnitt aller Haushalte in Deutschland (Statistisches Bundesamt, Laufende Wirtschaftsrechnungen 2024).${ausgelassen ? ' Versicherungsbeiträge und Kinderbetreuung sind nicht enthalten, weil die Statistik sie nicht als Konsum zählt.' : ''} Abweichungen sind kein Fehler: Nach dem Engel- und dem Schwabe-Gesetz geben Haushalte mit kleinem Einkommen anteilig mehr für Essen und Wohnen aus. ${wissen('w-engel', 'Engel- und Schwabe-Gesetz')}</p>`;
  }

  function opportunitaet(a, e) {
    const wuensche = a.posten.filter((p) => p.beduerfnis !== 'existenz' && p.monatlich > 0).sort((x, y) => y.monatlich - x.monatlich);
    if (!wuensche.length) return leer('Keine Wünsche eingetragen, also auch keine Opportunitätskosten.');
    const gesamt = L.opportunitaetskosten(a.wuensche, e.rendite);
    return html`
      <p class="lead">Würdest du deine Wünsche (${euroGanz(a.wuensche)} im Monat) stattdessen zu ${L.zahlText(e.rendite)} % anlegen, hättest du nach 20 Jahren <strong>${euroGanz(gesamt[1].wert)}</strong>, davon ${euroGanz(gesamt[1].wert - gesamt[1].eingezahlt)} Zinsen. Das heißt nicht, dass du verzichten sollst, aber jede Ausgabe hat einen Preis über den Kassenbon hinaus.</p>
      <div class="tabelle-rahmen"><table class="tabelle">
        <thead><tr><th>Wunsch</th><th class="zahl">pro Monat</th><th class="zahl">in 10 Jahren</th><th class="zahl">in 20 Jahren</th><th class="zahl nur-breit">in 30 Jahren</th></tr></thead>
        <tbody>${wuensche.map((p) => {
          const o = L.opportunitaetskosten(p.monatlich, e.rendite);
          return html`<tr><td>${p.name}<small>${L.BEDUERFNISSE[p.beduerfnis].name}</small></td><td class="zahl">${euro(p.monatlich)}</td>
            <td class="zahl">${euroGanz(o[0].wert)}</td><td class="zahl">${euroGanz(o[1].wert)}</td><td class="zahl nur-breit">${euroGanz(o[2].wert)}</td></tr>`;
        })}</tbody>
        <tfoot><tr><th>Alle Wünsche</th><td class="zahl">${euro(a.wuensche)}</td><td class="zahl">${euroGanz(gesamt[0].wert)}</td><td class="zahl">${euroGanz(gesamt[1].wert)}</td><td class="zahl nur-breit">${euroGanz(gesamt[2].wert)}</td></tr></tfoot>
      </table></div>
      <p class="erklaerung">Gerechnet mit ${L.zahlText(e.rendite)} % Rendite im Jahr (änderbar unter „Aufteilung“ → Annahmen), ohne Steuern und Inflation. ${wissen('w-opportunitaet', 'Opportunitätskosten erklärt')}</p>`;
  }

  function kostenProNutzung(a) {
    const mit = a.posten.filter((p) => p.nutzungen !== null).sort((x, y) => y.monatlich - x.monatlich);
    if (!mit.length) {
      return leer('Trag bei Abos und Mitgliedschaften ein, wie oft du sie im Monat nutzt (Feld „Nutzungen pro Monat“). Dann siehst du hier, was dich jede Nutzung kostet.');
    }
    return html`<div class="tabelle-rahmen"><table class="tabelle">
        <thead><tr><th>Posten</th><th class="zahl">pro Monat</th><th class="zahl">Nutzungen</th><th class="zahl">pro Nutzung</th><th>Urteil</th></tr></thead>
        <tbody>${mit.map((p) => {
          const pro = p.nutzungen > 0 ? p.monatlich / p.nutzungen : null;
          const selten = p.nutzungen <= 2;
          return html`<tr><td>${p.name}</td><td class="zahl">${euro(p.monatlich)}</td><td class="zahl">${L.zahlText(p.nutzungen)}×</td>
            <td class="zahl">${pro === null ? '–' : euro(pro)}</td><td>${selten ? chip('mittel', 'selten genutzt') : chip('gut', 'lohnt sich')}</td></tr>`;
        })}</tbody>
      </table></div>
      <p class="erklaerung">Je seltener du etwas nutzt, desto teurer wird jede einzelne Nutzung. Vergleiche mit dem Einzelpreis: Ein Kinobesuch, eine Tageskarte, ein einzelner Film zum Leihen. ${wissen('w-grenznutzen', 'Grenznutzen erklärt')}</p>`;
  }

  function einzelurteile(plan) {
    const urteile = L.bewertePosten(haushalt, plan);
    const zaehlen = {};
    urteile.forEach((u) => {
      zaehlen[u.urteil] = (zaehlen[u.urteil] || 0) + 1;
    });
    return html`
      <ul class="urteil-summe">
        ${Object.keys(URTEIL)
          .filter((k) => zaehlen[k])
          .map((k) => html`<li>${chip(URTEIL[k].stufe, URTEIL[k].name)}<span>${zaehlen[k]}×</span></li>`)}
      </ul>
      <div class="tabelle-rahmen"><table class="tabelle tabelle--urteile">
        <thead><tr><th>Ausgabe</th><th class="zahl">pro Monat</th><th>Urteil</th><th>Begründung</th></tr></thead>
        <tbody>${urteile.map(
          (u) => html`<tr>
            <td>${u.name}<small>${L.BEDUERFNISSE[u.beduerfnis].name} · Klasse ${u.klasse}</small></td>
            <td class="zahl">${euro(u.monatlich)}</td>
            <td>${chip(URTEIL[u.urteil].stufe, URTEIL[u.urteil].name)}</td>
            <td class="begruendung">${u.begruendung}</td>
          </tr>`
        )}</tbody>
      </table></div>`;
  }

  // ================================================================ Aufteilung

  function ansichtAufteilung() {
    const plan = L.planen(haushalt);
    const e = haushalt.einstellungen;
    const titel = kopf('Aufteilung', 'So verteilst du dein Einkommen realistisch: erst das Nötige, dann ein Puffer, dann Wünsche mit Deckel, und der Rest arbeitet für dich.');
    if (plan.status === 'leer') {
      return html`${titel}${leer('Trag zuerst deine Einnahmen ein.', knopf('Einnahme eintragen', 'neu-einnahme', { klasse: 'knopf--primaer' }))}`;
    }
    return html`${titel}
      <div class="raster raster--seite">
        <aside class="spalte-annahmen">${karte('Annahmen', annahmen(e), { unter: 'Alles hier wirkt sofort auf Plan und Fahrplan' })}</aside>
        <div class="spalte-plan">
          ${statusBox(plan)}
          ${karte('Dein Monatsplan', monatsplan(plan), { unter: 'Wohin jeder Euro deines Nettoeinkommens geht' })}
          ${plan.status === 'ok' ? karte('Daueraufträge zum Zahltag', dauerauftraege(plan), { unter: 'Zahl dich zuerst selbst: die Sparrate verlässt das Girokonto, bevor du etwas ausgibst' }) : ''}
          ${plan.status === 'ok' && plan.kuerzung > 0 ? karte('So erreichst du das Wunsch-Budget', kuerzungen(plan), { unter: 'Erst Verzichtbares, dann Wichtiges, nie das Lebensnotwendige' }) : ''}
          ${plan.status === 'ok' ? karte('Dein Fahrplan', fahrplan(plan), { unter: 'Monat für Monat simuliert, mit Zinsen und frei werdenden Kreditraten' }) : ''}
          ${plan.status === 'ok' ? karte('Vermögensentwicklung', vermoegen(plan), { unter: 'Rücklagen, Sparziele und Anlagen minus Schulden' }) : ''}
        </div>
      </div>`;
  }

  function annahmen(e) {
    const strategien = [
      ['entspannt', 'Entspannt', '25 % des freien Geldes sparen'],
      ['ausgewogen', 'Ausgewogen', '40 %, entspricht der 50/30/20-Regel'],
      ['ehrgeizig', 'Ehrgeizig', '60 %, für schnelle Ziele'],
    ];
    const zahlEingabe = (id, name, wert, einheit, hilfe) => html`<label class="feld"><span>${name}</span>
      <span class="eingabe-einheit"><input id="${id}" data-einstellung="${id.slice(2)}" inputmode="decimal" autocomplete="off" value="${L.zahlText(wert)}"><span>${einheit}</span></span>
      ${hilfe ? html`<small>${hilfe}</small>` : ''}</label>`;
    return html`<form class="formular formular--eng" data-formular="annahmen">
      <fieldset class="feld">
        <legend>Sparstrategie</legend>
        <div class="wahlkarten">
          ${strategien.map(
            ([id, name, text]) => html`<label class="wahlkarte">
              <input type="radio" name="strategie" value="${id}" data-einstellung="strategie" ${e.strategie === id ? html`checked` : ''}>
              <span><strong>${name}</strong><small>${text}</small></span>
            </label>`
          )}
        </div>
      </fieldset>
      <label class="feld"><span>Puffer für Ungeplantes</span>
        <select id="a-puffer" data-einstellung="puffer">${[0, 3, 5, 8, 10].map((p) => html`<option value="${p}" ${e.puffer === p ? html`selected` : ''}>${p} % des Einkommens</option>`)}</select></label>
      <label class="feld"><span>Notgroschen-Ziel</span>
        <select id="a-notgroschenMonate" data-einstellung="notgroschenMonate">${[1, 2, 3, 4, 6, 9, 12].map((m) => html`<option value="${m}" ${e.notgroschenMonate === m ? html`selected` : ''}>${m} ${m === 1 ? 'Monat' : 'Monate'} Pflichtausgaben</option>`)}</select></label>
      ${zahlEingabe('a-rendite', 'Erwartete Rendite langfristig', e.rendite, '% p. a.', 'Breit gestreute Aktien-ETFs: historisch etwa 5–7 %, aber ohne Garantie.')}
      ${zahlEingabe('a-tagesgeld', 'Zins für Tagesgeld', e.tagesgeld, '% p. a.')}
      ${zahlEingabe('a-inflation', 'Inflation', e.inflation, '% p. a.', 'Ziel der Europäischen Zentralbank: 2 %.')}
      ${wissen('w-annahmen', 'Grenzen der Rechnung')}
    </form>`;
  }

  function statusBox(plan) {
    const a = plan.analyse;
    if (plan.status === 'defizit') {
      const gross = a.posten.filter((p) => p.beduerfnis === 'existenz').sort((x, y) => y.monatlich - x.monatlich).slice(0, 3);
      return html`<div class="statusbox statusbox--schlecht" role="status">
        <h2>${chip('schlecht', 'Defizit')} Schon die Pflichtausgaben übersteigen dein Einkommen</h2>
        <p>Pflichtausgaben und Raten: ${euroGanz(a.pflicht)}, Einkommen: ${euroGanz(a.einkommen)}. Sparen ist erst möglich, wenn sich an den großen Posten oder am Einkommen etwas ändert.</p>
        <ul>
          <li>Größte Pflichtposten: ${gross.map((p) => p.name + ' (' + euroGanz(p.monatlich) + ')').join(', ')}. Geht es günstiger (Minimalprinzip)?</li>
          <li>Prüfe Ansprüche: Wohngeld, Berufsausbildungsbeihilfe (BAB), BAföG, Kinderzuschlag.</li>
          <li>Bei Schulden hilft eine kostenlose Schuldnerberatung, zum Beispiel bei Verbraucherzentrale, Caritas oder Diakonie.</li>
        </ul>
      </div>`;
    }
    const strategie = L.STRATEGIEN[plan.strategie].name;
    if (plan.kuerzung > 0) {
      return html`<div class="statusbox statusbox--mittel" role="status">
        <h2>${chip('mittel', 'Anpassung nötig')} Sparrate ${euroGanz(plan.sparen)}, wenn die Wünsche um ${euroGanz(plan.kuerzung)} sinken</h2>
        <p>Heute bleiben dir ${euroGanz(Math.max(0, a.ueberschuss))} im Monat übrig. Für die Strategie „${strategie}“ dürfen Wünsche höchstens ${euroGanz(plan.wunschBudget)} kosten, bisher sind es ${euroGanz(a.wuensche)}. Konkrete Vorschläge stehen weiter unten.</p>
      </div>`;
    }
    return html`<div class="statusbox statusbox--gut" role="status">
      <h2>${chip('gut', 'Machbar')} Du kannst ${euroGanz(plan.sparen)} im Monat sparen, ohne auf etwas zu verzichten</h2>
      <p>Dazu bleiben ${euroGanz(plan.puffer)} als Puffer auf dem Girokonto. ${plan.sparanteil < 0.6 && a.wuensche > plan.frei * (1 - L.STRATEGIEN.ehrgeizig.sparanteil) ? 'Mit einer ehrgeizigeren Strategie würdest du deine Ziele schneller erreichen, müsstest dafür aber bei den Wünschen kürzen.' : ''}</p>
    </div>`;
  }

  function monatsplanDefizit(a) {
    const posten = [
      { id: 'pflicht', name: 'Pflichtausgaben', betrag: a.bedarf },
      { id: 'raten', name: 'Kreditraten', betrag: a.raten },
      { id: 'wuensche', name: 'Wünsche', betrag: a.wuensche },
    ]
      .filter((t) => t.betrag > 0.005)
      .map((t) => Object.assign(t, { anteil: t.betrag / a.ausgaben, farbe: FARBE[t.id], bezug: 'deiner Ausgaben' }));
    return html`
      ${stapel(posten, 'Ausgaben')}
      ${legende(posten)}
      <p class="warnzeile">${chip('schlecht', 'Lücke')} Dein Einkommen von ${euroGanz(a.einkommen)} deckt nur ${prozent(a.einkommen / a.ausgaben)} deiner Ausgaben. Es fehlen ${euroGanz(a.ausgaben - a.einkommen)} im Monat.</p>
      <p class="erklaerung">Erst wenn die Pflichtausgaben unter dem Einkommen liegen, kann die App Wünsche, Puffer und Sparrate verteilen. Bis dahin gilt: Wünsche aussetzen und die größten Pflichtposten angehen.</p>`;
  }

  function monatsplan(plan) {
    const a = plan.analyse;
    if (plan.status === 'defizit') return monatsplanDefizit(a);
    const wofuer = {
      pflicht: 'Existenzbedürfnisse: Wohnen, Essen, Mobilität, Versicherungen',
      raten: 'Vereinbarte Kreditraten',
      wuensche: plan.kuerzung > 0 ? 'Gedeckelt nach deiner Strategie, bisher ' + euroGanz(a.wuensche) : 'Kultur- und Luxusbedürfnisse in bisheriger Höhe',
      puffer: 'Bleibt auf dem Girokonto für Ungeplantes wie Geschenke oder Reparaturen. Was am Monatsende übrig ist, geht in den Notgroschen.',
      sparen: 'Geht per Dauerauftrag am Zahltag weg, aufgeteilt wie unten beschrieben',
    };
    const toepfe = plan.toepfe
      .filter((t) => t.betrag > 0.005)
      .map((t) => Object.assign({}, t, { anteil: t.betrag / a.einkommen, farbe: FARBE[t.id] }));
    return html`
      ${stapel(toepfe, 'Monatsplan')}
      ${legende(toepfe)}
      <div class="tabelle-rahmen"><table class="tabelle">
        <thead><tr><th>Topf</th><th class="zahl">pro Monat</th><th class="zahl">Anteil</th><th class="nur-breit">Wofür</th></tr></thead>
        <tbody>${toepfe.map(
          (t) => html`<tr><td class="topf"><span class="punkt-farbe" style="background:${t.farbe}"></span>${t.name}</td><td class="zahl">${euro(t.betrag)}</td><td class="zahl">${prozent(t.anteil, 1)}</td><td class="nur-breit begruendung">${wofuer[t.id]}</td></tr>`
        )}</tbody>
        <tfoot><tr><th>Nettoeinkommen</th><td class="zahl">${euro(a.einkommen)}</td><td class="zahl">100 %</td><td class="nur-breit"></td></tr></tfoot>
      </table></div>`;
  }

  /** Name und Begründung eines Spartopfs aus der Simulation. */
  function topfBeschreibung(schluessel, plan) {
    const sim = plan.simulation;
    const e = haushalt.einstellungen;
    const a = plan.analyse;
    const [art, id] = schluessel.split(':');
    if (art === 'notgroschen') {
      const grundstock = a.notgroschen < a.pflicht;
      return {
        name: 'Notgroschen',
        konto: 'Tagesgeldkonto',
        grund: grundstock
          ? 'Erst ein Grundstock von einem Monat Pflichtausgaben (' + euroGanz(a.pflicht) + '), damit eine Panne nicht im Dispo endet.'
          : 'Auffüllen auf ' + L.zahlText(e.notgroschenMonate) + ' Monate Pflichtausgaben (' + euroGanz(sim.notgroschenZiel) + ')' + (sim.notgroschenVollMonat ? ', voll voraussichtlich ' + L.monatName(sim.notgroschenVollMonat) + '.' : '.'),
      };
    }
    if (art === 'schuld') {
      const s = sim.schulden.find((x) => x.id === id);
      return {
        name: 'Sondertilgung ' + s.name,
        konto: 'Kreditkonto',
        grund:
          (s.teuer ? L.zahlText(s.zins) + ' % Zinsen: Jeder getilgte Euro ist eine sichere Rendite in dieser Höhe.' : 'Der Zins liegt über der erwarteten Rendite, deshalb schlägt Tilgen das Anlegen.') +
          (s.getilgtMonat ? ' Getilgt voraussichtlich ' + L.monatName(s.getilgtMonat) + '.' : ''),
      };
    }
    if (art === 'ziel') {
      const z = sim.ziele.find((x) => x.id === id);
      return {
        name: z.name,
        konto: anlageName(z.form),
        grund:
          euroGanz(z.betrag) + ' bis ' + L.monatName(z.termin) + '. ' +
          (z.erreicht === null ? 'Mit dieser Rate nicht erreichbar.' : z.puenktlich ? 'Klappt pünktlich.' : 'Mit dieser Rate erreicht ' + L.monatName(z.erreichtMonat) + '.'),
      };
    }
    return {
      name: 'Vermögensaufbau',
      konto: 'ETF-Sparplan oder Altersvorsorge',
      grund: 'Alles Weitere arbeitet langfristig für dich (Zinseszins). Angenommen sind ' + L.zahlText(e.rendite) + ' % Rendite im Jahr.',
    };
  }

  const TOPF_REIHENFOLGE = (k) => ({ notgroschen: 0, schuld: 1, ziel: 2, vermoegen: 3 }[k.split(':')[0]]);

  function dauerauftraege(plan) {
    const erster = plan.simulation.ersterMonat;
    const schluessel = Object.keys(erster)
      .filter((k) => erster[k] >= 0.5)
      .sort((x, y) => TOPF_REIHENFOLGE(x) - TOPF_REIHENFOLGE(y) || erster[y] - erster[x]);
    if (!plan.sparen || !schluessel.length) {
      return leer('Für Daueraufträge bleibt im Moment nichts übrig. Schau dir die Empfehlungen unter „Bewertung“ an.');
    }
    return html`
      <div class="auftraege">
        ${schluessel.map((k) => {
          const t = topfBeschreibung(k, plan);
          return html`<article class="ueberweisung">
            <div class="ueberweisung-kopf"><span>Dauerauftrag · monatlich</span><span>${t.konto}</span></div>
            <div class="ueberweisung-felder">
              <div class="ueberweisung-feld"><span>Zweck</span><strong>${t.name}</strong></div>
              <div class="ueberweisung-feld ueberweisung-betrag"><span>Betrag</span><strong>${euroGanz(erster[k])}</strong></div>
            </div>
            <p>${t.grund}</p>
          </article>`;
        })}
      </div>
      <p class="erklaerung">Diese Aufteilung gilt ab jetzt. Sobald ein Ziel erreicht oder ein Kredit getilgt ist, ändern sich die Beträge; der Fahrplan zeigt, wann. ${wissen('w-finanzpyramide', 'Warum diese Reihenfolge?')}</p>`;
  }

  function kuerzungen(plan) {
    const k = plan.kuerzungen;
    return html`
      <div class="tabelle-rahmen"><table class="tabelle">
        <thead><tr><th>Ausgabe</th><th class="zahl">bisher</th><th class="zahl">neu</th><th class="zahl">weniger</th></tr></thead>
        <tbody>${k.posten.map(
          (p) => html`<tr><td>${p.name}</td><td class="zahl">${euro(p.vorher)}</td><td class="zahl">${euro(p.nachher)}</td><td class="zahl">−${euro(p.kuerzung)}</td></tr>`
        )}</tbody>
        <tfoot><tr><th>Summe</th><td></td><td></td><td class="zahl">−${euro(L.summe(k.posten.map((p) => p.kuerzung)))}</td></tr></tfoot>
      </table></div>
      ${k.ungedeckt > 0.5 ? html`<p class="warnzeile">${chip('mittel')} Es fehlen noch ${euroGanz(k.ungedeckt)}. Wähle eine entspanntere Strategie oder prüfe die großen Pflichtposten.</p>` : ''}
      <p class="erklaerung">Verzichtbares wird zuerst halbiert, Wichtiges höchstens um ein Viertel gekürzt. Große Umstellungen halten besser, wenn du sie auf zwei, drei Monate verteilst.</p>`;
  }

  function fahrplan(plan) {
    const sim = plan.simulation;
    const phasen = sim.phasen.slice(0, 7);
    const letzte = sim.phasen.length - 1;
    const meilensteine = [];
    sim.schulden.forEach((s) => {
      if (s.getilgtMonat) meilensteine.push({ monat: s.getilgtMonat, text: s.name + ' getilgt', stufe: 'gut', zusatz: s.ohneMonate && s.getilgt < s.ohneMonate ? (s.ohneMonate - s.getilgt) + ' Monate früher, ' + euroGanz(s.ohneZinsen - s.zinsen) + ' Zinsen gespart' : '' });
      else if (s.waechst) meilensteine.push({ monat: null, text: s.name + ': Rate deckt nicht einmal die Zinsen', stufe: 'schlecht', zusatz: 'Die Schuld wächst. Rate erhöhen oder umschulden.' });
    });
    if (sim.notgroschenVollMonat && sim.notgroschenVoll > 0) meilensteine.push({ monat: sim.notgroschenVollMonat, text: 'Notgroschen vollständig (' + euroGanz(sim.notgroschenZiel) + ')', stufe: 'gut', zusatz: '' });
    sim.ziele.forEach((z) => {
      if (z.erreicht === 0) return;
      meilensteine.push({
        monat: z.erreichtMonat,
        text: z.name + (z.erreichtMonat ? ' erreicht' : ' nicht erreichbar'),
        stufe: z.erreicht === null ? 'schlecht' : z.puenktlich ? 'gut' : 'mittel',
        zusatz: z.erreicht === null ? 'Mehr sparen, Ziel verkleinern oder später ansetzen.' : z.puenktlich ? 'geplant ' + L.monatName(z.termin) : 'geplant ' + L.monatName(z.termin) + ', also ' + (L.monatsIndex(z.erreichtMonat) - L.monatsIndex(z.termin)) + ' Monate später',
      });
    });
    meilensteine.sort((x, y) => (x.monat === null ? 1 : y.monat === null ? -1 : x.monat < y.monat ? -1 : 1));
    return html`
      <ol class="fahrplan">
        ${phasen.map((p, i) => {
          const dauerhaft = i === letzte && p.stufen.length === 1 && p.stufen[0] === 'vermoegen';
          const toepfe = Object.keys(p.durchschnitt)
            .filter((k) => p.durchschnitt[k] >= 1)
            .sort((x, y) => TOPF_REIHENFOLGE(x) - TOPF_REIHENFOLGE(y))
            .map((k) => topfBeschreibung(k, plan).name + ' ' + euroGanz(p.durchschnitt[k]));
          return html`<li class="phase">
            <div class="phase-zeit">
              <strong>${dauerhaft ? 'ab ' + L.monatName(p.vonMonat) : p.dauer === 1 ? L.monatName(p.vonMonat) : L.monatName(p.vonMonat) + ' – ' + L.monatName(p.bisMonat)}</strong>
              <span>${dauerhaft ? 'dauerhaft' : L.dauerText(p.dauer)}</span>
            </div>
            <div class="phase-inhalt">
              <h3>${p.titel}</h3>
              <p>${toepfe.length ? 'Im Schnitt pro Monat: ' + toepfe.join(', ') : 'Keine Sparrate'}</p>
            </div>
          </li>`;
        })}
        ${sim.phasen.length > phasen.length ? html`<li class="phase phase--mehr"><div class="phase-zeit"><span>danach</span></div><div class="phase-inhalt"><p>${sim.phasen.length - phasen.length} weitere Phasen, am Ende fließt alles in den Vermögensaufbau.</p></div></li>` : ''}
      </ol>
      ${meilensteine.length ? html`<h3 class="zwischentitel">Meilensteine</h3>
        <ul class="meilensteine">${meilensteine.map((m) => html`<li>${chip(m.stufe, m.monat ? L.monatName(m.monat) : 'offen')}<span><strong>${m.text}</strong>${m.zusatz ? html`<small>${m.zusatz}</small>` : ''}</span></li>`)}</ul>` : ''}
      <p class="erklaerung">Reihenfolge: Grundstock (1 Monat), teure Kredite ab ${L.GRENZE_TEUER} % Zins, Notgroschen auffüllen und Sparziele nach Priorität, dann Kredite über der Rendite, zuletzt Vermögensaufbau. Getilgte Kreditraten fließen in die Sparrate (Schneeball-Effekt). ${wissen('w-finanzpyramide', 'Die Finanzpyramide')}</p>`;
  }

  function vermoegen(plan) {
    const e = haushalt.einstellungen;
    const monate = ui.horizont * 12;
    const verlauf = plan.simulation.verlauf.slice(0, monate + 1);
    const nominal = verlauf.map((v) => v.vermoegen);
    const real = verlauf.map((v, i) => v.vermoegen / Math.pow(1 + e.inflation / 100, i / 12));
    const titel = (i) => (i === 0 ? 'Heute' : L.monatName(L.monatPlus(plan.startMonat, i - 1)) + ' · nach ' + L.dauerText(i));
    const xText = (i) => (i === 0 ? 'heute' : String(Number(plan.startMonat.slice(0, 4)) + Math.floor((Number(plan.startMonat.slice(5)) - 1 + i - 1) / 12)));
    const ende = verlauf[verlauf.length - 1];
    const jahresZeilen = [];
    for (let j = 0; j <= ui.horizont; j++) {
      const i = j * 12;
      if (verlauf[i]) jahresZeilen.push([j === 0 ? 'heute' : 'nach ' + j + (j === 1 ? ' Jahr' : ' Jahren'), euroGanz(nominal[i]), euroGanz(real[i])]);
    }
    return html`
      <div class="segment" role="group" aria-label="Zeitraum">
        ${[5, 10, 20, 30].map((j) => html`<button type="button" class="segment-knopf" data-aktion="horizont" data-wert="${j}" aria-pressed="${ui.horizont === j ? 'true' : 'false'}">${j} Jahre</button>`)}
      </div>
      <p class="lead">In ${ui.horizont} Jahren: <strong>${euroGanz(ende.vermoegen)}</strong>, das entspricht heute einer Kaufkraft von ${euroGanz(real[real.length - 1])} (bei ${L.zahlText(e.inflation)} % Inflation).</p>
      ${diagramm('linien', {
        serien: [
          { name: 'Nettovermögen', farbe: FARBE.pflicht, werte: nominal, flaeche: true },
          { name: 'in heutiger Kaufkraft', farbe: FARBE.grau, werte: real, strich: 'gestrichelt' },
        ],
        xText,
        titel,
        yFormat: kurzEuro,
        beschreibung: 'Entwicklung des Nettovermögens über ' + ui.horizont + ' Jahre',
      })}
      <ul class="legende legende--klein">
        <li><span class="legende-farbe legende-farbe--linie" style="background:${FARBE.pflicht}"></span><span class="legende-name">Nettovermögen</span></li>
        <li><span class="legende-farbe legende-farbe--linie" style="background:${FARBE.grau}"></span><span class="legende-name">in heutiger Kaufkraft</span></li>
      </ul>
      ${tabellenAnsicht('Werte als Tabelle', ['Zeitpunkt', 'Nettovermögen', 'Kaufkraft heute'], jahresZeilen)}
      <p class="erklaerung">Erreichte Sparziele gelten als ausgegeben (der Urlaub ist dann bezahlt). Einkommen und Ausgaben bleiben gleich, Steuern sind nicht berücksichtigt. ${wissen('w-inflation', 'Inflation und Kaufkraft')}</p>`;
  }

  function kurzEuro(v) {
    const b = Math.abs(v);
    const vz = v < 0 ? '−' : '';
    if (b >= 1e6) return vz + L.zahlText(Math.round(b / 1e5) / 10) + ' Mio. €';
    if (b >= 1e4) return vz + L.zahlText(Math.round(b / 1e3)) + ' Tsd. €';
    return vz + L.zahlText(Math.round(b)) + ' €';
  }

  // ================================================================ Sparpläne

  function ansichtSparplaene() {
    const plan = L.planen(haushalt);
    return html`
      ${kopf('Sparpläne', 'Für jedes Ziel: was du monatlich brauchst, was der Plan hergibt, und wo das Geld am besten liegt.', knopf('+ Sparziel', 'neu-ziel', { klasse: 'knopf--primaer' }))}
      <div class="ziel-raster">
        ${notgroschenKarte(plan)}
        ${haushalt.ziele.map((z) => zielKarte(z, plan))}
      </div>
      ${haushalt.ziele.length ? '' : leer('Noch keine eigenen Sparziele. Führerschein, Urlaub, Auto, Umzug: Ein Ziel mit Termin macht Sparen greifbar.', knopf('Erstes Sparziel anlegen', 'neu-ziel', { klasse: 'knopf--primaer' }))}
      ${karte('Sparplan-Rechner', rechner(), { id: 'rechner', unter: 'Zinseszins zum Ausprobieren, unabhängig von deinen Daten' })}`;
  }

  function dreieck(form) {
    const f = L.ANLAGEFORMEN[form];
    const punkte = (n) => html`<span class="punkte" aria-label="${n} von 3">${[1, 2, 3].map((i) => html`<span class="${i <= n ? 'voll' : ''}"></span>`)}</span>`;
    return html`<dl class="dreieck" aria-label="Magisches Dreieck">
      <div><dt>Sicherheit</dt><dd>${punkte(f.sicherheit)}</dd></div>
      <div><dt>Verfügbarkeit</dt><dd>${punkte(f.liquiditaet)}</dd></div>
      <div><dt>Rendite</dt><dd>${punkte(f.rendite)}</dd></div>
    </dl>`;
  }

  function notgroschenKarte(plan) {
    const a = plan.analyse;
    const e = haushalt.einstellungen;
    const ziel = a.pflicht * e.notgroschenMonate;
    const sim = plan.simulation;
    const voll = ziel > 0 && a.notgroschen >= ziel;
    return html`<article class="zielkarte zielkarte--notgroschen">
      <div class="zielkarte-kopf"><h2>Notgroschen</h2>${chip(voll ? 'gut' : a.reichweite !== null && a.reichweite >= 1 ? 'mittel' : 'schlecht', voll ? 'vollständig' : 'im Aufbau')}</div>
      <p class="zielkarte-stand"><strong>${euroGanz(a.notgroschen)}</strong> von ${euroGanz(ziel)}</p>
      ${meter(ziel > 0 ? a.notgroschen / ziel : 0, voll ? 'gut' : 'akzent', 'Notgroschen')}
      <dl class="angaben">
        <div><dt>Ziel</dt><dd>${L.zahlText(e.notgroschenMonate)} Monate Pflichtausgaben</dd></div>
        <div><dt>Voll</dt><dd>${voll ? 'schon erreicht' : sim && sim.notgroschenVollMonat ? 'voraussichtlich ' + L.monatName(sim.notgroschenVollMonat) : 'mit aktueller Sparrate nicht absehbar'}</dd></div>
        <div><dt>Anlage</dt><dd>${anlageName('tagesgeld')}</dd></div>
      </dl>
      ${dreieck('tagesgeld')}
      <div class="knopfreihe">${knopf('Stand ändern', 'ruecklagen-dialog', { klasse: 'knopf--klein' })}${wissen('w-notgroschen', 'Warum zuerst?')}</div>
    </article>`;
  }

  function zielKarte(z, plan) {
    const e = haushalt.einstellungen;
    const sim = plan.simulation && plan.simulation.ziele.find((x) => x.id === z.id);
    const frist = Math.max(1, L.monatsIndex(z.termin) - L.monatsIndex(L.monatVon(new Date())) + 1);
    const form = z.anlage === 'auto' ? L.anlageFuerLaufzeit(frist) : z.anlage;
    const zins = L.zinsFuerAnlage(form, e);
    const noetig = L.noetigeRate(z.betrag, z.bereits, frist, zins);
    const erreicht = z.bereits >= z.betrag;
    const prognose = erreicht
      ? html`${chip('gut', 'erreicht')}`
      : !sim
        ? html`${chip('neutral', 'kein Plan')}`
        : sim.erreicht === null
          ? html`${chip('schlecht', 'nicht erreichbar')}`
          : html`${L.monatName(sim.erreichtMonat)} ${chip(sim.puenktlich ? 'gut' : 'mittel', sim.puenktlich ? 'pünktlich' : 'später')}`;
    return html`<article class="zielkarte">
      <div class="zielkarte-kopf"><h2>${z.name}</h2>${chip('neutral', 'Priorität ' + L.PRIORITAETEN[z.prioritaet])}</div>
      <p class="zielkarte-stand"><strong>${euroGanz(z.bereits)}</strong> von ${euroGanz(z.betrag)}</p>
      ${meter(z.betrag ? z.bereits / z.betrag : 0, erreicht ? 'gut' : 'akzent', z.name)}
      <dl class="angaben">
        <div><dt>Termin</dt><dd>${L.monatName(z.termin)} (${L.dauerText(frist)})</dd></div>
        <div><dt>Nötige Rate</dt><dd>${erreicht ? '–' : euroGanz(noetig) + ' pro Monat'}</dd></div>
        <div><dt>Im Plan jetzt</dt><dd>${sim ? (sim.ersteRate >= 0.5 ? euroGanz(sim.ersteRate) + ' pro Monat' : 'noch nichts, erst Rücklage und Kredite') : '–'}</dd></div>
        <div><dt>Voraussichtlich</dt><dd>${prognose}</dd></div>
        <div><dt>Anlage</dt><dd>${anlageName(form)}${z.anlage === 'auto' ? html`<small>gewählt nach Laufzeit, ${L.zahlText(zins)} % angenommen</small>` : html`<small>${L.zahlText(zins)} % angenommen</small>`}</dd></div>
      </dl>
      ${dreieck(form)}
      <div class="knopfreihe">
        ${knopf('Einzahlung', 'einzahlung', { id: z.id, klasse: 'knopf--klein knopf--primaer' })}
        ${knopf('Bearbeiten', 'bearbeiten-ziel', { id: z.id, klasse: 'knopf--klein' })}
        ${knopf('Löschen', 'loeschen-ziel', { id: z.id, klasse: 'knopf--klein knopf--leise' })}
      </div>
    </article>`;
  }

  function rechner() {
    const r = ui.rechner;
    const feld = (id, name, wert, einheit) => html`<label class="feld"><span>${name}</span>
      <span class="eingabe-einheit"><input id="rechner-${id}" data-rechner="${id}" inputmode="decimal" autocomplete="off" value="${L.zahlText(wert)}"><span>${einheit}</span></span></label>`;
    return html`<div class="rechner">
      <form class="formular formular--eng rechner-form" data-formular="rechner">
        <div class="segment" role="group" aria-label="Was soll berechnet werden?">
          <button type="button" class="segment-knopf" data-aktion="rechner-modus" data-wert="endwert" aria-pressed="${r.modus === 'endwert' ? 'true' : 'false'}">Endkapital</button>
          <button type="button" class="segment-knopf" data-aktion="rechner-modus" data-wert="rate" aria-pressed="${r.modus === 'rate' ? 'true' : 'false'}">Nötige Rate</button>
        </div>
        ${feld('start', 'Startkapital', r.start, '€')}
        ${r.modus === 'endwert' ? feld('rate', 'Monatliche Rate', r.rate, '€') : feld('ziel', 'Zielbetrag', r.ziel, '€')}
        ${feld('jahre', 'Laufzeit', r.jahre, 'Jahre')}
        ${feld('rendite', 'Rendite', r.rendite, '% p. a.')}
        ${feld('dynamik', 'Dynamik (Rate steigt jährlich)', r.dynamik, '%')}
        ${feld('inflation', 'Inflation', r.inflation, '% p. a.')}
      </form>
      <div class="rechner-ergebnis" id="rechner-ergebnis" aria-live="polite">${rechnerErgebnis()}</div>
    </div>`;
  }

  function rechnerErgebnis() {
    const r = ui.rechner;
    const eingabe = { start: r.start, rate: r.rate, jahre: r.jahre, rendite: r.rendite, dynamik: r.dynamik, inflation: r.inflation };
    let rate = r.rate;
    if (r.modus === 'rate') {
      rate = L.rateFuerZiel(r.ziel, eingabe);
      eingabe.rate = rate;
    }
    const sp = L.sparplan(eingabe);
    const v = sp.verlauf;
    return html`
      <div class="kacheln kacheln--rechner">
        ${r.modus === 'rate' ? kachel('Nötige Rate', euroGanz(rate), 'pro Monat' + (r.dynamik > 0 ? ', im ersten Jahr' : '')) : kachel('Endkapital', euroGanz(sp.endkapital), 'nach ' + (v.length - 1) + (v.length === 2 ? ' Jahr' : ' Jahren'))}
        ${kachel('Eingezahlt', euroGanz(sp.eingezahlt), 'inklusive Startkapital')}
        ${kachel('Zinserträge', euroGanz(sp.zinsen), sp.endkapital > 0 ? prozent(sp.zinsen / sp.endkapital) + ' des Endkapitals' : '')}
        ${kachel('Kaufkraft heute', euroGanz(sp.real), 'bei ' + L.zahlText(r.inflation) + ' % Inflation')}
      </div>
      ${diagramm('linien', {
        serien: [
          { name: 'Kapital', farbe: FARBE.sparen, werte: v.map((x) => x.kapital), flaeche: true, unten: v.map((x) => x.eingezahlt) },
          { name: 'Eingezahlt', farbe: FARBE.pflicht, werte: v.map((x) => x.eingezahlt), flaeche: true },
          { name: 'Kaufkraft heute', farbe: FARBE.grau, werte: v.map((x) => x.real), strich: 'gestrichelt' },
        ],
        xText: (i) => (i === 0 ? 'Start' : i + ' J.'),
        titel: (i) => (i === 0 ? 'Start' : 'nach ' + i + (i === 1 ? ' Jahr' : ' Jahren')),
        yFormat: kurzEuro,
        beschreibung: 'Sparplan: Kapital, Einzahlungen und Kaufkraft über die Laufzeit',
      })}
      <ul class="legende legende--klein">
        <li><span class="legende-farbe" style="background:${FARBE.pflicht}"></span><span class="legende-name">Eingezahlt</span></li>
        <li><span class="legende-farbe" style="background:${FARBE.sparen}"></span><span class="legende-name">Zinserträge (bis zur Linie Kapital)</span></li>
        <li><span class="legende-farbe legende-farbe--linie" style="background:${FARBE.grau}"></span><span class="legende-name">Kaufkraft heute</span></li>
      </ul>
      ${sp.verdopplung ? html`<p class="erklaerung">72er-Regel: Bei ${L.zahlText(r.rendite)} % verdoppelt sich Geld in etwa ${L.zahlText(Math.round(sp.verdopplung * 10) / 10)} Jahren. ${wissen('w-zinseszins', 'Zinseszins erklärt')}</p>` : ''}
      ${tabellenAnsicht('Jahr für Jahr', ['Jahr', 'Rate', 'Eingezahlt', 'Zinsen', 'Kapital', 'Kaufkraft'], v.slice(1).map((x) => [String(x.jahr), euro(x.rate), euroGanz(x.eingezahlt), euroGanz(x.zinsen), euroGanz(x.kapital), euroGanz(x.real)]))}`;
  }

  function rechnerAktualisieren() {
    const ziel = $('#rechner-ergebnis');
    if (!ziel) return;
    diagrammAuftraege = [];
    ziel.innerHTML = rechnerErgebnis().s;
    diagrammeZeichnen();
    uiSpeichern();
  }

  // ================================================================ Haushaltsbuch

  function ansichtBuch() {
    const si = L.sollIst(haushalt, ui.monat);
    const fehlend = L.wiederkehrendeBuchungen(haushalt, ui.monat);
    const laufend = ui.monat === L.monatVon(new Date());
    return html`
      ${kopf('Haushaltsbuch', 'Erfasse, was wirklich passiert, und vergleiche es mit deinem Plan (Soll-Ist-Vergleich).')}
      <div class="monatswahl">
        <button type="button" class="knopf knopf--symbol" data-aktion="monat" data-wert="-1" aria-label="Vorheriger Monat">‹</button>
        <h2 aria-live="polite">${L.monatName(ui.monat, true)}</h2>
        <button type="button" class="knopf knopf--symbol" data-aktion="monat" data-wert="1" aria-label="Nächster Monat">›</button>
        ${laufend ? '' : knopf('Heute', 'monat', { wert: 0, klasse: 'knopf--klein knopf--leise' })}
      </div>
      <div class="kacheln kacheln--drei">
        ${kachel('Eingenommen', euroGanz(si.einnahmen), 'Plan ' + euroGanz(si.planEinnahmen))}
        ${kachel('Ausgegeben', euroGanz(si.ausgaben), 'Plan ' + euroGanz(si.planAusgaben), si.planAusgaben > 0 && si.ausgaben > si.planAusgaben ? chip('schlecht', 'über Plan') : '')}
        ${kachel('Saldo', euroGanz(si.saldo), laufend ? 'Monat läuft noch' : 'Monatsergebnis', si.saldo < 0 ? chip('schlecht', 'Minus') : '')}
      </div>
      ${karte('Neue Buchung', buchungsformular(), {
        aktion: fehlend.length ? knopf(fehlend.length + ' Fixposten eintragen', 'fixposten', { klasse: 'knopf--klein' }) : '',
        unter: fehlend.length ? 'Monatliche Fixkosten, Einnahmen und Kreditraten lassen sich mit einem Klick übernehmen' : '',
      })}
      <div class="raster raster--zwei">
        ${karte('Soll-Ist-Vergleich', sollIstListe(si, laufend), { unter: 'Balken: ausgegeben, Strich: geplant' })}
        ${karte('Buchungen', buchungsliste(), { unter: si.anzahl + ' im ' + L.monatName(ui.monat, true) })}
      </div>
      ${karte('Die letzten 12 Monate', verlaufZwoelf(), { unter: 'Einnahmen und Ausgaben laut Haushaltsbuch' })}`;
  }

  function buchungsformular() {
    const heute = new Date();
    const datum = ui.monat === L.monatVon(heute) ? ui.monat + '-' + String(heute.getDate()).padStart(2, '0') : ui.monat + '-01';
    const v = ui.buchung;
    const kategorien = Object.keys(L.BUCHUNGSKATEGORIEN).filter((k) => k !== 'einkommen');
    return html`<form class="formular formular--zeile" data-formular="buchung">
      <label class="feld"><span>Datum</span><input id="b-datum" name="datum" type="date" required value="${datum}"></label>
      <label class="feld"><span>Art</span><select id="b-typ" name="typ">
        <option value="ausgabe" ${v.typ === 'ausgabe' ? html`selected` : ''}>Ausgabe</option>
        <option value="einnahme" ${v.typ === 'einnahme' ? html`selected` : ''}>Einnahme</option></select></label>
      <label class="feld"><span>Betrag in €</span><input id="b-betrag" name="betrag" inputmode="decimal" autocomplete="off" required placeholder="z. B. 23,90"></label>
      <label class="feld"><span>Kategorie</span><select id="b-kategorie" name="kategorie">
        ${kategorien.map((k) => html`<option value="${k}" ${v.kategorie === k ? html`selected` : ''}>${L.BUCHUNGSKATEGORIEN[k].name}</option>`)}</select></label>
      <label class="feld feld--breit"><span>Notiz</span><input id="b-notiz" name="notiz" maxlength="80" autocomplete="off" placeholder="z. B. Wocheneinkauf"></label>
      <div class="feld feld--knopf"><button type="submit" class="knopf knopf--primaer">Buchen</button></div>
      <p class="formfehler" role="alert" hidden></p>
    </form>`;
  }

  function sollIstListe(si, laufend) {
    const zeilen = si.kategorien.filter((k) => k.plan > 0 || k.ist > 0);
    if (!zeilen.length) return leer('Weder Plan noch Buchungen für diesen Monat.');
    return html`
      ${balkenliste(
        zeilen.map((k) => {
          const ueber = k.plan > 0 ? k.ist / k.plan : null;
          const stufe = ueber === null ? 'neutral' : ueber > 1.25 ? 'schlecht' : ueber > 1.05 ? 'mittel' : 'gut';
          return {
            name: k.name,
            wert: k.ist,
            soll: k.plan,
            text: euroGanz(k.ist) + ' / ' + euroGanz(k.plan),
            chip: k.plan === 0 ? chip('neutral', 'ungeplant') : k.differenz > 0.5 ? chip(stufe, '+' + euroGanz(k.differenz)) : chip('gut', 'im Rahmen'),
            tipWert: euroGanz(k.ist) + ' ausgegeben',
            tipName: 'geplant ' + euroGanz(k.plan),
          };
        }),
        { sollName: 'geplant' }
      )}
      <p class="erklaerung">${laufend ? 'Der Monat läuft noch, Abweichungen nach unten sind normal. ' : ''}Jährliche Kosten stecken anteilig im Plan; im Monat der Zahlung liegt das Ist deshalb darüber. ${wissen('w-sollist', 'Soll-Ist-Vergleich')}</p>`;
  }

  function buchungsliste() {
    const eintraege = haushalt.buchungen.filter((b) => b.datum.slice(0, 7) === ui.monat);
    if (!eintraege.length) return leer('Keine Buchungen in diesem Monat.');
    eintraege.sort((x, y) => (x.datum < y.datum ? 1 : x.datum > y.datum ? -1 : 0));
    return html`<div class="tabelle-rahmen tabelle-rahmen--hoch"><table class="tabelle">
      <thead><tr><th>Tag</th><th>Kategorie</th><th class="zahl">Betrag</th><th><span class="sr-only">Aktionen</span></th></tr></thead>
      <tbody>${eintraege.map(
        (b) => html`<tr>
          <td>${b.datum.slice(8, 10) + '.' + b.datum.slice(5, 7) + '.'}</td>
          <td>${L.BUCHUNGSKATEGORIEN[b.kategorie].name}${b.notiz ? html`<small>${b.notiz}</small>` : ''}</td>
          <td class="zahl ${b.typ === 'einnahme' ? 'betrag-plus' : ''}">${b.typ === 'einnahme' ? '+' : '−'}${euro(b.betrag)}</td>
          <td><div class="zeilen-aktionen"><button type="button" class="knopf knopf--symbol" data-aktion="loeschen-buchung" data-id="${b.id}" aria-label="Buchung ${b.notiz || ''} löschen" title="Löschen">✕</button></div></td>
        </tr>`
      )}</tbody>
    </table></div>`;
  }

  function verlaufZwoelf() {
    const werte = L.monatsverlauf(haushalt, ui.monat, 12);
    if (!werte.some((w) => w.einnahmen || w.ausgaben)) return leer('Noch keine Buchungen in den letzten zwölf Monaten.');
    return html`
      ${diagramm('saeulen', {
        kategorien: werte.map((w) => L.monatName(w.monat).split(' ')[0]),
        serien: [
          { name: 'Einnahmen', farbe: FARBE.pflicht, werte: werte.map((w) => w.einnahmen) },
          { name: 'Ausgaben', farbe: FARBE.raten, werte: werte.map((w) => w.ausgaben) },
        ],
        titel: (i) => L.monatName(werte[i].monat, true),
        yFormat: kurzEuro,
        beschreibung: 'Einnahmen und Ausgaben der letzten zwölf Monate',
      })}
      <ul class="legende legende--klein">
        <li><span class="legende-farbe" style="background:${FARBE.pflicht}"></span><span class="legende-name">Einnahmen</span></li>
        <li><span class="legende-farbe" style="background:${FARBE.raten}"></span><span class="legende-name">Ausgaben</span></li>
      </ul>
      ${tabellenAnsicht('Werte als Tabelle', ['Monat', 'Einnahmen', 'Ausgaben', 'Saldo'], werte.map((w) => [L.monatName(w.monat), euroGanz(w.einnahmen), euroGanz(w.ausgaben), euroGanz(w.einnahmen - w.ausgaben)]))}`;
  }

  // ================================================================ Dialoge und Formulare

  function dialogOeffnen(inhalt) {
    const d = $('#dialog');
    d.innerHTML = inhalt.s;
    if (!d.open) d.showModal();
    const erstes = d.querySelector('input:not([type="hidden"]):not([type="radio"]), select, textarea, button');
    if (erstes) erstes.focus();
  }

  function dialogSchliessen() {
    const d = $('#dialog');
    if (d.open) d.close();
  }

  function dialogRahmen(titel, felder, formular, id, absenden) {
    return html`<form class="formular" data-formular="${formular}" ${id ? html`data-id="${id}"` : ''} novalidate>
      <h2 id="dialog-titel">${titel}</h2>
      <div class="felder">${felder}</div>
      <p class="formfehler" role="alert" hidden></p>
      <div class="dialog-aktionen">
        <button type="button" class="knopf knopf--leise" data-aktion="dialog-schliessen">Abbrechen</button>
        <button type="submit" class="knopf knopf--primaer">${absenden || 'Speichern'}</button>
      </div>
    </form>`;
  }

  const textfeld = (id, name, wert, o) => {
    const opt = o || {};
    return html`<label class="feld ${opt.breit ? 'feld--breit' : ''}"><span>${name}${opt.optional ? html` <small>(optional)</small>` : ''}</span>
      <input id="${id}" name="${opt.name || id.slice(2)}" ${opt.zahl ? html`inputmode="decimal"` : ''} ${opt.typ ? html`type="${opt.typ}"` : ''} autocomplete="off" maxlength="60" value="${wert === null || wert === undefined ? '' : wert}" ${opt.platzhalter ? html`placeholder="${opt.platzhalter}"` : ''}>
      ${opt.hilfe ? html`<small>${opt.hilfe}</small>` : ''}</label>`;
  };

  const auswahlfeld = (id, name, optionen, wert) => html`<label class="feld"><span>${name}</span>
    <select id="${id}" name="${id.slice(2)}">${Object.keys(optionen).map((k) => html`<option value="${k}" ${String(wert) === k ? html`selected` : ''}>${typeof optionen[k] === 'string' ? optionen[k] : optionen[k].name}</option>`)}</select></label>`;

  function dialogEinnahme(id) {
    const x = haushalt.einnahmen.find((y) => y.id === id) || { name: '', betrag: null, intervall: 'monatlich' };
    dialogOeffnen(
      dialogRahmen(
        id ? 'Einnahme bearbeiten' : 'Neue Einnahme',
        html`${textfeld('f-name', 'Bezeichnung', x.name, { breit: true, platzhalter: 'z. B. Gehalt (netto)' })}
          ${textfeld('f-betrag', 'Betrag in €', zahlFeld(x.betrag), { zahl: true, platzhalter: 'z. B. 1.850' })}
          ${auswahlfeld('f-intervall', 'Wie oft?', L.INTERVALLE, x.intervall)}`,
        'einnahme',
        id
      )
    );
  }

  function dialogAusgabe(id) {
    const x = haushalt.ausgaben.find((y) => y.id === id) || { name: '', betrag: null, intervall: 'monatlich', kategorie: 'lebensmittel', beduerfnis: 'existenz', art: 'fix', nutzungen: null };
    dialogOeffnen(
      dialogRahmen(
        id ? 'Ausgabe bearbeiten' : 'Neue Ausgabe',
        html`${textfeld('f-name', 'Bezeichnung', x.name, { breit: true, platzhalter: 'z. B. Handyvertrag' })}
          ${textfeld('f-betrag', 'Betrag in €', zahlFeld(x.betrag), { zahl: true, platzhalter: 'z. B. 12,99' })}
          ${auswahlfeld('f-intervall', 'Wie oft?', L.INTERVALLE, x.intervall)}
          ${auswahlfeld('f-kategorie', 'Kategorie', L.KATEGORIEN, x.kategorie)}
          <fieldset class="feld feld--breit"><legend>Welches Bedürfnis steckt dahinter?</legend>
            <div class="wahlkarten wahlkarten--drei">
              ${Object.keys(L.BEDUERFNISSE).map(
                (b) => html`<label class="wahlkarte"><input type="radio" name="beduerfnis" value="${b}" ${x.beduerfnis === b ? html`checked` : ''}>
                  <span><strong>${L.BEDUERFNISSE[b].name}</strong><small>${BEDUERFNIS_TEXT[b]}</small></span></label>`
              )}
            </div>
          </fieldset>
          <fieldset class="feld"><legend>Kostenart</legend>
            <div class="wahlreihe">
              <label><input type="radio" name="art" value="fix" ${x.art === 'fix' ? html`checked` : ''}> Fix (Vertrag, immer gleich)</label>
              <label><input type="radio" name="art" value="variabel" ${x.art === 'variabel' ? html`checked` : ''}> Variabel (schwankt)</label>
            </div>
          </fieldset>
          ${textfeld('f-nutzungen', 'Nutzungen pro Monat', x.nutzungen === null ? '' : zahlFeld(x.nutzungen), { zahl: true, optional: true, hilfe: 'Für Abos und Mitgliedschaften: Wie oft nutzt du es wirklich?' })}`,
        'ausgabe',
        id
      )
    );
  }

  function dialogSchuld(id) {
    const x = haushalt.schulden.find((y) => y.id === id) || { name: '', rest: null, zins: null, rate: null };
    dialogOeffnen(
      dialogRahmen(
        id ? 'Kredit bearbeiten' : 'Neuer Kredit',
        html`${textfeld('f-name', 'Bezeichnung', x.name, { breit: true, platzhalter: 'z. B. Dispokredit, Ratenkauf Handy' })}
          ${textfeld('f-rest', 'Offene Restschuld in €', zahlFeld(x.rest), { zahl: true })}
          ${textfeld('f-zins', 'Effektiver Jahreszins in %', zahlFeld(x.zins), { zahl: true, hilfe: 'Steht im Vertrag. Dispo meist 10–15 %, Kreditkarte oft über 15 %.' })}
          ${textfeld('f-rate', 'Monatliche Rate in €', zahlFeld(x.rate), { zahl: true, hilfe: 'Beim Dispo: was du jeden Monat zurückzahlst.' })}`,
        'schuld',
        id
      )
    );
  }

  function dialogZiel(id) {
    const x = haushalt.ziele.find((y) => y.id === id) || { name: '', betrag: null, bereits: 0, termin: L.monatPlus(L.monatVon(new Date()), 12), prioritaet: 2, anlage: 'auto' };
    const anlagen = {};
    Object.keys(L.ANLAGEFORMEN).forEach((k) => {
      anlagen[k] = k === 'auto' ? 'Automatisch nach Laufzeit (empfohlen)' : L.ANLAGEFORMEN[k].name;
    });
    dialogOeffnen(
      dialogRahmen(
        id ? 'Sparziel bearbeiten' : 'Neues Sparziel',
        html`${textfeld('f-name', 'Wofür sparst du?', x.name, { breit: true, platzhalter: 'z. B. Führerschein' })}
          ${textfeld('f-betrag', 'Zielbetrag in €', zahlFeld(x.betrag), { zahl: true })}
          ${textfeld('f-bereits', 'Schon angespart in €', zahlFeld(x.bereits), { zahl: true })}
          ${textfeld('f-termin', 'Bis wann?', x.termin, { typ: 'month', platzhalter: 'JJJJ-MM', hilfe: 'Monat und Jahr, z. B. 2027-06' })}
          ${auswahlfeld('f-prioritaet', 'Priorität', { 1: 'hoch', 2: 'mittel', 3: 'niedrig' }, x.prioritaet)}
          ${auswahlfeld('f-anlage', 'Wo liegt das Geld?', anlagen, x.anlage)}
          <p class="feld feld--breit hilfetext">Bis 3 Jahre: Tagesgeld (sicher, jederzeit verfügbar). 3 bis 10 Jahre: Mischung. Ab 10 Jahren: breit gestreuter ETF, weil Kursschwankungen sich über lange Zeit ausgleichen. ${wissen('w-dreieck', 'Magisches Dreieck')}</p>`,
        'ziel',
        id
      )
    );
  }

  function dialogEinzahlung(id) {
    const z = haushalt.ziele.find((y) => y.id === id);
    if (!z) return;
    dialogOeffnen(
      dialogRahmen(
        'Einzahlung für „' + z.name + '“',
        html`${textfeld('f-betrag', 'Betrag in €', '', { zahl: true, platzhalter: 'z. B. 50' })}
          <p class="feld hilfetext">Bisher angespart: ${euroGanz(z.bereits)} von ${euroGanz(z.betrag)}.</p>`,
        'einzahlung',
        id,
        'Einzahlen'
      )
    );
  }

  function dialogRuecklagen() {
    dialogOeffnen(
      dialogRahmen(
        'Rücklagen',
        html`${textfeld('f-notgroschen', 'Notgroschen (Tagesgeld, Sparkonto) in €', zahlFeld(haushalt.ruecklagen.notgroschen), { zahl: true, breit: true })}
          ${textfeld('f-anlagen', 'Geldanlagen (Depot, ETF) in €', zahlFeld(haushalt.ruecklagen.anlagen), { zahl: true, breit: true })}`,
        'ruecklagen'
      )
    );
  }

  function dialogDaten() {
    const json = JSON.stringify(haushalt, null, 2);
    dialogOeffnen(html`<div class="formular">
      <h2 id="dialog-titel">Daten & Beispiele</h2>
      <section class="dialog-abschnitt">
        <h3>Beispiel laden</h3>
        <p class="hilfetext">Ersetzt die aktuellen Daten. Mit „Rückgängig“ kommst du zurück.</p>
        ${beispielAuswahl()}
      </section>
      <section class="dialog-abschnitt">
        <h3>Sichern</h3>
        <p class="hilfetext">Deine Daten liegen nur in diesem Browser und werden nirgendwohin gesendet. Sichere sie ab und zu als Datei.</p>
        <div class="knopfreihe">${knopf('Als Datei speichern', 'export-datei', { klasse: 'knopf--primaer knopf--klein' })}${knopf('In die Zwischenablage kopieren', 'export-kopieren', { klasse: 'knopf--klein' })}</div>
        <details class="tabellen-ansicht"><summary>Daten als Text anzeigen</summary><textarea id="export-text" class="datenfeld" readonly rows="6">${json}</textarea></details>
      </section>
      <section class="dialog-abschnitt">
        <h3>Wiederherstellen</h3>
        <label class="feld"><span>Sicherungsdatei wählen</span><input id="import-datei" type="file" accept="application/json,.json"></label>
        <label class="feld"><span>oder Text einfügen</span><textarea id="import-text" class="datenfeld" rows="4" placeholder='{"version":1, …}'></textarea></label>
        <p class="formfehler" id="import-fehler" role="alert" hidden></p>
        <div class="knopfreihe">${knopf('Daten übernehmen', 'import', { klasse: 'knopf--klein' })}</div>
      </section>
      <section class="dialog-abschnitt">
        <h3>Neu beginnen</h3>
        <div class="knopfreihe">${knopf('Alle Daten löschen', 'alles-loeschen', { klasse: 'knopf--klein knopf--gefahr' })}</div>
      </section>
      <div class="dialog-aktionen"><button type="button" class="knopf knopf--leise" data-aktion="dialog-schliessen">Schließen</button></div>
    </div>`);
  }

  function fehlerZeigen(form, text) {
    const el = form.querySelector('.formfehler');
    el.textContent = text;
    el.hidden = false;
  }

  /** Liest ein Betragsfeld; `null`, wenn leer, `NaN`, wenn unlesbar. */
  function betragLesen(form, name) {
    const feld = form.elements[name];
    if (!feld || feld.value.trim() === '') return null;
    const z = L.zahlAusText(feld.value);
    return z === null ? NaN : z;
  }

  function pruefeBetrag(form, name, bezeichnung, o) {
    const opt = o || {};
    const z = betragLesen(form, name);
    if (z === null && opt.optional) return { ok: true, wert: null };
    if (z === null || Number.isNaN(z)) return { ok: false, text: bezeichnung + ': bitte eine Zahl eingeben, z. B. 12,50.' };
    if (z < 0 || (opt.positiv && z <= 0)) return { ok: false, text: bezeichnung + ': bitte einen Wert ' + (opt.positiv ? 'größer als 0' : 'ab 0') + ' eingeben.' };
    return { ok: true, wert: z };
  }

  function felderPruefen(form, regeln) {
    const werte = {};
    for (const r of regeln) {
      const p = pruefeBetrag(form, r[0], r[1], r[2]);
      if (!p.ok) {
        fehlerZeigen(form, p.text);
        const feld = form.elements[r[0]];
        if (feld) feld.focus();
        return null;
      }
      werte[r[0]] = p.wert;
    }
    return werte;
  }

  function speichernInListe(liste, id, eintrag) {
    if (id) {
      const i = liste.findIndex((x) => x.id === id);
      if (i >= 0) liste[i] = Object.assign({}, liste[i], eintrag);
    } else {
      liste.push(Object.assign({ id: L.neueId('n') }, eintrag));
    }
  }

  const FORMULARE = {
    // Enter in einem Annahmen- oder Rechnerfeld: Feld verlassen, damit „change“ greift
    annahmen() {
      if (document.activeElement) document.activeElement.blur();
    },
    rechner() {},
    einnahme(form, id) {
      const w = felderPruefen(form, [['betrag', 'Betrag', { positiv: true }]]);
      if (!w) return;
      const name = form.elements.name.value.trim() || 'Einnahme';
      aendern((h) => speichernInListe(h.einnahmen, id, { name, betrag: w.betrag, intervall: form.elements.intervall.value }), id ? '„' + name + '“ gespeichert' : '„' + name + '“ hinzugefügt');
      dialogSchliessen();
    },
    ausgabe(form, id) {
      const w = felderPruefen(form, [['betrag', 'Betrag', { positiv: true }], ['nutzungen', 'Nutzungen', { optional: true }]]);
      if (!w) return;
      const name = form.elements.name.value.trim() || 'Ausgabe';
      const eintrag = {
        name,
        betrag: w.betrag,
        intervall: form.elements.intervall.value,
        kategorie: form.elements.kategorie.value,
        beduerfnis: (form.querySelector('input[name="beduerfnis"]:checked') || {}).value,
        art: (form.querySelector('input[name="art"]:checked') || {}).value,
        nutzungen: w.nutzungen,
      };
      aendern((h) => speichernInListe(h.ausgaben, id, eintrag), id ? '„' + name + '“ gespeichert' : '„' + name + '“ hinzugefügt');
      dialogSchliessen();
    },
    schuld(form, id) {
      const w = felderPruefen(form, [['rest', 'Restschuld'], ['zins', 'Zins'], ['rate', 'Rate']]);
      if (!w) return;
      if (w.zins > 100) return fehlerZeigen(form, 'Zins: bitte als Prozentzahl eingeben, z. B. 12,5.');
      const name = form.elements.name.value.trim() || 'Kredit';
      aendern((h) => speichernInListe(h.schulden, id, { name, rest: w.rest, zins: w.zins, rate: w.rate }), id ? '„' + name + '“ gespeichert' : '„' + name + '“ hinzugefügt');
      dialogSchliessen();
    },
    ziel(form, id) {
      const w = felderPruefen(form, [['betrag', 'Zielbetrag', { positiv: true }], ['bereits', 'Schon angespart', { optional: true }]]);
      if (!w) return;
      const termin = form.elements.termin.value.trim();
      if (!/^\d{4}-(0[1-9]|1[0-2])$/.test(termin)) return fehlerZeigen(form, 'Termin: bitte Monat und Jahr angeben, z. B. 2027-06.');
      const name = form.elements.name.value.trim() || 'Sparziel';
      aendern(
        (h) =>
          speichernInListe(h.ziele, id, {
            name,
            betrag: w.betrag,
            bereits: w.bereits || 0,
            termin,
            prioritaet: Number(form.elements.prioritaet.value),
            anlage: form.elements.anlage.value,
          }),
        id ? '„' + name + '“ gespeichert' : 'Sparziel „' + name + '“ angelegt'
      );
      dialogSchliessen();
    },
    einzahlung(form, id) {
      const w = felderPruefen(form, [['betrag', 'Betrag', { positiv: true }]]);
      if (!w) return;
      aendern((h) => {
        const z = h.ziele.find((x) => x.id === id);
        if (z) z.bereits += w.betrag;
      }, euroGanz(w.betrag) + ' eingezahlt');
      dialogSchliessen();
    },
    ruecklagen(form) {
      const w = felderPruefen(form, [['notgroschen', 'Notgroschen', { optional: true }], ['anlagen', 'Geldanlagen', { optional: true }]]);
      if (!w) return;
      aendern((h) => {
        h.ruecklagen.notgroschen = w.notgroschen || 0;
        h.ruecklagen.anlagen = w.anlagen || 0;
      }, 'Rücklagen gespeichert');
      dialogSchliessen();
    },
    buchung(form) {
      const w = felderPruefen(form, [['betrag', 'Betrag', { positiv: true }]]);
      if (!w) return;
      const datum = form.elements.datum.value;
      if (!/^\d{4}-\d{2}-\d{2}$/.test(datum)) return fehlerZeigen(form, 'Datum: bitte ein gültiges Datum wählen.');
      const typ = form.elements.typ.value === 'einnahme' ? 'einnahme' : 'ausgabe';
      const kategorie = typ === 'einnahme' ? 'einkommen' : form.elements.kategorie.value;
      ui.buchung = { typ, kategorie: typ === 'einnahme' ? ui.buchung.kategorie : kategorie };
      ui.monat = datum.slice(0, 7);
      aendern(
        (h) => h.buchungen.push({ id: L.neueId('b'), datum, betrag: w.betrag, typ, kategorie, notiz: form.elements.notiz.value.trim(), quelle: null }),
        (typ === 'einnahme' ? '+' : '−') + euro(w.betrag) + ' gebucht'
      );
      const betrag = $('#b-betrag');
      if (betrag) betrag.focus();
    },
  };

  // ---------------------------------------------------------------- Import und Export

  function dateiname() {
    return 'finanzkompass-' + new Date().toISOString().slice(0, 10) + '.json';
  }

  function importieren(text) {
    const fehler = $('#import-fehler');
    let daten;
    try {
      daten = JSON.parse(text);
    } catch (e) {
      fehler.textContent = 'Das ist kein gültiges JSON. Kopiere den kompletten Text aus einer Sicherung.';
      fehler.hidden = false;
      return;
    }
    if (!daten || typeof daten !== 'object' || (!Array.isArray(daten.einnahmen) && !Array.isArray(daten.ausgaben))) {
      fehler.textContent = 'In diesen Daten fehlen Einnahmen und Ausgaben. Ist es eine Sicherung aus dem Finanzkompass?';
      fehler.hidden = false;
      return;
    }
    aendern((h) => Object.assign(h, L.normalisiere(daten)), 'Sicherung übernommen');
    dialogSchliessen();
  }

  // ---------------------------------------------------------------- Aktionen

  function liste(art) {
    return { einnahme: 'einnahmen', ausgabe: 'ausgaben', schuld: 'schulden', ziel: 'ziele', buchung: 'buchungen' }[art];
  }

  function loeschen(art, id) {
    const eintrag = haushalt[liste(art)].find((x) => x.id === id);
    if (!eintrag) return;
    const name = eintrag.name || eintrag.notiz || 'Buchung';
    aendern((h) => {
      h[liste(art)] = h[liste(art)].filter((x) => x.id !== id);
    }, '„' + name + '“ gelöscht');
  }

  function datenErsetzen(neu, meldung) {
    aendern((h) => {
      Object.keys(h).forEach((k) => delete h[k]);
      Object.assign(h, neu);
    }, meldung);
  }

  const AKTIONEN = {
    'neu-einnahme': () => dialogEinnahme(null),
    'neu-ausgabe': () => dialogAusgabe(null),
    'neu-schuld': () => dialogSchuld(null),
    'neu-ziel': () => dialogZiel(null),
    'bearbeiten-einnahme': (el) => dialogEinnahme(el.dataset.id),
    'bearbeiten-ausgabe': (el) => dialogAusgabe(el.dataset.id),
    'bearbeiten-schuld': (el) => dialogSchuld(el.dataset.id),
    'bearbeiten-ziel': (el) => dialogZiel(el.dataset.id),
    'loeschen-einnahme': (el) => loeschen('einnahme', el.dataset.id),
    'loeschen-ausgabe': (el) => loeschen('ausgabe', el.dataset.id),
    'loeschen-schuld': (el) => loeschen('schuld', el.dataset.id),
    'loeschen-ziel': (el) => loeschen('ziel', el.dataset.id),
    'loeschen-buchung': (el) => loeschen('buchung', el.dataset.id),
    einzahlung: (el) => dialogEinzahlung(el.dataset.id),
    'ruecklagen-dialog': () => dialogRuecklagen(),
    'dialog-schliessen': () => dialogSchliessen(),
    'daten-dialog': () => dialogDaten(),
    beispiel: (el) => {
      dialogSchliessen();
      datenErsetzen(L.normalisiere(BSP.laden(el.dataset.id)), 'Beispiel „' + BSP.VORLAGEN[el.dataset.id].titel + '“ geladen');
    },
    'beispiel-behalten': () => aendern((h) => {
      h.beispiel = null;
    }, 'Die Beispielzahlen sind jetzt deine Vorlage. Passe sie unter „Einnahmen & Ausgaben“ an.'),
    'neu-beginnen': () => {
      datenErsetzen(L.leererHaushalt(), 'Alles bereit für deine eigenen Zahlen');
      location.hash = '#haushalt';
    },
    'alles-loeschen': () => {
      dialogSchliessen();
      datenErsetzen(L.leererHaushalt(), 'Alle Daten gelöscht');
    },
    rueckgaengig: () => {
      if (!toastVorher) return;
      haushalt = L.normalisiere(JSON.parse(toastVorher));
      toastVorher = null;
      speichern();
      rendern();
      $('#toast').hidden = true;
    },
    'export-datei': () => {
      const blob = new Blob([JSON.stringify(haushalt, null, 2)], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = dateiname();
      document.body.appendChild(a);
      a.click();
      a.remove();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
      toast('Sicherung „' + dateiname() + '“ wird gespeichert. Klappt das nicht, nutze „In die Zwischenablage kopieren“.');
    },
    'export-kopieren': () => {
      const text = JSON.stringify(haushalt, null, 2);
      const auswaehlen = () => {
        const feld = $('#export-text');
        if (!feld) return;
        feld.closest('details').open = true;
        feld.focus();
        feld.select();
        toast('Text markiert: mit Strg+C kopieren.');
      };
      if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(text).then(() => toast('Daten in die Zwischenablage kopiert'), auswaehlen);
      } else {
        auswaehlen();
      }
    },
    import: () => {
      const datei = $('#import-datei');
      const text = $('#import-text').value.trim();
      if (datei && datei.files && datei.files[0]) {
        const f = datei.files[0];
        if (f.size > 5 * 1024 * 1024) {
          const fehler = $('#import-fehler');
          fehler.textContent = 'Die Datei ist größer als 5 MB. Das ist keine Sicherung aus dem Finanzkompass.';
          fehler.hidden = false;
          return;
        }
        const leser = new FileReader();
        leser.onload = () => importieren(String(leser.result));
        leser.readAsText(f);
      } else if (text) {
        importieren(text);
      } else {
        const fehler = $('#import-fehler');
        fehler.textContent = 'Wähle eine Sicherungsdatei oder füge den Text ein.';
        fehler.hidden = false;
      }
    },
    springen: (el) => {
      const ziel = document.getElementById(el.dataset.id);
      if (ziel) {
        ziel.scrollIntoView({ behavior: matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth', block: 'start' });
        ziel.setAttribute('tabindex', '-1');
        ziel.focus({ preventScroll: true });
      }
    },
    horizont: (el) => {
      ui.horizont = Number(el.dataset.wert);
      uiSpeichern();
      rendern();
    },
    'rechner-modus': (el) => {
      ui.rechner.modus = el.dataset.wert === 'rate' ? 'rate' : 'endwert';
      uiSpeichern();
      rendern();
    },
    monat: (el) => {
      const schritt = Number(el.dataset.wert);
      ui.monat = schritt === 0 ? L.monatVon(new Date()) : L.monatPlus(ui.monat, schritt);
      rendern();
    },
    fixposten: () => {
      const neu = L.wiederkehrendeBuchungen(haushalt, ui.monat);
      aendern((h) => {
        neu.forEach((b) => h.buchungen.push(Object.assign({ id: L.neueId('b') }, b)));
      }, neu.length + ' Fixposten gebucht');
    },
  };

  // ---------------------------------------------------------------- Ereignisse

  document.addEventListener('click', (ev) => {
    const el = ev.target.closest('[data-aktion]');
    if (!el || el.disabled) return;
    const aktion = AKTIONEN[el.dataset.aktion];
    if (aktion) {
      ev.preventDefault();
      aktion(el, ev);
    }
  });

  document.addEventListener('submit', (ev) => {
    const form = ev.target.closest('[data-formular]');
    if (!form) return;
    ev.preventDefault();
    const fn = FORMULARE[form.dataset.formular];
    if (fn) fn(form, form.dataset.id || null);
  });

  // Annahmen: übernehmen, sobald ein Feld verlassen oder eine Auswahl getroffen wird
  document.addEventListener('change', (ev) => {
    const el = ev.target;
    if (el.dataset && el.dataset.einstellung) {
      const name = el.dataset.einstellung;
      let wert = el.value;
      if (name !== 'strategie') {
        wert = L.zahlAusText(String(el.value));
        if (wert === null || !Number.isFinite(wert)) {
          rendern();
          return;
        }
      }
      aendern((h) => {
        h.einstellungen[name] = wert;
      });
    }
  });

  // Sparplan-Rechner: rechnet bei jeder Eingabe live
  document.addEventListener('input', (ev) => {
    const el = ev.target;
    if (el.dataset && el.dataset.rechner) {
      const z = L.zahlAusText(el.value);
      if (z === null || !Number.isFinite(z)) return;
      const grenzen = { start: [0, 1e9], rate: [0, 1e7], ziel: [0, 1e9], jahre: [1, 60], rendite: [-10, 20], dynamik: [0, 20], inflation: [0, 20] }[el.dataset.rechner];
      ui.rechner[el.dataset.rechner] = Math.min(grenzen[1], Math.max(grenzen[0], z));
      rechnerAktualisieren();
    }
    if (el.closest && el.closest('[data-formular]')) {
      const fehler = el.closest('[data-formular]').querySelector('.formfehler');
      if (fehler) fehler.hidden = true;
    }
  });

  // Tooltip für HTML-Balken (SVG-Diagramme steuern ihren Tooltip selbst)
  let htmlTip = null;
  function tipZeigen(el, x, y) {
    htmlTip = el;
    el.classList.add('aktiv');
    D.tip.zeigen(el.dataset.tipTitel, [{ farbe: el.dataset.tipFarbe || null, wert: el.dataset.tipWert, name: el.dataset.tipName, form: 'kasten' }], x, y);
  }
  function tipWeg() {
    if (!htmlTip) return;
    htmlTip.classList.remove('aktiv');
    htmlTip = null;
    D.tip.verbergen();
  }
  document.addEventListener('pointermove', (ev) => {
    const el = ev.target.closest && ev.target.closest('[data-tip-wert]');
    if (el) tipZeigen(el, ev.clientX, ev.clientY);
    else tipWeg();
  });
  document.addEventListener('focusin', (ev) => {
    const el = ev.target.closest && ev.target.closest('[data-tip-wert]');
    if (el) {
      const r = el.getBoundingClientRect();
      tipZeigen(el, r.left + r.width / 2, r.top);
    }
  });
  document.addEventListener('focusout', tipWeg);
  window.addEventListener('scroll', () => D.tip.verbergen(), { passive: true });

  window.addEventListener('hashchange', navigieren);

  let breiteVorher = window.innerWidth;
  let groesseTimer = null;
  window.addEventListener('resize', () => {
    clearTimeout(groesseTimer);
    groesseTimer = setTimeout(() => {
      if (window.innerWidth !== breiteVorher) {
        breiteVorher = window.innerWidth;
        diagrammeZeichnen();
      }
    }, 150);
  });

  // In einem anderen Tab geändert? Dann neu laden, damit nichts überschrieben wird.
  window.addEventListener('storage', (ev) => {
    if (ev.key === SCHLUESSEL && ev.newValue) {
      try {
        haushalt = L.normalisiere(JSON.parse(ev.newValue));
        rendern();
      } catch (e) {
        /* fremde Daten ignorieren */
      }
    }
  });

  // ---------------------------------------------------------------- Start

  haushalt = laden();
  uiLaden();
  ui.ansicht = route().ansicht;
  speicherstatus();
  navigieren();
  if (speicherbar) speichern();
})();
