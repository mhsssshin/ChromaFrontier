// PNG 입출력과 RGBA 이미지. 외부 의존성 없음 (node:zlib 만 사용).
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { dirname } from 'node:path';
import { deflateSync, inflateSync } from 'node:zlib';

export class Image {
  constructor(w, h) {
    this.w = w;
    this.h = h;
    this.px = new Uint8Array(w * h * 4);
  }

  get(x, y) {
    const i = (y * this.w + x) * 4;
    return [this.px[i], this.px[i + 1], this.px[i + 2], this.px[i + 3]];
  }

  set(x, y, p) {
    if (x < 0 || y < 0 || x >= this.w || y >= this.h) return;
    this.px.set(p, (y * this.w + x) * 4);
  }

  crop(x0, y0, w, h) {
    const out = new Image(w, h);
    for (let y = 0; y < h; y++)
      for (let x = 0; x < w; x++)
        if (x0 + x < this.w && y0 + y < this.h) out.set(x, y, this.get(x0 + x, y0 + y));
    return out;
  }

  /** 알파 합성으로 src 를 (ox, oy)에 얹는다. */
  over(src, ox, oy) {
    for (let y = 0; y < src.h; y++)
      for (let x = 0; x < src.w; x++) {
        const [r, g, b, a] = src.get(x, y);
        if (!a) continue;
        const [R, G, B, A] = this.get(ox + x, oy + y);
        const t = a / 255, oa = t + (A / 255) * (1 - t);
        const mix = (s, d) => Math.round((s * t + d * (A / 255) * (1 - t)) / (oa || 1));
        this.set(ox + x, oy + y, [mix(r, R), mix(g, G), mix(b, B), Math.round(oa * 255)]);
      }
  }

  fill([r, g, b, a]) {
    for (let i = 0; i < this.px.length; i += 4) this.px.set([r, g, b, a], i);
  }

  save(path) {
    mkdirSync(dirname(path), { recursive: true });
    writeFileSync(path, this.toPng());
  }

  toPng() {
    const raw = Buffer.alloc((this.w * 4 + 1) * this.h);
    for (let y = 0; y < this.h; y++) {
      raw[y * (this.w * 4 + 1)] = 0;
      Buffer.from(this.px.buffer, y * this.w * 4, this.w * 4).copy(raw, y * (this.w * 4 + 1) + 1);
    }
    const ihdr = Buffer.alloc(13);
    ihdr.writeUInt32BE(this.w, 0);
    ihdr.writeUInt32BE(this.h, 4);
    ihdr.set([8, 6, 0, 0, 0], 8);
    return Buffer.concat([
      Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
      chunk('IHDR', ihdr),
      chunk('IDAT', deflateSync(raw, { level: 9 })),
      chunk('IEND', Buffer.alloc(0)),
    ]);
  }
}

const CRC = new Uint32Array(256).map((_, n) => {
  let c = n;
  for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1;
  return c >>> 0;
});
const crc32 = (buf) => {
  let c = 0xffffffff;
  for (const b of buf) c = CRC[(c ^ b) & 255] ^ (c >>> 8);
  return (c ^ 0xffffffff) >>> 0;
};
function chunk(type, data) {
  const len = Buffer.alloc(4);
  len.writeUInt32BE(data.length);
  const td = Buffer.concat([Buffer.from(type, 'ascii'), data]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(td));
  return Buffer.concat([len, td, crc]);
}

/** RGBA 8비트 PNG 만 읽는다 (Chrome 스크린샷 형식). */
export function readPng(path) {
  const b = readFileSync(path);
  let o = 8, w = 0, h = 0, type = 6;
  const idat = [];
  while (o < b.length) {
    const len = b.readUInt32BE(o), t = b.toString('ascii', o + 4, o + 8), d = b.subarray(o + 8, o + 8 + len);
    if (t === 'IHDR') { w = d.readUInt32BE(0); h = d.readUInt32BE(4); type = d[9]; if (d[8] !== 8 || d[12]) throw new Error(`${path}: unsupported PNG`); }
    else if (t === 'IDAT') idat.push(d);
    o += 12 + len;
  }
  const chans = { 2: 3, 6: 4 }[type];
  if (!chans) throw new Error(`${path}: PNG color type ${type} not supported`);
  const stride = w * chans, raw = inflateSync(Buffer.concat(idat)), px = Buffer.alloc(stride * h);
  for (let y = 0; y < h; y++) {
    const f = raw[y * (stride + 1)], src = raw.subarray(y * (stride + 1) + 1, (y + 1) * (stride + 1));
    for (let x = 0; x < stride; x++) {
      const a = x >= chans ? px[y * stride + x - chans] : 0, up = y ? px[(y - 1) * stride + x] : 0, c = y && x >= chans ? px[(y - 1) * stride + x - chans] : 0;
      let v = src[x];
      if (f === 1) v += a; else if (f === 2) v += up; else if (f === 3) v += (a + up) >> 1;
      else if (f === 4) { const p = a + up - c, pa = Math.abs(p - a), pb = Math.abs(p - up), pc = Math.abs(p - c); v += pa <= pb && pa <= pc ? a : pb <= pc ? up : c; }
      px[y * stride + x] = v & 255;
    }
  }
  const img = new Image(w, h);
  for (let y = 0; y < h; y++)
    for (let x = 0; x < w; x++) {
      const p = y * stride + x * chans;
      img.set(x, y, [px[p], px[p + 1], px[p + 2], chans === 4 ? px[p + 3] : 255]);
    }
  return img;
}
