// 아이소메트릭 타일과 타일 위 오버레이, 장식(나무·바위), 배경.
// 타일 캔버스 136x104: 윗면 마름모 128x64 의 중심이 (68, 34). 옆면 두께 28.
// Unity 쪽 피벗은 Editor/ArtImporter.cs 의 TilePivot·DecorPivot 과 맞물린다 — 숫자를 바꾸면 거기도 고칠 것.
import { Pic, poly, rounded, ell, star, mix, dark, light, rng, f } from './kit.mjs';

export const TILE = { w: 136, h: 104, cx: 68, cy: 34, hw: 64, hh: 32, depth: 28 };

const T = TILE;
const top = (inset = 0, r = 10) => {
  const hw = T.hw - inset * 2, hh = T.hh - inset;
  return rounded([[T.cx, T.cy - hh], [T.cx + hw, T.cy], [T.cx, T.cy + hh], [T.cx - hw, T.cy]], r);
};

/** 윗면 색, 옆면 띠 색으로 타일 한 장. */
function tile(name, { topA, topB, band, seed, grass = true, sprinkle = null, pattern = null }) {
  const p = new Pic(T.w, T.h);
  const L = [T.cx - T.hw, T.cy], B = [T.cx, T.cy + T.hh], R = [T.cx + T.hw, T.cy];
  const d = T.depth;
  const down = ([x, y], k = d) => [x, y + k];

  // 옆면: 위쪽 띠는 타일 색, 아래는 모래흙. 왼쪽 면이 더 밝다.
  const sandL = '#f3c766', sandR = '#d89c45';
  p.path(rounded([L, B, down(B), down(L)], 6), p.lin([[0, sandL], [1, mix(sandL, '#c98a3a', 0.35)]]));
  p.path(rounded([B, R, down(R), down(B)], 6), p.lin([[0, sandR], [1, mix(sandR, '#a8692c', 0.35)]]));
  p.path(poly([L, B, down(B, 11), down(L, 11)]), dark(band, 0.05));
  p.path(poly([B, R, down(R, 11), down(B, 11)]), dark(band, 0.25));
  // 흙 알갱이
  const r = rng(seed);
  for (let i = 0; i < 7; i++) {
    const left = i % 2 === 0, u = 0.15 + r() * 0.7, v = 0.55 + r() * 0.3;
    // 왼쪽 면은 L→B 로 내려가고, 오른쪽 면은 B→R 로 올라간다.
    const [ax, ay] = left ? L : B;
    const x = ax + 64 * u, y = ay + (left ? 32 : -32) * u + d * v;
    p.path(ell(x, y, 2.2, 1.4), left ? '#d9a54f' : '#b97c34', 'opacity="0.8"');
  }
  // 바닥 가장자리 어둡게
  p.path(poly([down(L), down(B), down(B, d - 3), down(L, d - 3)]), '#000', 'opacity="0.10"');
  p.path(poly([down(B), down(R), down(R, d - 3), down(B, d - 3)]), '#000', 'opacity="0.14"');

  // 윗면: 둥근 마름모 + 왼쪽 위 빛 그라데이션 + 안쪽 볼록 하이라이트
  p.path(top(0, 12), p.lin([[0, topB], [1, topA]], 0, 0, 1, 1));
  p.path(top(5, 10), p.rad([[0, light(topB, 0.35)], [0.7, topB, 0], [1, topB, 0]], 0.35, 0.3, 0.75), 'opacity="0.9"');
  if (grass) {
    for (let i = 0; i < 6; i++) {
      const u = r() - 0.5, v = r() - 0.5;
      const x = T.cx + (u - v) * T.hw * 0.9, y = T.cy + (u + v) * T.hh * 0.9;
      p.path(`M${f(x - 3)} ${f(y + 1)} Q${f(x - 1)} ${f(y - 4)} ${f(x)} ${f(y + 1)} Q${f(x + 1)} ${f(y - 5)} ${f(x + 3)} ${f(y + 1)} Z`, dark(topA, 0.12), 'opacity="0.7"');
    }
  }
  if (pattern) skinPattern(p, pattern, topA, topB, seed);
  // 테마 흩뿌림: 꽃잎·낙엽·조약돌·반짝임
  if (sprinkle) {
    const r2 = rng(seed * 7 + 3);
    for (let i = 0; i < sprinkle.count; i++) {
      const u = r2() - 0.5, v = r2() - 0.5;
      const x = T.cx + (u - v) * T.hw * 0.8, y = T.cy + (u + v) * T.hh * 0.8;
      if (sprinkle.star) p.path(star(x, y, 4.5, 1.4), sprinkle.color, 'opacity="0.9"');
      else p.path(ell(x, y, 3.4, 2.2), sprinkle.color, `opacity="0.85" transform="rotate(${Math.round(r2() * 180)} ${f(x)} ${f(y)})"`);
    }
  }
  // 위쪽 두 변 하이라이트, 아래 두 변 그늘
  p.path(`M${f(T.cx - T.hw + 8)} ${f(T.cy)} L${f(T.cx)} ${f(T.cy - T.hh + 4)} L${f(T.cx + T.hw - 8)} ${f(T.cy)}`, 'none',
    `stroke="${light(topB, 0.6)}" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" opacity="0.8"`);
  p.path(top(0, 12), 'none', `stroke="${dark(band, 0.45)}" stroke-width="1.5" opacity="0.55"`);
  return p.sprite(name);
}

