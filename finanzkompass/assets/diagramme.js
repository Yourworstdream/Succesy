/* Finanzkompass – SVG-Diagramme ohne Fremdbibliothek.
 * Jedes Diagramm wird in der tatsächlichen Breite seines Containers gezeichnet (keine skalierte Schrift)
 * und hat eine Tooltip-Ebene für Maus und Tastatur. Texte werden nur über textContent gesetzt. */
window.Diagramme = (function () {
  'use strict';

  const NS = 'http://www.w3.org/2000/svg';

  function knoten(name, attribute, eltern) {
    const k = document.createElementNS(NS, name);
    Object.keys(attribute || {}).forEach((a) => k.setAttribute(a, attribute[a]));
    if (eltern) eltern.appendChild(k);
    return k;
  }

  function beschriftung(eltern, x, y, inhalt, attribute) {
    const t = knoten('text', Object.assign({ x, y }, attribute), eltern);
    t.textContent = inhalt;
    return t;
  }

  /** „Schöne“ Achsenteilung: Schritte von 1, 2, 2,5 oder 5 mal einer Zehnerpotenz. */
  function skala(maxWert, anzahl) {
    const max = maxWert > 0 ? maxWert : 1;
    const roh = max / (anzahl || 4);
    const potenz = Math.pow(10, Math.floor(Math.log10(roh)));
    const schritt = [1, 2, 2.5, 5, 10].map((f) => f * potenz).find((s) => s >= roh);
    return { max: Math.ceil(max / schritt - 1e-9) * schritt, schritt };
  }

  // ------------------------------------------------------------ Tooltip

  const tip = {
    el: null,
    holen() {
      if (!this.el) this.el = document.getElementById('tooltip');
      return this.el;
    },
    /** zeilen: [{ farbe, wert, name, form: 'linie' | 'kasten' }] */
    zeigen(titel, zeilen, x, y) {
      const el = this.holen();
      if (!el) return;
      el.textContent = '';
      if (titel) {
        const t = document.createElement('div');
        t.className = 'tip-titel';
        t.textContent = titel;
        el.appendChild(t);
      }
      zeilen.forEach((z) => {
        const zeile = document.createElement('div');
        zeile.className = 'tip-zeile';
        if (z.farbe) {
          const schluessel = document.createElement('span');
          schluessel.className = 'tip-schluessel tip-schluessel--' + (z.form || 'linie');
          schluessel.style.background = z.farbe;
          zeile.appendChild(schluessel);
        }
        const wert = document.createElement('strong');
        wert.textContent = z.wert;
        zeile.appendChild(wert);
        if (z.name) {
          const name = document.createElement('span');
          name.className = 'tip-name';
          name.textContent = z.name;
          zeile.appendChild(name);
        }
        el.appendChild(zeile);
      });
      el.hidden = false;
      const breite = el.offsetWidth;
      const hoehe = el.offsetHeight;
      const links = Math.min(Math.max(8, x + 14), window.innerWidth - breite - 8);
      const oben = y - hoehe - 12 < 8 ? y + 18 : y - hoehe - 12;
      el.style.left = links + 'px';
      el.style.top = oben + 'px';
    },
    verbergen() {
      const el = this.holen();
      if (el) el.hidden = true;
    },
  };

  function mittelpunkt(element) {
    const r = element.getBoundingClientRect();
    return { x: r.left + r.width / 2, y: r.top };
  }

  // ------------------------------------------------------------ Gemeinsame Achsen

  function rahmen(container, hoehe, yMax, yFormat, yMin) {
    container.textContent = '';
    const breite = Math.max(260, Math.floor(container.clientWidth));
    const svg = knoten('svg', { width: breite, height: hoehe, viewBox: '0 0 ' + breite + ' ' + hoehe, class: 'diagramm-svg', role: 'img' }, container);
    const unten = Math.min(0, yMin || 0);
    const s = skala(Math.max(yMax, 0) - unten, 4);
    const start = unten < 0 ? Math.floor(unten / s.schritt - 1e-9) * s.schritt : 0;
    const ende = start + Math.ceil((Math.max(yMax, 0) - start) / s.schritt - 1e-9) * s.schritt || s.schritt;
    const ticks = [];
    for (let v = start; v <= ende + 1e-9; v += s.schritt) ticks.push(Math.abs(v) < 1e-9 ? 0 : v);
    const tickTexte = ticks.map(yFormat);
    const rand = { oben: 12, rechts: 12, unten: 30, links: Math.max(36, Math.max.apply(null, tickTexte.map((t) => t.length)) * 7 + 10) };
    const pb = breite - rand.links - rand.rechts;
    const ph = hoehe - rand.oben - rand.unten;
    const y = (v) => rand.oben + ph - ((v - start) / (ende - start)) * ph;
    const raster = knoten('g', { class: 'achse' }, svg);
    ticks.forEach((v, i) => {
      knoten('line', { x1: rand.links, x2: breite - rand.rechts, y1: y(v), y2: y(v), class: v === 0 ? 'grundlinie' : 'gitter' }, raster);
      beschriftung(raster, rand.links - 8, y(v) + 4, tickTexte[i], { 'text-anchor': 'end', class: 'achsentext' });
    });
    return { svg, breite, hoehe, rand, pb, ph, y };
  }

  function xBeschriftungen(r, anzahl, xPos, xText) {
    const g = knoten('g', { class: 'achse' }, r.svg);
    const platz = Math.max(1, Math.floor(r.pb / 64));
    const schritt = Math.max(1, Math.ceil(anzahl / platz));
    for (let i = 0; i < anzahl; i += schritt) {
      const anker = i === 0 && xPos(i) - r.rand.links < 20 ? 'start' : 'middle';
      beschriftung(g, xPos(i), r.hoehe - 10, xText(i), { 'text-anchor': anker, class: 'achsentext' });
    }
  }

  // ------------------------------------------------------------ Linien und Flächen

  /**
   * serien: [{ name, farbe, werte: [], flaeche?: bool, unten?: [] (Band statt Fläche ab 0), strich?: 'gestrichelt' }]
   * xText(i): Beschriftung, titel(i): Tooltip-Überschrift, yFormat(v): Achsen- und Tooltipwert
   */
  function linien(container, o) {
    const n = o.serien[0].werte.length;
    const yMax = Math.max.apply(null, o.serien.map((s) => Math.max.apply(null, s.werte.concat([0]))));
    const yMin = Math.min.apply(null, o.serien.map((s) => Math.min.apply(null, s.werte.concat([0]))));
    const r = rahmen(container, o.hoehe || 260, yMax, o.yFormat, yMin);
    r.svg.setAttribute('aria-label', o.beschreibung || '');
    const x = (i) => r.rand.links + (n <= 1 ? 0 : (i / (n - 1)) * r.pb);
    const pfad = (werte) => werte.map((v, i) => (i ? 'L' : 'M') + x(i).toFixed(1) + ',' + r.y(v).toFixed(1)).join(' ');

    o.serien.forEach((s) => {
      if (!s.flaeche) return;
      const unten = s.unten || s.werte.map(() => 0);
      const rueck = unten
        .map((v, i) => ({ v, i }))
        .reverse()
        .map((p) => 'L' + x(p.i).toFixed(1) + ',' + r.y(p.v).toFixed(1))
        .join(' ');
      knoten('path', { d: pfad(s.werte) + ' ' + rueck + ' Z', fill: s.farbe, class: 'flaeche' }, r.svg);
    });
    o.serien.forEach((s) => {
      knoten('path', { d: pfad(s.werte), stroke: s.farbe, fill: 'none', class: 'linie' + (s.strich ? ' linie--' + s.strich : '') }, r.svg);
    });
    // Endpunkt der ersten Serie hervorheben
    const erste = o.serien[0];
    knoten('circle', { cx: x(n - 1), cy: r.y(erste.werte[n - 1]), r: 4, fill: erste.farbe, class: 'punkt' }, r.svg);
    xBeschriftungen(r, n, x, o.xText);

    // Fadenkreuz
    const kreuz = knoten('line', { y1: r.rand.oben, y2: r.rand.oben + r.ph, class: 'fadenkreuz', visibility: 'hidden' }, r.svg);
    const punkte = o.serien.map((s) => knoten('circle', { r: 4, fill: s.farbe, class: 'punkt', visibility: 'hidden' }, r.svg));
    const flaeche = knoten('rect', {
      x: r.rand.links,
      y: r.rand.oben,
      width: r.pb,
      height: r.ph,
      fill: 'transparent',
      class: 'trefferflaeche',
      tabindex: 0,
      'aria-label': (o.beschreibung || 'Diagramm') + '. Pfeiltasten zeigen einzelne Werte.',
    }, r.svg);
    let aktuell = n - 1;
    const markieren = (i, px, py) => {
      aktuell = Math.max(0, Math.min(n - 1, i));
      kreuz.setAttribute('x1', x(aktuell));
      kreuz.setAttribute('x2', x(aktuell));
      kreuz.setAttribute('visibility', 'visible');
      o.serien.forEach((s, k) => {
        punkte[k].setAttribute('cx', x(aktuell));
        punkte[k].setAttribute('cy', r.y(s.werte[aktuell]));
        punkte[k].setAttribute('visibility', 'visible');
      });
      const box = r.svg.getBoundingClientRect();
      tip.zeigen(
        o.titel ? o.titel(aktuell) : o.xText(aktuell),
        o.serien.map((s) => ({ farbe: s.farbe, wert: o.yFormat(s.werte[aktuell]), name: s.name, form: 'linie' })),
        px !== undefined ? px : box.left + x(aktuell),
        py !== undefined ? py : box.top + r.rand.oben
      );
    };
    const aus = () => {
      kreuz.setAttribute('visibility', 'hidden');
      punkte.forEach((p) => p.setAttribute('visibility', 'hidden'));
      tip.verbergen();
    };
    flaeche.addEventListener('pointermove', (ev) => {
      const box = r.svg.getBoundingClientRect();
      const rel = (ev.clientX - box.left - r.rand.links) / r.pb;
      markieren(Math.round(rel * (n - 1)), ev.clientX, ev.clientY);
    });
    flaeche.addEventListener('pointerleave', aus);
    flaeche.addEventListener('focus', () => markieren(aktuell));
    flaeche.addEventListener('blur', aus);
    flaeche.addEventListener('keydown', (ev) => {
      if (ev.key === 'ArrowLeft' || ev.key === 'ArrowRight') {
        ev.preventDefault();
        markieren(aktuell + (ev.key === 'ArrowRight' ? 1 : -1));
      }
    });
  }

  // ------------------------------------------------------------ Säulen

  function saeulenPfad(x, y0, y1, breite) {
    const h = y0 - y1;
    const r = Math.min(4, breite / 2, h);
    return [
      'M', x, y0, 'L', x, y1 + r, 'Q', x, y1, x + r, y1, 'L', x + breite - r, y1, 'Q', x + breite, y1, x + breite, y1 + r, 'L', x + breite, y0, 'Z',
    ].join(' ');
  }

  /** Gruppierte Säulen: kategorien = Beschriftungen, serien = [{ name, farbe, werte }] */
  function saeulen(container, o) {
    const n = o.kategorien.length;
    const yMax = Math.max.apply(null, o.serien.map((s) => Math.max.apply(null, s.werte.concat([0]))));
    const r = rahmen(container, o.hoehe || 240, yMax, o.yFormat);
    r.svg.setAttribute('aria-label', o.beschreibung || '');
    const band = r.pb / n;
    const luecke = 2;
    const saeule = Math.max(3, Math.min(24, (band * 0.7 - luecke * (o.serien.length - 1)) / o.serien.length));
    const gruppe = saeule * o.serien.length + luecke * (o.serien.length - 1);
    const mitte = (i) => r.rand.links + band * i + band / 2;
    o.serien.forEach((s, k) => {
      s.werte.forEach((v, i) => {
        if (!(v > 0)) return;
        const x0 = mitte(i) - gruppe / 2 + k * (saeule + luecke);
        knoten('path', { d: saeulenPfad(x0, r.y(0), r.y(Math.max(0, v)), saeule), fill: s.farbe, class: 'saeule' }, r.svg);
      });
    });
    xBeschriftungen(r, n, mitte, (i) => o.kategorien[i]);
    for (let i = 0; i < n; i++) {
      const treffer = knoten('rect', {
        x: r.rand.links + band * i,
        y: r.rand.oben,
        width: band,
        height: r.ph,
        fill: 'transparent',
        class: 'trefferflaeche',
        tabindex: 0,
        'aria-label': o.kategorien[i] + ': ' + o.serien.map((s) => s.name + ' ' + o.yFormat(s.werte[i])).join(', '),
      }, r.svg);
      const zeigen = (px, py) => {
        treffer.classList.add('aktiv');
        const p = mittelpunkt(treffer);
        tip.zeigen(
          o.titel ? o.titel(i) : o.kategorien[i],
          o.serien.map((s) => ({ farbe: s.farbe, wert: o.yFormat(s.werte[i]), name: s.name, form: 'kasten' })),
          px !== undefined ? px : p.x,
          py !== undefined ? py : p.y + 20
        );
      };
      const weg = () => {
        treffer.classList.remove('aktiv');
        tip.verbergen();
      };
      treffer.addEventListener('pointermove', (ev) => zeigen(ev.clientX, ev.clientY));
      treffer.addEventListener('pointerleave', weg);
      treffer.addEventListener('focus', () => zeigen());
      treffer.addEventListener('blur', weg);
    }
  }

  // ------------------------------------------------------------ Pareto (ABC-Analyse)

  /** posten: [{ name, anteil, kumuliert, klasse, wertText }] – Säulen und Summenlinie auf derselben %-Achse. */
  function pareto(container, o) {
    const posten = o.posten;
    const n = posten.length;
    const r = rahmen(container, o.hoehe || 260, 1, (v) => Math.round(v * 100) + ' %');
    r.svg.setAttribute('aria-label', o.beschreibung || '');
    const band = r.pb / n;
    const saeule = Math.max(3, Math.min(24, band - 2));
    const mitte = (i) => r.rand.links + band * i + band / 2;
    // Grenzen 80 % und 95 %
    // Klassengrenzen links beschriften: Dort ist die Summenlinie noch niedrig und kreuzt nicht.
    // Beide Texte stehen im Band zwischen 80 % und 95 %, so berühren sie keine Gitterlinie.
    [[0.8, 'Grenze A · 80 %', -4], [0.95, 'Grenze B · 95 %', 12]].forEach(([g, text, versatz]) => {
      knoten('line', { x1: r.rand.links, x2: r.breite - r.rand.rechts, y1: r.y(g), y2: r.y(g), class: 'grenze' }, r.svg);
      beschriftung(r.svg, r.rand.links + 6, r.y(g) + versatz, text, { class: 'achsentext' });
    });
    posten.forEach((p, i) => {
      knoten('path', { d: saeulenPfad(mitte(i) - saeule / 2, r.y(0), r.y(p.anteil), saeule), fill: o.farbeSaeule, class: 'saeule' }, r.svg);
    });
    const linie = posten.map((p, i) => (i ? 'L' : 'M') + mitte(i).toFixed(1) + ',' + r.y(p.kumuliert).toFixed(1)).join(' ');
    knoten('path', { d: linie, stroke: o.farbeLinie, fill: 'none', class: 'linie' }, r.svg);
    posten.forEach((p, i) => {
      knoten('circle', { cx: mitte(i), cy: r.y(p.kumuliert), r: 4, fill: o.farbeLinie, class: 'punkt' }, r.svg);
    });
    const g = knoten('g', { class: 'achse' }, r.svg);
    const platz = Math.max(1, Math.floor(r.pb / 22));
    const schritt = Math.max(1, Math.ceil(n / platz));
    posten.forEach((p, i) => {
      if (i % schritt === 0) beschriftung(g, mitte(i), r.hoehe - 10, p.klasse + (i + 1), { 'text-anchor': 'middle', class: 'achsentext' });
    });
    posten.forEach((p, i) => {
      const treffer = knoten('rect', {
        x: r.rand.links + band * i,
        y: r.rand.oben,
        width: band,
        height: r.ph,
        fill: 'transparent',
        class: 'trefferflaeche',
        tabindex: 0,
        'aria-label': p.name + ', Klasse ' + p.klasse,
      }, r.svg);
      const zeigen = (px, py) => {
        treffer.classList.add('aktiv');
        const m = mittelpunkt(treffer);
        tip.zeigen(
          (i + 1) + '. ' + p.name + ' · Klasse ' + p.klasse,
          [
            { farbe: o.farbeSaeule, wert: p.wertText, name: 'Anteil ' + p.anteilText, form: 'kasten' },
            { farbe: o.farbeLinie, wert: p.kumuliertText, name: 'kumuliert', form: 'linie' },
          ],
          px !== undefined ? px : m.x,
          py !== undefined ? py : m.y + 20
        );
      };
      const weg = () => {
        treffer.classList.remove('aktiv');
        tip.verbergen();
      };
      treffer.addEventListener('pointermove', (ev) => zeigen(ev.clientX, ev.clientY));
      treffer.addEventListener('pointerleave', weg);
      treffer.addEventListener('focus', () => zeigen());
      treffer.addEventListener('blur', weg);
    });
  }

  return { linien, saeulen, pareto, tip, skala };
})();
