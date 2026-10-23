# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

사용자와의 대화는 항상 한국어로 한다. 기획 기준 문서는 `PRD.md`.

## 개요

**크로마 프론티어 (Chroma Frontier)**(가제): 쿼터뷰 픽셀 영토전. 턴제 전략 땅따먹기 퍼즐 PvE이고, 엔진은 Unity 6000.3.25f1이다.
- 최종 목표는 **안드로이드 APK**다. 진행도, 덱, 해금은 기기에 저장하고 오프라인으로도 다 할 수 있다. 웹 버전은 **Firebase**(프로젝트 `chroma-frontier`)로 로그인·클라우드 저장·PvP 대전을 한다(아래 온라인). Android 의 온라인 기능은 아직 없다.
- 지금은 **WebGL 빌드를 브라우저에서 테스트하는 단계**다. 화면은 세로(9:16)다.
- `게임컨셉시안.jpg`는 분위기를 잡는 참고용이다. 시안에 있는 Shield/Heal 카드, 하트, 코인/물 재화는 PRD에 없으므로 PRD를 따른다.

## 툴체인

도구는 이 저장소에 없고 **Gate Defense 프로젝트의 `C:\Project\Gate Defense\tools`를 공유**한다. 설치와 복구는 그 프로젝트의 `tools\install-tools.ps1`로 한다.
- `call env.bat`을 실행하면 `UNITY_EXE`, `JAVA_HOME`, `ANDROID_HOME`, `DOTNET_ROOT` 등이 설정된다. 도구 위치는 `CF_TOOLS`로 바꿀 수 있다.
- Android JDK/SDK/NDK는 Unity 모듈에 내장된 것(`Editor/Data/PlaybackEngines/AndroidPlayer`)을 쓴다.
- Unity 배치 모드에는 Personal 라이선스가 필요하다. 라이선스는 `Unity Hub.exe`에 로그인해서 활성화한다.

## 명령

Unity CLI 인자는 Windows 절대 경로여야 한다. Git Bash에서는 `cygpath -w`로 바꾼다.

```bash
T="/c/Project/Gate Defense/tools"

# Core 규칙 테스트: Unity 없이 돌고 가장 빠르다 (Core 소스와 NUnit 테스트를 그대로 컴파일)
"$T/dotnet/dotnet.exe" test tests/Core.Tests
"$T/dotnet/dotnet.exe" test tests/Core.Tests --filter "FullyQualifiedName~RulesTests.Red_ClaimsCross"

# WebGL 개발 빌드 → build/webgl
"$T/unity/6000.3.25f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath "<abs>\client" \
  -executeMethod ChromaFrontier.Editor.BuildScript.BuildWebGL -logFile "<abs>\build\unity-webgl.log"
# Android는 같은 방식으로 ChromaFrontier.Editor.BuildScript.BuildAndroid → build/android/ChromaFrontier.apk

# Unity EditMode 테스트
"$T/unity/6000.3.25f1/Editor/Unity.exe" -batchmode -nographics -projectPath "<abs>\client" \
  -runTests -testPlatform EditMode -testResults "<abs>\build\editmode-results.xml" -logFile "<abs>\build\unity-tests.log"

# WebGL 실행: 0.0.0.0에 바인딩하므로 같은 Wi-Fi의 휴대폰에서도 접속할 수 있다 (두 번째 인자로 폴더 지정)
"$T/node/node.exe" scripts/serve-webgl.mjs 8080
"$T/node/node.exe" scripts/serve-webgl.mjs 8081 build/webgl-release   # 배포용 빌드 확인

# 배포용 WebGL 빌드 → build/webgl-release (약 7.6MB, 3~4분) → GitHub Pages(gh-pages 브랜치)로 올리기
"$T/unity/6000.3.25f1/Editor/Unity.exe" -batchmode -nographics -quit -projectPath "<abs>\client" \
  -executeMethod ChromaFrontier.Editor.BuildScript.BuildWebGLRelease -logFile "<abs>\build\unity-webgl-release.log"
bash scripts/deploy-pages.sh

# 크로마 첨탑 퍼즐 후보 생성 (도구, [Explicit]) → build/puzzle-candidates.txt. 단계 1~6, 6단계는 수십 분 걸린다
CF_GEN_TIER=3 "$T/dotnet/dotnet.exe" test tests/Core.Tests --filter "FullyQualifiedName~PuzzleGen"

# 아트 재생성 → client/Assets/Resources/Art/**.png + build/art-preview.png (약 2초, 시스템 Chrome 필요)
"$T/node/node.exe" art/build.mjs
```

- 빌드 성공 여부는 로그의 `[ChromaFrontier] WebGL build Succeeded`로 판정한다.
- 컴파일 오류는 로그에서 `error CS`로 찾는다.
- 첫 빌드는 약 5분, 이후 증분 빌드는 약 2분 걸린다.
- 개발 빌드는 압축 없이 32MB다.
- 배포용(`BuildWebGLRelease`)은 Gzip 압축에 `decompressionFallback`을 켠다. GitHub Pages처럼 Content-Encoding 헤더를 안 붙이는 정적 호스팅에서도 로더가 브라우저에서 직접 풀기 때문이다. 배포용은 개발 빌드가 아니라서 `?unlock`/`?reset`/U 키가 꺼진다.
- 배포:
  - 배포용 빌드는 템플릿의 `net.js`(온라인)도 내용 해시 이름 `Build/net.<해시>.js` 로 옮기고 index.html 을 고친다(`BuildScript.HashNetScript`). 다른 빌드 파일과 같은 이유다.
  - 저장소는 https://github.com/mhsssshin/ChromaFrontier 다.
  - 소스는 `main`, 빌드 결과물은 `gh-pages` 브랜치에 둔다. `scripts/deploy-pages.sh`가 매번 단일 커밋으로 덮어쓴다.
  - 배포용 빌드는 파일 이름을 내용 해시로 짓는다(`nameFilesAsHashes`). 이름이 같으면 이전 버전을 받아 둔 브라우저가 옛 파일과 새 파일을 섞어 실행해 `RangeError: Maximum call stack size exceeded`로 시작하지 못한다(실제로 겪음).
  - GitHub Pages는 index.html을 최대 10분 캐시한다. 그래서 배포 스크립트는 직전 배포의 index.html이 가리키던 `Build/` 파일을 함께 남긴다.
  - `mhsssshin.github.io`의 localStorage는 사용자의 다른 Pages 프로젝트와 공유된다. 그래서 키는 반드시 `cf.` 접두사를 쓰고, `LocalStore.Clear()`도 `cf.`로 시작하는 것만 지운다.
  - 서비스 주소는 https://mhsssshin.github.io/ChromaFrontier/ 다.
  - Unity 빌드에 로컬 라이선스가 필요해서 CI가 아니라 로컬에서 만들어 올린다.
- `ProjectSettings`가 없는 상태로 Unity를 처음 열면 `Packages/manifest.json`이 기본 템플릿(Purchasing, Analytics 등)으로 덮어써진다. 그러면 WebGL 빌드가 51MB까지 커진다. 최소 구성(31MB)을 유지할 것.

## 아트 파이프라인