/** 윗면 모양만 흰색으로 — 강조/번쩍임/잠금/보호막 오버레이의 바탕. */
function overlay(name, draw) {
  const p = new Pic(T.w, T.h);
  draw(p);
  return p.sprite(name);
}

export function tiles() {
  return [
    tile('Tiles/tile_grass_a', { topA: '#6cc04a', topB: '#9ee070', band: '#5aa63a', seed: 1 }),
    tile('Tiles/tile_grass_b', { topA: '#b9d24a', topB: '#e1ec7a', band: '#9cb53a', seed: 2 }),
    tile('Tiles/tile_player', { topA: '#2f7fe8', topB: '#6db4ff', band: '#2a6ad0', seed: 3, grass: false }),
    tile('Tiles/tile_ai', { topA: '#e2463f', topB: '#ff8a78', band: '#c43838', seed: 4, grass: false }),

    overlay('Tiles/overlay_glow', (p) => {
      p.path(top(0, 12), p.rad([[0, '#ffffff', 0.55], [1, '#ffffff', 0.95]]));
    }),
    overlay('Tiles/overlay_select', (p) => {
      p.path(top(3, 10), 'none', 'stroke="#ffffff" stroke-width="6" stroke-linejoin="round"');
      p.path(top(3, 10), 'none', 'stroke="#ffe14a" stroke-width="3" stroke-linejoin="round"');
    }),
    overlay('Tiles/overlay_lock', (p) => {
      p.path(top(0, 12), '#1c2033', 'opacity="0.55"');
      // 사슬 무늬 (대각선 줄)
      const clip = p.clip(top(0, 12));
      let lines = '';
      for (let k = -6; k <= 6; k++) lines += `M${T.cx + k * 16 - 40} ${T.cy - 40} l80 80 `;
      p.add(`<path d="${lines}" stroke="#9aa6c8" stroke-width="2.5" stroke-dasharray="6 4" opacity="0.55" clip-path="${clip}"/>`);
      // 자물쇠
      const x = T.cx, y = T.cy + 2;
      p.path(`M${x - 8} ${y - 4} V${y - 10} A8 8 0 0 1 ${x + 8} ${y - 10} V${y - 4}`, 'none', 'stroke="#3a3f55" stroke-width="7" stroke-linecap="round"');
      p.path(`M${x - 8} ${y - 4} V${y - 10} A8 8 0 0 1 ${x + 8} ${y - 10} V${y - 4}`, 'none', 'stroke="#c8d0e8" stroke-width="3.5" stroke-linecap="round"');
      p.path(rounded([[x - 13, y - 6], [x + 13, y - 6], [x + 13, y + 12], [x - 13, y + 12]], 4), p.lin([[0, '#ffd65a'], [1, '#e09a2a']]), 'stroke="#5a3a1a" stroke-width="2.5"');
      p.path(ell(x, y + 2, 3, 3), '#5a3a1a');
    }),
    // 물 방향 화살표: 격자 +X 방향(화면 오른쪽 아래). Unity 에서 flipX/flipY 로 나머지 세 방향을 만든다.
    overlay('Tiles/overlay_arrow', (p) => {
      const g = ([gx, gy]) => [T.cx + (gx - gy) * T.hw, T.cy + (gx + gy) * T.hh];
      const arrow = [[-0.32, -0.08], [0.08, -0.08], [0.08, -0.22], [0.42, 0], [0.08, 0.22], [0.08, 0.08], [-0.32, 0.08]].map(g);
      p.path(rounded(arrow, 3), '#1a3a7a', 'stroke="#1a3a7a" stroke-width="7" stroke-linejoin="round"');
      p.path(rounded(arrow, 3), p.lin([[0, '#ffffff'], [1, '#bfe8ff']]));
    }),
    overlay('Tiles/overlay_shield', (p) => {
      p.path(top(0, 12), p.rad([[0, '#bff6ff', 0.15], [1, '#7fe8ff', 0.6]]));
      p.path(top(4, 10), 'none', 'stroke="#e6fdff" stroke-width="2.5" stroke-dasharray="10 6" opacity="0.9"');
    }),

    ...skins(),
    ...terrain(),
    ...decor(THEMES[0]),
    background(THEMES[0]),
    ...THEMES.slice(1).flatMap((t) => [
      tile(`Tiles/tile_grass_a_${t.id}`, { ...t.a, seed: 1 }),
      tile(`Tiles/tile_grass_b_${t.id}`, { ...t.b, seed: 2 }),
      ...decor(t),
      background(t),
    ]),
  ];
}

