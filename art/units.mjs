// 원소 유닛 (1~3단계 + 무속성)과 팀 링, 지뢰.
// 유닛 캔버스 96x112, 바닥점 (48, 100) — Unity 쪽 Editor/ArtImporter.cs 의 UnitPivot 과 맞물린다.
import { Pic, ell, rounded, poly, star, dark, light, stroke, f } from './kit.mjs';

export const UNIT = { w: 96, h: 112, gx: 48, gy: 100 };
const G = UNIT;

const shadow = (p, w = 22) => p.path(ell(G.gx, G.gy, w, w * 0.38), '#10202a', 'opacity="0.28"');

/** 눈 두 개 + 반짝임 + 작은 입 (물방울·회색 돌에 쓰는 귀여운 얼굴) */
function face(p, cx, cy, s = 1, mouth = true) {
  for (const dx of [-9, 9]) {
    p.path(ell(cx + dx * s, cy, 4.2 * s, 5.4 * s), '#20304a');
    p.path(ell(cx + dx * s - 1.4 * s, cy - 2 * s, 1.6 * s, 1.8 * s), '#ffffff');
  }
  if (mouth) p.path(`M${f(cx - 4 * s)} ${f(cy + 7 * s)} Q${f(cx)} ${f(cy + 11 * s)} ${f(cx + 4 * s)} ${f(cy + 7 * s)}`, 'none', `stroke="#20304a" stroke-width="${f(2.2 * s)}" stroke-linecap="round"`);
  p.path(ell(cx - 15 * s, cy + 6 * s, 3.5 * s, 2 * s), '#ff8fb0', 'opacity="0.55"');
  p.path(ell(cx + 15 * s, cy + 6 * s, 3.5 * s, 2 * s), '#ff8fb0', 'opacity="0.55"');
}

function flame(p, cx, by, h, w, cols) {
  const [outer, mid, core] = cols;
  const shape = (s) => {
    const hh = h * s, ww = w * s, y0 = by - hh;
    return `M${f(cx)} ${f(by)} C${f(cx - ww)} ${f(by)} ${f(cx - ww * 1.05)} ${f(by - hh * 0.45)} ${f(cx - ww * 0.55)} ${f(by - hh * 0.7)}
      C${f(cx - ww * 0.5)} ${f(by - hh * 0.5)} ${f(cx - ww * 0.25)} ${f(by - hh * 0.55)} ${f(cx - ww * 0.2)} ${f(by - hh * 0.62)}
      C${f(cx - ww * 0.35)} ${f(by - hh * 0.85)} ${f(cx - ww * 0.05)} ${f(y0 + hh * 0.05)} ${f(cx + ww * 0.1)} ${f(y0)}
      C${f(cx + ww * 0.05)} ${f(by - hh * 0.75)} ${f(cx + ww * 0.45)} ${f(by - hh * 0.8)} ${f(cx + ww * 0.55)} ${f(by - hh * 0.62)}
      C${f(cx + ww * 1.1)} ${f(by - hh * 0.4)} ${f(cx + ww)} ${f(by)} ${f(cx)} ${f(by)} Z`;
  };
  p.path(shape(1), p.lin([[0, light(outer, 0.15)], [1, outer]]), stroke(3, dark(outer, 0.55)));
  p.path(shape(0.68), mid);
  p.path(shape(0.38), core);
}

function bolt(cx, cy, s) {
  return poly([[cx + 4 * s, cy - 22 * s], [cx - 10 * s, cy + 2 * s], [cx - 1 * s, cy + 2 * s], [cx - 5 * s, cy + 22 * s], [cx + 10 * s, cy - 4 * s], [cx + 1 * s, cy - 4 * s]].map(([x, y]) => [x, y]));
}

