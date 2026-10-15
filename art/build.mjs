// 아트 에셋 빌드: 정의(tiles/units/fx/ui)를 SVG → 헤드리스 Chrome → PNG 로 굽는다.
//   node art/build.mjs
// 출력: client/Assets/Resources/Art/<폴더>/<이름>.png, 미리보기 build/art-preview.png
// PNG 를 직접 고치지 말고 정의를 고친 뒤 다시 굽는다.
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { Image } from './lib.mjs';
import { renderSvgSprites } from './render-svg.mjs';
import { tiles, boardFoam } from './tiles.mjs';
import { units } from './units.mjs';
import { fx } from './fx.mjs';
import { ui } from './ui.mjs';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const outDir = join(root, 'client', 'Assets', 'Resources', 'Art');

const sprites = [...tiles(), boardFoam(), ...units(), ...fx(), ...ui()];
const t0 = Date.now();
const rendered = renderSvgSprites(sprites);
for (const { name, image } of rendered) image.save(join(outDir, `${name}.png`));
console.log(`[art] ${rendered.length} sprites → ${outDir} (${Date.now() - t0} ms)`);

// 미리보기: 배경·거품을 뺀 전부를 어두운 바탕에 격자로
const items = rendered.filter((r) => !r.name.startsWith('Bg/'));
const cols = 12, cell = 140;
const sheet = new Image(cols * cell, Math.ceil(items.length / cols) * cell);
sheet.fill([40, 46, 66, 255]);
items.forEach(({ image }, i) => {
  const s = Math.min(1, (cell - 8) / Math.max(image.w, image.h));
  const img = s < 1 ? downscale(image, s) : image;
  sheet.over(img, (i % cols) * cell + ((cell - img.w) >> 1), ((i / cols) | 0) * cell + ((cell - img.h) >> 1));
});
sheet.save(join(root, 'build', 'art-preview.png'));
console.log('[art] preview → build/art-preview.png');

function downscale(img, s) {
  const out = new Image(Math.max(1, Math.round(img.w * s)), Math.max(1, Math.round(img.h * s)));
  for (let y = 0; y < out.h; y++)
    for (let x = 0; x < out.w; x++) out.set(x, y, img.get(Math.min(img.w - 1, (x / s) | 0), Math.min(img.h - 1, (y / s) | 0)));
  return out;
}
