// 영문 로고 "CHROMA FRONTIER" (1024x440).
// CHROMA 는 글자마다 무지개색(원소의 색), FRONTIER 는 큰 금색. 두꺼운 남색 외곽선 + 흰 테두리 + 입체 그림자 + 광택.
// 글꼴은 Jua (SIL OFL 1.1, art/vendor/fonts/Jua-OFL.txt) — base64 로 SVG 안에 넣어 헤드리스 Chrome 이 바로 쓰게 한다.
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { Pic, star, light, dark } from './kit.mjs';

const FONT = readFileSync(fileURLToPath(new URL('./vendor/fonts/Jua-Regular.ttf', import.meta.url))).toString('base64');
const INK = '#1c2347';

/** 글자 한 줄: 그림자(입체) → 남색 외곽선 → 흰 테두리 → 색 채움 → 위쪽 광택. fills 는 글자마다 그라데이션 url. */
function word(p, text, x, y, size, fills, { spacing = 0, depth = 12, outline = 22, rim = 8 } = {}) {
  const tspans = (fill) =>
    [...text].map((ch, i) => `<tspan fill="${typeof fill === 'function' ? fill(i) : fill}">${ch}</tspan>`).join('');
  const base = `x="${x}" y="${y}" font-family="Jua" font-size="${size}" text-anchor="middle" letter-spacing="${spacing}"`;
  const strokeAttr = (w, c) => `stroke="${c}" stroke-width="${w}" stroke-linejoin="round" paint-order="stroke"`;
  // 입체: 아래로 여러 겹 쌓아 두께를 낸다
  for (let d = depth; d > 0; d -= 2)
    p.add(`<text ${base} transform="translate(0 ${d})" ${strokeAttr(outline, INK)}>${tspans(INK)}</text>`);
  p.add(`<text ${base} ${strokeAttr(outline, INK)}>${tspans(INK)}</text>`);
  p.add(`<text ${base} ${strokeAttr(rim, '#ffffff')}>${tspans((i) => fills[i % fills.length])}</text>`);
  // 광택: 글자 위쪽 절반만 밝게
  const clip = p.id();
  p.defs.push(`<clipPath id="${clip}"><rect x="0" y="${y - size * 0.78}" width="${p.w}" height="${size * 0.36}"/></clipPath>`);
  p.add(`<text ${base} clip-path="url(#${clip})" opacity="0.38">${tspans('#ffffff')}</text>`);
}

export function logo() {
  const p = new Pic(1024, 440);
  p.defs.push(`<style>@font-face { font-family: 'Jua'; src: url(data:font/ttf;base64,${FONT}) format('truetype'); }</style>`);

  // 글자마다 위→아래 그라데이션 (원소 색 무지개)
  const rainbow = ['#ff4a3a', '#ff9a2a', '#ffd21f', '#44c95a', '#3d8ff0', '#9a5ae6'].map((c) =>
    p.lin([[0, light(c, 0.35)], [0.55, c], [1, dark(c, 0.25)]]));
  const gold = p.lin([[0, '#fffbd0'], [0.45, '#ffd23a'], [1, '#e07a10']]);

  // 뒤쪽 빛 번짐
  p.path(`M120 330 Q512 120 904 330 Q512 420 120 330 Z`, p.rad([[0, '#ffffff', 0.55], [1, '#ffffff', 0]]), `filter="${p.blur(18)}"`);

  word(p, 'CHROMA', 512, 170, 150, rainbow, { spacing: 6, depth: 10, outline: 20, rim: 8 });
  word(p, 'FRONTIER', 512, 352, 176, [gold], { spacing: 4, depth: 14, outline: 24, rim: 9 });

  // 반짝임
  for (const [x, y, r] of [[118, 92, 26], [905, 74, 20], [948, 268, 16], [76, 300, 14]])
    p.path(star(x, y, r, r * 0.3), '#ffffff', `stroke="${INK}" stroke-width="3" stroke-linejoin="round"`);
  return p.sprite('UI/logo_title');
}
