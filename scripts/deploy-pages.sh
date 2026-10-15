#!/usr/bin/env bash
# 배포용 WebGL 빌드(build/webgl-release)를 GitHub Pages 용 gh-pages 브랜치로 올린다.
#   bash scripts/deploy-pages.sh            # origin 저장소로
#   bash scripts/deploy-pages.sh <remote-url>
# 먼저 배포용 빌드를 만들 것: Unity -executeMethod ChromaFrontier.Editor.BuildScript.BuildWebGLRelease
# gh-pages 브랜치는 빌드 결과물만 담은 단일 커밋으로 매번 덮어쓴다 (소스 브랜치와 이력이 섞이지 않게).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
SRC="$ROOT/build/webgl-release"
REMOTE="${1:-$(git -C "$ROOT" remote get-url origin)}"

if [ ! -f "$SRC/index.html" ]; then
  echo "[deploy] $SRC/index.html 이 없습니다. 먼저 BuildWebGLRelease 로 빌드하세요." >&2
  exit 1
fi

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT
cp -r "$SRC/." "$TMP/"
touch "$TMP/.nojekyll" # Jekyll 처리를 끄면 밑줄로 시작하는 파일도 그대로 서비스된다

cd "$TMP"
git init -q
git checkout -q -b gh-pages
git add -A
git -c user.name="$(git -C "$ROOT" config user.name || echo deploy)" \
    -c user.email="$(git -C "$ROOT" config user.email || echo deploy@localhost)" \
    commit -q -m "Deploy WebGL build ($(date '+%Y-%m-%d %H:%M'))"
git push -f "$REMOTE" gh-pages
echo "[deploy] gh-pages 브랜치로 올렸습니다: $REMOTE"