// ---------------------------------------------------------------- 보드 테마 (Core ThemeShop 과 id 를 맞춘다)
// 빈 칸 풀밭 두 장(a/b), 장식 색, 바다. 내 땅·상대 땅 타일은 테마와 상관없이 그대로다.
const BASE_DECOR = {
  leaf: ['#9ee26a', '#4fb33a', '#2f8a2a'], leafLine: '#2a5a20', leafHi: '#c8f59a',
  pine: ['#6fd06a', '#2f8a3a'], pineLine: '#22562a',
  bush: ['#a6e878', '#3e9a34'], bushLine: '#2a5a20',
  rock: ['#c9c4bd', '#7d7670'], rockLine: '#4a4440', snow: false,
};
export const THEMES = [
  { id: '', decor: BASE_DECOR, sea: ['#7fd3f5', '#4fb4ea', '#3a8fd6'], wave: '#ffffff' },
  { id: 'blossom',
    a: { topA: '#7fca62', topB: '#ade07e', band: '#64a646', sprinkle: { color: '#ffb6d2', count: 6 } },
    b: { topA: '#a9cf5a', topB: '#d3ea88', band: '#8eb048', sprinkle: { color: '#ffd0e2', count: 5 } },
    decor: { ...BASE_DECOR, leaf: ['#ffe4f0', '#ff9fc6', '#e0679a'], leafLine: '#9a3f68', leafHi: '#fff4fa', bush: ['#ffd2e6', '#e27aa8'], bushLine: '#9a3f68' },
    sea: ['#a6e6f2', '#7fcbeb', '#6aa6e2'], wave: '#ffffff' },
  { id: 'autumn',
    a: { topA: '#c39a34', topB: '#e2c058', band: '#a07c26', sprinkle: { color: '#e0662a', count: 5 } },
    b: { topA: '#d1ae44', topB: '#ecd27a', band: '#ae8c30', sprinkle: { color: '#b8401e', count: 4 } },
    decor: { ...BASE_DECOR, leaf: ['#ffd27a', '#f0902a', '#bf501a'], leafLine: '#7a3410', leafHi: '#ffe8b0', pine: ['#5a9a50', '#2a5a32'], bush: ['#ffb060', '#c0582a'], bushLine: '#7a3410' },
    sea: ['#63bccb', '#3f9ab2', '#2a7192'], wave: '#e8fbff' },
  { id: 'snow',
    a: { topA: '#c9d6e0', topB: '#e4edf4', band: '#a4b8c8', grass: false, sprinkle: { color: '#ffffff', count: 4, star: true } },
    b: { topA: '#d6e0e8', topB: '#f0f5f9', band: '#b0c2d0', grass: false, sprinkle: { color: '#ffffff', count: 3, star: true } },
    decor: { ...BASE_DECOR, leaf: ['#ffffff', '#d6e6f2', '#a2bad2'], leafLine: '#5f7a98', leafHi: '#ffffff', pine: ['#6fb39a', '#2c665a'], pineLine: '#1e4a40', bush: ['#f6f9ff', '#b6c6da'], bushLine: '#5f7a98', snow: true },
    sea: ['#5c8cba', '#3c6c9c', '#2a4a7c'], wave: '#e8f4ff' },
  { id: 'desert',
    a: { topA: '#ddb260', topB: '#f0d08c', band: '#be9244', grass: false, sprinkle: { color: '#b98446', count: 5 } },
    b: { topA: '#e6c070', topB: '#f6dea2', band: '#c8a050', grass: false, sprinkle: { color: '#c89a5a', count: 4 } },
    decor: { ...BASE_DECOR, leaf: ['#cfe07a', '#8eaa3a', '#5a7a2a'], leafLine: '#3e5418', pine: ['#80c474', '#3a7a42'], pineLine: '#24502a', bush: ['#dcc474', '#9a883a'], bushLine: '#5e5220', rock: ['#e6a878', '#a2603a'], rockLine: '#5e3418' },
    sea: ['#ffb27a', '#f07a7e', '#8a5aa8'], wave: '#fff0d2' },
  { id: 'night',
    a: { topA: '#3a7460', topB: '#538f6c', band: '#2c5848', sprinkle: { color: '#fff27a', count: 3, star: true } },
    b: { topA: '#46826a', topB: '#62a07a', band: '#346452', sprinkle: { color: '#fff7a8', count: 2, star: true } },
    decor: { ...BASE_DECOR, leaf: ['#6ab08e', '#2f6a5a', '#1c4638'], leafLine: '#0e2c26', leafHi: '#a8e8c8', pine: ['#40806e', '#1a4840'], pineLine: '#0e2c26', bush: ['#62a284', '#2a5a4a'], bushLine: '#0e2c26', rock: ['#9294aa', '#4c4c64'], rockLine: '#2a2a3c' },
    sea: ['#2c3c7c', '#1c265a', '#10143a'], wave: '#9fb4ff', stars: '#fff6c0' },
  { id: 'sky',
    a: { topA: '#94d8a6', topB: '#c6eebc', band: '#74b88e', sprinkle: { color: '#ffffff', count: 3 } },
    b: { topA: '#a8e0b8', topB: '#d8f6d2', band: '#84c49c', sprinkle: { color: '#fff8d0', count: 3 } },
    decor: { ...BASE_DECOR, leaf: ['#e6fff2', '#a0e8c8', '#58bea0'], leafLine: '#2a7a64', leafHi: '#ffffff', pine: ['#b4e8ff', '#68aee0'], pineLine: '#346c9a', bush: ['#ffffff', '#c6dcf0'], bushLine: '#7a96b4', rock: ['#f2f2ff', '#b0b8d8'], rockLine: '#6a7298' },
    sea: ['#d2f0ff', '#a2daff', '#7cb6f0'], wave: '#ffffff', clouds: true },
  { id: 'golden',
    a: { topA: '#dcb036', topB: '#f2d866', band: '#bc8e26', grass: false, sprinkle: { color: '#fffbe0', count: 4, star: true } },
    b: { topA: '#e6c04a', topB: '#f8e48a', band: '#c69c30', grass: false, sprinkle: { color: '#ffffff', count: 3, star: true } },
    decor: { ...BASE_DECOR, leaf: ['#fff0a0', '#f0c040', '#bf8820'], leafLine: '#7a5210', leafHi: '#fffbe0', pine: ['#f2d462', '#b08a20'], pineLine: '#6a4e10', bush: ['#ffe890', '#cfa030'], bushLine: '#6a4e10', rock: ['#fff4d2', '#c0a060'], rockLine: '#6a5220' },
    sea: ['#6c4cba', '#4a3092', '#2a1a62'], wave: '#d8c8ff', stars: '#ffe27a' },
];