- 그림체는 `게임컨셉시안.jpg`를 따른다. 통통한 입체 타일(풀, 파랑, 빨강 윗면에 모래색 옆면), 귀여운 원소 유닛, 나무와 바위 장식, 바다 배경을 쓰는 매끈한 카툰 스타일이다. PRD는 "픽셀 아트"라고 하지만 사용자가 시안 스타일로 정했다.
- 에셋은 모두 `art/*.mjs`에서 **코드로 그린다**.
  - 그리는 방식: SVG 문자열 → 헤드리스 Chrome으로 시트 한 장에 렌더링(`render-svg.mjs`) → 잘라서 PNG로 저장.
  - PNG를 직접 고치지 말고, 정의를 고친 뒤 다시 굽는다. 결과는 `build/art-preview.png`를 열어 눈으로 확인한다.
  - 정의 파일:
    - `kit.mjs`: 색 섞기(`mix`/`dark`/`light`), 그라데이션이나 필터를 쓰는 `Pic` 클래스, 둥근 도형
    - `tiles.mjs`: 타일 4종, 내 땅 스킨(`SKIN_MATERIALS` 7 × `SKIN_COLORS` 9, Core `TileSkins` 와 id·색을 맞춤 → `Tiles/skin_<재질>_<색>`, 기본 재질의 파랑·빨강은 `tile_player`·`tile_ai`), 윗면 오버레이(glow/select/lock/shield), 장식, 바다, 거품. 보드 테마(`THEMES`, id는 Core `ThemeShop`과 맞춤)마다 풀밭 두 장(`tile_grass_a_<id>`/`_b_<id>`), 장식 4종(`Decor/<이름>_<id>`), 바다(`Bg/sea_<id>`, 빌드 크기 때문에 반 해상도 270x480)를 따로 굽는다
    - `units.mjs`: 원소 유닛 9종, 팀 링, 지뢰, 거점 깃발
    - `units_halloween.mjs`: 유닛 스킨 세트 "할로윈"(`Units/unit_<원소>_halloween` 9종: 잭오랜턴·물귀신·묘비 번개·마녀 모자 회오리·박쥐 구름·마녀 솥·유령 촛불·검은 고양이·묘비 석상). 
    - `units_jeheon.mjs`: 유닛 스킨 세트 "제헌절"(`_jeheon`: 청사초롱·갓 쓴 물방울·헌법 책 번개·무궁화 회오리·무궁화 구름·놋쇠 화로·달항아리·태극 구(건곤감리)·비석). 글자는 쓰지 않는다
    - 새 세트는 같은 방식으로 파일을 더하고 build.mjs 와 Core `UnitSkins.All`(id·이름·값·설명·꼬리표 `Tag`·카드 색 `HexA`)에 맞춰 넣는다. 상점 유닛 탭은 세트 3개까지 한 화면에 들어간다(더 늘리면 쪽 넘기기가 필요)
    - `fx.mjs`: 프레임 이펙트 `<이름>_<n>`
    - `emote_packs.mjs`: 상점 이모티콘 팩 3종(`UI/emote_8..31`, 번호 = 팩 순서 × 8 + 칸): 킹받는 팩(메롱·어쩔티비·깐족·선글라스 ㅋ·응 아니야·하품·짝짝짝·그걸 놓네?), 귀요미 팩(부끄·반짝·쪽·쿨쿨·꽃·브이·사랑해·화이팅), 멘붕 팩(멘붕·영혼 탈출·눈물바다·폭발·해탈·흰자·띠용·털썩). 원소 몸통(`BODIES`)에 눈·입 조각을 조합해 그린다. 순서와 문구는 Core `EmotePacks` 와 맞춘다
    - `emotes.mjs`: 대전 이모티콘 스티커 8종(`UI/emote_0..7`, 160x160, 흰 스티커 테두리), 말풍선 꼬리 `UI/bubble_tail`, 버튼 아이콘 `UI/icon_emote`
    - `ui.mjs`: 카드 틀, 아이콘(카드, 결정 `icon_gem`, 덱, 퀘스트, 첨탑 `icon_tower`, 대전 `icon_pvp`, 상점 `icon_shop`, 계정 `icon_user`, 싱글 `icon_single`), 반투명 재료(짙은 유리 `panel` 28, 밝은 유리 `glass` 28, 캡슐 `pill` 22, 모드 카드 `mode_card` 28, 후광 `glow`, 위·아래 그늘 띠 `fade_top`/`fade_bottom` — `Hud.Fill` 로 늘려 그림), 바, 버튼, 패널, 밝은 말풍선(`bubble`, 튜토리얼), 스플래시 엠블럼
    - `logo.mjs`: 영문 로고 "CHROMA FRONTIER"(`UI/logo_title`). 스플래시와 대기실에 쓰고, 빌드할 때 WebGL 템플릿(`WebGLTemplates/Portrait/logo.png`, 웹 로딩 화면)에도 복사한다.
      - 글꼴은 `art/vendor/fonts/Jua-Regular.ttf`(SIL OFL, `Jua-OFL.txt`)다. base64로 SVG에 넣고, Chrome은 `--virtual-time-budget`으로 글꼴이 다 불러와진 뒤 찍는다.
      - 게임 제목은 한글 텍스트 대신 이 로고 이미지로 보여 준다.
- **캔버스 규칙은 Unity 임포터와 맞물린다.** 타일은 136x104에 윗면 중심이 (68, 34), 유닛은 96x112에 바닥점이 (48, 100), 장식은 64x80에 바닥점이 (32, 72)다. `Editor/ArtImporter.cs`가 이 값으로 피벗을 정한다. PPU 64, 밉맵 없음, 무압축, Bilinear다. 캔버스를 바꾸면 임포터 상수도 같이 바꾼다.
- 런타임 로드는 `Art.Sprite("폴더/이름")`, `Art.Texture(...)`(HUD용), `Art.Anim(prefix, 프레임 수)`로 한다. 프레임 수를 바꾸면 `FxView`의 상수도 맞춘다.
- UI 텍스처는 기준 좌표(540x960)의 2배 해상도로 그린다. `Hud.Nine(rect, tex, border)`의 border(텍스처 px)는 `ui.mjs` 주석의 9-slice 값과 같아야 한다.

## 아키텍처

```
client/Assets/Scripts/
  Core/    ChromaFrontier.Core   — 순수 C# (noEngineReferences). 게임 규칙의 단일 진실 공급원
  Client/  ChromaFrontier.Client — Unity 뷰와 HUD. Core 상태를 읽기만 함
  Editor/  ChromaFrontier.Editor — ProjectSetup(씬과 PlayerSettings를 코드로 관리), BuildScript(CLI 빌드)
client/Assets/Tests/Core/        — NUnit 테스트. Unity Test Runner와 tests/Core.Tests(dotnet) 양쪽에서 실행
client/Assets/WebGLTemplates/Portrait/ — 9:16 비율로 창에 맞추는 WebGL 템플릿
```

**Core**
- Core는 UnityEngine을 참조하지 않는다. dotnet 테스트 프로젝트가 같은 소스를 C# 9로 컴파일하므로, C# 10 이상의 문법이나 Unity API를 쓰면 한쪽이 깨진다.
- Core는 결정적이어야 한다. 난수는 자체 `Rng`(xorshift, 복제 가능)만 쓴다.
- `Game`이 한 판의 권위 상태다.
  - 수는 `Play(handIndex, cell, blueDir?)`(유닛), `Cast(handIndex, target, target2)`(스킬), `Mulligan()`으로만 둔다. 턴을 끝내는 것은 유닛과 패 버리기뿐이다. 턴이 끝나면 손패를 채우고 보호막·잠금 턴을 줄인다(`Board.TickTimers`).
  - **스킬은 턴을 쓰지 않는 보조 행동**이다. 한 턴에 한 장까지(`Game.SkillUsed`, 턴이 끝나면 초기화) 쓰고, 이어서 유닛을 놓거나 패 버리기를 한다. 예전에는 스킬이 턴을 통째로 써서 배치에 밀려 거의 안 쓰였다.
  - 대상 검증은 `CanPlace`(유닛), `CanTarget`/`CanCast`(스킬)가 한다. `Resolver`는 검증하지 않는다.
  - `CloneFor(viewer)`는 viewer가 모르는 상대 지뢰를 지운 복제본이다. AI와 물 방향 계산은 반드시 이걸로 시뮬레이션해서 숨은 지뢰를 엿보지 않게 한다.
  - `Turn`은 양측을 합친 턴 수이고 `MaxTurns`(스테이지 규칙, 기본 20)가 되면 끝난다. 흑색의 추가 턴은 `Turn`을 늘리지 않는다.
  - 플레이어 → AI가 한 라운드다. 라운드가 끝날 때(`EndRound`) 맵 기믹이 움직인다. 순서는 컨베이어 이동 → 블랙홀 흡입 → 거점 확장 → (`StageRules.LavaEvery`마다) 화산 분출이다. 화산은 양쪽에서 각각 무작위 유닛 `LavaCount`개를 터뜨린다(유닛 제거, 칸은 주인 없음, 보호된 칸은 제외). 칸이 막히지는 않고 한 번 터지면 끝이다. 일어난 일은 `Game.RoundEvents`에 남아 뷰가 연출한다.
  - 차례가 된 쪽이 유닛을 놓을 칸이 하나도 없으면(`LegalCells` 비어 있음) 그 자리에서 끝나고 점유 칸 수로 승패를 가린다(`Game.Stuck`). 잠금은 곧 풀리므로 **잠금 때문에만 막힌 것은 치지 않는다**(`CanPlaceIgnoringLock`). 그 턴에는 스킬이나 패 버리기를 하면 된다. 공간 왜곡 직후에도 검사한다.
  - `Clone()`은 보드, 덱, Rng를 통째로 복제한다. AI와 물 방향 계산이 이걸로 시뮬레이션한다.
