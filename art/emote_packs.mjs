// 상점에서 파는 이모티콘 팩 3종 (UI/emote_8 ~ 31, 160x160). 번호 = 팩 × 8 + 칸. 기본 팩(0~7)은 emotes.mjs.
// 순서와 문구는 Core EmotePacks 와 맞춘다.
//   킹받는 팩  8 메롱~ 9 어쩔티비 10 깐족깐족 11 ㅋ 12 응 아니야 13 하암~ 14 짝짝짝 15 그걸 놓네?
//   귀요미 팩 16 부끄 17 반짝반짝 18 쪽 19 쿨쿨 20 꽃 받아 21 브이 22 사랑해 23 화이팅
//   멘붕 팩   24 멘붕 25 영혼 탈출 26 눈물바다 27 폭발 28 해탈 29 흰자 30 띠용 31 털썩
import { Pic, ell, poly, star, stroke, f } from './kit.mjs';
import { bolt } from './units.mjs';

const S = 160;
const INK = '#20304a';

function sticker(p, d) {
  p.path(d, '#000000', 'opacity="0.22" transform="translate(3 6)"');
  p.path(d, '#ffffff', 'stroke="#ffffff" stroke-width="14" stroke-linejoin="round"');
}
const line = (p, d, w = 4, c = INK) => p.path(d, 'none', `stroke="${c}" stroke-width="${w}" stroke-linecap="round" stroke-linejoin="round"`);
const heart = (cx, cy, s) =>
  `M${f(cx)} ${f(cy + 8 * s)} C${f(cx - 12 * s)} ${f(cy)} ${f(cx - 10 * s)} ${f(cy - 10 * s)} ${f(cx - 4 * s)} ${f(cy - 9 * s)} C${f(cx - 1 * s)} ${f(cy - 9 * s)} ${f(cx)} ${f(cy - 6 * s)} ${f(cx)} ${f(cy - 5 * s)} C${f(cx)} ${f(cy - 6 * s)} ${f(cx + 1 * s)} ${f(cy - 9 * s)} ${f(cx + 4 * s)} ${f(cy - 9 * s)} C${f(cx + 10 * s)} ${f(cy - 10 * s)} ${f(cx + 12 * s)} ${f(cy)} ${f(cx)} ${f(cy + 8 * s)} Z`;

// ---------------------------------------------------------------- 몸통 (원소 캐릭터). 얼굴 중심을 돌려준다.
const BODIES = {
  drop: (p, tint) => {
    const d = 'M80 24 C92 46 120 68 120 98 C120 126 102 142 80 142 C58 142 40 126 40 98 C40 68 68 46 80 24 Z';
    sticker(p, d);
    const g = tint === 'grey' ? [['#eceaf0'], ['#a9a4b0'], ['#77727f']] : [['#bfe8ff'], ['#45a8ff'], ['#1f6fd8']];
    p.path(d, p.rad([[0, g[0][0]], [0.55, g[1][0]], [1, g[2][0]]], 0.38, 0.45, 0.75), stroke(4, tint === 'grey' ? '#4a4652' : '#1a4a9a'));
    line(p, 'M58 78 Q60 60 74 48', 7, '#ffffff');
    return [80, 104];
  },
  flame: (p) => {
    const d = 'M80 146 C40 146 34 104 52 84 C54 96 62 98 64 92 C58 70 70 46 92 26 C88 52 104 58 110 76 C116 66 116 58 112 50 C134 74 134 146 80 146 Z';
    sticker(p, d);
    p.path(d, p.lin([[0, '#ffb04a'], [1, '#ff3a1a']]), stroke(4, '#7a1a08'));
    p.path('M80 140 C60 140 56 116 66 104 C70 112 78 110 78 104 C76 92 84 80 94 72 C94 92 104 98 104 110 C110 128 100 140 80 140 Z', '#ffe36a', 'opacity="0.85"');
    return [80, 112];
  },
  bolt: (p) => {
    const d = bolt(80, 78, 2.7);
    sticker(p, d);
    p.path(d, p.lin([[0, '#fff6a0'], [0.5, '#ffd21f'], [1, '#f39a00']]), stroke(4, '#8a4a00'));
    return [78, 76];
  },
  tornado: (p) => {
    const d = 'M36 34 Q80 18 124 34 Q116 62 104 78 Q96 100 92 118 Q88 136 80 148 Q76 134 72 118 Q64 96 58 78 Q42 58 36 34 Z';
    sticker(p, d);
    p.path(d, p.lin([[0, '#d6ffb0'], [1, '#3fbf5a']], 0, 0, 1, 1), stroke(4, '#1f6a34'));
    return [80, 62];
  },
  crystal: (p) => {
    const d = poly([[80, 18], [114, 64], [102, 146], [58, 146], [46, 64]]);
    sticker(p, d);
    p.path(d, p.lin([[0, '#ffffff'], [0.6, '#e8f2ff'], [1, '#b8c8f0']], 0, 0, 1, 1), stroke(4, '#6a7aa8'));
    p.path(poly([[80, 18], [80, 146], [58, 146], [46, 64]]), '#ffffff', 'opacity="0.5"');
    return [80, 92];
  },
  cloud: (p) => {
    const d = 'M28 118 C10 118 10 86 34 86 C30 56 66 48 78 66 C88 42 130 52 122 86 C146 86 144 120 120 120 Z';
    sticker(p, d);
    p.path(d, p.rad([[0, '#d6b6ff'], [0.6, '#9a5ae6'], [1, '#5a2aa6']], 0.4, 0.35, 0.8), stroke(4, '#3a1a6a'));
    line(p, 'M36 86 Q42 74 54 78', 5, '#f0e2ff');
    return [78, 98];
  },
  orb: (p) => {
    const d = ell(80, 90, 52, 52);
    sticker(p, d);
    p.path(d, p.rad([[0, '#3a1a5a'], [0.75, '#160a26'], [1, '#3a1a5a']], 0.5, 0.5, 0.5), stroke(4, '#c48aff'));
    p.path(ell(62, 66, 10, 6), '#e6c8ff', 'opacity="0.5"');
    return [80, 90];
  },
};
const dark = (kind) => kind === 'orb'; // 어두운 몸통은 얼굴을 밝게