// ---------------------------------------------------------------- 지형 (스테이지 기믹)
function terrain() {
  const out = [];
  const g = ([gx, gy]) => [T.cx + (gx - gy) * T.hw, T.cy + (gx + gy) * T.hh]; // 격자 좌표(-0.5~0.5) → 윗면 픽셀

  // 얼음: 소유 색이 비치도록 반투명 서리 + 반짝이는 금
  out.push(overlay('Tiles/overlay_ice', (p) => {
    p.path(top(0, 12), p.lin([[0, '#ffffff', 0.75], [0.5, '#d8f4ff', 0.35], [1, '#bfe8ff', 0.55]], 0, 0, 1, 1));
    p.path(`M${g([-0.3, -0.1]).join(' ')} L${g([0, 0.05]).join(' ')} L${g([0.12, 0.3]).join(' ')} M${g([0, 0.05]).join(' ')} L${g([0.3, -0.05]).join(' ')}`,
      'none', 'stroke="#ffffff" stroke-width="2.5" stroke-linecap="round" opacity="0.9"');
    p.path(star(...g([-0.2, -0.25]), 7, 2), '#ffffff');
  }));

  // 침묵 지대: 보랏빛 룬 원
  out.push(overlay('Tiles/overlay_silence', (p) => {
    p.path(top(0, 12), '#5a2a8a', 'opacity="0.35"');
    p.path(ell(T.cx, T.cy, 30, 15), 'none', 'stroke="#e0b8ff" stroke-width="3" opacity="0.9"');
    p.path(ell(T.cx, T.cy, 18, 9), 'none', 'stroke="#e0b8ff" stroke-width="2" stroke-dasharray="4 4" opacity="0.9"');
    p.path(`M${T.cx - 8} ${T.cy - 4} L${T.cx + 8} ${T.cy + 4} M${T.cx + 8} ${T.cy - 4} L${T.cx - 8} ${T.cy + 4}`, 'none', 'stroke="#ffffff" stroke-width="3" stroke-linecap="round"');
  }));

  // 컨베이어 벨트: 어두운 벨트 + 격자 +X 방향 갈매기 무늬
  out.push(overlay('Tiles/overlay_conveyor', (p) => {
    p.path(poly([g([-0.5, -0.32]), g([0.5, -0.32]), g([0.5, 0.32]), g([-0.5, 0.32])]), '#3a3f55', 'opacity="0.85"');
    for (const s of [-0.28, 0.02, 0.32]) {
      const chev = [[s - 0.08, -0.2], [s + 0.08, 0], [s - 0.08, 0.2]].map(g);
      p.path(`M${chev.map((q) => q.join(' ')).join(' L')}`, 'none', 'stroke="#ffd23a" stroke-width="4" stroke-linecap="round" stroke-linejoin="round"');
    }
  }));

  // 용암 칸: 검은 바위 옆면 + 끓는 용암 윗면
  {
    const p = new Pic(T.w, T.h);
    const L = [T.cx - T.hw, T.cy], B = [T.cx, T.cy + T.hh], R = [T.cx + T.hw, T.cy];
    const down = ([x, y]) => [x, y + T.depth];
    p.path(rounded([L, B, down(B), down(L)], 6), '#4a2a26');
    p.path(rounded([B, R, down(R), down(B)], 6), '#2e1a18');
    p.path(top(0, 12), p.rad([[0, '#fff08a'], [0.35, '#ff9a2a'], [0.8, '#e2381a'], [1, '#7a1a0c']], 0.45, 0.45, 0.7));
    p.path(`M${g([-0.3, 0.1]).join(' ')} Q${g([-0.1, -0.1]).join(' ')} ${g([0.1, 0.05]).join(' ')} T${g([0.35, -0.2]).join(' ')}`, 'none', 'stroke="#3a1a10" stroke-width="3" opacity="0.6"');
    for (const [x, y, r] of [[-0.15, -0.2, 4], [0.2, 0.15, 3], [0.05, -0.05, 2.5]]) p.path(ell(...g([x, y]), r, r * 0.7), '#fff6c0');
    out.push(p.sprite('Tiles/tile_lava'));
  }

  // 블랙홀 칸: 어두운 테 + 빨려 들어가는 소용돌이
  {
    const p = new Pic(T.w, T.h);
    const L = [T.cx - T.hw, T.cy], B = [T.cx, T.cy + T.hh], R = [T.cx + T.hw, T.cy];
    const down = ([x, y]) => [x, y + T.depth];
    p.path(rounded([L, B, down(B), down(L)], 6), '#2a2240');
    p.path(rounded([B, R, down(R), down(B)], 6), '#1a1430');
    p.path(top(0, 12), '#2a1a4a');
    p.path(ell(T.cx, T.cy, 50, 25), p.rad([[0, '#000000'], [0.6, '#1a0a2a'], [1, '#7a3ac8', 0.4]]));
    for (let k = 0; k < 3; k++)
      p.add(`<path d="M${T.cx - 40} ${T.cy} a40 20 0 0 1 60 -12" fill="none" stroke="#c48aff" stroke-width="3" stroke-linecap="round" opacity="0.8" transform="rotate(${k * 120} ${T.cx} ${T.cy}) "/>`);
    out.push(p.sprite('Tiles/tile_blackhole'));
  }
  return out;
}

