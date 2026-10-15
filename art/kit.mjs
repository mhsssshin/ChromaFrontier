// SVG 그림 도구: 색 섞기, 고유 id 그라데이션, 둥근 도형. 모든 정의 파일(tiles/units/fx/ui)이 같이 쓴다.

export const rgb = (h) => [1, 3, 5].map((i) => parseInt(h.slice(i, i + 2), 16));
export const hex = (c) => '#' + c.map((v) => Math.max(0, Math.min(255, Math.round(v))).toString(16).padStart(2, '0')).join('');
export const mix = (a, b, t) => hex(rgb(a).map((v, i) => v + (rgb(b)[i] - v) * t));
export const dark = (c, t = 0.3) => mix(c, '#2a1838', t);   // 그림자는 보랏빛 어둠으로
export const light = (c, t = 0.45) => mix(c, '#fffbe8', t); // 밝음은 따뜻한 흰색으로
export const f = (n) => Math.round(n * 100) / 100;

/** 스프라이트 하나: 정의(defs)를 모아 SVG 문자열을 만든다. id 는 스프라이트마다 고유. */
let serial = 0;
export class Pic {
  constructor(w, h) {
    this.w = w;
    this.h = h;
    this.defs = [];
    this.body = [];
    this.p = `s${serial++}_`;
    this.n = 0;
  }
  id() { return `${this.p}${this.n++}`; }
  /** 선형 그라데이션. stops = [[offset, color, opacity?], ...], 방향 (x1,y1)→(x2,y2) 는 0~1 */
  lin(stops, x1 = 0, y1 = 0, x2 = 0, y2 = 1) {
    const id = this.id();
    this.defs.push(`<linearGradient id="${id}" x1="${x1}" y1="${y1}" x2="${x2}" y2="${y2}">${stopTags(stops)}</linearGradient>`);
    return `url(#${id})`;
  }
  /** 방사형 그라데이션 (cx, cy, r 은 0~1, 도형 기준) */
  rad(stops, cx = 0.5, cy = 0.5, r = 0.5, fx = cx, fy = cy) {
    const id = this.id();
    this.defs.push(`<radialGradient id="${id}" cx="${cx}" cy="${cy}" r="${r}" fx="${fx}" fy="${fy}">${stopTags(stops)}</radialGradient>`);
    return `url(#${id})`;
  }
  blur(sd) {
    const id = this.id();
    this.defs.push(`<filter id="${id}" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="${sd}"/></filter>`);
    return `url(#${id})`;
  }
  clip(d) {
    const id = this.id();
    this.defs.push(`<clipPath id="${id}"><path d="${d}"/></clipPath>`);
    return `url(#${id})`;
  }
  add(...parts) { this.body.push(...parts); return this; }
  path(d, fill, extra = '') { return this.add(`<path d="${d}" fill="${fill}" ${extra}/>`); }
  svg() {
    return `<svg xmlns="http://www.w3.org/2000/svg" width="${this.w}" height="${this.h}" viewBox="0 0 ${this.w} ${this.h}"><defs>${this.defs.join('')}</defs>${this.body.join('')}</svg>`;
  }
  sprite(name) { return { name, w: this.w, h: this.h, svg: this.svg() }; }
}

const stopTags = (stops) => stops.map(([o, c, a = 1]) => `<stop offset="${o}" stop-color="${c}" stop-opacity="${a}"/>`).join('');

export const poly = (pts) => 'M' + pts.map(([x, y]) => `${f(x)} ${f(y)}`).join(' L') + ' Z';

/** 모서리가 둥근 다각형 */
export function rounded(pts, r = 8) {
  const n = pts.length;
  let d = '';
  for (let i = 0; i < n; i++) {
    const [px, py] = pts[(i - 1 + n) % n], [x, y] = pts[i], [nx, ny] = pts[(i + 1) % n];
    const l1 = Math.hypot(x - px, y - py), l2 = Math.hypot(nx - x, ny - y);
    const r1 = Math.min(r, l1 / 2), r2 = Math.min(r, l2 / 2);
    const ax = x + ((px - x) / l1) * r1, ay = y + ((py - y) / l1) * r1;
    const bx = x + ((nx - x) / l2) * r2, by = y + ((ny - y) / l2) * r2;
    d += `${i ? 'L' : 'M'}${f(ax)} ${f(ay)} Q${f(x)} ${f(y)} ${f(bx)} ${f(by)} `;
  }
  return d + 'Z';
}

export const ell = (cx, cy, rx, ry = rx) =>
  `M${f(cx - rx)} ${f(cy)} A${f(rx)} ${f(ry)} 0 1 0 ${f(cx + rx)} ${f(cy)} A${f(rx)} ${f(ry)} 0 1 0 ${f(cx - rx)} ${f(cy)} Z`;

export const rect = (x, y, w, h, r = 0) =>
  `M${f(x + r)} ${f(y)} H${f(x + w - r)} Q${f(x + w)} ${f(y)} ${f(x + w)} ${f(y + r)} V${f(y + h - r)} Q${f(x + w)} ${f(y + h)} ${f(x + w - r)} ${f(y + h)} H${f(x + r)} Q${f(x)} ${f(y + h)} ${f(x)} ${f(y + h - r)} V${f(y + r)} Q${f(x)} ${f(y)} ${f(x + r)} ${f(y)} Z`;

/** n각 별 (뾰족한 반짝임) */
export function star(cx, cy, rOut, rIn, n = 4, rot = -90) {
  const pts = [];
  for (let i = 0; i < n * 2; i++) {
    const a = ((rot + (i * 180) / n) * Math.PI) / 180, r = i % 2 ? rIn : rOut;
    pts.push([cx + Math.cos(a) * r, cy + Math.sin(a) * r]);
  }
  return poly(pts);
}

/** 결정적 난수 (같은 seed 면 같은 그림) */
export function rng(seed) {
  let s = seed >>> 0 || 1;
  return () => ((s = (s * 1664525 + 1013904223) >>> 0) / 4294967296);
}

export const OUTLINE = '#3a2433';
export const stroke = (w = 3, c = OUTLINE) => `stroke="${c}" stroke-width="${w}" stroke-linejoin="round" stroke-linecap="round"`;
