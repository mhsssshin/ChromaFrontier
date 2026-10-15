// SVG 스프라이트들을 헤드리스 Chrome 으로 한 장의 시트에 렌더링한 뒤 잘라서 Image 로 돌려준다.
// (안티앨리어싱·그라데이션·블러 필터를 쓰기 위함. Chrome 은 시스템에 설치된 것을 쓰고, CHROME_PATH 로 바꿀 수 있다.)
import { execFileSync } from 'node:child_process';
import { existsSync, mkdirSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { readPng } from './lib.mjs';

const CHROME_CANDIDATES = [
  process.env.CHROME_PATH,
  'C:/Program Files/Google/Chrome/Application/chrome.exe',
  'C:/Program Files (x86)/Google/Chrome/Application/chrome.exe',
  'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
].filter(Boolean);

/** sprites: [{ name, w, h, svg }] → [{ name, image }] */
export function renderSvgSprites(sprites) {
  const chrome = CHROME_CANDIDATES.find((p) => existsSync(p));
  if (!chrome) throw new Error('Chrome/Edge not found — set CHROME_PATH');

  const pad = 4;
  let x = pad, y = pad, rowH = 0;
  const maxW = Math.max(2048, ...sprites.map((s) => s.w + pad * 2));
  const cells = sprites.map((s) => {
    if (x + s.w + pad > maxW) { x = pad; y += rowH + pad; rowH = 0; }
    const cell = { ...s, x, y };
    x += s.w + pad;
    rowH = Math.max(rowH, s.h);
    return cell;
  });
  const W = maxW, H = y + rowH + pad;

  const dir = join(tmpdir(), 'chroma-frontier-svg');
  mkdirSync(join(dir, 'profile'), { recursive: true });
  const html = `<!doctype html><html><body style="margin:0;background:transparent;width:${W}px;height:${H}px;position:relative">${cells
    .map((c) => `<div style="position:absolute;left:${c.x}px;top:${c.y}px;width:${c.w}px;height:${c.h}px">${c.svg}</div>`)
    .join('')}</body></html>`;
  const htmlPath = join(dir, 'sheet.html'), pngPath = join(dir, 'sheet.png');
  writeFileSync(htmlPath, html);
  execFileSync(chrome, [
    '--headless=new', '--disable-gpu', '--hide-scrollbars', '--force-device-scale-factor=1',
    `--user-data-dir=${join(dir, 'profile')}`, '--default-background-color=00000000',
    `--window-size=${W},${H}`, `--screenshot=${pngPath}`, `file:///${htmlPath.replace(/\\/g, '/')}`,
  ], { stdio: 'ignore' });

  const sheet = readPng(pngPath);
  return cells.map((c) => ({ name: c.name, image: sheet.crop(c.x, c.y, c.w, c.h) }));
}
