// PvP 이모티콘 8종 (UI/emote_0 ~ 7, 160x160). 원소 캐릭터의 표정으로 만든 스티커: 흰 테두리 + 그림자.
// 순서와 문구는 Client/Hud.Online.cs 의 EmoteCaptions 와 맞춘다.
//   0 좋아요(하트 물방울) 1 화난다(불꽃) 2 헉(번개) 3 흑흑(우는 물방울) 4 ㅋㅋㅋ(웃는 회오리)
//   5 잘했어(반짝 수정) 6 음…(생각하는 구름) 7 GG(백기 든 석상)
import { Pic, ell, rounded, poly, star, dark, light, stroke, f } from './kit.mjs';
import { flame, bolt } from './units.mjs';

const S = 160;

/** 스티커 바탕: 스티커처럼 두꺼운 흰 테두리를 얹을 모양을 먼저 그린다 (흰색, 그림자). */
function sticker(p, d) {
  p.path(d, '#000000', 'opacity="0.22" transform="translate(3 6)"');
  p.path(d, '#ffffff', 'stroke="#ffffff" stroke-width="14" stroke-linejoin="round"');
}

const eyes = (p, cx, cy, s = 1) => {
  for (const dx of [-12, 12]) {
    p.path(ell(cx + dx * s, cy, 5.5 * s, 7 * s), '#20304a');
    p.path(ell(cx + dx * s - 1.8 * s, cy - 2.5 * s, 2 * s, 2.4 * s), '#ffffff');
  }
};
const blush = (p, cx, cy, s = 1) => {
  p.path(ell(cx - 22 * s, cy, 5 * s, 3 * s), '#ff8fb0', 'opacity="0.6"');
  p.path(ell(cx + 22 * s, cy, 5 * s, 3 * s), '#ff8fb0', 'opacity="0.6"');
};
const heart = (cx, cy, s) =>
  `M${f(cx)} ${f(cy + 8 * s)} C${f(cx - 12 * s)} ${f(cy)} ${f(cx - 10 * s)} ${f(cy - 10 * s)} ${f(cx - 4 * s)} ${f(cy - 9 * s)} C${f(cx - 1 * s)} ${f(cy - 9 * s)} ${f(cx)} ${f(cy - 6 * s)} ${f(cx)} ${f(cy - 5 * s)} C${f(cx)} ${f(cy - 6 * s)} ${f(cx + 1 * s)} ${f(cy - 9 * s)} ${f(cx + 4 * s)} ${f(cy - 9 * s)} C${f(cx + 10 * s)} ${f(cy - 10 * s)} ${f(cx + 12 * s)} ${f(cy)} ${f(cx)} ${f(cy + 8 * s)} Z`;

const DROP = 'M80 22 C92 44 120 66 120 96 C120 124 102 140 80 140 C58 140 40 124 40 96 C40 66 68 44 80 22 Z';
const dropFill = (p) => p.rad([[0, '#bfe8ff'], [0.55, '#45a8ff'], [1, '#1f6fd8']], 0.38, 0.45, 0.75);