- `Resolver.PlaceUnit`이 원소 반응의 전부다. 순서는 다음과 같다.
  0. 얼음 칸이면 밀고 나온 방향으로 한 칸 더 미끄러진다(`SlideDir`, `CanLand`). 실제로 놓인 칸은 `PlayResult.At`이다.
  1. 배치한 칸을 점유한다.
  2. 기본 효과를 낸다.
     - 빨강: 십자로 퍼진다.
     - 파랑: 직선으로 퍼진다.
     - 노랑: 반경 안의 무작위 칸을 점유한다.
  3. 상하좌우 이웃과 융합한다. 색은 비트 마스크(`Element.Colors()`: 빨 1, 파 2, 노 4)로 다룬다. 2단계 유닛은 색 두 개를 품은 촉매다.
     - 놓은 원소와 이웃 1~2개의 색을 합쳐 세 색이 다 모이면 3단계가 된다. 촉매 중에 상대 유닛이 있으면 흑색이고, 없으면 백색이다.
     - **3차 조합:** 2단계 옆에 남은 한 색만 놓아도 3단계가 된다. 이렇게 2단계 촉매를 쓰면 `PlayResult.Boosted`가 켜지고, 효과 반경이 `Balance.Tier3Boost`(+1)만큼 커진다.
     - 조합 후보가 여럿이면 흑색 > 2단계 촉매 사용 > 촉매 적게 순으로 고른다.
     - 3단계가 안 되면, 다른 1단계 원소 이웃 하나와 2단계(초록, 보라, 주황)를 만든다. 3단계와 회색은 더 이상 촉매가 아니다.
     - **공명:** 융합할 수 없고 같은 원소 이웃이 있으면 그 이웃을 소모해 기본 효과를 강화한다(`PlayResult.Resonance`). 판정 순서는 `FindFusion` → 공명 → 기본 효과 → `ApplyFusion`이다.
       - 화염+화염 = 업화: 3x3 + 십자 2칸
       - 물+물 = 해일: 길이 4, 폭 3 (`Resolver.WaterPath`)
       - 번개+번개 = 뇌우: 무작위 8칸, 반경 3
     - 침묵 지대(`Terrain.Silence`)에 놓은 유닛은 융합도 공명도 하지 않는다. 침묵 지대에 선 유닛은 촉매가 되지 못한다.
  - 촉매로 쓴 이웃 유닛은 소모된다. 촉매를 고를 때는 상대 유닛을 우선한다.
  - 반응에 휘말린 칸은 모두 카드를 낸 쪽의 것이 된다.
  - 보호(`Tile.Shield` > 0)된 칸은 소유자가 아닌 쪽이 바꿀 수 없다. 보라 침묵과 백색 성역이 이 보호를 쓴다.
- 밸런스 수치는 전부 `Balance` 상수에 모여 있다.
- **지형**(`Tile.Terrain`): 평지, 낭떠러지, 얼음, 침묵 지대, 블랙홀, 컨베이어가 있다.
  - `Tile.Blocked`는 계산되는 속성이다(낭떠러지, 블랙홀). 막힌 칸은 배치·점유가 안 되고 점유율 분모(`Board.Playable`)에서도 빠진다.
  - 맵은 `Board.Parse`의 ASCII 지도로 만든다. 기호: `.` 평지, `#` 낭떠러지, `~` 얼음, `s` 침묵, `O` 블랙홀, `>` 컨베이어(+X로 흐름), `P`/`A` 시작 땅.
- **스테이지**(`Stages.cs`): `StageCatalog.All`에 50개(5챕터 × 10, 각 챕터 10번째가 보스)가 있다. `StageDef`는 맵, `StageRules`, 상대 성향(`AiPersona`), 기본 난이도, AI 덱을 갖는다.
  - 1장 초원(기믹 없음, 7x7 → 8x8) → 2장 화산(유닛 무작위 폭발) → 3장 얼음성(얼음) → 4장 끊어진 다리(낭떠러지) → 5장 공허의 탑(9x9, 컨베이어·블랙홀·침묵, 22턴).
  - 보스: 1장 이그니스(화염 덱, 어그로), 2장 각성 이그니스, 3장 글라시아(물·잠금·탈색, 통제), 4장 호문클루스(약탈), 5장 각성 호문클루스.
  - 기본 난이도는 스테이지 0~14가 입문, 15~29가 쉬움, 30~44가 보통, 45 이상이 어려움이다. 보스는 한 단계 위다. 1장 일반 스테이지의 AI 덱은 유닛만 있다(스킬 없음). 
  - **모드**: 쉬움 → 보통 → 어려움(`StageCatalog.Modes`). 쉬움 모드 5-10을 깨면 보통 모드가, 보통 모드 5-10을 깨면 어려움 모드가 열리고 1-1부터 다시 한다. AI 난이도는 기본값에 모드별 −1/0/+1을 더하고, 앞 모드와 같아지지 않게 모드별 하한(입문/쉬움/보통)을 둔다(`StageCatalog.LevelForMode`). 판 보상은 보통 1.5배, 어려움 2배이고, 첫 클리어 보너스도 모드마다 다시 받는다. 예전의 대기실 난이도 보정은 없앴다.
  - 별(`StageCatalog.Stars`): 승리 1개, 점유율 50% 이상 2개, 65% 이상 3개.
  - 테스트 `EveryStage_StartsWithMovesForBothSides_AndPlaysToTheEnd`가 50개 스테이지를 모두 AI끼리 끝까지 돌려 본다. 맵을 고치면 이 테스트로 확인한다.
- 배치 규칙(`Game.CanPlace`, `Board.IsFrontline`): 빈 칸이어야 하고, 내 타일이거나 내 타일과 상하좌우로 맞닿은 칸이어야 한다.
- 카드(`Card`)는 1단계 원소 유닛이거나 스킬(`Skill`)이다. 원소 변환 카드는 바꿀 색을 `Card.Element`에 담는다(`Card.Transmute(e)`). 기본 덱(`Game.StarterDeck`)은 유닛 12장과 스킬 9장(왜곡·잠금·탈색·지뢰·재점화·거점 1장씩, 변환 3색)이다. AI 덱과 테스트가 이 덱을 쓴다. 플레이어 덱은 아래 덱 편성으로 정한다.
- 스킬은 전선 규칙 없이 보드 어디에나 쓴다. 효과는 `Resolver.CastSkill`에 있다. 대상 검증은 카드를 받는 `CanTarget(side, card, cell)`이다(변환은 색이 카드마다 달라서).
  - 공간 왜곡: 두 칸의 내용(소유, 유닛, 보호막, 잠금, 지뢰, 거점)을 맞바꾼다(`Tile.SwapContents`). 지형은 제자리에 남는다.
  - 영역 잠금: 3x3 칸의 `Tile.Lock`을 설정한다. 잠긴 칸에는 양쪽 모두 유닛을 놓을 수 없다.
    - 초반에는 쓸 수 없다(`StageRules.LockFromTurn`, 기본 6 = 각자 4번째 턴). 그 전에는 손패에 아예 들어오지 않는다(`Deck.Refill(holdLock)`가 덱에 남겨 둠). 시작하자마자 상대의 2x2 시작 땅을 잠가 판을 끝내는 것을 막기 위해서다.
  - 탈색: 유닛을 `Element.Grey`로 바꾼다. 회색 유닛은 융합 촉매가 될 수 없다.
  - 원소 변환: 1단계나 회색 유닛(상대 것도)을 카드의 색으로 바꾼다. 융합·흑색 판을 짜는 카드다. 바뀌기 전 원소는 `PlayResult.Bleached`에 남는다.
  - 재점화: 내 1단계 유닛이 그 자리에서 기본 효과를 한 번 더 낸다(공명·융합 없음, 유닛은 남음). 물은 가장 많이 먹는 방향으로 흐른다(`Game.BestEchoDirection`).
  - 원소 폭주(`Surge`, 상점에서 재점화를 대신함): 내 유닛(회색 제외) 주변 3x3 을 차지한다. 유닛은 남는다. Lv2 + 십자 2칸, Lv3 5x5(`Balance.SurgeRadius`).
  - 흡수(`Siphon`): 상대 1·2단계 유닛 하나와 그 칸을 빼앗는다(융합 재료로 쓰는 카드). Lv2 + 상하좌우 칸, Lv3 + 주변 3x3.
  - 방벽(`Barrier`): 내 땅 3x3 에 보호막(2턴). Lv2 3턴, Lv3 5x5 3턴.
  - 분열(`Split`): 내 1단계 유닛이 상하좌우 빈 칸(잠금·지뢰·상대 보호 없는 곳)에 같은 원소로 복제되고 그 칸을 차지한다(효과는 안 냄). 1/2/4칸. 공명·융합 판을 짜는 카드.
  - 지진(`Quake`): 3x3 안의 상대 유닛(보호 안 받는 것)을 부순다. 부술 유닛이 있어야 쓸 수 있다. Lv2 부순 칸 차지, Lv3 5x5.
  - 재점화(`Echo`)는 효과가 약하다는 의견에 상점·덱에서 빼고 원소 폭주로 바꿨다. 옛 저장의 `echo` 는 `surge` 로 옮겨 읽는다(`CardShop.Migrate`). 첨탑 퍼즐과 AI 덱에는 재점화가 그대로 있다.
  - 거점: 내 땅에 깃발(`Tile.Beacon`)을 꽂는다. 라운드 끝마다 주변 8칸 중 내 것이 아닌 칸을 `Balance.BeaconClaims`(1)개 무작위로 점유한다. 칸 주인이 바뀌면 무너진다(소유 변경은 `Tile.SetOwner`로 해야 거점이 같이 정리된다).
  - 잠복 지뢰: 유닛이 없는 칸이면 어디든(상대 땅, 빈 땅 포함) `Tile.Mine`을 설정한다. 상대 지뢰가 있는지는 모르므로 그 위에도 묻을 수 있고 덮어쓴다(내 지뢰 위에는 불가). 상대가 그 칸에 유닛을 놓으면 유닛은 사라지고 주변 3x3이 지뢰 주인의 것이 된다(`PlayResult.MineTriggered`).
  - 지속 효과(잠금, 보라 침묵, 백색 성역)는 "내 턴 기준"으로 센다. 타이머는 양측 턴마다 1씩 줄어들기 때문에 상수는 `턴 수 × Balance.TicksPerRound`(2)로 정의한다. 예를 들어 잠금 2턴은 상대 턴 2번 동안 유지되고, 그사이 내 다음 턴에도 잠겨 있다.