// ---------------------------------------------------------------- 장식 (캔버스 64x80, 바닥점 (32, 72))
function decor(theme) {
  const d = theme.decor, tail = theme.id ? `_${theme.id}` : '';
  const shadow = (p, w = 18) => p.path(ell(32, 72, w, w * 0.4), '#1a2a10', 'opacity="0.25"');
  const out = [];

  let p = new Pic(64, 80);
  shadow(p);
  p.path('M28 72 L29 52 L35 52 L36 72 Z', p.lin([[0, '#a0683a'], [1, '#6e4426']], 0, 0, 1, 0), 'stroke="#4a2c18" stroke-width="2"');
  p.path(ell(32, 36, 20, 21), p.rad([[0, d.leaf[0]], [0.7, d.leaf[1]], [1, d.leaf[2]]], 0.38, 0.32, 0.7), `stroke="${d.leafLine}" stroke-width="2.5"`);
  p.path(ell(25, 28, 6, 4.5), d.leafHi, 'opacity="0.8"');
  out.push(p.sprite(`Decor/tree_round${tail}`));

  p = new Pic(64, 80);
  shadow(p, 15);
  p.path('M29 72 L30 60 L34 60 L35 72 Z', '#7a4a2a', 'stroke="#4a2c18" stroke-width="2"');
  for (const [y, w] of [[62, 17], [48, 14], [34, 10]])
    p.path(rounded([[32, y - 22], [32 + w, y], [32 - w, y]], 4), p.lin([[0, d.pine[0]], [1, d.pine[1]]], 0, 0, 1, 1), `stroke="${d.pineLine}" stroke-width="2.5"`);
  if (d.snow) for (const [y, w] of [[62, 17], [48, 14], [34, 10]])
    p.path(rounded([[32, y - 22], [32 + w * 0.45, y - 12], [32 - w * 0.45, y - 12]], 3), '#ffffff', 'opacity="0.95"');
  out.push(p.sprite(`Decor/tree_pine${tail}`));

  p = new Pic(64, 80);
  shadow(p, 17);
  p.path(rounded([[14, 72], [17, 56], [30, 48], [46, 52], [52, 66], [48, 72]], 7), p.lin([[0, d.rock[0]], [1, d.rock[1]]], 0, 0, 1, 1), `stroke="${d.rockLine}" stroke-width="2.5"`);
  p.path('M24 56 Q30 52 38 54', 'none', 'stroke="#eeebe6" stroke-width="3" stroke-linecap="round" opacity="0.8"');
  if (d.snow) p.path(rounded([[19, 57], [30, 49], [46, 53], [48, 58], [32, 56]], 5), '#ffffff', 'opacity="0.95"');
  out.push(p.sprite(`Decor/rock${tail}`));

  p = new Pic(64, 80);
  shadow(p, 16);
  for (const [x, y, r] of [[22, 62, 10], [42, 62, 10], [32, 56, 12]])
    p.path(ell(x, y, r, r * 0.9), p.rad([[0, d.bush[0]], [1, d.bush[1]]], 0.4, 0.3, 0.7), `stroke="${d.bushLine}" stroke-width="2"`);
  out.push(p.sprite(`Decor/bush${tail}`));
  return out;
}