export function emotes() {
  const out = [];
  let p;

  // 0 좋아요: 하트 눈 물방울 + 떠오르는 하트
  p = new Pic(S, S);
  sticker(p, DROP);
  p.path(DROP, dropFill(p), stroke(4, '#1a4a9a'));
  p.path('M58 76 Q60 58 74 46', 'none', 'stroke="#ffffff" stroke-width="7" stroke-linecap="round" opacity="0.85"');
  for (const dx of [-14, 14]) p.path(heart(80 + dx, 100, 0.85), '#ff3a6a', stroke(2, '#8a1030'));
  p.path('M70 118 Q80 128 90 118', 'none', 'stroke="#20304a" stroke-width="3.5" stroke-linecap="round"');
  blush(p, 80, 114);
  for (const [x, y, s] of [[128, 40, 1.2], [140, 72, 0.8], [30, 46, 0.9]]) p.path(heart(x, y, s), '#ff5a8a', stroke(2.5, '#ffffff'));
  out.push(p.sprite('UI/emote_0'));

  // 1 화난다: 찡그린 불꽃 + 김
  p = new Pic(S, S);
  const fl = `M80 146 C40 146 34 104 52 84 C54 96 62 98 64 92 C58 70 70 46 92 26 C88 52 104 58 110 76 C116 66 116 58 112 50 C134 74 134 146 80 146 Z`;
  sticker(p, fl);
  p.path(fl, p.lin([[0, '#ffb04a'], [1, '#ff3a1a']]), stroke(4, '#7a1a08'));
  p.path('M80 140 C60 140 56 116 66 104 C70 112 78 110 78 104 C76 92 84 80 94 72 C94 92 104 98 104 110 C110 128 100 140 80 140 Z', '#ffe36a', 'opacity="0.9"');
  p.path('M58 92 L74 100 M102 92 L86 100', 'none', 'stroke="#3a0a04" stroke-width="5" stroke-linecap="round"');
  for (const dx of [-12, 12]) p.path(ell(80 + dx, 108, 4.5, 5.5), '#3a0a04');
  p.path('M70 128 Q80 120 90 128', 'none', 'stroke="#3a0a04" stroke-width="4" stroke-linecap="round"');
  for (const [x, y] of [[28, 50], [132, 40]]) {
    p.path(`${ell(x, y, 11, 8)} ${ell(x + 8, y - 8, 9, 7)} ${ell(x - 7, y - 9, 7, 6)}`, '#ffffff', 'stroke="#c8ccd8" stroke-width="2.5"');
  }
  p.path('M114 24 l8 -6 M122 30 l10 -2 M118 16 l2 -10', 'none', 'stroke="#ff3a1a" stroke-width="4" stroke-linecap="round"');
  out.push(p.sprite('UI/emote_1'));

  // 2 헉: 번개가 놀라 튀어 오름 + 느낌표
  p = new Pic(S, S);
  const bd = bolt(78, 76, 2.6);
  sticker(p, bd);
  p.path(bd, p.lin([[0, '#fff6a0'], [0.5, '#ffd21f'], [1, '#f39a00']]), stroke(4, '#8a4a00'));
  for (const dx of [-9, 9]) {
    p.path(ell(76 + dx, 72, 7, 8), '#ffffff', stroke(2.5, '#20304a'));
    p.path(ell(76 + dx, 73, 3, 3.5), '#20304a');
  }
  p.path(ell(78, 94, 5, 7), '#20304a');
  const ex = 'M126 22 L138 22 L134 80 L130 80 Z';
  sticker(p, ex);
  p.path(ex, '#ff3a3a', stroke(3, '#7a0a0a'));
  p.path(ell(132, 96, 7, 7), '#ff3a3a', stroke(3, '#7a0a0a'));
  for (const [x, y, a] of [[30, 40, -30], [26, 70, 0], [32, 100, 30]]) p.add(`<path d="M0 0 h-14" stroke="#ffd21f" stroke-width="5" stroke-linecap="round" transform="translate(${x} ${y}) rotate(${a})"/>`);
  out.push(p.sprite('UI/emote_2'));

  // 3 흑흑: 우는 물방울 (눈물 줄기)
  p = new Pic(S, S);
  sticker(p, DROP);
  p.path(DROP, dropFill(p), stroke(4, '#1a4a9a'));
  p.path('M58 76 Q60 58 74 46', 'none', 'stroke="#ffffff" stroke-width="7" stroke-linecap="round" opacity="0.85"');
  for (const dx of [-14, 14]) p.path(`M${80 + dx - 8} 100 Q${80 + dx} 92 ${80 + dx + 8} 100`, 'none', 'stroke="#20304a" stroke-width="4" stroke-linecap="round"');
  for (const dx of [-14, 14]) p.path(`M${80 + dx} 104 Q${80 + dx - 4} 124 ${80 + dx + (dx < 0 ? -10 : 10)} 148`, 'none', 'stroke="#bfefff" stroke-width="8" stroke-linecap="round" opacity="0.95"');
  p.path('M70 124 Q80 116 90 124', 'none', 'stroke="#20304a" stroke-width="3.5" stroke-linecap="round"');
  out.push(p.sprite('UI/emote_3'));

  // 4 ㅋㅋㅋ: 배꼽 잡고 웃는 회오리 (^^ 눈, 큰 입, 땀방울)
  p = new Pic(S, S);
  const tw = 'M38 34 Q80 18 122 34 Q114 62 102 78 Q94 100 90 118 Q86 136 80 148 Q76 134 72 118 Q64 96 58 78 Q42 58 38 34 Z';
  sticker(p, tw);
  p.path(tw, p.lin([[0, '#d6ffb0'], [1, '#3fbf5a']], 0, 0, 1, 1), stroke(4, '#1f6a34'));
  for (const dx of [-14, 14]) p.path(`M${80 + dx - 9} 58 Q${80 + dx} 48 ${80 + dx + 9} 58`, 'none', 'stroke="#1a3a20" stroke-width="5" stroke-linecap="round"');
  p.path('M62 70 Q80 100 98 70 Z', '#7a1a2a', stroke(3, '#1a3a20'));
  p.path('M70 82 Q80 92 90 82 Q80 86 70 82 Z', '#ff8a9a');
  blush(p, 80, 66, 0.9);
  p.path('M126 46 C132 56 132 64 126 66 C120 64 120 56 126 46 Z', '#9fe0ff', stroke(2, '#2a6ad0'));
  for (const [x, y] of [[24, 30], [138, 96], [22, 100]]) p.path(star(x, y, 9, 3), '#fff36a', stroke(2, '#ffffff'));
  out.push(p.sprite('UI/emote_4'));

  // 5 잘했어: 별 눈 수정 + 반짝이
  p = new Pic(S, S);
  const cr = poly([[80, 18], [112, 64], [100, 146], [60, 146], [48, 64]]);
  p.path(ell(80, 80, 72, 72), p.rad([[0, '#fffbe0', 0.9], [0.5, '#ffe9a0', 0.4], [1, '#ffe9a0', 0]]));
  sticker(p, cr);
  p.path(cr, p.lin([[0, '#ffffff'], [0.6, '#e8f2ff'], [1, '#b8c8f0']], 0, 0, 1, 1), stroke(4, '#6a7aa8'));
  p.path(poly([[80, 18], [80, 146], [60, 146], [48, 64]]), '#ffffff', 'opacity="0.55"');
  for (const dx of [-12, 12]) p.path(star(80 + dx, 84, 9, 3.6, 5), '#ffc81f', stroke(1.5, '#8a5a00'));
  p.path('M68 104 Q80 116 92 104', 'none', 'stroke="#3a4a7a" stroke-width="4" stroke-linecap="round"');
  for (const [x, y, s] of [[30, 34, 12], [134, 50, 9], [128, 120, 11], [26, 112, 8]]) p.path(star(x, y, s, s * 0.32), '#ffd84a', stroke(2, '#ffffff'));
  out.push(p.sprite('UI/emote_5'));

  // 6 음…: 곁눈질하는 생각 구름 + 말줄임 거품
  p = new Pic(S, S);
  const cl = 'M30 112 C14 112 14 82 36 82 C32 54 66 46 78 64 C88 42 128 52 120 82 C142 82 140 114 118 114 Z';
  sticker(p, cl);
  p.path(cl, p.rad([[0, '#d6b6ff'], [0.6, '#9a5ae6'], [1, '#5a2aa6']], 0.4, 0.35, 0.8), stroke(4, '#3a1a6a'));
  for (const dx of [-12, 12]) {
    p.path(ell(78 + dx, 92, 5, 6), '#ffffff');
    p.path(ell(81 + dx, 91, 3, 3.5), '#20304a');
  }
  p.path('M70 106 H88', 'none', 'stroke="#2a1050" stroke-width="3.5" stroke-linecap="round"');
  for (const [x, y, r] of [[120, 40, 7], [134, 24, 10]]) { sticker(p, ell(x, y, r, r)); p.path(ell(x, y, r, r), '#efe2ff', stroke(2.5, '#7a4ac8')); }
  for (const dx of [-5, 0, 5]) p.path(ell(134 + dx, 24, 1.6, 1.6), '#5a2aa6');
  out.push(p.sprite('UI/emote_6'));

  // 7 GG: 백기를 든 꾸벅 석상
  p = new Pic(S, S);
  const st = 'M70 40 C80 56 104 72 104 96 C104 122 92 140 70 140 C48 140 36 122 36 96 C36 72 60 56 70 40 Z';
  sticker(p, st);
  p.path(st, p.rad([[0, '#eceaf0'], [0.6, '#a9a4b0'], [1, '#77727f']], 0.38, 0.45, 0.75), stroke(4, '#4a4652'));
  p.path('M56 100 Q60 105 64 100 M76 100 Q80 105 84 100', 'none', 'stroke="#4a4652" stroke-width="3.5" stroke-linecap="round"');
  p.path('M64 118 Q70 123 76 118', 'none', 'stroke="#4a4652" stroke-width="3" stroke-linecap="round"');
  blush(p, 70, 112, 0.8);
  p.path('M112 146 V30', 'none', 'stroke="#ffffff" stroke-width="12" stroke-linecap="round"');
  p.path('M112 146 V30', 'none', 'stroke="#8a5a2a" stroke-width="5" stroke-linecap="round"');
  const flag = 'M114 32 C126 26 136 38 150 32 L146 56 C134 62 126 50 114 56 Z';
  sticker(p, flag);
  p.path(flag, '#ffffff', stroke(3, '#8a8aa0'));
  p.path('M104 100 Q110 92 114 96', 'none', 'stroke="#4a4652" stroke-width="5" stroke-linecap="round"');
  out.push(p.sprite('UI/emote_7'));

  // 이모티콘 말풍선 꼬리 (위를 가리키는 삼각형, 48x28). 바탕 말풍선은 UI/bubble 과 같은 색·테두리
  p = new Pic(48, 28);
  p.path('M24 2 L44 27 L4 27 Z', p.lin([[0, '#fffdf4'], [1, '#fffaee']]));
  p.path('M4 27 L24 2 L44 27', 'none', 'stroke="#b07a2a" stroke-width="4" stroke-linejoin="round"');
  out.push(p.sprite('UI/bubble_tail'));

  // 이모티콘 버튼 아이콘: 웃는 물방울 말풍선
  p = new Pic(128, 128);
  const bub = 'M20 22 H108 Q118 22 118 32 V84 Q118 94 108 94 H54 L34 112 L38 94 H20 Q10 94 10 84 V32 Q10 22 20 22 Z';
  p.path(bub, '#000000', 'opacity="0.2" transform="translate(2 4)"');
  p.path(bub, p.lin([[0, '#ffffff'], [1, '#dfe6f5']]), stroke(4, '#2a3a6a'));
  p.path('M64 32 C70 42 82 50 82 62 C82 74 74 82 64 82 C54 82 46 74 46 62 C46 50 58 42 64 32 Z', dropFill(p), stroke(3, '#1a4a9a'));
  for (const dx of [-6, 6]) p.path(`M${64 + dx - 3} 62 Q${64 + dx} 58 ${64 + dx + 3} 62`, 'none', 'stroke="#20304a" stroke-width="2.5" stroke-linecap="round"');
  p.path('M58 70 Q64 76 70 70', 'none', 'stroke="#20304a" stroke-width="2.5" stroke-linecap="round"');
  p.path(heart(98, 40, 0.7), '#ff3a6a');
  out.push(p.sprite('UI/icon_emote'));

  return out;
}