- **메타 성장**(`Meta.cs`): 재화(크로마 결정), 덱 편성, 카드 강화, 소모품, 퀘스트.
  - 덱 편성은 **스킬 카드만** 한다. 유닛은 원소별 4장 고정이고, 스킬은 `Collection.Slots`장까지(기본 `CardShop.SkillSlots` 9, 별의 길 보상으로 최대 11), 한 종류는 `CardShop.MaxCopies`(2)장까지다. 덱이 슬롯보다 적어도 된다.
  - `CardShop.All`: 13종의 값과 처음 가진 장수. 처음에는 변환 3색·탈색·지뢰·원소 폭주를 1장씩 갖고(스킬 6장, 총 18장), 거점(120)·공간 왜곡(160)·영역 잠금(200)·분열(110)·방벽(120)·흡수(150)·지진(170)은 결정으로 해금한다. 덱 편성·상점 스킬 카드 화면은 9장씩 쪽을 넘긴다(`Hud.CardPager`). 두 번째 장도 같은 값으로 산다.
  - `Collection`이 소장(종류별 장수), 덱, 강화 단계를 갖는다. `Serialize`/`Parse`로 저장(`owned|deck|levels`, 강화 전 두 칸짜리 저장도 읽음)하고, 가진 장수를 넘는 덱 카드는 읽을 때 버린다.
  - **카드 강화**: 종류마다 Lv1~`CardShop.MaxLevel`(3). 값은 한 장 값의 1.5배(Lv2), 3배(Lv3)(`UpgradePrice`). 효과는 `CardShop.LevelText`에 적혀 있고 실제 처리는 `Resolver.CastSkill(level)`, 지뢰는 터질 때 `PlaceUnit(mineLevel)`, 거점은 `Game.GrowBeacons`(단계 × 1칸)다. 판에는 `Loadout.Levels`로 넘기고 `Game.LevelOf(side, card)`로 읽는다. **AI 는 늘 Lv1**이다.
    - 변환·탈색: Lv2 바꾼 칸 점유, Lv3 + 상하좌우 점유. 재점화: Lv2 + 상하좌우 점유, Lv3 공명급(업화·해일·뇌우). 지뢰: 3x3 → 3x3+십자 2칸 → 5x5. 거점: 라운드마다 1/2/3칸. 공간 왜곡: Lv2 두 칸 점유, Lv3 + 두 칸의 상하좌우. 영역 잠금: 2/3/4턴(`Balance.LockTurnsAt`).
  - **소모품**(`BoostShop`): 판 시작 전 고르고 출격할 때 값을 낸다(이번 판에만). 개척(시작 땅 옆 3칸, 20), 목표 추가(게임 중 목표 4개, 15), 선공(첫 턴에 유닛 두 번, 25). `Loadout.Boosts` → `StageDef.CreateGame` / `Game.ApplyLoadout`. 무르기(15)는 사서 모아 두고 판 중에 쓴다(클라이언트가 처리).
  - 판 보상(`Rewards.ForBattle`): 패배 5, 승리 10 + 별당 5, 첫 클리어 +15(보스 +50), 게임 중 목표 하나당 +3. 카드를 금방 다 사서 쓸 곳이 없다는 의견에 퀘스트 보상과 함께 절반으로 낮췄다.
  - 퀘스트(`QuestCatalog.Lines`): 누적 기록(`Stat`: 판 수, 승리, 융합, 3단계, 공명, 상대 유닛 융합, 스킬, 차지한 칸, 게임 중 목표)으로 채우는 단계형 목표다. 보상을 받으면 같은 줄의 다음 단계(더 큰 목표)가 열린다. 퀘스트 창의 모두 받기(`Progress.ClaimAllQuests`)는 받을 수 있는 보상을 한 번에 받고, 기록이 여러 단계를 넘었으면 그 단계들까지 이어서 받는다.
- **크로마 첨탑**(`Puzzles.cs`, `PuzzleCatalog.cs`): 층마다 정해진 판·카드로 정해진 수 안에 **모든 칸을 내 색으로** 물들이는 퍼즐 모드. 30층이고 1층씩 연다.
  - 규칙은 `StageRules.Solo`: 상대는 두지 않고(`EndTurn`이 차례를 안 넘기고 라운드 기믹도 없음), 카드는 정해진 것만 한 번씩(`Deck(recycle: false)`), `MaxTurns` = 층의 `Moves`. 모든 칸을 차지하면 그 자리에서 `Game.Solved`, 둘 카드가 없으면 `OutOfCards`로 끝난다. 흑색의 추가 턴은 수를 쓰지 않는다.
  - 운이 끼지 않게 손패에 번개(무작위 낙뢰)·지뢰·잠금·거점을 넣지 않고, 첨탑에서는 번개 유닛 재점화도 막는다(`CanTarget`). 판에 미리 놓인 번개는 융합 재료로만 쓴다.
  - `PuzzleDef`: `Map`(Board.Parse 기호) + `Units`(r b y g v o w k x, 대문자=내 유닛) + `Hand`("R B Tb Bl Wp Ec") + `Moves`·`Name`·`Hint`.
  - `PuzzleSolver`: 모든 수(유닛 칸·물 방향·스킬 대상·왜곡 쌍)를 깊이 우선으로 두고 같은 상태는 한 번만 본다. `CountWinningFirstActions`(묘수가 몇 개인지), `GreedySolves`(욕심쟁이로 풀리면 쉬운 문제).
  - 층은 손으로 짜지 않고 `tests/Core.Tests/Tools/PuzzleGen.cs`가 무작위 판을 만들어 풀이기로 거른 후보에서 골랐다(Moves 수로 풀리고, 한 수 적게는 안 풀리고, 첫 묘수가 적고, 위층은 욕심쟁이로 안 풀리는 것). 각 층 위 주석이 해답 하나다. 테스트 `EveryFloor_IsSolvableInItsMoves_ButNotInFewer`가 30층을 모두 확인한다. 층을 고치면 이 테스트를 돌린다.
  - 처음 깬 층은 결정 `Rewards.ForFloor`(10 + 층×2)를 준다. 다시 깨면 없다.
  - 층마다 별 3개: 깨기, 무르기 없이 깨기, 힌트 없이 깨기(그 실행에서 그 층의 힌트를 한 번이라도 열면 못 받음). 비트로 `cf.tower.stars`에 저장한다(`Progress.RecordFloor`). 실패 결과 창의 힌트는 버튼을 눌러야 보인다.
- **별의 길**(`Meta.cs` `StarRoad`, `ThemeShop`): 게임의 장기 목표. 별은 세 곳에서 모은다 — 스테이지(쉬움·보통·어려움 모드마다 3개씩, 450), 크로마 첨탑(층마다 깨기·무르기 없이·힌트 없이 3개, 90), 퀘스트(받은 단계마다 1개, 39). 합 579(`StarRoad.MaxStars`).
  - 모은 별 수로 단계 보상 20개(`StarRoad.Steps`)를 받는다: 결정, 무르기, 덱 스킬 슬롯 +1(두 번), 보드 테마. 테마 해금(`ThemeUnlock`) 단계는 받기 없이 별만 모으면 열리고 그 테마를 결정으로 살 수 있게 된다(250~600). 마지막 단계(560)는 황금 성역 테마 선물.
  - 칭호(`StarRoad.Ranks`, 8단계: 새싹 크로마술사 → 크로마 마스터)는 별 수로 정해진다.
  - 보드 테마는 빈 칸 풀밭·장식·바다만 바꾼다. 내 땅·상대 땅 색은 그대로라 게임에는 영향이 없다. 결정이 쌓이기만 한다는 의견에 게임에 영향 없는 큰 사용처로 넣었다.
- **PvP**(`Pvp.cs`): 방을 만들고 들어온 사람끼리 1:1 / 2:2. 서버 판정 없이 모든 기기가 같은 seed 와 같은 수 목록으로 같은 `Game` 을 재생한다(결정적 lockstep).
  - `Game.CreateSeats`: 자리마다 덱 하나(2자리 또는 4자리). 짝수 자리는 Player 팀, 홀수 자리는 Ai 팀이고 차례는 자리 0 → 1 → 2 → 3 으로 돈다(팀이 번갈아, 팀 안에서는 두 사람이 번갈아). 같은 팀은 땅을 함께 쓰고 손패는 따로다. `CurrentSeat`, `DeckOfSeat`, `SideOfSeat`. `DeckOf(side)` 는 그 팀의 지금(또는 다음) 자리 덱이라 1:1 과 기존 코드는 그대로 돈다.
  - `PvpCatalog`: 맵(1:1 3종 8x8~9x9 20턴, 2:2 3종 10x10 24턴, 같은 팀 시작 땅은 대각선 두 모서리), 차례 제한 `TurnSeconds`(40), 보상(승 20·무 10·패 5). 덱은 각자 편성한 스킬 덱(CardShop 코드 목록), 강화와 소모품·게임 중 목표는 없다(모두 Lv1).
  - `PvpMove`: 수 하나를 문자열 `자리,종류(P/C/M),손패,x,y,a,b` 로. `TryApply` 는 차례가 아닌 자리이거나 규칙에 어긋난 수면 판을 안 건드리고 false — 모든 기기에서 똑같이 건너뛴다. 테스트 `Replay_FromEncodedMoves_ReachesTheSameBoard` 가 PvP 맵 6개를 AI 끼리 두고 문자열만으로 재생해 같은 판이 되는지 본다.