export function units() {
  const out = [];
  let p;

  // 빨강: 화염
  p = new Pic(G.w, G.h);
  shadow(p);
  flame(p, 48, 98, 76, 30, ['#ff4a2a', '#ff9a2a', '#ffe36a']);
  out.push(p.sprite('Units/unit_red'));

  // 파랑: 물방울 (얼굴)
  p = new Pic(G.w, G.h);
  shadow(p);
  const drop = `M48 18 C56 34 74 50 74 70 C74 88 62 98 48 98 C34 98 22 88 22 70 C22 50 40 34 48 18 Z`;
  p.path(drop, p.rad([[0, '#bfe8ff'], [0.55, '#45a8ff'], [1, '#1f6fd8']], 0.38, 0.45, 0.75), stroke(3, '#1a4a9a'));
  p.path('M34 58 Q36 44 46 34', 'none', 'stroke="#ffffff" stroke-width="5" stroke-linecap="round" opacity="0.85"');
  face(p, 48, 74, 0.95);
  out.push(p.sprite('Units/unit_blue'));

  // 노랑: 번개 받침돌 + 번개
  p = new Pic(G.w, G.h);
  shadow(p, 24);
  p.path(rounded([[26, 98], [26, 82], [36, 74], [60, 74], [70, 82], [70, 98]], 6), p.lin([[0, '#bdb6c4'], [1, '#6f6878']], 0, 0, 1, 0), stroke(3));
  p.path(ell(48, 76, 21, 7), p.lin([[0, '#e4e0ea'], [1, '#a59eb0']]), stroke(2.5));
  p.path(bolt(48, 44, 1.45), p.lin([[0, '#fff6a0'], [0.5, '#ffd21f'], [1, '#f39a00']]), stroke(3, '#8a4a00'));
  p.path(ell(48, 44, 26, 26), p.rad([[0, '#fff3a0', 0.45], [1, '#fff3a0', 0]]));
  out.push(p.sprite('Units/unit_yellow'));

  // 초록: 돌풍 회오리
  p = new Pic(G.w, G.h);
  shadow(p, 20);
  const tw = `M30 30 Q48 22 70 30 Q66 44 58 52 Q54 64 52 76 Q50 88 46 98 Q44 86 42 76 Q36 62 34 52 Q26 42 30 30 Z`;
  p.path(tw, p.lin([[0, '#c8ffb0'], [1, '#3fbf5a']], 0, 0, 1, 1), stroke(3, '#1f6a34'));
  for (const [y, w] of [[36, 16], [50, 12], [64, 8], [78, 5]])
    p.path(`M${48 - w} ${y} Q48 ${y + 6} ${48 + w + 4} ${y - 2}`, 'none', 'stroke="#ffffff" stroke-width="3" stroke-linecap="round" opacity="0.85"');
  for (const [x, y, a] of [[22, 40, -30], [74, 56, 30], [28, 70, -10]])
    p.add(`<path d="${ell(0, 0, 6, 3)}" fill="#7fe05a" ${stroke(1.5, '#1f6a34')} transform="translate(${x} ${y}) rotate(${a})"/>`);
  out.push(p.sprite('Units/unit_green'));

  // 보라: 폭풍 구름
  p = new Pic(G.w, G.h);
  shadow(p, 22);
  for (const [x, y] of [[40, 90], [56, 90]]) p.path(bolt(x, y - 6, 0.5), '#ffe24a', stroke(1.5, '#7a4a00'));
  const cloud = `M22 70 C14 70 14 54 26 54 C24 38 44 34 50 44 C56 32 78 38 74 54 C86 54 84 72 72 72 Z`;
  p.path(cloud, p.rad([[0, '#d6b6ff'], [0.6, '#9a5ae6'], [1, '#5a2aa6']], 0.4, 0.35, 0.8), stroke(3, '#3a1a6a'));
  p.path('M30 54 Q36 46 46 50', 'none', 'stroke="#f0e2ff" stroke-width="4" stroke-linecap="round" opacity="0.8"');
  face(p, 50, 60, 0.7, false);
  out.push(p.sprite('Units/unit_purple'));

  // 주황: 마그마 바위 + 불꽃
  p = new Pic(G.w, G.h);
  shadow(p, 24);
  flame(p, 48, 66, 46, 20, ['#ff6a1a', '#ffb02a', '#fff08a']);
  p.path(rounded([[22, 98], [20, 78], [34, 62], [62, 62], [76, 78], [74, 98]], 9), p.lin([[0, '#6a3a2a'], [1, '#3a1e18']], 0, 0, 1, 1), stroke(3, '#24100c'));
  p.path('M30 80 L40 74 L46 84 L58 72 L66 82', 'none', 'stroke="#ff9a2a" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"');
  p.path('M30 80 L40 74 L46 84 L58 72 L66 82', 'none', 'stroke="#fff08a" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round"');
  out.push(p.sprite('Units/unit_orange'));

  // 백색: 빛나는 수정 + 후광
  p = new Pic(G.w, G.h);
  shadow(p, 20);
  p.path(ell(48, 52, 40, 40), p.rad([[0, '#fffbe0', 0.9], [0.5, '#ffe9a0', 0.35], [1, '#ffe9a0', 0]]));
  for (let i = 0; i < 8; i++) p.add(`<path d="M48 52 L52 14 L44 14 Z" fill="#fff6c8" opacity="0.5" transform="rotate(${i * 45} 48 52)"/>`);
  const crystal = poly([[48, 16], [66, 44], [58, 96], [38, 96], [30, 44]]);
  p.path(crystal, p.lin([[0, '#ffffff'], [0.6, '#e8f2ff'], [1, '#b8c8f0']], 0, 0, 1, 1), stroke(3, '#6a7aa8'));
  p.path(poly([[48, 16], [48, 96], [38, 96], [30, 44]]), '#ffffff', 'opacity="0.6"');
  p.path(star(62, 30, 8, 2.5), '#ffffff');
  out.push(p.sprite('Units/unit_white'));

  // 흑색: 공허의 구 + 보랏빛 소용돌이
  p = new Pic(G.w, G.h);
  shadow(p, 22);
  p.path(ell(48, 60, 40, 40), p.rad([[0, '#b45aff', 0.6], [0.6, '#5a1aa0', 0.3], [1, '#5a1aa0', 0]]));
  p.path(ell(48, 62, 26, 26), p.rad([[0, '#000000'], [0.75, '#1a0a2a'], [1, '#3a1a5a']], 0.5, 0.5, 0.5), stroke(3, '#c48aff'));
  for (const a of [0, 120, 240])
    p.add(`<path d="M48 62 m-18 0 a18 18 0 0 1 30 -12" fill="none" stroke="#a24aff" stroke-width="3.5" stroke-linecap="round" opacity="0.9" transform="rotate(${a} 48 62)"/>`);
  p.path(ell(40, 52, 5, 3.5), '#e6c8ff', 'opacity="0.7"');
  out.push(p.sprite('Units/unit_black'));

  // 무속성(탈색): 회색 돌 석상 (얼굴은 잠든 눈)
  p = new Pic(G.w, G.h);
  shadow(p);
  p.path(drop, p.rad([[0, '#e6e3e8'], [0.6, '#a19ca8'], [1, '#6f6a78']], 0.38, 0.45, 0.75), stroke(3, '#4a4652'));
  p.path('M38 74 Q42 78 46 74 M50 74 Q54 78 58 74', 'none', 'stroke="#4a4652" stroke-width="2.5" stroke-linecap="round"');
  p.path('M30 60 L38 66 M64 52 L58 60 L62 64', 'none', 'stroke="#6f6a78" stroke-width="2" stroke-linecap="round"');
  out.push(p.sprite('Units/unit_grey'));

  // 팀 링 (흰색 — 코드에서 팀 색으로 틴트하지 않고 각각 굽는다)
  for (const [name, c] of [['ring_player', '#3d8ff0'], ['ring_ai', '#ef4f4f']]) {
    p = new Pic(80, 36);
    p.path(ell(40, 18, 34, 14), 'none', `stroke="${dark(c, 0.4)}" stroke-width="7"`);
    p.path(ell(40, 18, 34, 14), 'none', `stroke="${light(c, 0.25)}" stroke-width="4"`);
    out.push(p.sprite(`Units/${name}`));
  }

  // 잠복 지뢰 (바닥에 반쯤 묻힘, 48x32, 중심 피벗)
  p = new Pic(48, 32);
  p.path(ell(24, 18, 18, 9), p.lin([[0, '#5a5560'], [1, '#2a2630']]), stroke(2.5, '#141018'));
  p.path(ell(24, 15, 12, 5), '#3a3640');
  p.path(ell(24, 13, 4.5, 3.5), p.rad([[0, '#ffd0c0'], [0.5, '#ff3a2a'], [1, '#a01010']]), stroke(1.5, '#3a0808'));
  for (const dx of [-14, 14]) p.path(`M${24 + dx} 14 l${dx > 0 ? 4 : -4} -4`, 'none', 'stroke="#2a2630" stroke-width="3" stroke-linecap="round"');
  out.push(p.sprite('Units/mine'));

  return out;
}