// ---------------------------------------------------------------- 얼굴 조각
function eyes(p, kind, cx, cy, type, s = 1) {
  const c = dark(kind) ? '#ffe36a' : INK;
  for (const [i, dx] of [[0, -13], [1, 13]].map(([i, d]) => [i, d * s])) {
    const x = cx + dx;
    switch (type) {
      case 'normal':
        p.path(ell(x, cy, 5.5 * s, 7 * s), c);
        if (!dark(kind)) p.path(ell(x - 1.8 * s, cy - 2.5 * s, 2 * s, 2.4 * s), '#ffffff');
        break;
      case 'happy': line(p, `M${x - 7 * s} ${cy + 2 * s} Q${x} ${cy - 7 * s} ${x + 7 * s} ${cy + 2 * s}`, 4.5 * s, c); break;
      case 'sad': line(p, `M${x - 7 * s} ${cy - 2 * s} Q${x} ${cy + 6 * s} ${x + 7 * s} ${cy - 2 * s}`, 4.5 * s, c); break;
      case 'smug': // 반쯤 감은 눈 (위 눈꺼풀이 평평)
        p.path(`M${x - 7 * s} ${cy - 1 * s} H${x + 7 * s} Q${x + 7 * s} ${cy + 7 * s} ${x} ${cy + 7 * s} Q${x - 7 * s} ${cy + 7 * s} ${x - 7 * s} ${cy - 1 * s} Z`, c);
        line(p, `M${x - 9 * s} ${cy - 2 * s} H${x + 9 * s}`, 3.5 * s, c);
        break;
      case 'side': // 곁눈질 (흰자 + 한쪽으로 몰린 눈동자)
        p.path(ell(x, cy, 7 * s, 6 * s), '#ffffff', stroke(2 * s, INK));
        p.path(ell(x + 3.5 * s, cy + 1 * s, 3 * s, 3.5 * s), INK);
        break;
      case 'wink': if (i === 0) line(p, `M${x - 7 * s} ${cy} Q${x} ${cy - 6 * s} ${x + 7 * s} ${cy}`, 4.5 * s, c); else { p.path(ell(x, cy, 5.5 * s, 7 * s), c); if (!dark(kind)) p.path(ell(x - 1.8 * s, cy - 2.5 * s, 2 * s, 2.4 * s), '#ffffff'); } break;
      case 'white': p.path(ell(x, cy, 7 * s, 8 * s), '#ffffff', stroke(2.5 * s, INK)); break;
      case 'up': p.path(ell(x, cy, 7 * s, 8 * s), '#ffffff', stroke(2.5 * s, INK)); p.path(ell(x, cy - 5 * s, 3.5 * s, 2.5 * s), INK); break;
      case 'spiral': line(p, `M${x} ${cy} m-1 0 a1.5 1.5 0 1 1 3 0 a3.5 3.5 0 1 1 -7 0 a5.5 5.5 0 1 1 11 0`, 2.4 * s, c); break;
      case 'x': line(p, `M${x - 5 * s} ${cy - 5 * s} L${x + 5 * s} ${cy + 5 * s} M${x + 5 * s} ${cy - 5 * s} L${x - 5 * s} ${cy + 5 * s}`, 4 * s, c); break;
      case 'star': p.path(star(x, cy, 9 * s, 3.6 * s, 5), '#ffc81f', stroke(1.5, '#8a5a00')); break;
      case 'line': line(p, `M${x - 7 * s} ${cy} H${x + 7 * s}`, 4 * s, c); break;
    }
  }
}
function mouth(p, kind, cx, cy, type, s = 1) {
  const c = dark(kind) ? '#ffe36a' : INK;
  switch (type) {
    case 'smile': line(p, `M${cx - 9 * s} ${cy} Q${cx} ${cy + 9 * s} ${cx + 9 * s} ${cy}`, 4 * s, c); break;
    case 'smirk': line(p, `M${cx - 9 * s} ${cy + 3 * s} Q${cx + 2 * s} ${cy + 6 * s} ${cx + 11 * s} ${cy - 4 * s}`, 4 * s, c); break;
    case 'flat': line(p, `M${cx - 8 * s} ${cy} H${cx + 8 * s}`, 4 * s, c); break;
    case 'wavy': line(p, `M${cx - 12 * s} ${cy} q3 -4 6 0 t6 0 t6 0 t6 0`, 3.5 * s, c); break;
    case 'o': p.path(ell(cx, cy, 5 * s, 6 * s), c); break;
    case 'tongue':
      line(p, `M${cx - 10 * s} ${cy} Q${cx} ${cy + 6 * s} ${cx + 10 * s} ${cy}`, 4 * s, c);
      p.path(`M${cx - 3 * s} ${cy + 3 * s} Q${cx - 4 * s} ${cy + 16 * s} ${cx + 3 * s} ${cy + 16 * s} Q${cx + 9 * s} ${cy + 15 * s} ${cx + 7 * s} ${cy + 3 * s} Z`, '#ff6a8a', stroke(2.5 * s, '#8a1030'));
      break;
    case 'laugh': p.path(`M${cx - 15 * s} ${cy - 2 * s} Q${cx} ${cy + 22 * s} ${cx + 15 * s} ${cy - 2 * s} Z`, '#7a1a2a', stroke(3 * s, INK)); p.path(`M${cx - 8 * s} ${cy + 8 * s} Q${cx} ${cy + 15 * s} ${cx + 8 * s} ${cy + 8 * s} Q${cx} ${cy + 10 * s} ${cx - 8 * s} ${cy + 8 * s} Z`, '#ff8a9a'); break;
    case 'yawn': p.path(ell(cx, cy + 4 * s, 9 * s, 13 * s), '#7a1a2a', stroke(3 * s, INK)); p.path(ell(cx, cy + 11 * s, 5 * s, 4 * s), '#ff8a9a'); break;
    case 'kiss': line(p, `M${cx - 2 * s} ${cy - 5 * s} Q${cx + 6 * s} ${cy - 3 * s} ${cx} ${cy} Q${cx + 6 * s} ${cy + 3 * s} ${cx - 2 * s} ${cy + 5 * s}`, 3.5 * s, c); break;
    case 'grit': p.path(`M${cx - 12 * s} ${cy - 4 * s} H${cx + 12 * s} V${cy + 6 * s} H${cx - 12 * s} Z`, '#ffffff', stroke(3 * s, INK)); line(p, `M${cx - 4 * s} ${cy - 4 * s} V${cy + 6 * s} M${cx + 4 * s} ${cy - 4 * s} V${cy + 6 * s}`, 2 * s, INK); break;
  }
}
const blush = (p, cx, cy, s = 1, a = 0.6) => {
  p.path(ell(cx - 24 * s, cy, 6 * s, 3.5 * s), '#ff7fa8', `opacity="${a}"`);
  p.path(ell(cx + 24 * s, cy, 6 * s, 3.5 * s), '#ff7fa8', `opacity="${a}"`);
};
const sweat = (p, x, y, s = 1) => p.path(`M${x} ${y - 10 * s} C${x + 6 * s} ${y} ${x + 6 * s} ${y + 6 * s} ${x} ${y + 7 * s} C${x - 6 * s} ${y + 6 * s} ${x - 6 * s} ${y} ${x} ${y - 10 * s} Z`, '#9fe0ff', stroke(2, '#2a6ad0'));
const sparkles = (p, pts) => pts.forEach(([x, y, r]) => p.path(star(x, y, r, r * 0.32), '#fff36a', stroke(2, '#ffffff')));
const glove = (p, x, y, rot, s = 1) => {
  // 하얀 장갑 손 (주먹 + 검지): 손가락이 위를 향하게 그린 뒤 rot 만큼 돌린다
  p.add(`<g transform="translate(${x} ${y}) rotate(${rot}) scale(${s})">
    <path d="M-9 0 Q-11 -10 -6 -12 L-3 -12 L-3 -30 Q0 -34 3 -30 L3 -12 L8 -12 Q12 -10 11 0 Q10 10 0 11 Q-9 10 -9 0 Z" fill="#ffffff" ${stroke(3, '#2a2f45')}/>
    <path d="M-6 -4 H8 M-6 2 H8" stroke="#9aa2bb" stroke-width="2" stroke-linecap="round"/></g>`);
};
const hand = (p, x, y, s = 1) => p.path(ell(x, y, 9 * s, 8 * s), '#ffffff', stroke(3, '#2a2f45'));