- **상점·스킨**(`Meta.cs` `TileSkins`, `BoostShop`, `EmotePacks`): 홈의 상점(또는 결정 표시)을 누르면 상점 창(`LobbyPanel.Shop`, `Hud.Shop.cs`). 탭 넷: 스킬 카드(9종 사기), 아이템(무르기 1개·5개 묶음 60, 소모품 이용권 — 출격 준비에서 켜면 결정 대신 이용권을 쓴다, `cf.items.<boost>`), 스킨(땅 재질 7종·땅 색 8종·유닛 세트·보드 테마), 이모티콘(팩마다 8장 진열, 마우스를 올리면 문구).
  - 내 땅 타일 = 재질 × 색. 따로 사서(`cf.skin.mats`, `cf.skin.colors`) 맞추고 지금 것은 `cf.skin`("재질:색", `Progress.TileSkin`). 빨강은 상대 색이라 팔지 않는다. AI 땅은 늘 기본 빨강. `BoardView.UseSkins(mine, foe)` 가 그림과 `Palette.Mine/Foe`(점유율 막대 등 색)를 정하고, 보드를 만들 때 내 스킨으로 초기화된다.
  - 유닛 스킨 세트(`UnitSkins`, 상점 스킨 → 유닛 탭): 원소 유닛 9종 그림을 통째로 바꾼다. 기본(무료), 할로윈(500, "할로윈 한정"), 제헌절(500, "제헌절 기념"). `cf.uskins`(가진 세트), `cf.uskin`(쓰는 세트). `BoardView.UseUnitSkins(mine, foe)` 로 팀마다 다르게 그리고, 싱글 모드의 AI 는 늘 기본. 대전 자리 스킨은 "재질:색:유닛세트"(`Progress.SeatSkin`, `TileSkins.UnitPart`)라 상대 유닛은 상대 세트로 보인다. 홈 카드 그림도 내 세트로 그린다. 안 산 세트가 있으면 홈 상점 아이콘에 NEW.
  - PvP 1:1 은 각자 스킨(색이 같으면 상대를 빨강으로, `TileSkins.AgainstMine`). 2:2 는 같은 편이 헷갈리지 않게 팀 색을 우리 파랑·상대 빨강으로 고정하고 재질만 스킨을 쓴다. 칸마다 마지막으로 차지한 자리를 기억해(`PvpSession.Claim`, `ApplyPvpMove`) 그 사람 재질로 그린다(`BoardView.CellTile`). 자리 스킨은 방 seats 의 `skin`.
- **게임 중 목표**(`Objectives.cs`): 판마다 플레이어에게 목표 3개(`ObjectiveCatalog.Pick`, seed로 결정적)를 준다. 덱으로 못 이루는 목표(지뢰가 없는데 지뢰 폭발 등)는 뽑지 않는다. `StageDef.CreateGame`이 뽑는다. AI에게는 없다.
  - 수가 끝날 때마다(`Game.AfterResolve`) 한 판 기록(`Game.StatsOf`, `MatchStats`)을 쌓고 목표를 확인한다. 달성하면 **그 자리에서 효과가 발동**한다: 땅 N칸 흡수(내 땅과 맞닿은 칸, 상대 땅 우선), 전선 2턴 보호, 거점 세우기, 한 번 더 두기(다음에 유닛을 놓은 뒤, `Game.BonusTurnTaken`).
  - 상대 차례에 일어난 일(상대가 내 지뢰를 밟음)로도 이뤄진다. 달성한 목표와 효과로 바뀐 칸은 `Game.ObjectiveEvents`에 남아 뷰가 연출한다.
  - 기록과 목표는 `Clone()`이 복사하므로 AI 시뮬레이션이 실제 판을 건드리지 않는다.
- `Deck`: 스몰 덱 순환이다.
  - 덱이 비면 무덤을 섞어 다시 덱으로 쓰고, 손패는 항상 5장을 유지한다.
  - 드로우 보정: 손패에 유닛이 한 장도 없으면 스킬 한 장을 덱 맨 아래로 돌리고 유닛을 대신 넣는다.
  - 패 버리기(코드 이름 `Mulligan`, 턴 소모)는 손패를 덱에 넣고 섞은 뒤 새로 뽑는다. 화면 문구는 "패 버리기"로 쓴다(멀리건이라는 말은 쓰지 않음).
- `AiBrain`: 탐욕 AI다. 모든 후보 수에 점수를 매겨 최고점을 둔다.
  - 유닛: 실제로 시뮬레이션해서 (내 칸 − 상대 칸)이 얼마나 늘었는지로 점수를 매긴다.
  - 스킬은 턴을 안 쓰므로 유닛 수와 따로 고른다. 먼저 유닛 최선 점수(무작위 없는 값)를 구하고, 스킬을 쓴 뒤의 최선 수가 그보다 나으면(점수 > 0) 스킬을 먼저 쓴다. 다음 호출에서 유닛을 둔다. 실수하는 턴(mistake)에는 스킬을 안 쓴다.
  - 탈색, 잠금, 지뢰, 거점: 휴리스틱 점수를 쓴다. 손패에 스킬이 쌓일수록 가산해서 손패가 막히지 않게 한다. 재점화는 바로 늘어나는 칸, 변환은 바꾼 유닛 옆에 둘 최선의 수 − 그냥 둘 최선의 수다.
  - 지뢰는 `CloneFor`로 안 보이므로 AI도 플레이어 지뢰를 피하지 않는다(테스트 `Ai_DoesNotDodgeHiddenMine`). AI가 묻을 때는 상대가 다음 턴에 놓을 수 있는 칸, 유닛 옆 칸을 높게 친다.
  - 공간 왜곡: 2수까지 본다. 유닛을 내 전선 옆으로 끌어온 뒤 그 옆에 유닛을 놓는 후속 수까지 계산한다. 후보 수는 난이도별 `Profile`의 warpCandidates로 제한한다.
  - 테스트 `AiVsAi_UsesEverySkillAcrossGames`가 AI가 스킬 7종을 모두 쓰는지 확인한다. `AiVsAi_SkillUsageReport`는 턴당 스킬 사용률을 출력한다(측정값: 보통 AI끼리 턴의 56%에서 스킬 사용).
  - **성향**(`AiPersona`)은 점수 가중치다. StealWeight는 빼앗은 칸, CatalystSteal은 상대 유닛 촉매, LoneUnitPenalty는 상대 유닛과 안 맞닿은 배치 감점, ShieldWeight는 보호 칸, Lock/Bleach/MineWeight는 스킬 배율, WarpBonus는 공간 왜곡 가산이다.
  - 난이도 `AiLevel`(입문, 쉬움, 보통, 어려움)은 `AiBrain.Profile`에서 정한다. `Perfect`는 실수 없는 기준 AI로, 게임에서는 쓰지 않고 테스트에서 플레이어 자리 대역으로만 쓴다. 플레이어들이 너무 어렵다고 해서 전체를 순하게 낮췄다. 후보 점수에 무작위를 섞고(noise), 일정 확률로 아무 수나 두게 하고(mistake), 공간 왜곡 탐색 후보 수를 줄여서 AI를 약하게 만든다.
  - 실제 난이도는 스테이지 기본값과 모드로 정한다(`LevelForMode`).
  - 테스트 `AiLevels_EasierLevelsLoseMoreOften`이 Perfect AI를 플레이어 자리에 두고 승률을 잰다. 측정값은 대 입문 100%, 대 쉬움 100%, 대 보통 92%, 대 어려움 74%, 대 Perfect 53%다(각 60판, 스킬 보조 행동 도입 후). 난이도 숫자를 바꾸면 상세 출력(`--logger "console;verbosity=detailed"`)으로 승률을 다시 확인한다.

