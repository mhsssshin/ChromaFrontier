// 유닛 스킨 "제헌절" (Core UnitSkins 의 id "jeheon"). 기본 유닛과 같은 캔버스 96x112, 바닥점 (48, 100).
// 태극(빨강·파랑)·건곤감리·무궁화·청사초롱·갓·헌법 책·백자 같은 우리 상징으로 원소를 꾸민다. 글자는 쓰지 않는다.
//   빨강 청사초롱 불꽃 · 파랑 갓 쓴 물방울 · 노랑 헌법 책 번개 · 초록 무궁화 회오리 · 보라 무궁화 구름
//   주황 놋쇠 화로 · 백색 달항아리 · 흑색 태극 구(건곤감리) · 회색 비석
import { Pic, ell, rounded, poly, star, dark, light, stroke, f } from './kit.mjs';
import { UNIT, shadow, face, flame, bolt } from './units.mjs';

const G = UNIT;
const NAME = (e) => `Units/unit_${e}_jeheon`;
const RED = '#cd2e3a', BLUE = '#0047a0', INK = '#1a1a24';

/** 태극 문양 (cx, cy, 반지름 r): 위 빨강, 아래 파랑이 S 자로 맞물린다. */
function taegeuk(p, cx, cy, r, rot = 0) {
  const g = `transform="rotate(${rot} ${cx} ${cy})"`;
  p.add(`<path d="M${f(cx - r)} ${f(cy)} A${f(r)} ${f(r)} 0 0 1 ${f(cx + r)} ${f(cy)} A${f(r / 2)} ${f(r / 2)} 0 0 1 ${f(cx)} ${f(cy)} A${f(r / 2)} ${f(r / 2)} 0 0 0 ${f(cx - r)} ${f(cy)} Z" fill="${RED}" ${g}/>`);
  p.add(`<path d="M${f(cx - r)} ${f(cy)} A${f(r)} ${f(r)} 0 0 0 ${f(cx + r)} ${f(cy)} A${f(r / 2)} ${f(r / 2)} 0 0 1 ${f(cx)} ${f(cy)} A${f(r / 2)} ${f(r / 2)} 0 0 0 ${f(cx - r)} ${f(cy)} Z" fill="${BLUE}" ${g}/>`);
  p.path(ell(cx, cy, r, r), 'none', stroke(2, '#ffffff'));
}

/** 괘 (건곤감리 중 하나): bars 는 위에서부터 [true=통짜, false=가운데 끊김]. (cx, cy) 중심, 각도 rot. */
function trigram(p, cx, cy, bars, s = 1, rot = 0) {
  const w = 14 * s, h = 2.6 * s, gap = 2 * s;
  let d = '';
  bars.forEach((solid, i) => {
    const y = cy - 1.5 * (h + gap) + i * (h + gap);
    if (solid) d += `M${f(cx - w / 2)} ${f(y)} h${f(w)} v${f(h)} h${f(-w)} Z `;
    else d += `M${f(cx - w / 2)} ${f(y)} h${f(w * 0.42)} v${f(h)} h${f(-w * 0.42)} Z M${f(cx + w * 0.08)} ${f(y)} h${f(w * 0.42)} v${f(h)} h${f(-w * 0.42)} Z `;
  });
  p.add(`<path d="${d}" fill="${INK}" transform="rotate(${rot} ${cx} ${cy})"/>`);
}

/** 무궁화 꽃 (cx, cy, 크기 s): 분홍 꽃잎 다섯 장 + 붉은 단심. */
function mugunghwa(p, cx, cy, s = 1) {
  for (let i = 0; i < 5; i++)
    p.add(`<path d="${ell(cx, cy - 6 * s, 4.6 * s, 6.4 * s)}" fill="#f7a8c8" ${stroke(1.2, '#b0466e')} transform="rotate(${i * 72} ${cx} ${cy})"/>`);
  p.path(ell(cx, cy, 3.2 * s, 3.2 * s), '#c8143c');
  p.path(ell(cx, cy, 1.2 * s, 1.2 * s), '#ffe36a');
}