// ---------------------------------------------------------------- 배경: 물결치는 바다 (540x960, 세로)
function background(theme) {
  const p = new Pic(540, 960);
  const [s0, s1, s2] = theme.sea;
  p.add(`<rect width="540" height="960" fill="${p.lin([[0, s0], [0.5, s1], [1, s2]])}"/>`);
  const r = rng(7);
  for (let i = 0; i < 70; i++) {
    const x = r() * 560 - 10, y = r() * 960, w = 18 + r() * 34;
    p.path(`M${f(x)} ${f(y)} q${f(w / 4)} ${-5} ${f(w / 2)} 0 t${f(w / 2)} 0`, 'none', `stroke="${theme.wave}" stroke-width="3" stroke-linecap="round" opacity="${f(0.15 + r() * 0.25)}"`);
  }
  if (theme.stars) for (let i = 0; i < 60; i++) p.path(star(r() * 540, r() * 960, 3 + r() * 4, 1), theme.stars, `opacity="${f(0.5 + r() * 0.5)}"`);
  if (theme.clouds) for (let i = 0; i < 9; i++) {
    const x = r() * 540, y = r() * 960, w = 40 + r() * 40;
    for (const [dx, dy, k] of [[-0.5, 0.1, 0.55], [0, -0.15, 0.75], [0.5, 0.1, 0.55]]) p.path(ell(x + dx * w, y + dy * w, w * k, w * k * 0.6), '#ffffff', 'opacity="0.8"');
  }
  if (!theme.id) return p.sprite('Bg/sea');
  const sp = p.sprite(`Bg/sea_${theme.id}`);
  return { ...sp, w: 270, h: 480, svg: sp.svg.replace('width="540" height="960"', 'width="270" height="480"') };
}