**온라인**(웹만: Firebase Auth + Realtime Database)
- 실제 일은 WebGL 템플릿의 `net.js`(`window.CFNet`)가 한다. Firebase compat SDK(10.14.1)를 gstatic 에서 불러오고, C# 은 `Plugins/WebGL/Net.jslib` → `Client/Online.cs` 로 부르고 응답 메시지(JSON)를 매 프레임 `Online.Poll()` 로 가져온다. 에디터·Android 는 `Online.Available` 이 false 라 오프라인으로만 논다. Firebase 설정값은 `Online.FirebaseConfig`.
- DB: `users/{uid}/name`, `users/{uid}/save {at, data}`(localStorage 의 `cf.*` 전부, `cf.sync.*`·`cf.local.*` 제외), `rooms/{id}`(제목, 팀 인원, 방장, status wait|play|done, 맵, seed, seats/0..3 {uid,name,ready,left,deck}), `moves/{id}/{n}`(n 번째 수 문자열, 한 번 쓰면 못 바꿈). 보안 규칙은 `firebase/database.rules.json` — Firebase 콘솔 Realtime Database 규칙에 붙여 넣는다.
- 로그인: Google(팝업) / 게스트(익명 계정) / 로그인 없이 하기(`cf.local.offline`). 스플래시를 누르면 로그인 화면(`AppScreen.Login`)이 뜬다. 브라우저는 사용자 입력 이벤트 안에서만 팝업을 열어 주는데 Unity 는 한 프레임 늦게 클릭을 받으므로, 버튼을 누르는 순간 `Online.Arm` 으로 걸어 두어 net.js 가 `pointerup` 안에서 연다(`Hud.PopupButton`).
- 클라우드 저장: 로그인하면 계정 기록(`users/{uid}/save`)과 이 기기 기록을 맞춘다. 같은 계정 기록이면 새 쪽, 계정에 기록이 없으면 이 기기 것을 올리고, 로그인 전에 하던 기록과 계정 기록이 둘 다 있으면 고르게 한다(충돌 화면). 그 뒤로 `LocalStorage.jslib` 가 `cf.*` 를 쓸 때마다 `window.CFNetDirty` 를 불러 2.5초 뒤 올린다(탭을 숨기거나 닫을 때도). 기록을 받으면 "reload" → `LocalStore.DropLegacy`(옛 PlayerPrefs 가 되살아나지 않게) + `Progress.Reload()`. 로그아웃은 올린 뒤 이 기기의 `cf.*` 를 지운다.
- PvP 흐름: 대기실 대전 → `AppScreen.PvpLobby`(열린 방 목록, 1:1/2:2 방 만들기) → `AppScreen.PvpRoom`(맵 미리보기, 자리 카드, 빈 자리 누르면 이동, 준비, 방장은 맵 바꾸기·시작) → 방장이 시작하면 status=play, seed 가 정해지고 모두 전투로(`GameBootstrap.Online.cs`).
  - 내 차례가 되면 화면 가운데에 "내 차례!" 배너가 1.8초 뜬다(`Hud.DrawMyTurnBanner`).
  - 이모티콘: 손패 위 이모티콘 버튼 → 유리 판(위에 가진 팩 탭, 아래 스티커 4x2) → 하나를 누르면 보낸다(3초 쿨타임). 팩은 Core `EmotePacks`(기본 무료, 킹받는·귀요미·멘붕 각 300, `cf.emotepacks`, 상점 이모티콘 탭에서 산다). 보내는 값은 전체 번호라 받는 쪽은 팩이 없어도 보인다. 원소 캐릭터 표정 스티커 `UI/emote_0..7`(art/emotes.mjs: 좋아요·화난다·헉·흑흑·ㅋㅋㅋ·잘했어·음…·GG, 문구는 `EmotePacks`). 전송은 내 자리의 `rooms/{id}/seats/{n}/emote` = "번호:시각" 을 쓰는 것이라 방을 보는 모두가 받고(내 것도 돌아와서 뜸), 판 시작 때의 값은 띄우지 않는다(`PvpSession.SeenEmote`). 받은 이모티콘은 위쪽 자리 줄의 보낸 사람 이름 칸에서 말풍선(`UI/bubble` + 꼬리 `UI/bubble_tail`)이 톡 튀어나와 3초 동안 보여 주고 작아지며 사라진다(`Hud.DrawEmotes`).
  - 판(`PvpSession`): 내 수는 바로 두고 `moves/{id}/{번호}` 로 보낸다. 받은 수는 번호 순으로 0.8초씩 띄워 둔다. 이미 둔 번호의 수가 다르게 오면 처음부터 다시 재생한다(`ResyncPvp`). 차례 40초가 넘으면 그 자리 기기가 패 버리기를 보낸다. 나간 사람(또는 60초 넘게 응답 없는 사람)의 차례는 남은 사람 중 가장 앞 자리가 그 번호가 비어 있을 때만(트랜잭션) 패 버리기를 대신 쓴다. 한 팀이 모두 나가면 남은 팀이 이긴다. 판 중에 나가면(기권, 두 번 눌러야 함) 패배로 기록하고 결정은 없다(`Progress.RecordPvpForfeit`).
  - 화면의 '나'는 `GameBootstrap.Me`(2팀이면 Side.Ai), 손패는 `MyDeck`. 2팀이 보면 `Palette.Viewer` 로 두 팀의 타일·링·깃발·색을 맞바꿔 내 팀이 늘 파랑이다. 지뢰는 내 팀 것만 보인다.
  - 방에서 기다리다 끊기면 방장은 방을 지우고 나머지는 자리를 비운다(onDisconnect). 판이 시작되면 끊길 때 `left` 로 표시한다. 3시간 넘은 방은 목록을 볼 때 지운다.
- 시험: 주소에 `?mock` 을 붙이면 Firebase 대신 브라우저 안의 가짜 DB(localStorage `cfmock.db`, 탭끼리 공유)와 탭마다 따로인 가짜 로그인을 쓴다. 탭 두 개로 방 만들기·입장·대전을 시험할 수 있다(단, `cf.*` 진행은 같은 브라우저라 공유된다). `?tab` 은 실제 Firebase 로그인을 탭마다 따로 유지한다.
- Firebase 콘솔에서 할 일: Authentication 에서 Google·익명 로그인을 켜고, 승인된 도메인에 `mhsssshin.github.io` 를 넣고, Realtime Database 규칙에 `firebase/database.rules.json` 을 넣는다.

**Client**
- `GameBootstrap`이 유일한 진입점이다. 씬에는 카메라와 이것뿐이고 오브젝트는 모두 코드로 만든다. partial 클래스로 세 파일에 나뉜다.
  - `GameBootstrap.cs`: 화면 전환(`AppScreen`: 대기실 ↔ 전투), AI 진행(0.8초 뒤), 결과 기록, 카메라.
  - `GameBootstrap.Input.cs`: 드래그 앤 드롭 입력, 보드 강조, 미리보기.
  - `GameBootstrap.Effects.cs`: 메시지, 이펙트, 라운드 기믹 연출.
  - `GameBootstrap.Tutorial.cs`: 튜토리얼 진행과 상황 팁.
