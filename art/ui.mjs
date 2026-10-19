// HUD 텍스처. 화면 기준 좌표(540x960)의 2배 해상도로 그린다.
// 9-slice 로 쓰는 것은 이름 옆 주석의 테두리 px 가 Hud.cs 의 Slice(...) 값과 같아야 한다.
import { Pic, ell, rect, poly, star, stroke } from './kit.mjs';

const icon = (name, draw) => {
  const p = new Pic(128, 128);
  draw(p);
  return p.sprite(`UI/${name}`);
};

export function ui() {
  const out = [];
  let p;

  // 카드 틀 (9-slice 28)
  p = new Pic(192, 272);
  p.path(rect(4, 8, 184, 260, 20), '#000000', 'opacity="0.25"');
  p.path(rect(2, 2, 184, 260, 20), p.lin([[0, '#ffd36a'], [1, '#e08a2a']]), stroke(3, '#7a3a10'));
  p.path(rect(12, 12, 164, 240, 13), p.lin([[0, '#fffaf0'], [1, '#f3e6c8']]));
  out.push(p.sprite('UI/card_unit'));

  p = new Pic(192, 272);
  p.path(rect(4, 8, 184, 260, 20), '#000000', 'opacity="0.25"');
  p.path(rect(2, 2, 184, 260, 20), p.lin([[0, '#b49aff'], [1, '#5a3ec8']]), stroke(3, '#24145a'));
  p.path(rect(12, 12, 164, 240, 13), p.lin([[0, '#3a3d6a'], [1, '#22244a']]));
  out.push(p.sprite('UI/card_skill'));

  // 카드 뒷면 (덱 더미·뽑기 모션, 192x272 고정 비율)
  p = new Pic(192, 272);
  p.path(rect(4, 8, 184, 260, 20), '#000000', 'opacity="0.25"');
  p.path(rect(2, 2, 184, 260, 20), p.lin([[0, '#ffd36a'], [1, '#c8781a']]), stroke(3, '#5a2a08'));
  p.path(rect(12, 12, 164, 240, 13), p.lin([[0, '#3a4fb0'], [1, '#1c2466']], 0, 0, 1, 1));
  let lattice = '';
  for (let k = -10; k <= 10; k++) lattice += `M${94 + k * 18} 22 l120 220 M${94 + k * 18} 22 l-120 220 `;
  p.add(`<path d="${lattice}" stroke="#ffffff" stroke-width="1.5" opacity="0.12" clip-path="${p.clip(rect(22, 22, 144, 220, 10))}"/>`);
  // 가운데 문장: 삼원색 원 세 개가 겹친 별
  for (const [dx, dy, c] of [[0, -16, '#ff5a3a'], [-14, 10, '#3a9aff'], [14, 10, '#ffd21f']])
    p.path(ell(94 + dx, 132 + dy, 24, 24), c, 'opacity="0.85" stroke="#1c2466" stroke-width="3"');
  p.path(star(94, 132, 18, 7), '#ffffff', stroke(2.5, '#1c2466'));
  out.push(p.sprite('UI/card_back'));

  // 카드 그림 칸 (흰색 → 카드 색으로 틴트, 고정 비율 160x136)
  p = new Pic(160, 136);
  p.path(rect(0, 0, 160, 136, 12), p.lin([[0, '#ffffff'], [1, '#c8c8c8']]));
  p.path(rect(6, 6, 148, 50, 10), '#ffffff', 'opacity="0.35"');
  out.push(p.sprite('UI/card_art'));

  // 점유율 바 틀 (9-slice 30) / 채움 (9-slice 22, 흰색 → 팀 색 틴트)
  p = new Pic(256, 64);
  p.path(rect(2, 4, 252, 58, 29), '#000000', 'opacity="0.25"');
  p.path(rect(2, 2, 252, 56, 28), p.lin([[0, '#ffffff'], [1, '#cfd8e8']]), stroke(3, '#2a3a5a'));
  p.path(rect(8, 8, 240, 44, 22), '#1c2a44');
  out.push(p.sprite('UI/bar_frame'));

  p = new Pic(96, 44);
  p.path(rect(0, 0, 96, 44, 22), p.lin([[0, '#ffffff'], [1, '#bdbdbd']]));
  p.path(rect(10, 5, 76, 12, 6), '#ffffff', 'opacity="0.6"');
  out.push(p.sprite('UI/bar_fill'));

  // 버튼 (9-slice 22, 흰색 → 틴트)
  p = new Pic(128, 72);
  p.path(rect(2, 8, 124, 62, 20), '#000000', 'opacity="0.3"');
  p.path(rect(2, 2, 124, 62, 20), p.lin([[0, '#ffffff'], [1, '#b8b8c8']]), stroke(3, '#2a2a40'));
  p.path(rect(10, 8, 108, 18, 9), '#ffffff', 'opacity="0.55"');
  out.push(p.sprite('UI/button'));

  // 반투명 패널 (9-slice 28)
  p = new Pic(128, 128);
  p.path(rect(2, 2, 124, 124, 24), '#14203a', 'opacity="0.86"');
  p.path(rect(2, 2, 124, 124, 24), 'none', 'stroke="#ffffff" stroke-width="3" opacity="0.35"');
  out.push(p.sprite('UI/panel'));

  // 밝은 말풍선 (9-slice 28): 튜토리얼 안내. 진한 글자를 올려 잘 읽히게
  p = new Pic(128, 128);
  p.path(rect(3, 7, 122, 118, 24), '#000000', 'opacity="0.3"');
  p.path(rect(2, 2, 122, 118, 24), p.lin([[0, '#fffdf4'], [1, '#f6ebcf']]), stroke(4, '#b07a2a'));
  p.path(rect(10, 9, 106, 14, 7), '#ffffff', 'opacity="0.7"');
  out.push(p.sprite('UI/bubble'));

  // ---- 대기실: 별(획득/미획득), 보스 왕관 (64x64)
  for (const [name, fill, line] of [['star_on', p0 => p0.lin([[0, '#fff3a0'], [1, '#ffb21f']]), '#8a4a00'], ['star_off', () => '#3a4466', '#1c2240']]) {
    p = new Pic(64, 64);
    p.path(star(32, 34, 28, 12, 5), fill(p), stroke(3.5, line));
    if (name === 'star_on') p.path(ell(24, 26, 5, 3.5), '#ffffff', 'opacity="0.7"');
    out.push(p.sprite(`UI/${name}`));
  }
  p = new Pic(64, 64);
  p.path(poly([[8, 50], [8, 20], [22, 34], [32, 12], [42, 34], [56, 20], [56, 50]]), p.lin([[0, '#fff3a0'], [1, '#e8941f']]), stroke(3.5, '#7a3a08'));
  for (const x of [8, 32, 56]) p.path(ell(x, x === 32 ? 12 : 20, 4, 4), '#ff5a5a', stroke(2, '#7a1a1a'));
  out.push(p.sprite('UI/crown'));

  // ---- 튜토리얼 손가락 (96x96, 손끝이 (20, 8) — Hud 의 FingerTip 과 맞물림)
  p = new Pic(96, 96);
  const glove = `M20 8 C26 6 30 10 30 16 L30 40 C34 34 44 36 44 42 C48 38 56 40 56 46 C60 43 68 45 68 52
    L68 70 C68 84 58 92 46 92 L38 92 C28 92 20 86 16 76 L8 58 C5 51 12 46 18 51 L22 56 L22 16 C22 12 16 10 20 8 Z`;
  p.path(glove, '#000000', 'opacity="0.25" transform="translate(3 4)"');
  p.path(glove, p.lin([[0, '#ffffff'], [1, '#e2e6f2']], 0, 0, 1, 1), stroke(3.5, '#2a2f45'));
  p.path('M30 40 L30 52 M44 42 L44 54 M56 46 L56 56', 'none', 'stroke="#9aa2bb" stroke-width="2.5" stroke-linecap="round"');
  p.path('M24 66 Q40 74 62 68', 'none', 'stroke="#ff7a5a" stroke-width="5" stroke-linecap="round"');
  out.push(p.sprite('UI/finger'));

  // ---- 스플래시 엠블럼 (512x512): 파랑·빨강으로 갈린 쿼터뷰 섬 위에 삼원색 원소가 모여 빛나는 별
  {
    p = new Pic(512, 512);
    const cx = 256, cy = 300;
    // 뒤쪽 후광과 빛줄기
    p.path(ell(cx, cy - 40, 230, 230), p.rad([[0, '#fff6c8', 0.55], [0.5, '#9fd8ff', 0.18], [1, '#9fd8ff', 0]]));
    for (let k = 0; k < 12; k++)
      p.add(`<path d="${poly([[cx - 10, cy - 60], [cx + 10, cy - 60], [cx, cy - 290]])}" fill="#fffbe8" opacity="0.18" transform="rotate(${k * 30} ${cx} ${cy - 60})"/>`);
    // 섬: 윗면 마름모(왼쪽 파랑, 오른쪽 빨강) + 모래 옆면
    const hw = 170, hh = 85, d = 46;
    const L = [cx - hw, cy], T = [cx, cy - hh], R = [cx + hw, cy], B = [cx, cy + hh];
    const down = ([x, y]) => [x, y + d];
    p.path(poly([L, B, down(B), down(L)]), p.lin([[0, '#f3c766'], [1, '#c98a3a']]), stroke(4, '#5a3a1a'));
    p.path(poly([B, R, down(R), down(B)]), p.lin([[0, '#d89c45'], [1, '#a8692c']]), stroke(4, '#5a3a1a'));
    p.path(poly([L, B, [B[0], B[1] + 12], [L[0], L[1] + 12]]), '#2a6ad0', 'opacity="0.9"');
    p.path(poly([B, R, [R[0], R[1] + 12], [B[0], B[1] + 12]]), '#c43838', 'opacity="0.9"');
    p.path(poly([L, T, B]), p.lin([[0, '#7ab8ff'], [1, '#2f7fe8']], 0, 0, 1, 1), stroke(4, '#1a3a7a'));
    p.path(poly([T, R, B]), p.lin([[0, '#ff9a88'], [1, '#e2463f']], 0, 0, 1, 1), stroke(4, '#7a1a1a'));
    p.path(`M${L[0] + 16} ${L[1]} L${T[0]} ${T[1] + 8} L${R[0] - 16} ${R[1]}`, 'none', 'stroke="#ffffff" stroke-width="5" stroke-linecap="round" opacity="0.6"');
    // 세 원소 (불꽃·물방울·번개) 가 별을 둘러싼다
    const orb = (x, y, c1, c2) => p.path(ell(x, y, 48, 48), p.rad([[0, c1], [1, c2]], 0.38, 0.32, 0.7), stroke(5, '#2a1838'));
    orb(cx - 112, cy - 150, '#ffd0a0', '#e2381a');
    orb(cx + 112, cy - 150, '#fff3a0', '#e8a01f');
    orb(cx, cy - 20, '#bfe8ff', '#1f6fd8');
    // 불꽃
    p.path(`M${cx - 112} ${cy - 118} C${cx - 140} ${cy - 120} ${cx - 140} ${cy - 160} ${cx - 122} ${cy - 172} C${cx - 122} ${cy - 158} ${cx - 112} ${cy - 160} ${cx - 108} ${cy - 186} C${cx - 92} ${cy - 168} ${cx - 84} ${cy - 140} ${cx - 112} ${cy - 118} Z`, '#fff3a0', stroke(3, '#7a1a08'));
    // 번개
    p.path(poly([[cx + 118, cy - 192], [cx + 96, cy - 146], [cx + 112, cy - 146], [cx + 104, cy - 108], [cx + 130, cy - 160], [cx + 114, cy - 160]]), '#ffffff', stroke(3, '#8a4a00'));
    // 물방울
    p.path(`M${cx} ${cy - 58} C${cx + 8} ${cy - 40} ${cx + 22} ${cy - 30} ${cx + 22} ${cy - 14} C${cx + 22} ${cy + 2} ${cx + 12} ${cy + 10} ${cx} ${cy + 10} C${cx - 12} ${cy + 10} ${cx - 22} ${cy + 2} ${cx - 22} ${cy - 14} C${cx - 22} ${cy - 30} ${cx - 8} ${cy - 40} ${cx} ${cy - 58} Z`, '#ffffff', stroke(3, '#123a7a'));
    // 세 원소를 잇는 빛의 고리 + 가운데 별
    p.path(ell(cx, cy - 106, 128, 64), 'none', 'stroke="#ffffff" stroke-width="5" stroke-dasharray="14 10" opacity="0.7"');
    p.path(star(cx, cy - 128, 46, 14), p.rad([[0, '#ffffff'], [1, '#ffe27a']]), stroke(4, '#8a5a00'));
    p.path(star(cx, cy - 128, 18, 6), '#ffffff');
    out.push(p.sprite('UI/logo_emblem'));
  }

  // ---- 카드 아이콘 (128x128)
  out.push(icon('icon_red', (p) => {
    p.path(ell(64, 70, 34, 34), p.rad([[0, '#fff3a0'], [0.5, '#ff9a2a'], [1, '#e2381a']], 0.4, 0.4, 0.6), stroke(4, '#7a1a08'));
    p.path('M40 52 C34 30 52 26 50 8 C70 24 64 38 74 40 C78 30 88 30 90 22 C98 44 92 56 88 62', p.lin([[0, '#ffe36a'], [1, '#ff6a2a']]), stroke(4, '#7a1a08'));
    p.path(ell(52, 62, 10, 7), '#ffffff', 'opacity="0.6"');
  }));
  out.push(icon('icon_blue', (p) => {
    p.path('M10 96 C20 60 46 30 82 30 C108 30 120 48 116 66 C104 52 84 52 76 66 C70 78 82 90 96 88 C80 104 40 108 10 96 Z',
      p.lin([[0, '#bfe8ff'], [0.5, '#45a8ff'], [1, '#1f5fc8']], 0, 0, 1, 1), stroke(4, '#123a7a'));
    p.path('M30 84 C40 62 58 46 80 42', 'none', 'stroke="#ffffff" stroke-width="5" stroke-linecap="round" opacity="0.8"');
    p.path(ell(100, 40, 5, 5), '#ffffff');
  }));
  out.push(icon('icon_yellow', (p) => {
    p.path('M22 64 C8 64 10 40 28 42 C28 20 56 14 64 30 C74 14 104 22 100 44 C118 44 118 66 100 66 Z',
      p.lin([[0, '#e6e8f2'], [1, '#8a8fa8']]), stroke(4, '#3a3f55'));
    for (const [x, y, s] of [[44, 88, 1.1], [78, 92, 0.9]])
      p.path(poly([[x + 4 * s, y - 22 * s], [x - 10 * s, y + 2 * s], [x - 1 * s, y + 2 * s], [x - 5 * s, y + 22 * s], [x + 10 * s, y - 4 * s], [x + 1 * s, y - 4 * s]]),
        '#ffd21f', stroke(3, '#8a4a00'));
  }));
  out.push(icon('icon_warp', (p) => {
    p.path(ell(64, 64, 46, 46), p.rad([[0, '#1a0a3a'], [0.6, '#6a3ac8'], [1, '#c8a8ff']]), stroke(4, '#24145a'));
    for (let k = 0; k < 3; k++)
      p.add(`<path d="M64 64 m-30 0 a30 30 0 0 1 50 -18" fill="none" stroke="#f0e2ff" stroke-width="5" stroke-linecap="round" transform="rotate(${k * 120} 64 64)"/>`);
    p.path(star(64, 64, 12, 4), '#ffffff');
  }));
  out.push(icon('icon_lock', (p) => {
    p.path('M38 60 V42 A26 26 0 0 1 90 42 V60', 'none', 'stroke="#3a3f55" stroke-width="16" stroke-linecap="round"');
    p.path('M38 60 V42 A26 26 0 0 1 90 42 V60', 'none', 'stroke="#d8deef" stroke-width="9" stroke-linecap="round"');
    p.path(rect(24, 56, 80, 60, 12), p.lin([[0, '#ffe07a'], [1, '#e0962a']]), stroke(4, '#5a3a1a'));
    p.path(ell(64, 80, 8, 8), '#5a3a1a');
    p.path('M64 84 V100', 'none', 'stroke="#5a3a1a" stroke-width="7" stroke-linecap="round"');
  }));
  out.push(icon('icon_bleach', (p) => {
    p.path('M64 14 C74 34 98 54 98 80 C98 102 82 116 64 116 C46 116 30 102 30 80 C30 54 54 34 64 14 Z',
      p.lin([[0, '#ffffff'], [1, '#9a96a8']], 0, 0, 1, 1), stroke(4, '#4a4652'));
    p.path('M44 76 Q46 58 58 46', 'none', 'stroke="#ffffff" stroke-width="6" stroke-linecap="round"');
    p.path(star(92, 30, 16, 5), '#ffffff', stroke(2, '#8a86a0'));
  }));
  out.push(icon('icon_mine', (p) => {
    p.path(ell(64, 76, 40, 34), p.rad([[0, '#7a7484'], [1, '#2a2630']], 0.4, 0.35, 0.7), stroke(4, '#141018'));
    for (let k = 0; k < 6; k++) p.add(`<path d="M64 76 m0 -40 v-10" stroke="#2a2630" stroke-width="7" stroke-linecap="round" transform="rotate(${k * 60 - 30} 64 76)"/>`);
    p.path(ell(64, 70, 12, 10), p.rad([[0, '#ffd0c0'], [0.5, '#ff3a2a'], [1, '#a01010']]), stroke(3, '#3a0808'));
    p.path(ell(48, 62, 7, 5), '#ffffff', 'opacity="0.45"');
  }));
  out.push(icon('icon_transmute', (p) => {
    // 세 색 구슬을 도는 화살표 (카드 아트 칸이 바꿀 색으로 물든다)
    p.path(ell(64, 64, 44, 44), p.rad([[0, '#ffffff'], [1, '#d8d4ea']]), stroke(4, '#3a3456'));
    for (let k = 0; k < 2; k++)
      p.add(`<path d="M64 64 m-32 0 a32 32 0 0 1 56 -16" fill="none" stroke="#3a3456" stroke-width="7" stroke-linecap="round" transform="rotate(${k * 180} 64 64)"/>`
        + `<path d="M64 64 m28 -26 l-2 14 l-13 -5 z" fill="#3a3456" transform="rotate(${k * 180} 64 64)"/>`);
    for (const [dx, dy, c] of [[0, -14, '#ff5a3a'], [-12, 8, '#3a9aff'], [12, 8, '#ffd21f']])
      p.path(ell(64 + dx, 64 + dy, 10, 10), c, stroke(2.5, '#3a3456'));
  }));
  out.push(icon('icon_echo', (p) => {
    // 불씨 하나에서 퍼지는 메아리 고리
    for (const [r, o] of [[52, 0.35], [40, 0.6]])
      p.path(ell(64, 70, r, r * 0.8), 'none', `stroke="#ffe08a" stroke-width="6" opacity="${o}"`);
    p.path('M64 30 C78 48 88 60 88 78 C88 94 77 104 64 104 C51 104 40 94 40 78 C40 60 50 48 64 30 Z',
      p.lin([[0, '#fff2a8'], [0.5, '#ff9a3a'], [1, '#e0401a']]), stroke(4, '#6a1a08'));
    p.path('M64 62 C70 70 74 76 74 84 C74 91 69 96 64 96 C59 96 54 91 54 84 C54 76 58 70 64 62 Z', '#fff6c8');
  }));
  out.push(icon('icon_beacon', (p) => {
    p.path(ell(48, 112, 26, 9), p.lin([[0, '#b0a898'], [1, '#6a6458']]), stroke(3, '#2a2620'));
    p.path('M48 112 V18', 'none', 'stroke="#4a3018" stroke-width="10" stroke-linecap="round"');
    p.path('M48 112 V18', 'none', 'stroke="#d09a5a" stroke-width="5" stroke-linecap="round"');
    p.path('M50 20 C68 12 80 28 108 20 L100 44 L108 68 C80 76 68 60 50 68 Z', p.lin([[0, '#ffe07a'], [1, '#f0a020']]), stroke(4, '#6a3a08'));
    p.path(star(76, 44, 12, 5), '#ffffff');
    p.path(ell(48, 16, 7, 7), '#ffd84a', stroke(3, '#7a5010'));
  }));

  // ---- 메타 아이콘: 재화(크로마 결정), 덱, 퀘스트
  out.push(icon('icon_gem', (p) => {
    // 세 원소 색이 도는 다면체 보석
    p.path(poly([[64, 12], [106, 44], [64, 118], [22, 44]]), p.lin([[0, '#ffb0e8'], [0.5, '#8a6aff'], [1, '#3ac8ff']], 0, 0, 1, 1), stroke(5, '#2a1858'));
    p.path(poly([[22, 44], [106, 44], [64, 118]]), p.lin([[0, '#6a4ae0'], [1, '#2a8ad8']], 0, 0, 0, 1), 'opacity="0.55"');
    p.path(poly([[42, 44], [64, 12], [86, 44], [64, 118]]), 'none', 'stroke="#ffffff" stroke-width="3" opacity="0.7"');
    p.path('M22 44 H106', 'none', 'stroke="#2a1858" stroke-width="4"');
    p.path(poly([[46, 26], [60, 18], [50, 40]]), '#ffffff', 'opacity="0.85"');
    p.path(star(98, 22, 12, 4), '#ffffff');
  }));
  out.push(icon('icon_deck', (p) => {
    // 부채꼴로 겹친 카드 석 장
    for (const [rot, c1, c2] of [[-18, '#7a8ad8', '#3a4a98'], [0, '#ff9a6a', '#d04a2a'], [18, '#ffe07a', '#e0962a']])
      p.add(`<g transform="rotate(${rot} 64 112)">`
        + `<path d="${rect(38, 22, 52, 76, 9)}" fill="${c1}" stroke="#2a2440" stroke-width="4"/>`
        + `<path d="${rect(45, 30, 38, 34, 5)}" fill="${c2}" opacity="0.8"/></g>`);
    p.path(star(64, 47, 10, 4), '#ffffff');
  }));
  out.push(icon('icon_quest', (p) => {
    // 두루마리 + 체크
    p.path(rect(26, 18, 76, 96, 10), p.lin([[0, '#fff6dc'], [1, '#e8cf98']]), stroke(4, '#6a4a1a'));
    p.path(ell(64, 18, 40, 8), p.lin([[0, '#e0b060'], [1, '#a8742a']]), stroke(3, '#6a4a1a'));
    p.path(ell(64, 114, 40, 8), p.lin([[0, '#e0b060'], [1, '#a8742a']]), stroke(3, '#6a4a1a'));
    for (const y of [44, 64, 84]) p.path(`M58 ${y} H90`, 'none', 'stroke="#b08a4a" stroke-width="5" stroke-linecap="round"');
    p.path('M34 64 L44 74 L60 50', 'none', 'stroke="#2aa84a" stroke-width="8" stroke-linecap="round" stroke-linejoin="round"');
    p.path(ell(42, 44, 5, 5), '#e0962a');
    p.path(ell(42, 84, 5, 5), '#e0962a');
  }));

  return out;
}