/** 갓 (cx, by = 챙 가운데). */
function gat(p, cx, by, s = 1) {
  p.path(ell(cx, by, 28 * s, 7 * s), '#1e1e28', 'opacity="0.88" ' + stroke(2, '#08080c'));
  p.path(ell(cx, by - 1, 22 * s, 4.5 * s), '#3a3a48', 'opacity="0.5"');
  p.path(`M${f(cx - 10 * s)} ${f(by - 1)} L${f(cx - 9 * s)} ${f(by - 18 * s)} Q${f(cx)} ${f(by - 21 * s)} ${f(cx + 9 * s)} ${f(by - 18 * s)} L${f(cx + 10 * s)} ${f(by - 1)} Z`, '#1e1e28', 'opacity="0.92" ' + stroke(2, '#08080c'));
  p.path(`M${f(cx - 9 * s)} ${f(by - 6 * s)} Q${f(cx)} ${f(by - 8 * s)} ${f(cx + 9 * s)} ${f(by - 6 * s)}`, 'none', `stroke="#8a6a3a" stroke-width="${f(2 * s)}"`);
}

export function jeheonUnits() {
  const out = [];
  let p;

  // 빨강: 청사초롱 — 붉은 위·푸른 아래 초롱 안에 불꽃, 위로 불길
  p = new Pic(G.w, G.h);
  shadow(p, 20);
  p.path('M48 30 V40', 'none', 'stroke="#6a4a2a" stroke-width="3" stroke-linecap="round"');
  flame(p, 48, 52, 30, 12, ['#ff4a2a', '#ff9a2a', '#ffe36a']);
  p.path(rounded([[30, 48], [66, 48], [70, 72], [66, 96], [30, 96], [26, 72]], 8), BLUE, stroke(3, '#102050'));
  p.path(rounded([[30, 48], [66, 48], [70, 72], [26, 72]], 6), RED, stroke(3, '#5a1018'));
  p.path(ell(48, 70, 12, 14), p.rad([[0, '#fff3a0', 0.9], [1, '#ffb02a', 0]]));
  for (const y of [48, 96]) p.path(rounded([[32, y - 3], [64, y - 3], [64, y + 3], [32, y + 3]], 2), '#d8a83a', stroke(2, '#6a4a10'));
  p.path('M48 99 V106', 'none', `stroke="${RED}" stroke-width="3" stroke-linecap="round"`);
  out.push(p.sprite(NAME('red')));

  // 파랑: 갓 쓴 물방울
  p = new Pic(G.w, G.h);
  shadow(p);
  const drop = 'M48 26 C56 40 74 54 74 72 C74 88 62 98 48 98 C34 98 22 88 22 72 C22 54 40 40 48 26 Z';
  p.path(drop, p.rad([[0, '#bfe8ff'], [0.55, '#45a8ff'], [1, '#1f6fd8']], 0.38, 0.45, 0.75), stroke(3, '#1a4a9a'));
  p.path('M34 62 Q36 50 44 42', 'none', 'stroke="#ffffff" stroke-width="5" stroke-linecap="round" opacity="0.85"');
  face(p, 48, 76, 0.95);
  gat(p, 48, 40, 1);
  p.path('M30 42 Q28 66 34 78 M66 42 Q68 66 62 78', 'none', 'stroke="#3a3a48" stroke-width="1.5" opacity="0.7"');
  out.push(p.sprite(NAME('blue')));

  // 노랑: 헌법 책 번개 — 펼친 책(태극 책갈피) 위로 번개
  p = new Pic(G.w, G.h);
  shadow(p, 26);
  p.path('M18 84 Q34 76 48 84 Q62 76 78 84 L78 96 Q62 88 48 96 Q34 88 18 96 Z', '#8a1a24', stroke(2.5, '#3a0a10'));
  p.path('M20 80 Q34 72 48 80 L48 92 Q34 84 20 92 Z', p.lin([[0, '#fffaf0'], [1, '#e8dcc0']]), stroke(2, '#6a5a3a'));
  p.path('M76 80 Q62 72 48 80 L48 92 Q62 84 76 92 Z', p.lin([[0, '#f3e8cc'], [1, '#fffaf0']]), stroke(2, '#6a5a3a'));
  for (const k of [0, 1, 2]) {
    p.path(`M${24} ${82 + k * 3 - k} Q34 ${76 + k * 3} 44 ${82 + k * 3}`, 'none', 'stroke="#a8987a" stroke-width="1.2"');
    p.path(`M${52} ${82 + k * 3} Q62 ${76 + k * 3} ${72} ${82 + k * 3 - k}`, 'none', 'stroke="#a8987a" stroke-width="1.2"');
  }
  taegeuk(p, 62, 86, 4.5, 0);
  p.path(bolt(48, 44, 1.35), p.lin([[0, '#fff6a0'], [0.5, '#ffd21f'], [1, '#f39a00']]), stroke(3, '#8a4a00'));
  p.path(ell(48, 44, 26, 26), p.rad([[0, '#fff3a0', 0.45], [1, '#fff3a0', 0]]));
  out.push(p.sprite(NAME('yellow')));

  // 초록: 무궁화 꽃잎이 휘도는 회오리
  p = new Pic(G.w, G.h);
  shadow(p, 20);
  const tw = 'M30 30 Q48 22 70 30 Q66 44 58 52 Q54 64 52 76 Q50 88 46 98 Q44 86 42 76 Q36 62 34 52 Q26 42 30 30 Z';
  p.path(tw, p.lin([[0, '#c8ffb0'], [1, '#3fbf5a']], 0, 0, 1, 1), stroke(3, '#1f6a34'));
  for (const [y, w] of [[36, 16], [50, 12], [64, 8], [78, 5]])
    p.path(`M${48 - w} ${y} Q48 ${y + 6} ${48 + w + 4} ${y - 2}`, 'none', 'stroke="#ffffff" stroke-width="3" stroke-linecap="round" opacity="0.85"');
  mugunghwa(p, 22, 44, 0.85);
  mugunghwa(p, 74, 60, 0.75);
  mugunghwa(p, 30, 76, 0.6);
  out.push(p.sprite(NAME('green')));

  // 보라: 무궁화를 머리에 꽂은 폭풍 구름
  p = new Pic(G.w, G.h);
  shadow(p, 22);
  for (const [x, y] of [[40, 90], [56, 90]]) p.path(bolt(x, y - 6, 0.5), '#ffe24a', stroke(1.5, '#7a4a00'));
  const cloud = 'M22 70 C14 70 14 54 26 54 C24 38 44 34 50 44 C56 32 78 38 74 54 C86 54 84 72 72 72 Z';
  p.path(cloud, p.rad([[0, '#d6b6ff'], [0.6, '#9a5ae6'], [1, '#5a2aa6']], 0.4, 0.35, 0.8), stroke(3, '#3a1a6a'));
  p.path('M30 54 Q36 46 46 50', 'none', 'stroke="#f0e2ff" stroke-width="4" stroke-linecap="round" opacity="0.8"');
  face(p, 50, 60, 0.7, false);
  mugunghwa(p, 66, 40, 1.05);
  out.push(p.sprite(NAME('purple')));

  // 주황: 놋쇠 화로 — 불꽃과 숯
  p = new Pic(G.w, G.h);
  shadow(p, 26);
  flame(p, 48, 66, 44, 19, ['#ff6a1a', '#ffb02a', '#fff08a']);
  p.path('M20 66 L76 66 Q74 90 58 94 L38 94 Q22 90 20 66 Z', p.lin([[0, '#f0c860'], [0.6, '#c8902a'], [1, '#8a5a18']], 0, 0, 1, 1), stroke(3, '#4a2a08'));
  p.path(ell(48, 66, 28, 6), '#3a2418', stroke(2.5, '#4a2a08'));
  for (const [x, c] of [[38, '#ff6a1a'], [48, '#ffb02a'], [58, '#ff4a2a']]) p.path(ell(x, 65, 5, 3), c);
  p.path(rounded([[36, 94], [60, 94], [64, 100], [32, 100]], 2), '#a8701e', stroke(2.5, '#4a2a08'));
  p.path('M26 74 Q30 84 38 88', 'none', 'stroke="#fff3c0" stroke-width="3" stroke-linecap="round" opacity="0.6"');
  for (const x of [34, 48, 62]) p.path(ell(x, 80, 2.5, 2.5), '#8a5a18', 'opacity="0.7"');
  out.push(p.sprite(NAME('orange')));

  // 백색: 달항아리 — 둥근 백자에 은은한 빛
  p = new Pic(G.w, G.h);
  shadow(p, 22);
  p.path(ell(48, 58, 40, 40), p.rad([[0, '#fffbe0', 0.85], [0.5, '#ffe9a0', 0.3], [1, '#ffe9a0', 0]]));
  for (let i = 0; i < 8; i++) p.add(`<path d="M48 58 L51 22 L45 22 Z" fill="#fff6c8" opacity="0.45" transform="rotate(${i * 45} 48 58)"/>`);
  p.path(rounded([[38, 30], [58, 30], [58, 36], [38, 36]], 2), p.lin([[0, '#ffffff'], [1, '#e2e2ea']]), stroke(2.5, '#8a8aa0'));
  p.path('M40 36 C14 42 14 92 40 96 L56 96 C82 92 82 42 56 36 Z', p.rad([[0, '#ffffff'], [0.7, '#f4f2ea'], [1, '#d8d6cc']], 0.4, 0.38, 0.7), stroke(3, '#8a8aa0'));
  p.path('M30 50 Q26 62 30 74', 'none', 'stroke="#ffffff" stroke-width="5" stroke-linecap="round" opacity="0.9"');
  face(p, 50, 70, 0.75);
  p.path(star(68, 34, 7, 2.2), '#ffffff');
  out.push(p.sprite(NAME('white')));

  // 흑색: 태극 구 — 흰 원판에 태극, 둘레에 건곤감리
  p = new Pic(G.w, G.h);
  shadow(p, 22);
  p.path(ell(48, 60, 40, 40), p.rad([[0, '#ffffff', 0.5], [0.6, '#c8d4ff', 0.2], [1, '#c8d4ff', 0]]));
  p.path(ell(48, 62, 30, 30), p.rad([[0, '#ffffff'], [1, '#e6e8f0']]), stroke(3, INK));
  taegeuk(p, 48, 62, 15, 0);
  // 건(☰) 왼쪽 위, 곤(☷) 오른쪽 아래, 감(☵) 오른쪽 위, 리(☲) 왼쪽 아래
  trigram(p, 28, 42, [true, true, true], 0.75, -56);
  trigram(p, 68, 82, [false, false, false], 0.75, -56);
  trigram(p, 68, 42, [false, true, false], 0.75, 56);
  trigram(p, 28, 82, [true, false, true], 0.75, 56);
  out.push(p.sprite(NAME('black')));

  // 무속성(탈색): 잠든 얼굴의 비석 (지붕돌 + 받침)
  p = new Pic(G.w, G.h);
  shadow(p, 24);
  p.path(rounded([[22, 98], [22, 88], [74, 88], [74, 98]], 4), p.lin([[0, '#bdb8c4'], [1, '#6f6a78']]), stroke(3, '#3a3444'));
  p.path(rounded([[32, 90], [32, 44], [64, 44], [64, 90]], 4), p.lin([[0, '#e6e3e8'], [1, '#8f8a98']], 0, 0, 1, 0), stroke(3, '#3a3444'));
  p.path('M24 46 Q48 30 72 46 L68 50 Q48 38 28 50 Z', p.lin([[0, '#cfcad6'], [1, '#7f7a88']]), stroke(3, '#3a3444'));
  p.path('M38 64 Q42 68 46 64 M50 64 Q54 68 58 64', 'none', 'stroke="#4a4652" stroke-width="2.5" stroke-linecap="round"');
  p.path('M44 76 Q48 79 52 76', 'none', 'stroke="#4a4652" stroke-width="2" stroke-linecap="round"');
  p.path(ell(40, 54, 4, 2.5), '#ffffff', 'opacity="0.45"');
  out.push(p.sprite(NAME('grey')));

  return out;
}