- **홈(메인 화면)**(`AppScreen.Home`, `GameBootstrap.Home.cs`, `Hud.Home.cs`): 스플래시·로그인·튜토리얼 건너뛰기 다음 화면. 디자인: 바다가 비쳐 보이게 반투명 유리를 겹친다. 위 그늘 띠 위에 로고·캡슐(결정·별)·계정 아이콘, 이름표 캡슐, 아래 유리 독(덱·상점·첨탑, 마우스를 올리면 후광). 모드 카드(`Hud.ModeCard`)는 틴트한 `mode_card` 위에 후광과 실제 타일·유닛 그림을 얹어 그린다(싱글: 내 스킨 네 칸 + 불·물·번개, 멀티: 내 땅 vs 빨강 땅 + VS). 위 줄 로고·결정(누르면 상점)·별의 길(받을 보상 배지)·계정, 이름·칭호, 내 땅 스킨으로 채운 8x8 섬 미리보기(띠 12%~48%)와 "내 땅: 재질 · 색 꾸미기" 버튼(상점 스킨 탭), 큰 버튼 싱글 모드(스테이지 대기실, 받을 퀘스트 배지) | 멀티 모드(대전 로비), 아래 줄 덱·상점·첨탑. 덱·상점 같은 창(LobbyPanel)은 홈과 대기실 위에 똑같이 뜬다. 스킨·테마를 바꾸면 `RefreshPreview` 가 지금 화면의 미리보기를 다시 그린다. 첨탑·대전 로비의 뒤로 버튼은 홈으로 간다.
- **대기실(싱글 모드)**: 고른 스테이지의 맵을 실제 `BoardView`로 미리 보여 준다(`_previewBoard`). 챕터 탭, 스테이지 노드(별·잠금·보스 왕관), 상대 정보, 모드, 출격 버튼이 있다.
  - 진행 저장은 `Progress`가 `LocalStore`를 통해 맡는다. 키는 `cf.unlocked`, `cf.stars`(쉬움 모드, 보통·어려움은 뒤에 `.1`/`.2`), `cf.mode`(지금 모드), `cf.tutorial`, `cf.tutorialLesson`, `cf.tip.<id>`, `cf.gems`(결정), `cf.cards`(소장·덱, `Collection.Serialize`), `cf.stat.<stat>`(퀘스트 누적 기록), `cf.quest.<id>`(받은 단계 수), `cf.items.undo`(모아 둔 무르기), `cf.tower`(첨탑에서 깬 층 수), `cf.tower.stars`(층별 별 비트), `cf.road`(받은 별의 길 단계 번호 목록), `cf.themes`(가진 테마), `cf.theme`(쓰는 테마), `cf.pvp.win|draw|lose`(대전 전적), `cf.skin`·`cf.skin.mats`·`cf.skin.colors`(땅 스킨), `cf.items.<pioneer|extraobjective|firststrike>`(소모품 이용권)다. 온라인용으로 `cf.sync.uid|at`(이 기기 기록의 계정·바뀐 시각), `cf.local.offline`(로그인 없이 하기)이 있고 이 둘은 클라우드에 올리지 않는다. 이기면 다음 스테이지가 해금된다.
  - 판이 끝나면 `Progress.RecordBattle`이 별·해금 기록, 누적 기록 더하기, 결정 지급을 한 번에 한다. 결과 창에 받은 결정과 목표 달성 수를 보여 준다.
  - 위쪽 줄에 ← 홈, "싱글 모드", 보유 결정(누르면 상점)이 있다. 별의 길(`LobbyPanel.Road`)과 계정(`LobbyPanel.Account`: 이름 바꾸기, 게스트면 Google 연결, 로그아웃, 전적, 튜토리얼 다시 보기)은 홈 위 줄에 있다.
  - 별의 길 창: 칭호·별·다음 칭호 바·별 출처, 탭 두 개(보상: 한 쪽에 7단계씩 넘기기 + 모두 받기 / 보드 테마: 8종 2x4, 사기·사용하기). 테마를 바꾸면 `GameBootstrap.UseTheme`이 미리보기를 다시 그리고, `BoardView`는 만들어질 때 `Progress.Theme`의 그림 이름을 쓴다. 아래 줄은 모드(열린 모드끼리 돌아가며 바꿈, 하나뿐이면 여는 방법을 잠깐 보여 줌), 퀘스트(받을 보상 수 배지), 출격이다.
  - 출격을 누르면 출격 준비 창(`LobbyPanel.Prep`)이 뜬다. 소모품을 켜고 끄고(`GameBootstrap.Boosts`), 무르기를 사고, 출격(`Sortie`)하면 값을 낸다. 다시·다음 스테이지는 소모품 없이 시작한다.
  - 덱·퀘스트 창(`LobbyPanel`)은 대기실 위에 뜬다. 창이 떠 있으면 대기실은 Repaint 때만 그려서 아래 버튼이 클릭을 받지 않는다. 덱 창은 9종 카드를 3x3으로 보여 주고, −/+로 넣고 빼고, 결정으로 사고 강화한다. 강화 버튼은 처음 누르면 다음 단계 효과를 보여 주고 4초 안에 한 번 더 눌러야 강화한다. 손패 카드에도 Lv2 이상이면 단계 배지가 붙는다.
  - 개발 빌드의 대기실에서 `G` 키를 누르면 결정 500개를 받는다.
  - `LocalStore`는 WebGL에서 브라우저 **localStorage**에 직접 쓴다(`Plugins/WebGL/LocalStorage.jslib`). 동기 저장이라 탭을 바로 닫아도 남는다. 에디터와 Android에서는 PlayerPrefs를 쓴다.
  - 예전 PlayerPrefs(IndexedDB) 값은 처음 읽을 때 localStorage로 옮긴다. PlayerPrefs를 직접 쓰지 말고 `LocalStore`를 쓴다.
  - 개발 빌드 전용 단축: 주소에 `?unlock`(전체 해금), `?reset`(진행·튜토리얼·팁 전부 초기화), 대기실에서 `U` 키(전체 해금)를 쓸 수 있다.
- **첨탑 화면**(`AppScreen.Tower`, `GameBootstrap.Tower.cs`, `Hud.DrawTower`): 고른 층의 판 미리보기(띠 13%~43%), 층 정보(수·카드·힌트 버튼), 1층이 왼쪽 아래인 6x5 층 버튼, 도전 버튼. 전투는 일반 판과 같은 화면이고 `GameBootstrap.Puzzle`이 있으면 상단(`DrawPuzzleTop`: 남은 수, 층, 힌트, n/전체 칸)과 결과(`DrawPuzzleResult`)가 바뀐다. 패 버리기 대신 공짜 무르기가 있고, 실패한 결과 창에서도 마지막 수를 무를 수 있다. 다시는 확인 없이 바로 시작한다. 처음 들어가면 규칙 팁(`TowerTip`)을 띄운다.
- **튜토리얼**: 레슨 데이터는 Core의 `TutorialCatalog`에 있다(8단계: 화염 → 물 방향 → 번개 → 융합 → 가로채기 → 3차 조합(흑색) → 공명 → 탈색).
  - 레슨은 6x6 연습 보드에 카드 한 장(`Lesson.Card`, 섞지 않은 덱 `Game.Create(..., shuffle: false)`)을 목표 칸(`Target`, 물은 `WaterDir`까지)에 내는 것이다. 테스트 `EveryLesson_TargetIsPlayable_AndProducesTheLessonResult`가 목표 수가 `Check`를 만족하는지 검증한다. 레슨을 고치면 이 테스트로 확인한다.
  - 놓을 수 있는 칸은 목표로만 좁힌다(`RestrictToLesson`). AI는 두지 않는다. 카드는 미리 고르지 않는다. 실제 게임과 똑같이 카드를 끌어 놓거나, 카드를 누른 뒤 칸을 눌러야 놓인다. 목표 칸은 카드를 고르기 전부터 빛나고 그쪽으로 확대된다. 카드 없이 칸을 누르면 카드를 먼저 고르라는 안내가 뜬다.
  - **개념 카드**(`ConceptPage`, `GameBootstrap.Concepts`): 레슨마다 시작 전에 그림(유닛·타일 그림과 `+`·`=`·`→`·`vs`)과 짧은 설명을 먼저 보여 준다. 첫 레슨은 "게임 목표", "카드 놓는 곳" 두 장이다. 떠 있는 동안은 입력을 막는다(`PlayerTurn`이 거짓).
  - (HUD 공통 디자인) 보조 버튼(나가기·닫기·취소)은 반투명 캡슐 `Hud.PillButton`, 큰 버튼(출격·시작·모드·스테이지 노드·방 만들기·자리 카드)은 카드 재질 `Hud.CardButton`(마우스를 올리면 후광, 출격·시작은 후광이 숨 쉬듯 깜빡임). 전투·대기실·대전 화면은 위·아래에 그늘 띠를 깔고, 위쪽 정보(턴·스테이지·점유율 막대·메시지·게임 중 목표)는 캡슐(`pill`)로 그린다. 승리 결과 창 제목 뒤에는 금빛 후광.
  - 안내 글은 밝은 말풍선(`UI/bubble`)에 진한 큰 글자(19~20pt)로 쓰고, 핵심 말은 리치 텍스트 `<color>`로 강조한다(`Hud.Rich`). 예전의 어두운 패널 위 15pt 글자는 잘 안 보였다. 목표 칸 위에는 통통 튀는 "여기!"(물 방향은 "이쪽으로!") 표시가 뜬다(`TutorialTarget`).
  - 가이드 방울이 말풍선(`Hud.DrawCoach`)과, 카드에서 목표 칸까지 끄는(물은 쓸어 넘기는) 손가락 시범(`HintGesture`, `Hud.DrawFinger`, `UI/finger`)이 있다.
  - 처음 실행(`Progress.TutorialDone` false)이면 자동으로 시작한다. 마지막 레슨을 마치는 순간 완료로 기록되어 다시 나오지 않는다. 중간에 나가면 `cf.tutorialLesson`의 레슨부터 이어진다. 대기실 배우기로 다시 볼 수 있다. 마치면 1-1로 바로 이어진다.
- **상황 팁**(`TipDef`, `Tips`): 스테이지를 시작할 때 처음 보는 챕터 기믹(ch2~ch5)이나 보스(ignis, glacia, homunculus)가 있으면 설명 카드를 띄운다. 본 팁은 `cf.tip.<id>`에 남는다. 카드가 떠 있는 동안은 입력을 막는다.
- **전투 입력(드래그 앤 드롭)**:
  - 카드를 끌어 보드에 떼면 낸다. 끄는 동안 노리는 칸(`_aim`)은 손가락보다 `CardLift`만큼 위다. 그래서 손가락이 칸을 가리지 않는다.
  - 노리는 칸은 가장 가까운 고를 수 있는 칸에 자석처럼 붙는다(`Snap`, 반경 `CardSnap`/`BoardSnap`). 손을 뗀 순간의 위치로 다시 계산한다.
  - 손패 쪽이나 빈 곳에서 떼면 취소된다. 카드를 탭하면 선택만 되고(다시 탭하면 해제), 선택한 채로 보드를 눌렀다 떼도 낸다.
  - 물: 놓은 뒤 보드를 원하는 방향으로 쓸어 넘기거나(`WaterDir`, 최소 `SwipeMin`) 화살표 줄을 탭한다. 얼음이면 미끄러진 칸(`_blueAt`)에서 흐르고, 실제로 낼 때는 고른 칸(`_blueTap`)을 넘긴다.
  - 공간 왜곡: 첫 칸에 놓은 뒤, 보드를 눌러 끌어 두 번째 칸을 고른다.
  - 얼음: 유닛 카드를 고르면 놓을 수 있는 얼음 칸마다 미끄러질 방향이 반투명 화살표(`Highlight.Slides`)로 뜨고, 노리는 칸이 얼음이면 실제로 멈출 칸에 반투명 유닛(`Highlight.Ghost`)이 뜬다.
  - 미리보기(`AddPreview`)는 화염과 물 방향을 `CloneFor`로 실제 시뮬레이션해서 보여 준다. 융합, 공명, 얼음이 반영된다. 번개는 무작위라 놓는 칸만 보여 준다.
  - 보드 강조는 `BuildHighlight()`가 매 프레임 `Highlight`(Legal, Preview, Arrows, Dim, Picked, Hover)로 채운다. 카드를 고르면 고를 수 없는 칸은 어둡게 만들고, 고를 수 있는 칸만 밝게 반짝인다.
