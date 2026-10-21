// 유닛 스킨 "할로윈" (Core UnitSkins 의 id "halloween"). 기본 유닛과 같은 캔버스 96x112, 바닥점 (48, 100).
// 원소가 무엇인지는 그대로 알아볼 수 있게 색과 실루엣을 지키고, 할로윈 소품을 입힌다.
//   빨강 잭오랜턴 불꽃 · 파랑 물귀신 · 노랑 묘비 번개 · 초록 마녀 모자 회오리 · 보라 박쥐 날개 구름
//   주황 마녀 솥 마그마 · 백색 유령 촛불 · 흑색 검은 고양이 구 · 회색 묘비 석상
import { Pic, ell, rounded, poly, star, dark, light, stroke, f } from './kit.mjs';
import { UNIT, shadow, face, flame, bolt } from './units.mjs';

const G = UNIT;
const NAME = (e) => `Units/unit_${e}_halloween`;

/** 마녀 모자 (cx, by = 챙 가운데). s 크기, rot 기울기. */
function witchHat(p, cx, by, s = 1, rot = -10, band = '#ff8a1f') {
  const g = `transform="rotate(${rot} ${cx} ${by})"`;
  p.add(`<path d="${ell(cx, by, 22 * s, 6 * s)}" fill="#2a1a3a" ${stroke(2.5, '#120818')} ${g}/>`);
  p.add(`<path d="M${f(cx - 13 * s)} ${f(by - 1)} Q${f(cx - 6 * s)} ${f(by - 22 * s)} ${f(cx + 4 * s)} ${f(by - 34 * s)} Q${f(cx + 12 * s)} ${f(by - 38 * s)} ${f(cx + 18 * s)} ${f(by - 30 * s)} Q${f(cx + 8 * s)} ${f(by - 26 * s)} ${f(cx + 13 * s)} ${f(by - 1)} Z" fill="#3a2452" ${stroke(2.5, '#120818')} ${g}/>`);
  p.add(`<path d="M${f(cx - 12 * s)} ${f(by - 5 * s)} Q${f(cx)} ${f(by - 8 * s)} ${f(cx + 12 * s)} ${f(by - 5 * s)} L${f(cx + 12 * s)} ${f(by - 1)} Q${f(cx)} ${f(by - 4 * s)} ${f(cx - 12 * s)} ${f(by - 1)} Z" fill="${band}" ${g}/>`);
  p.add(`<path d="${star(cx + 4 * s, by - 18 * s, 3.5 * s, 1.3 * s, 5)}" fill="#ffd84a" ${g}/>`);
}

/** 작은 박쥐 (cx, cy). */
function bat(p, cx, cy, s = 1) {
  const w = `M${f(cx)} ${f(cy)} C${f(cx - 6 * s)} ${f(cy - 8 * s)} ${f(cx - 14 * s)} ${f(cy - 6 * s)} ${f(cx - 18 * s)} ${f(cy - 2 * s)}
    C${f(cx - 14 * s)} ${f(cy - 1 * s)} ${f(cx - 12 * s)} ${f(cy + 2 * s)} ${f(cx - 11 * s)} ${f(cy + 4 * s)} C${f(cx - 8 * s)} ${f(cy + 1 * s)} ${f(cx - 5 * s)} ${f(cy + 2 * s)} ${f(cx)} ${f(cy + 4 * s)}
    C${f(cx + 5 * s)} ${f(cy + 2 * s)} ${f(cx + 8 * s)} ${f(cy + 1 * s)} ${f(cx + 11 * s)} ${f(cy + 4 * s)} C${f(cx + 12 * s)} ${f(cy + 2 * s)} ${f(cx + 14 * s)} ${f(cy - 1 * s)} ${f(cx + 18 * s)} ${f(cy - 2 * s)}
    C${f(cx + 14 * s)} ${f(cy - 6 * s)} ${f(cx + 6 * s)} ${f(cy - 8 * s)} ${f(cx)} ${f(cy)} Z`;
  p.path(w, '#2a1a3a', stroke(1.5, '#120818'));
  p.path(ell(cx, cy, 3.5 * s, 4 * s), '#2a1a3a');
  for (const dx of [-1.4, 1.4]) p.path(ell(cx + dx * s, cy - 0.5 * s, 0.9 * s, 0.9 * s), '#ffd84a');
}