/** 이모티콘 한 장: 몸통 + 얼굴 + 꾸밈 */
function make(name, kind, face, extra) {
  const p = new Pic(S, S);
  if (face.behind) face.behind(p);
  const [cx, cy] = BODIES[kind](p, face.tint);
  if (face.before) face.before(p, cx, cy);
  eyes(p, kind, cx, cy, face.eyes, face.s || 1);
  if (face.mouth) mouth(p, kind, cx, cy + (face.my ?? 18), face.mouth, face.s || 1);
  if (face.blush) blush(p, cx, cy + 12, face.s || 1, face.blush);
  if (extra) extra(p, cx, cy);
  return p.sprite(`UI/emote_${name}`);
}

export function emotePacks() {
  return [
    // ---------------- 킹받는 팩
    make(8, 'drop', { eyes: 'wink', mouth: 'tongue', blush: 0.5 }, (p) => {
      glove(p, 30, 112, -60, 0.9); // 아래 눈꺼풀 당기기 (메롱)
    }),
    make(9, 'flame', { eyes: 'smug', mouth: 'smirk' }, (p) => {
      // 어깨 으쓱: 양옆으로 벌린 손
      hand(p, 24, 98); hand(p, 136, 98);
      line(p, 'M30 92 Q22 80 18 86 M130 92 Q138 80 142 86', 3, '#ffffff');
    }),
    make(10, 'bolt', { eyes: 'side', mouth: 'smirk', s: 0.75, my: 13 }, (p) => {
      sweat(p, 112, 40);
      line(p, 'M120 70 l14 -4 M122 82 l14 2', 4, '#ffd21f');
    }),
    make(11, 'drop', { tint: 'grey', eyes: 'line', mouth: 'smirk' }, (p, cx, cy) => {
      // 선글라스
      p.path(`M${cx - 30} ${cy - 8} H${cx + 30} L${cx + 26} ${cy + 6} Q${cx + 16} ${cy + 12} ${cx + 6} ${cy + 4} L${cx} ${cy - 2} L${cx - 6} ${cy + 4} Q${cx - 16} ${cy + 12} ${cx - 26} ${cy + 6} Z`, '#14141e', stroke(2.5, '#000000'));
      p.path(`M${cx - 22} ${cy - 4} L${cx - 14} ${cy - 4}`, 'none', 'stroke="#ffffff" stroke-width="3" stroke-linecap="round" opacity="0.7"');
      sparkles(p, [[130, 40, 10]]);
    }),
    make(12, 'crystal', { eyes: 'happy', mouth: 'smirk' }, (p) => {
      glove(p, 130, 92, 15);
      line(p, 'M116 46 q6 -8 12 0 M136 44 q6 -8 12 0', 3, '#8aa0d8');
    }),
    make(13, 'cloud', { eyes: 'happy', mouth: 'yawn', my: 14 }, (p) => {
      p.path(ell(58, 96, 4, 6), '#9fe0ff', 'opacity="0.9"');
      for (const [x, y, sz] of [[124, 44, 14], [140, 26, 10]]) p.add(`<text x="${x}" y="${y}" font-size="${sz * 1.6}" font-weight="900" font-family="Arial" fill="#ffffff" stroke="#3a1a6a" stroke-width="3" paint-order="stroke">Z</text>`);
    }),
    make(14, 'tornado', { eyes: 'smug', mouth: 'smirk', my: 16 }, (p) => {
      hand(p, 70, 102); hand(p, 90, 102);
      line(p, 'M60 88 l-8 -8 M100 88 l8 -8 M80 84 v-8', 3.5, '#ffd21f');
    }),
    make(15, 'orb', { eyes: 'happy', mouth: 'laugh', my: 14 }, (p) => {
      glove(p, 140, 116, 70, 0.9);
      p.path('M50 86 C46 96 46 100 50 102 C54 100 54 96 50 86 Z', '#9fe0ff');
      line(p, 'M18 40 l10 8 M14 62 h12', 4, '#ffd84a');
    }),

    // ---------------- 귀요미 팩
    make(16, 'drop', { eyes: 'happy', mouth: 'smile', blush: 0.95 }, (p) => {
      line(p, 'M36 120 l-6 4 M38 112 l-8 0 M124 120 l6 4 M122 112 l8 0', 3, '#ff7fa8');
      p.path(heart(128, 50, 0.8), '#ff7fa8', stroke(2, '#ffffff'));
    }),
    make(17, 'flame', { eyes: 'star', mouth: 'smile' }, (p) => {
      sparkles(p, [[24, 40, 12], [140, 46, 10], [134, 118, 9], [24, 110, 8]]);
    }),
    make(18, 'drop', { eyes: 'wink', mouth: 'kiss', blush: 0.8 }, (p) => {
      for (const [x, y, s] of [[124, 60, 1.2], [140, 36, 0.8]]) p.path(heart(x, y, s), '#ff3a6a', stroke(2.5, '#ffffff'));
    }),
    make(19, 'cloud', { eyes: 'sad', mouth: 'o', s: 0.9 }, (p, cx, cy) => {
      p.path(ell(cx + 14, cy + 26, 9, 9), '#bfeaff', 'opacity="0.8" ' + stroke(2, '#5a8ad8'));
      for (const [x, y, sz] of [[118, 54, 12], [132, 38, 15], [146, 20, 18]]) p.add(`<text x="${x}" y="${y}" font-size="${sz}" font-weight="900" font-family="Arial" fill="#ffffff" stroke="#3a1a6a" stroke-width="3" paint-order="stroke">z</text>`);
    }),
    make(20, 'tornado', { eyes: 'happy', mouth: 'smile', blush: 0.7, my: 16 }, (p) => {
      line(p, 'M120 120 Q118 100 124 88', 4, '#3a8a2a');
      for (let i = 0; i < 5; i++) p.add(`<path d="${ell(124, 76, 6, 9)}" fill="#ff9fc6" ${stroke(1.5, '#b0466e')} transform="rotate(${i * 72} 124 84)"/>`);
      p.path(ell(124, 84, 4, 4), '#ffd84a');
      hand(p, 114, 108, 0.8);
    }),
    make(21, 'bolt', { eyes: 'wink', mouth: 'smile', s: 0.75, my: 13 }, (p) => {
      p.add(`<g transform="translate(126 70) rotate(15)"><path d="M-10 4 L-12 -22 Q-8 -26 -5 -22 L-2 -4 L2 -24 Q6 -27 9 -23 L6 4 Q8 14 -2 15 Q-11 14 -10 4 Z" fill="#ffffff" ${stroke(3, '#2a2f45')}/></g>`);
      sparkles(p, [[30, 30, 9]]);
    }),
    make(22, 'orb', { eyes: 'happy', mouth: 'smile', my: 14 }, (p) => {
      p.path(heart(80, 136, 2.2), '#ff3a6a', stroke(3, '#ffffff'));
      p.path(heart(80, 136, 2.2), 'none', stroke(2, '#8a1030'));
      for (const s of [-1, 1]) p.path(poly([[80 + s * 34, 52], [80 + s * 30, 30], [80 + s * 14, 42]]), '#160a26', stroke(3, '#c48aff'));
    }),
    make(23, 'flame', { eyes: 'normal', mouth: 'grit' }, (p, cx, cy) => {
      p.path(`M${cx - 32} ${cy - 22} Q${cx} ${cy - 30} ${cx + 32} ${cy - 22} L${cx + 32} ${cy - 14} Q${cx} ${cy - 22} ${cx - 32} ${cy - 14} Z`, '#ffffff', stroke(2.5, '#c82a2a'));
      p.path(ell(cx, cy - 20, 5, 5), '#e2382a');
      p.add(`<path d="M${cx + 32} ${cy - 18} l14 -6 l-4 10 Z" fill="#ffffff" ${stroke(2, '#c82a2a')}/>`);
      hand(p, 132, 70, 1.1);
      line(p, 'M126 52 l-4 -8 M136 50 l2 -9 M146 56 l6 -6', 3, '#ffd21f');
    }),

    // ---------------- 멘붕 팩
    make(24, 'drop', { eyes: 'spiral', mouth: 'wavy' }, (p) => {
      p.path('M30 140 Q40 128 56 134 Q70 124 86 134 Q104 126 116 136 Q132 132 132 142 Q80 152 30 140 Z', '#45a8ff', stroke(3, '#1a4a9a'));
      sweat(p, 124, 54);
    }),
    make(25, 'drop', { tint: 'grey', eyes: 'white', mouth: 'o' }, (p) => {
      // 머리 위로 빠져나가는 영혼
      const soul = 'M100 6 C116 6 124 18 120 30 C118 40 124 46 116 50 C110 46 108 52 100 50 C92 52 90 46 84 50 C78 44 82 38 80 30 C78 18 86 6 100 6 Z';
      sticker(p, soul);
      p.path(soul, '#ffffff', 'opacity="0.95" ' + stroke(2.5, '#9aa8c8'));
      for (const dx of [-6, 6]) p.path(ell(100 + dx, 24, 2, 2.5), INK);
      line(p, 'M96 34 Q100 37 104 34', 2, INK);
      line(p, 'M86 54 Q82 60 84 66', 3, '#c8d4ee');
    }),
    make(26, 'cloud', { eyes: 'sad', mouth: 'wavy' }, (p, cx, cy) => {
      for (const dx of [-13, 13]) line(p, `M${cx + dx} ${cy + 6} Q${cx + dx - 3} ${cy + 26} ${cx + dx + (dx < 0 ? -8 : 8)} ${cy + 44}`, 8, '#bfefff');
      p.path('M20 146 Q50 136 80 144 Q110 136 140 146 Q80 156 20 146 Z', '#7fd3ff', stroke(2.5, '#2a6ad0'));
    }),
    make(27, 'flame', {
      eyes: 'x', mouth: 'grit', behind: (p) => {
        p.path(star(80, 84, 78, 40, 12), '#ffe36a', 'opacity="0.95" ' + stroke(3, '#c85a10'));
      },
    }, (p) => {
      line(p, 'M56 90 L72 98 M104 90 L88 98', 5, '#3a0a04');
    }),
    make(28, 'crystal', { eyes: 'happy', mouth: 'smile' }, (p) => {
      p.path(ell(80, 14, 24, 7), 'none', 'stroke="#ffd84a" stroke-width="5" opacity="0.95"');
      p.path(ell(80, 90, 74, 74), p.rad([[0, '#fff6c8', 0], [0.7, '#fff6c8', 0], [1, '#fff6c8', 0.3]]));
      line(p, 'M30 120 q10 -10 20 0 M110 120 q10 -10 20 0', 3, '#b8c8f0');
    }),
    make(29, 'bolt', { eyes: 'up', mouth: 'flat', s: 0.8, my: 14 }, (p) => {
      line(p, 'M118 40 q8 6 16 0', 3, '#8a4a00');
      p.add(`<text x="112" y="132" font-size="26" font-weight="900" font-family="Arial" fill="#ffffff" stroke="#8a4a00" stroke-width="3" paint-order="stroke">...</text>`);
    }),
    make(30, 'tornado', { eyes: 'spiral', mouth: 'wavy', my: 16 }, (p) => {
      p.path(ell(80, 22, 34, 9), 'none', 'stroke="#ffd84a" stroke-width="2.5" stroke-dasharray="6 6"');
      sparkles(p, [[48, 20, 8], [112, 24, 8], [80, 12, 7]]);
    }),
    make(31, 'orb', { eyes: 'x', mouth: 'flat', my: 14 }, (p) => {
      // 한숨 연기
      p.path(`${ell(124, 128, 10, 7)} ${ell(136, 118, 8, 6)} ${ell(144, 106, 6, 5)}`, '#ffffff', 'stroke="#c8ccd8" stroke-width="2"');
      p.path(ell(80, 146, 40, 6), '#000000', 'opacity="0.18"');
    }),
  ];
}
