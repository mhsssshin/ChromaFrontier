// WebGL 빌드(build/webgl)를 띄우는 의존성 없는 정적 서버.
//   node scripts/serve-webgl.mjs [port] [폴더]   (폴더 기본값 build/webgl, 배포용 확인은 build/webgl-release)
// 0.0.0.0 에 바인딩하므로 같은 Wi-Fi의 휴대폰 브라우저에서도 세로 화면으로 테스트할 수 있다.
// 압축 빌드(.gz/.br)도 올바른 Content-Encoding 헤더로 서빙한다.
import { createServer } from 'node:http';
import { createReadStream, statSync } from 'node:fs';
import { networkInterfaces } from 'node:os';
import { extname, join, normalize, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const port = Number(process.argv[2] ?? 8080);
const root = process.argv[3]
  ? resolve(process.argv[3])
  : resolve(fileURLToPath(new URL('../build/webgl', import.meta.url)));

const types = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'application/javascript',
  '.wasm': 'application/wasm',
  '.data': 'application/octet-stream',
  '.json': 'application/json',
  '.css': 'text/css',
  '.png': 'image/png',
  '.ico': 'image/x-icon',
};
const encodings = { '.gz': 'gzip', '.br': 'br' };

createServer((req, res) => {
  const urlPath = decodeURIComponent(new URL(req.url, 'http://x').pathname);
  let file = normalize(join(root, urlPath));
  if (file !== root && !file.startsWith(root + sep)) {
    res.writeHead(403).end();
    return;
  }
  try {
    if (statSync(file).isDirectory()) file = join(file, 'index.html');
    statSync(file);
  } catch {
    res.writeHead(404).end('not found');
    return;
  }

  const headers = { 'Cache-Control': 'no-store' };
  let ext = extname(file);
  if (encodings[ext]) {
    headers['Content-Encoding'] = encodings[ext];
    ext = extname(file.slice(0, -ext.length));
  }
  headers['Content-Type'] = types[ext] ?? 'application/octet-stream';
  res.writeHead(200, headers);
  createReadStream(file).pipe(res);
}).listen(port, '0.0.0.0', () => {
  console.log(`Serving ${root}`);
  console.log(`  http://localhost:${port}/`);
  for (const addrs of Object.values(networkInterfaces())) {
    for (const a of addrs ?? []) {
      if (a.family === 'IPv4' && !a.internal) console.log(`  http://${a.address}:${port}/  (phone on same Wi-Fi)`);
    }
  }
});