/** 묘비 (cx, by 바닥), 위에 RIP. */
function tomb(p, cx, by, w, h, light1 = '#c8c4d0', dark1 = '#6f6a7a') {
  const d = `M${cx - w / 2} ${by} V${by - h + w / 2} A${w / 2} ${w / 2} 0 0 1 ${cx + w / 2} ${by - h + w / 2} V${by} Z`;
  p.path(d, p.lin([[0, light1], [1, dark1]], 0, 0, 1, 0), stroke(3, '#3a3444'));
  p.path(rounded([[cx - w / 2 - 5, by + 2], [cx - w / 2 - 5, by - 5], [cx + w / 2 + 5, by - 5], [cx + w / 2 + 5, by + 2]], 3), '#5a8a3a', stroke(2, '#2a4a1a'));
  return d;
}

export function halloweenUnits() {
  const out = [];
  let p;

  // 빨강: 잭오랜턴 — 호박 몸통에 새긴 얼굴이 빛나고, 머리 위로 불꽃
  p = new Pic(G.w, G.h);
  shadow(p, 24);
  flame(p, 48, 64, 46, 20, ['#ff4a2a', '#ff9a2a', '#ffe36a']);
  for (const [x, w] of [[36, 16], [60, 16], [48, 18]])
    p.path(ell(x, 78, w, 20), p.lin([[0, '#ffb04a'], [1, '#e2641a']], 0, 0, 1, 1), stroke(3, '#7a2a08'));
  p.path('M48 58 Q46 52 50 50', 'none', 'stroke="#3a6a1a" stroke-width="4" stroke-linecap="round"');
  p.path(poly([[34, 72], [42, 72], [38, 64]]), '#ffe36a', stroke(1.5, '#7a2a08'));
  p.path(poly([[54, 72], [62, 72], [58, 64]]), '#ffe36a', stroke(1.5, '#7a2a08'));
  p.path('M32 82 L38 86 L42 82 L48 88 L54 82 L58 86 L64 82 Q58 94 48 94 Q38 94 32 82 Z', '#ffe36a', stroke(1.5, '#7a2a08'));
  p.path(ell(30, 70, 4, 6), '#ffffff', 'opacity="0.35"');
  out.push(p.sprite(NAME('red')));

  // 파랑: 물귀신 — 물방울 몸이 아래로 너울지는 유령 천
  p = new Pic(G.w, G.h);
  shadow(p, 20);
  const ghost = 'M48 18 C60 30 74 48 74 66 L74 92 Q68 86 63 92 Q58 98 53 92 Q48 86 43 92 Q38 98 33 92 Q28 86 22 92 L22 66 C22 48 36 30 48 18 Z';
  p.path(ghost, p.rad([[0, '#e6f6ff'], [0.55, '#7cc4ff'], [1, '#2f7ae0']], 0.38, 0.4, 0.8), stroke(3, '#1a4a9a'));
  p.path('M34 54 Q36 42 46 32', 'none', 'stroke="#ffffff" stroke-width="5" stroke-linecap="round" opacity="0.85"');
  for (const dx of [-10, 10]) p.path(ell(48 + dx, 62, 5, 7), '#16264a');
  for (const dx of [-11.5, 8.5]) p.path(ell(48 + dx, 59, 1.8, 2.2), '#ffffff');
  p.path(ell(48, 76, 5, 6), '#16264a');
  p.path(ell(31, 70, 4, 2.2), '#ff8fb0', 'opacity="0.5"');
  p.path(ell(65, 70, 4, 2.2), '#ff8fb0', 'opacity="0.5"');
  out.push(p.sprite(NAME('blue')));

  // 노랑: 묘비 번개 — 이끼 낀 묘비 위로 번개, 옆에 박쥐
  p = new Pic(G.w, G.h);
  shadow(p, 24);
  tomb(p, 48, 98, 40, 36);
  p.path('M38 86 h20 M48 76 v20', 'none', 'stroke="#5a5466" stroke-width="3" stroke-linecap="round" opacity="0.7"');
  p.path(bolt(48, 40, 1.35), p.lin([[0, '#fff6a0'], [0.5, '#ffd21f'], [1, '#f39a00']]), stroke(3, '#8a4a00'));
  p.path(ell(48, 40, 26, 26), p.rad([[0, '#fff3a0', 0.45], [1, '#fff3a0', 0]]));
  bat(p, 74, 22, 0.9);
  out.push(p.sprite(NAME('yellow')));

  // 초록: 마녀 모자를 쓴 회오리
  p = new Pic(G.w, G.h);
  shadow(p, 20);
  const tw = 'M30 40 Q48 32 70 40 Q66 52 58 60 Q54 70 52 80 Q50 90 46 98 Q44 88 42 80 Q36 68 34 60 Q26 50 30 40 Z';
  p.path(tw, p.lin([[0, '#d6ffb0'], [1, '#3fbf5a']], 0, 0, 1, 1), stroke(3, '#1f6a34'));
  for (const [y, w] of [[46, 15], [58, 11], [70, 7], [82, 4]])
    p.path(`M${48 - w} ${y} Q48 ${y + 6} ${48 + w + 4} ${y - 2}`, 'none', 'stroke="#ffffff" stroke-width="3" stroke-linecap="round" opacity="0.85"');
  witchHat(p, 50, 40, 1, -12, '#7fe05a');
  for (const [x, y] of [[20, 54], [76, 66]]) p.path(star(x, y, 5, 1.6, 4), '#c8ff8a', 'opacity="0.9"');
  out.push(p.sprite(NAME('green')));

  // 보라: 박쥐 날개 폭풍 구름 (송곳니)
  p = new Pic(G.w, G.h);
  shadow(p, 22);
  for (const [x, y] of [[40, 90], [56, 90]]) p.path(bolt(x, y - 6, 0.5), '#ffe24a', stroke(1.5, '#7a4a00'));
  for (const s of [-1, 1]) {
    const x0 = 48 + s * 22;
    p.path(`M${x0} 54 C${x0 + s * 10} 40 ${x0 + s * 24} 38 ${x0 + s * 26} 48 C${x0 + s * 20} 48 ${x0 + s * 18} 54 ${x0 + s * 18} 58 C${x0 + s * 12} 54 ${x0 + s * 8} 58 ${x0} 62 Z`, '#3a2452', stroke(2.5, '#120818'));
  }
  const cloud = 'M22 72 C14 72 14 56 26 56 C24 40 44 36 50 46 C56 34 78 40 74 56 C86 56 84 74 72 74 Z';
  p.path(cloud, p.rad([[0, '#d6b6ff'], [0.6, '#9a5ae6'], [1, '#5a2aa6']], 0.4, 0.35, 0.8), stroke(3, '#3a1a6a'));
  p.path('M30 56 Q36 48 46 52', 'none', 'stroke="#f0e2ff" stroke-width="4" stroke-linecap="round" opacity="0.8"');
  for (const dx of [-7, 7]) p.path(ell(50 + dx, 60, 3, 3.6), '#ffe24a', stroke(1.2, '#3a1a6a'));
  p.path('M44 68 Q50 72 56 68', 'none', 'stroke="#3a1a6a" stroke-width="2.2" stroke-linecap="round"');
  p.path(poly([[46, 69], [48, 74], [49, 70]]), '#ffffff');
  p.path(poly([[51, 70], [52, 74], [54, 69]]), '#ffffff');
  out.push(p.sprite(NAME('purple')));

  // 주황: 마녀 솥 — 끓는 마그마와 불꽃, 거품
  p = new Pic(G.w, G.h);
  shadow(p, 26);
  flame(p, 48, 64, 40, 18, ['#ff6a1a', '#ffb02a', '#fff08a']);
  p.path('M20 66 Q18 98 48 98 Q78 98 76 66 Z', p.lin([[0, '#4a4456'], [1, '#1e1a26']], 0, 0, 1, 1), stroke(3, '#0e0a14'));
  p.path(ell(48, 66, 29, 8), '#ff8a1a', stroke(3, '#0e0a14'));
  p.path(ell(48, 65, 22, 5), '#ffd04a');
  for (const [x, r] of [[36, 4], [56, 5], [46, 3]]) p.path(ell(x, 62, r, r), '#ffe08a', stroke(1.5, '#c85a10'));
  for (const x of [26, 70]) p.path(`M${x} 96 l${x < 48 ? -4 : 4} 6`, 'none', 'stroke="#1e1a26" stroke-width="4" stroke-linecap="round"');
  p.path('M28 76 Q30 86 38 92', 'none', 'stroke="#8a8098" stroke-width="3" stroke-linecap="round" opacity="0.6"');
  out.push(p.sprite(NAME('orange')));

  // 백색: 유령 촛불 — 녹아내리는 흰 초와 푸르스름한 영혼 불꽃, 후광
  p = new Pic(G.w, G.h);
  shadow(p, 18);
  p.path(ell(48, 40, 38, 38), p.rad([[0, '#e8fbff', 0.9], [0.5, '#b8f0ff', 0.35], [1, '#b8f0ff', 0]]));
  flame(p, 48, 46, 34, 13, ['#7fe2f5', '#c8f8ff', '#ffffff']);
  p.path('M34 50 H62 V96 H34 Z', p.lin([[0, '#ffffff'], [1, '#d8dcec']], 0, 0, 1, 0), stroke(3, '#6a7aa8'));
  p.path('M34 50 H62 V56 Q58 62 56 56 Q52 66 49 56 Q45 62 42 56 Q38 64 34 56 Z', '#ffffff', stroke(2, '#6a7aa8'));
  p.path('M48 50 V44', 'none', 'stroke="#3a3a4a" stroke-width="2.5" stroke-linecap="round"');
  face(p, 48, 76, 0.75);
  p.path(star(66, 26, 6, 2), '#ffffff');
  out.push(p.sprite(NAME('white')));

  // 흑색: 검은 고양이 구 — 공허의 구에 고양이 귀와 빛나는 눈
  p = new Pic(G.w, G.h);
  shadow(p, 22);
  p.path(ell(48, 62, 40, 40), p.rad([[0, '#b45aff', 0.6], [0.6, '#5a1aa0', 0.3], [1, '#5a1aa0', 0]]));
  for (const s of [-1, 1]) p.path(poly([[48 + s * 24, 50], [48 + s * 22, 30], [48 + s * 8, 40]]), '#1a0a2a', stroke(3, '#c48aff'));
  for (const s of [-1, 1]) p.path(poly([[48 + s * 20, 46], [48 + s * 20, 35], [48 + s * 12, 41]]), '#5a2a8a');
  p.path(ell(48, 64, 26, 26), p.rad([[0, '#2a1a3a'], [0.75, '#120818'], [1, '#3a1a5a']], 0.5, 0.5, 0.5), stroke(3, '#c48aff'));
  for (const dx of [-10, 10]) {
    p.path(ell(48 + dx, 62, 6, 7), p.rad([[0, '#fff6a0'], [1, '#ffc81f']]), stroke(1.5, '#7a4a00'));
    p.path(ell(48 + dx, 62, 1.6, 5.5), '#120818');
  }
  p.path('M44 74 L48 77 L52 74', 'none', 'stroke="#c48aff" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"');
  for (const s of [-1, 1]) p.path(`M${48 + s * 8} 76 L${48 + s * 24} 73 M${48 + s * 8} 78 L${48 + s * 24} 80`, 'none', 'stroke="#c48aff" stroke-width="1.5" stroke-linecap="round" opacity="0.8"');
  out.push(p.sprite(NAME('black')));

  // 무속성(탈색): 잠든 얼굴의 묘비 석상
  p = new Pic(G.w, G.h);
  shadow(p, 22);
  tomb(p, 48, 98, 46, 62, '#e6e3e8', '#7f7a88');
  p.path('M36 70 Q40 74 44 70 M52 70 Q56 74 60 70', 'none', 'stroke="#4a4652" stroke-width="2.5" stroke-linecap="round"');
  p.path('M42 82 Q48 85 54 82', 'none', 'stroke="#4a4652" stroke-width="2" stroke-linecap="round"');
  p.path('M32 52 L38 58 M62 48 L58 54 L62 58', 'none', 'stroke="#6f6a78" stroke-width="2" stroke-linecap="round"');
  p.path(ell(40, 48, 6, 3.5), '#ffffff', 'opacity="0.4"');
  out.push(p.sprite(NAME('grey')));

  return out;
}