- **카메라**(`FitCamera`, `Frame`): 보드를 HUD 사이의 화면 띠에 맞춘다. 전투 띠는 위 27%~75%, 대기실 띠는 17%~48.5%다.
  - 카드를 고르면 고를 수 있는 칸들이 꽉 차게 **자동 확대**한다(최대 1.8배). 내려놓으면 전체 보기로 부드럽게 돌아간다.
- `HandView`는 손패의 화면상 움직임을 맡는다. 기준 좌표는 540x960이다. 끄는 카드는 손가락 바로 아래를 작게 따라온다.
  - Core 손패와 순서대로 맞춰서 바뀐 카드를 찾는다. 새 카드는 덱 더미에서 뒷면으로 날아와 뒤집히고, 낸 카드는 보드 쪽으로(패 버리기면 덱으로) 날아가며 사라진다.
  - 손패는 부채꼴로 놓인다. 마우스를 올린 카드와 고른 카드는 위로 올라온다.
  - 카드가 회전돼 있어서 `Hud`는 `GUI.Button` 대신 `HitCard`로 직접 클릭을 판정한다. 위에 그려진 카드가 먼저 클릭을 받는다.
  - 지뢰는 플레이어 자신의 것만 보인다. AI의 지뢰는 메시지로만 알린다.
  - 이번 턴에 스킬을 이미 썼으면 손패의 스킬 카드는 회색으로 그리고 고를 수 없다(`GameBootstrap.CanUse`).
- `BoardView`는 아이소메트릭 좌표 변환을 맡는다.
  - 칸 (X, Y)는 월드 ((X−Y)·1, −(X+Y)·0.5)로 옮긴다. X는 화면 오른쪽 아래, Y는 왼쪽 아래로 뻗는다.
  - 클릭 좌표는 `TryPick`으로 역변환한다.
  - 매 프레임 `Sync`로 Board를 그린다. 게임 상태는 들고 있지 않고, 직전 프레임의 소유와 유닛만 기억한다. 그래서 바뀐 칸에는 연출이 붙는다. 소유가 바뀐 타일은 튀어 오르고, 새 유닛은 떨어지며 통통 튄다.
  - 그리는 순서(`sortingOrder`): 바다(-200)와 거품 → 타일과 오버레이((x+y)·10 + 0~5: 타일, 지형, 잠금, 보호막, 빛, 테두리) → 장식, 팀 링, 유닛(1000 + (x+y)·10 + 0~2) → 반투명 유닛(1000 + (x+y)·10 + 3) → 물 방향·얼음 화살표(1000 + (x+y)·10 + 5) → 이펙트(5000~).
  - 거점 깃발(`Units/beacon_player`, `beacon_ai`)은 빈 칸이면 가운데에 크게, 유닛이 있으면 왼쪽 위 구석에 작게 선다. 깃발이 있는 칸은 장식을 숨긴다.
  - 장식은 칸 좌표 해시로 고정 배치하고, 유닛이나 지뢰가 있거나 평지가 아니면 숨긴다.
  - 지형: 낭떠러지는 칸을 통째로 숨겨 바다가 보이게 한다. 블랙홀은 전용 타일(`tile_blackhole`)로 그린다(`tile_lava`는 화산 팁 그림에만 쓴다). 얼음, 침묵, 컨베이어는 소유 색 위에 오버레이로 얹는다.
- `FxView`: 풀링된 SpriteRenderer로 프레임 이펙트를 재생한다. `delay`로 시차를 준다.
  - 연출은 `GameBootstrap.PlayEffects` 한 곳에서 정한다. 원소별 배치 이펙트 → 융합 이펙트 → 점유된 칸에 거리순으로 고리와 반짝임이 퍼진다.
  - 큰 반응(마그마, 백색, 흑색, 지뢰)은 화면을 흔든다(`_shake`).
  - **합성 연출**(`StartCine`): 융합하면 시간이 0.25배까지 느려지고(`Time.timeScale`), 카메라가 그 칸으로 클로즈업하고, 반응에 휘말린 칸만 밝게 남긴다(`CineHighlight`). HUD는 섬광과 융합 이름·효과 띠를 띄운다(`Hud.DrawCine`). 2단계 1.6초, 3단계 2.2초(실제 시간, `CineTime`은 `unscaledDeltaTime`로 센다)이고 AI의 융합은 0.75배다. 화면을 나가거나 새 판을 시작하면 `EndCine`으로 시간 배율을 되돌린다.
- `Hud`는 IMGUI다. 스킨은 `Resources/Art/UI` 텍스처를 쓴다.
  - 기준 해상도는 540x960이다. `GUI.matrix`를 쓰지 않고 사각형과 글자 크기를 직접 키운다. 행렬로 확대하면 글자가 흐려지기 때문이다.
  - 한글은 `Resources/Fonts/Pretendard-Bold`로 그린다(OFL, `ThirdParty/Pretendard`). WebGL 기본 폰트에는 한글이 없다.
  - 버튼이 쓰지 않은 클릭만 보드로 보낸다.
  - 전투 상단 메시지 아래에 게임 중 목표 3칸이 있다(`DrawObjectives`). 진행은 n/목표, 점유율은 현재 %, 한 수 목표는 최고 칸 수로 보인다. 이루면 초록이 되고, 이룬 순간 금빛으로 반짝인다(`GameBootstrap.ObjectiveFlash`). 그래서 전투 카메라 띠는 위 27%부터다.
  - **무르기**: 손패 위 패 버리기 옆 버튼. 내 차례가 시작될 때마다(`Game.TurnsTaken`이 바뀌면) 판을 복제해 둔다(`_history`). 이번 차례에 스킬을 썼으면 이번 차례 처음으로, 아니면 내 지난 차례 처음으로(그사이 AI 수까지) 되돌리고 무르기 1회를 쓴다.
  - 전투 상단에는 나가기, 다시(같은 스테이지 새 판, `GameBootstrap.Retry`) 버튼이 있다. 다시는 실수로 판을 날리지 않도록 3초 안에 한 번 더 눌러야 한다("한 번 더"로 바뀜). 튜토리얼에서는 숨긴다.
- 씬(`Assets/Scenes/Main.unity`)과 PlayerSettings(세로 모드, WebGL 템플릿, 패키지 ID)는 `ProjectSetup.Run()`이 만들고 적용한다. ProjectSettings는 손으로 고치지 말고 여기에 추가한다.

## 아직 안 된 것 (PRD 대비)

- 공간 왜곡으로 옮긴 유닛이 그 자리에서 바로 융합하는 것. 지금은 옮기기만 하고, 융합은 다음에 놓는 유닛이 일으킨다.
- 융합 결과가 다시 다른 반응을 일으키는 다단 연쇄. 2단계 유닛을 재료로 쓰는 3차 조합은 구현했다.
- 유닛 카드 편성, 사운드. (보드 테마·땅 스킨은 구현) 스킬 카드 덱 편성·강화·소모품·결정·퀘스트·게임 중 목표는 구현했다.
- 온라인(로그인·클라우드 저장·PvP)은 웹만 된다. Android 는 Firebase Unity SDK 나 REST 로 따로 붙여야 한다. PvP 는 판을 각 기기가 재생하므로 손패를 속일 수 있다(서버 판정 없음). 끊겼다 다시 들어와 이어 하기는 없다.
- 크로마 첨탑은 30층까지만 있다. 더 만들려면 PuzzleGen 으로 후보를 뽑아 PuzzleCatalog 에 붙인다.
- 날마다 바뀌는 일일 퀘스트는 없다. 퀘스트는 누적 단계형뿐이다.
- 스킬을 덱 대신 융합·공명으로 차는 에너지 게이지로 쓰는 안(설계안 C)은 아직 검토만 했다.
- 스테이지 이름은 아직 "챕터명 + 번호"다(보스만 고유 이름).
- 돌풍(밀어내기)은 PRD 문장을 단순하게 해석했다. 주변 8칸의 내용을 한 칸 바깥으로 민 뒤 그 8칸을 점유한다.