/** 보드 밑 물거품 섬 그림자 (보드 크기 8x8 기준, 1100x620 — 아래로 조금 길게). */
export function boardFoam() {
  const p = new Pic(1100, 620);
  const cx = 550, cy = 290;
  const dia = (hw, hh, dy = 0) => poly([[cx, cy - hh + dy], [cx + hw, cy + dy], [cx, cy + hh + dy], [cx - hw, cy + dy]]);
  p.path(dia(540, 290, 30), '#1f5f9a', `opacity="0.35" filter="${p.blur(14)}"`);
  p.path(dia(530, 280, 8), '#e9fbff', `opacity="0.75" filter="${p.blur(6)}"`);
  return p.sprite('Bg/foam');
}

// ---------------------------------------------------------------- 내 땅 타일 스킨 (Core TileSkins 와 id·색을 맞춘다)
// 재질 × 색 조합마다 한 장: Tiles/skin_<재질>_<색>. 기본 재질의 파랑·빨강은 tile_player·tile_ai 를 그대로 쓴다.
export const SKIN_MATERIALS = ['plain', 'grass', 'wood', 'stone', 'water', 'marble', 'crystal'];
export const SKIN_COLORS = {
  blue: ['#2f7fe8', '#6db4ff'], sky: ['#1fa8d0', '#7fe2f5'], navy: ['#3442b0', '#7f8cf0'], purple: ['#8048d8', '#c09cff'],
  pink: ['#d84f9c', '#ff9fd0'], orange: ['#e8801f', '#ffc070'], white: ['#aab2c6', '#ffffff'], black: ['#2e2e3e', '#6a6a86'],
  red: ['#e2463f', '#ff8a78'],
};

function skins() {
  const out = [];
  for (const m of SKIN_MATERIALS)
    for (const [id, [a, b]] of Object.entries(SKIN_COLORS)) {
      if (m === 'plain' && (id === 'blue' || id === 'red')) continue;
      out.push(tile(`Tiles/skin_${m}_${id}`, { topA: a, topB: b, band: dark(a, 0.08), seed: 3 + (id.length * 7) % 5, grass: false, pattern: m }));
    }
  return out;
}

