// 이펙트 프레임 애니메이션. 모두 128x128, 중심 피벗. 이름 규칙 <이름>_<프레임>.
// ring/sparkle/smoke 는 흰색 위주라 코드에서 원소 색으로 틴트한다.
import { Pic, ell, star, poly, rng, f } from './kit.mjs';

const S = 128, C = 64;
const ease = (t) => 1 - (1 - t) * (1 - t);

function frames(name, n, draw) {
  const out = [];
  for (let i = 0; i < n; i++) {
    const p = new Pic(S, S);
    draw(p, i / (n - 1), i);
    out.push(p.sprite(`Fx/${name}_${i}`));
  }
  return out;
}

export function fx() {
  return [
    // 화염 폭발: 불꽃 꽃잎이 퍼지며 사라짐
    ...frames('burst', 6, (p, t) => {
      const r = 14 + ease(t) * 44, a = 1 - t * 0.9;
      p.path(ell(C, C, r, r), p.rad([[0, '#fff6a0', a], [0.4, '#ffb02a', a], [0.8, '#ff4a2a', a * 0.8], [1, '#ff4a2a', 0]]));
      for (let k = 0; k < 8; k++) {
        const ang = (k * 45 + t * 40) * Math.PI / 180, d = r * 0.9;
        p.path(ell(C + Math.cos(ang) * d, C + Math.sin(ang) * d, 8 * (1 - t) + 2, 8 * (1 - t) + 2), '#ffd23a', `opacity="${f(a)}"`);
      }
    }),
    // 물보라: 방울이 둥글게 튀어 오름
    ...frames('splash', 5, (p, t) => {
      const a = 1 - t;
      p.path(ell(C, C + 16, 16 + t * 40, 6 + t * 14), 'none', `stroke="#d8f2ff" stroke-width="${f(6 * a + 1)}" opacity="${f(a)}"`);
      for (let k = 0; k < 7; k++) {
        const ang = (-160 + k * 23) * Math.PI / 180, d = 10 + ease(t) * 40;
        const x = C + Math.cos(ang) * d, y = C + 10 + Math.sin(ang) * d + t * t * 30;
        p.path(`M${f(x)} ${f(y - 7)} Q${f(x + 5)} ${f(y + 2)} ${f(x)} ${f(y + 4)} Q${f(x - 5)} ${f(y + 2)} ${f(x)} ${f(y - 7)} Z`, '#5ab8ff', `stroke="#ffffff" stroke-width="1.5" opacity="${f(a)}"`);
      }
    }),
    // 번개 스파크: 지그재그 갈래가 번쩍
    ...frames('zap', 4, (p, t, i) => {
      const r = rng(11 + i);
      p.path(ell(C, C, 30, 30), p.rad([[0, '#fff9c0', 0.8 * (1 - t)], [1, '#fff9c0', 0]]));
      for (let k = 0; k < 5; k++) {
        let x = C, y = C, d = `M${x} ${y}`;
        const ang = r() * Math.PI * 2;
        for (let s = 0; s < 4; s++) {
          x += Math.cos(ang) * 12 + (r() - 0.5) * 14;
          y += Math.sin(ang) * 12 + (r() - 0.5) * 14;
          d += ` L${f(x)} ${f(y)}`;
        }
        p.path(d, 'none', `stroke="#ffd21f" stroke-width="6" stroke-linejoin="round" opacity="${f(1 - t * 0.7)}"`);
        p.path(d, 'none', `stroke="#ffffff" stroke-width="2.5" stroke-linejoin="round" opacity="${f(1 - t * 0.7)}"`);
      }
    }),
    // 바닥 충격파 (납작한 고리, 흰색 → 틴트)
    ...frames('ring', 5, (p, t) => {
      const rx = 14 + ease(t) * 46, a = 1 - t;
      p.path(ell(C, C, rx, rx * 0.5), 'none', `stroke="#ffffff" stroke-width="${f(10 * a + 2)}" opacity="${f(a)}"`);
    }),
    // 반짝임 별 (흰색 → 틴트)
    ...frames('sparkle', 4, (p, t) => {
      const s = Math.sin(t * Math.PI);
      p.path(star(C, C, 10 + s * 22, 3 + s * 4), '#ffffff', `opacity="${f(0.3 + s * 0.7)}"`);
      p.path(star(C + 22, C - 18, 4 + s * 8, 1.5 + s * 2), '#ffffff', `opacity="${f(s)}"`);
      p.path(star(C - 20, C + 16, 3 + s * 6, 1 + s * 2), '#ffffff', `opacity="${f(s)}"`);
    }),
    // 연기 (흰 회색 → 틴트)
    ...frames('smoke', 5, (p, t) => {
      const a = 0.8 * (1 - t);
      for (const [dx, dy, r] of [[-14, 6, 16], [14, 4, 15], [0, -8, 18]])
        p.path(ell(C + dx * (1 + t), C + dy - t * 18, r * (0.7 + t * 0.6), r * (0.7 + t * 0.6)), '#eceaf0', `opacity="${f(a)}"`);
    }),
    // 공허(흑색 콤보): 보랏빛 소용돌이가 빨려 들어감
    ...frames('void', 6, (p, t) => {
      const r = 56 * (1 - t * 0.7);
      p.path(ell(C, C, r, r), p.rad([[0, '#000000', 0.9], [0.6, '#5a1aa0', 0.6], [1, '#b45aff', 0]]));
      for (let k = 0; k < 4; k++)
        p.add(`<path d="M${C} ${C} m-${f(r * 0.8)} 0 a${f(r * 0.8)} ${f(r * 0.8)} 0 0 1 ${f(r * 1.2)} -${f(r * 0.5)}" fill="none" stroke="#d9a8ff" stroke-width="4" stroke-linecap="round" opacity="${f(0.9 - t * 0.5)}" transform="rotate(${k * 90 + t * 200} ${C} ${C})"/>`);
    }),
    // 성역(백색 콤보): 빛줄기가 솟음
    ...frames('holy', 6, (p, t) => {
      const a = Math.sin(t * Math.PI);
      p.path(ell(C, C, 50, 50), p.rad([[0, '#ffffff', a], [0.5, '#fff1a0', a * 0.5], [1, '#fff1a0', 0]]));
      for (let k = 0; k < 6; k++)
        p.add(`<path d="${poly([[C - 4, C], [C + 4, C], [C, C - 60 * a]])}" fill="#fffbe0" opacity="${f(a * 0.8)}" transform="rotate(${k * 60 + t * 30} ${C} ${C})"/>`);
    }),
  ];
}