/** 윗면 재질 무늬 (윗면 마름모 안으로 잘라 그린다). */
function skinPattern(p, kind, topA, topB, seed) {
  if (kind === 'plain') return;
  const clip = `clip-path="${p.clip(top(0, 12))}"`;
  const g = (u, v) => [T.cx + (u - v) * T.hw, T.cy + (u + v) * T.hh]; // 격자 (u, v) ∈ [-0.5, 0.5] → 윗면
  const P = ([x, y]) => `${f(x)} ${f(y)}`;
  const r = rng(seed * 13 + kind.length);
  const ink = dark(topA, 0.35), hi = light(topB, 0.55);
  switch (kind) {
    case 'grass': {
      // 풀잎 다발과 진한 얼룩
      for (let i = 0; i < 4; i++) {
        const [x, y] = g(r() - 0.5, r() - 0.5);
        p.path(ell(x, y, 11, 5), dark(topA, 0.12), `opacity="0.45" ${clip}`);
      }
      for (let i = 0; i < 14; i++) {
        const [x, y] = g((r() - 0.5) * 0.9, (r() - 0.5) * 0.9);
        p.path(`M${f(x - 4)} ${f(y + 2)} Q${f(x - 2)} ${f(y - 6)} ${f(x)} ${f(y + 2)} Q${f(x + 1)} ${f(y - 7)} ${f(x + 4)} ${f(y + 2)} Z`, i % 3 ? dark(topA, 0.2) : hi, 'opacity="0.8"');
      }
      break;
    }
    case 'wood': {
      // 판자: v 방향으로 4장, 이음매와 나뭇결, 못
      for (let k = 1; k < 4; k++) {
        const v = -0.5 + k / 4;
        p.add(`<path d="M${P(g(-0.6, v))} L${P(g(0.6, v))}" stroke="${ink}" stroke-width="2.4" opacity="0.7" ${clip}/>`);
      }
      for (let k = 0; k < 4; k++) {
        const v0 = -0.5 + (k + 0.5) / 4;
        for (let j = 0; j < 2; j++) {
          const v = v0 + (j - 0.5) * 0.09, u0 = -0.5 + r() * 0.3;
          p.add(`<path d="M${P(g(u0, v))} Q${P(g(u0 + 0.25, v + 0.03))} ${P(g(u0 + 0.5, v))}" fill="none" stroke="${dark(topA, 0.2)}" stroke-width="1.3" opacity="0.6" ${clip}/>`);
        }
        const cut = -0.2 + r() * 0.4;
        p.add(`<path d="M${P(g(cut, v0 - 0.125))} L${P(g(cut, v0 + 0.125))}" stroke="${ink}" stroke-width="2" opacity="0.6" ${clip}/>`);
        for (const u of [-0.42, 0.42]) { const [x, y] = g(u, v0); p.path(ell(x, y, 1.8, 1.2), ink, 'opacity="0.8"'); }
      }
      break;
    }
    case 'stone': {
      // 돌길: 둥근 돌 여러 개 (밝은 윗변 + 진한 틈)
      const cells = [[-0.3, -0.3], [0.05, -0.33], [0.33, -0.22], [-0.33, 0.02], [0, 0], [0.32, 0.1], [-0.25, 0.3], [0.1, 0.32], [0.38, 0.38], [-0.42, -0.42 + 0.84]];
      for (const [u, v] of cells) {
        const [x, y] = g(u + (r() - 0.5) * 0.05, v + (r() - 0.5) * 0.05);
        const w = 13 + r() * 5, h = 7 + r() * 2.5;
        p.path(ell(x, y + 1.5, w, h), ink, `opacity="0.55" ${clip}`);
        p.path(ell(x, y, w - 1.5, h - 1.5), r() < 0.5 ? light(topA, 0.12) : topA, clip);
        p.path(ell(x - w * 0.25, y - h * 0.3, w * 0.45, h * 0.35), hi, `opacity="0.55" ${clip}`);
      }
      break;
    }
    case 'water': {
      // 물결: 밝은 물결선 + 반짝임
      for (let k = 0; k < 5; k++) {
        const v = -0.42 + k * 0.21;
        let d = `M${P(g(-0.6, v))}`;
        for (let u = -0.6; u < 0.6; u += 0.2) d += ` Q${P(g(u + 0.05, v - 0.06))} ${P(g(u + 0.1, v))} T${P(g(u + 0.2, v))}`;
        p.add(`<path d="${d}" fill="none" stroke="${hi}" stroke-width="2.4" stroke-linecap="round" opacity="${k % 2 ? 0.55 : 0.85}" ${clip}/>`);
      }
      for (let i = 0; i < 3; i++) { const [x, y] = g((r() - 0.5) * 0.7, (r() - 0.5) * 0.7); p.path(star(x, y, 4, 1.2), '#ffffff', 'opacity="0.9"'); }
      break;
    }
    case 'marble': {
      // 대리석: 밝게 덮고 가는 결
      p.path(top(0, 12), light(topB, 0.35), 'opacity="0.45"');
      for (let k = 0; k < 4; k++) {
        const a = g(-0.6, (r() - 0.5) * 0.9), b = g(0.6, (r() - 0.5) * 0.9);
        const c1 = g(-0.2, (r() - 0.5)), c2 = g(0.2, (r() - 0.5));
        p.add(`<path d="M${P(a)} C${P(c1)} ${P(c2)} ${P(b)}" fill="none" stroke="${k % 2 ? ink : '#ffffff'}" stroke-width="${k % 2 ? 1.3 : 2}" opacity="0.55" ${clip}/>`);
      }
      p.path(ell(T.cx - 22, T.cy - 8, 18, 6), '#ffffff', `opacity="0.35" ${clip}`);
      break;
    }
    case 'crystal': {
      // 크리스탈: 가운데에서 꼭짓점·변 중점으로 면을 나누고 번갈아 밝게
      const c = [T.cx, T.cy];
      const rim = [g(-0.5, -0.5), g(0, -0.5), g(0.5, -0.5), g(0.5, 0), g(0.5, 0.5), g(0, 0.5), g(-0.5, 0.5), g(-0.5, 0)];
      for (let i = 0; i < rim.length; i++) {
        const a = rim[i], b = rim[(i + 1) % rim.length];
        p.add(`<path d="M${P(c)} L${P(a)} L${P(b)} Z" fill="${i % 2 ? '#ffffff' : dark(topA, 0.15)}" opacity="${i % 2 ? 0.3 : 0.25}" ${clip}/>`);
        p.add(`<path d="M${P(c)} L${P(a)}" stroke="#ffffff" stroke-width="1.4" opacity="0.7" ${clip}/>`);
      }
      p.path(star(T.cx - 14, T.cy - 6, 6, 1.6), '#ffffff', 'opacity="0.95"');
      p.path(star(T.cx + 20, T.cy + 6, 4, 1.2), '#ffffff', 'opacity="0.8"');
      break;
    }
  }
}
