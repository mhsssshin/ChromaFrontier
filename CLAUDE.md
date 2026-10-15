# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

사용자와의 대화는 항상 한국어로 한다. 기획 기준 문서는 `PRD.md`.

## 개요

**크로마 프론티어 (Chroma Frontier)**(가제): 쿼터뷰 픽셀 영토전. 턴제 전략 땅따먹기 퍼즐 PvE이고, 엔진은 Unity 6000.3.25f1이다.
- 최종 목표는 **안드로이드 전용 오프라인 APK**다. 서버나 네트워크 없이 진행도, 덱, 해금을 모두 기기에 저장한다.
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

# 아트 재생성 → client/Assets/Resources/Art/**.png + build/art-preview.png (약 2초, 시스템 Chrome 필요)
"$T/node/node.exe" art/build.mjs
```

- 빌드 성공 여부는 로그의 `[ChromaFrontier] WebGL build Succeeded`로 판정한다.
- 컴파일 오류는 로그에서 `error CS`로 찾는다.
- 첫 빌드는 약 5분, 이후 증분 빌드는 약 2분 걸린다.
- 개발 빌드는 압축 없이 32MB다.
- 배포용(`BuildWebGLRelease`)은 Gzip 압축에 `decompressionFallback`을 켠다. GitHub Pages처럼 Content-Encoding 헤더를 안 붙이는 정적 호스팅에서도 로더가 브라우저에서 직접 풀기 때문이다. 배포용은 개발 빌드가 아니라서 `?unlock`/`?reset`/U 키가 꺼진다.
- 배포:
  - 저장소는 https://github.com/mhsssshin/ChromaFrontier 다.
  - 소스는 `main`, 빌드 결과물은 `gh-pages` 브랜치에 둔다. `scripts/deploy-pages.sh`가 매번 단일 커밋으로 덮어쓴다.
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
    - `tiles.mjs`: 타일 4종, 윗면 오버레이(glow/select/lock/shield), 장식, 바다, 거품
    - `units.mjs`: 원소 유닛 9종, 팀 링, 지뢰
    - `fx.mjs`: 프레임 이펙트 `<이름>_<n>`
    - `ui.mjs`: 카드 틀, 아이콘, 바, 버튼, 패널
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
  - 수는 `Play(handIndex, cell, blueDir?)`(유닛), `Cast(handIndex, target, target2)`(스킬), `Mulligan()`으로만 둔다. 공간 왜곡을 뺀 모든 수는 턴을 끝내고, 손패를 채우고, 보호막·잠금 턴을 줄인다(`Board.TickTimers`).
  - 대상 검증은 `CanPlace`(유닛), `CanTarget`/`CanCast`(스킬)가 한다. `Resolver`는 검증하지 않는다.
  - `CloneFor(viewer)`는 viewer가 모르는 상대 지뢰를 지운 복제본이다. AI와 물 방향 계산은 반드시 이걸로 시뮬레이션해서 숨은 지뢰를 엿보지 않게 한다.
  - `Turn`은 양측을 합친 턴 수이고 `MaxTurns`(스테이지 규칙, 기본 20)가 되면 끝난다. 흑색의 추가 턴은 `Turn`을 늘리지 않는다.
  - 플레이어 → AI가 한 라운드다. 라운드가 끝날 때(`EndRound`) 맵 기믹이 움직인다. 순서는 컨베이어 이동 → 블랙홀 흡입 → (`StageRules.LavaEvery`마다) 중앙 용암 분출이다. 일어난 일은 `Game.RoundEvents`에 남아 뷰가 연출한다.
  - 차례가 된 쪽이 유닛을 놓을 칸이 하나도 없으면(`LegalCells` 비어 있음) 그 자리에서 끝나고 점유 칸 수로 승패를 가린다(`Game.Stuck`). 잠긴 칸도 놓을 수 없는 칸으로 친다. 공간 왜곡 직후에도 검사한다.
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
- **지형**(`Tile.Terrain`): 평지, 낭떠러지, 얼음, 침묵 지대, 블랙홀, 컨베이어가 있고, 용암은 `Tile.Lava` 타이머로 다룬다.
  - `Tile.Blocked`는 계산되는 속성이다(낭떠러지, 블랙홀, 용암 중). 막힌 칸은 배치·점유가 안 되고 점유율 분모(`Board.Playable`)에서도 빠진다.
  - 맵은 `Board.Parse`의 ASCII 지도로 만든다. 기호: `.` 평지, `#` 낭떠러지, `~` 얼음, `s` 침묵, `O` 블랙홀, `>` 컨베이어(+X로 흐름), `P`/`A` 시작 땅.
- **스테이지**(`Stages.cs`): `StageCatalog.All`에 50개(5챕터 × 10, 각 챕터 10번째가 보스)가 있다. `StageDef`는 맵, `StageRules`, 상대 성향(`AiPersona`), 기본 난이도, AI 덱을 갖는다.
  - 1장 초원(기믹 없음, 7x7 → 8x8) → 2장 화산(용암) → 3장 얼음성(얼음) → 4장 끊어진 다리(낭떠러지) → 5장 공허의 탑(9x9, 컨베이어·블랙홀·침묵, 22턴).
  - 보스: 1장 이그니스(화염 덱, 어그로), 2장 각성 이그니스, 3장 글라시아(물·잠금·탈색, 통제), 4장 호문클루스(약탈), 5장 각성 호문클루스.
  - 기본 난이도는 스테이지 0~11이 쉬움, 12~31이 보통, 32 이상이 어려움이다. 보스는 한 단계 위다. 대기실의 난이도 보정(−1/0/+1)을 더한다(`StageCatalog.LevelFor`).
  - 별(`StageCatalog.Stars`): 승리 1개, 점유율 50% 이상 2개, 65% 이상 3개.
  - 테스트 `EveryStage_StartsWithMovesForBothSides_AndPlaysToTheEnd`가 50개 스테이지를 모두 AI끼리 끝까지 돌려 본다. 맵을 고치면 이 테스트로 확인한다.
- 배치 규칙(`Game.CanPlace`, `Board.IsFrontline`): 빈 칸이어야 하고, 내 타일이거나 내 타일과 상하좌우로 맞닿은 칸이어야 한다.
- 카드(`Card`)는 1단계 원소 유닛이거나 스킬(`Skill`)이다. 기본 덱(`Game.StarterDeck`)은 유닛 12장과 스킬 8장이다.
- 스킬은 전선 규칙 없이 보드 어디에나 쓴다. 효과는 `Resolver.CastSkill`에 있다.
  - 공간 왜곡: 두 칸의 내용(소유, 유닛, 보호막, 잠금, 지뢰)을 맞바꾼다(`Tile.SwapContents`). 지형은 제자리에 남는다. 턴을 끝내지 않는다.
  - 영역 잠금: 3x3 칸의 `Tile.Lock`을 설정한다. 잠긴 칸에는 양쪽 모두 유닛을 놓을 수 없다.
  - 탈색: 유닛을 `Element.Grey`로 바꾼다. 회색 유닛은 융합 촉매가 될 수 없다.
  - 잠복 지뢰: 내 빈 땅의 `Tile.Mine`을 설정한다. 상대가 그 칸에 유닛을 놓으면 유닛은 사라지고 주변 3x3이 지뢰 주인의 것이 된다(`PlayResult.MineTriggered`).
  - 지속 효과(잠금, 보라 침묵, 백색 성역)는 "내 턴 기준"으로 센다. 타이머는 양측 턴마다 1씩 줄어들기 때문에 상수는 `턴 수 × Balance.TicksPerRound`(2)로 정의한다. 예를 들어 잠금 2턴은 상대 턴 2번 동안 유지되고, 그사이 내 다음 턴에도 잠겨 있다.
- `Deck`: 스몰 덱 순환이다.
  - 덱이 비면 무덤을 섞어 다시 덱으로 쓰고, 손패는 항상 5장을 유지한다.
  - 드로우 보정: 손패에 유닛이 한 장도 없으면 스킬 한 장을 덱 맨 아래로 돌리고 유닛을 대신 넣는다.
  - 멀리건은 손패를 덱에 넣고 섞은 뒤 새로 뽑는다.
- `AiBrain`: 탐욕 AI다. 모든 후보 수에 점수를 매겨 최고점을 둔다.
  - 유닛: 실제로 시뮬레이션해서 (내 칸 − 상대 칸)이 얼마나 늘었는지로 점수를 매긴다.
  - 탈색, 잠금, 지뢰: 휴리스틱 점수를 쓴다. 손패에 스킬이 쌓일수록 가산해서 손패가 막히지 않게 한다.
  - 공간 왜곡: 2수까지 본다. 유닛을 내 전선 옆으로 끌어온 뒤 그 옆에 유닛을 놓는 후속 수까지 계산한다. 후보 수는 난이도별 `Profile`의 warpCandidates로 제한한다.
  - 테스트 `AiVsAi_UsesEverySkillAcrossGames`가 AI가 스킬 4종을 모두 쓰는지 확인한다.
  - **성향**(`AiPersona`)은 점수 가중치다. StealWeight는 빼앗은 칸, CatalystSteal은 상대 유닛 촉매, LoneUnitPenalty는 상대 유닛과 안 맞닿은 배치 감점, ShieldWeight는 보호 칸, Lock/Bleach/MineWeight는 스킬 배율, WarpBonus는 공간 왜곡 가산이다.
  - 난이도 `AiLevel`(쉬움, 보통, 어려움)은 `AiBrain.Profile`에서 정한다. 후보 점수에 무작위를 섞고(noise), 일정 확률로 아무 수나 두게 하고(mistake), 공간 왜곡 탐색 후보 수를 줄여서 AI를 약하게 만든다.
  - 실제 난이도는 스테이지 기본값에 대기실 난이도 보정을 더한 값이다.
  - 테스트 `AiLevels_EasierLevelsLoseMoreOften`이 어려움 AI를 플레이어 자리에 두고 승률을 잰다. 측정값은 대 쉬움 약 82%, 대 보통 약 62%, 대 어려움 약 52%다. 난이도 숫자를 바꾸면 상세 출력(`--logger "console;verbosity=detailed"`)으로 승률을 다시 확인한다.

**Client**
- `GameBootstrap`이 유일한 진입점이다. 씬에는 카메라와 이것뿐이고 오브젝트는 모두 코드로 만든다. partial 클래스로 세 파일에 나뉜다.
  - `GameBootstrap.cs`: 화면 전환(`AppScreen`: 대기실 ↔ 전투), AI 진행(0.8초 뒤), 결과 기록, 카메라.
  - `GameBootstrap.Input.cs`: 드래그 앤 드롭 입력, 보드 강조, 미리보기.
  - `GameBootstrap.Effects.cs`: 메시지, 이펙트, 라운드 기믹 연출.
  - `GameBootstrap.Tutorial.cs`: 튜토리얼 진행과 상황 팁.
- **대기실**: 고른 스테이지의 맵을 실제 `BoardView`로 미리 보여 준다(`_previewBoard`). 챕터 탭, 스테이지 노드(별·잠금·보스 왕관), 상대 정보, 난이도 보정, 출격 버튼이 있다.
  - 진행 저장은 `Progress`(PlayerPrefs `cf.unlocked`, `cf.stars`, `cf.levelOffset`)가 맡는다. 이기면 다음 스테이지가 해금된다.
  - 개발 빌드 전용 단축: 주소에 `?unlock`(전체 해금), `?reset`(진행·튜토리얼·팁 전부 초기화), 대기실에서 `U` 키(전체 해금)를 쓸 수 있다.
- **튜토리얼**: 레슨 데이터는 Core의 `TutorialCatalog`에 있다(8단계: 화염 → 물 방향 → 번개 → 융합 → 가로채기 → 3차 조합(흑색) → 공명 → 탈색).
  - 레슨은 6x6 연습 보드에 카드 한 장(`Lesson.Card`, 섞지 않은 덱 `Game.Create(..., shuffle: false)`)을 목표 칸(`Target`, 물은 `WaterDir`까지)에 내는 것이다. 테스트 `EveryLesson_TargetIsPlayable_AndProducesTheLessonResult`가 목표 수가 `Check`를 만족하는지 검증한다. 레슨을 고치면 이 테스트로 확인한다.
  - 놓을 수 있는 칸은 목표로만 좁힌다(`RestrictToLesson`). AI는 두지 않는다. 레슨 카드는 미리 선택돼 있어서 목표 칸이 처음부터 빛나고 그쪽으로 확대된다.
  - 가이드 방울이 말풍선(`Hud.DrawCoach`)과, 카드에서 목표 칸까지 끄는(물은 쓸어 넘기는) 손가락 시범(`HintGesture`, `Hud.DrawFinger`, `UI/finger`)이 있다.
  - 처음 실행(`Progress.TutorialDone` false)이면 자동으로 시작한다. 대기실 배우기로 다시 볼 수 있다. 마치면 1-1로 바로 이어진다.
- **상황 팁**(`TipDef`, `Tips`): 스테이지를 시작할 때 처음 보는 챕터 기믹(ch2~ch5)이나 보스(ignis, glacia, homunculus)가 있으면 설명 카드를 띄운다. 본 팁은 PlayerPrefs `cf.tip.<id>`에 남는다. 카드가 떠 있는 동안은 입력을 막는다.
- **전투 입력(드래그 앤 드롭)**:
  - 카드를 끌어 보드에 떼면 낸다. 끄는 동안 노리는 칸(`_aim`)은 손가락보다 `CardLift`만큼 위다. 그래서 손가락이 칸을 가리지 않는다.
  - 노리는 칸은 가장 가까운 고를 수 있는 칸에 자석처럼 붙는다(`Snap`, 반경 `CardSnap`/`BoardSnap`). 손을 뗀 순간의 위치로 다시 계산한다.
  - 손패 쪽이나 빈 곳에서 떼면 취소된다. 카드를 탭하면 선택만 되고(다시 탭하면 해제), 선택한 채로 보드를 눌렀다 떼도 낸다.
  - 물: 놓은 뒤 보드를 원하는 방향으로 쓸어 넘기거나(`WaterDir`, 최소 `SwipeMin`) 화살표 줄을 탭한다. 얼음이면 미끄러진 칸(`_blueAt`)에서 흐르고, 실제로 낼 때는 고른 칸(`_blueTap`)을 넘긴다.
  - 공간 왜곡: 첫 칸에 놓은 뒤, 보드를 눌러 끌어 두 번째 칸을 고른다.
  - 미리보기(`AddPreview`)는 화염과 물 방향을 `CloneFor`로 실제 시뮬레이션해서 보여 준다. 융합, 공명, 얼음이 반영된다. 번개는 무작위라 놓는 칸만 보여 준다.
  - 보드 강조는 `BuildHighlight()`가 매 프레임 `Highlight`(Legal, Preview, Arrows, Dim, Picked, Hover)로 채운다. 카드를 고르면 고를 수 없는 칸은 어둡게 만들고, 고를 수 있는 칸만 밝게 반짝인다.
- **카메라**(`FitCamera`, `Frame`): 보드를 HUD 사이의 화면 띠에 맞춘다. 전투 띠는 위 21.5%~75%, 대기실 띠는 17%~48.5%다.
  - 카드를 고르면 고를 수 있는 칸들이 꽉 차게 **자동 확대**한다(최대 1.8배). 내려놓으면 전체 보기로 부드럽게 돌아간다.
- `HandView`는 손패의 화면상 움직임을 맡는다. 기준 좌표는 540x960이다. 끄는 카드는 손가락 바로 아래를 작게 따라온다.
  - Core 손패와 순서대로 맞춰서 바뀐 카드를 찾는다. 새 카드는 덱 더미에서 뒷면으로 날아와 뒤집히고, 낸 카드는 보드 쪽으로(멀리건이면 덱으로) 날아가며 사라진다.
  - 손패는 부채꼴로 놓인다. 마우스를 올린 카드와 고른 카드는 위로 올라온다.
  - 카드가 회전돼 있어서 `Hud`는 `GUI.Button` 대신 `HitCard`로 직접 클릭을 판정한다. 위에 그려진 카드가 먼저 클릭을 받는다.
  - 지뢰는 플레이어 자신의 것만 보인다. AI의 지뢰는 메시지로만 알린다.
- `BoardView`는 아이소메트릭 좌표 변환을 맡는다.
  - 칸 (X, Y)는 월드 ((X−Y)·1, −(X+Y)·0.5)로 옮긴다. X는 화면 오른쪽 아래, Y는 왼쪽 아래로 뻗는다.
  - 클릭 좌표는 `TryPick`으로 역변환한다.
  - 매 프레임 `Sync`로 Board를 그린다. 게임 상태는 들고 있지 않고, 직전 프레임의 소유와 유닛만 기억한다. 그래서 바뀐 칸에는 연출이 붙는다. 소유가 바뀐 타일은 튀어 오르고, 새 유닛은 떨어지며 통통 튄다.
  - 그리는 순서(`sortingOrder`): 바다(-200)와 거품 → 타일과 오버레이((x+y)·10 + 0~5: 타일, 지형, 잠금, 보호막, 빛, 테두리) → 장식, 팀 링, 유닛(1000 + (x+y)·10 + 0~2) → 물 방향 화살표(1000 + (x+y)·10 + 5) → 이펙트(5000~).
  - 장식은 칸 좌표 해시로 고정 배치하고, 유닛이나 지뢰가 있거나 평지가 아니면 숨긴다.
  - 지형: 낭떠러지는 칸을 통째로 숨겨 바다가 보이게 한다. 용암과 블랙홀은 전용 타일(`tile_lava`, `tile_blackhole`)로 그린다. 얼음, 침묵, 컨베이어는 소유 색 위에 오버레이로 얹는다.
- `FxView`: 풀링된 SpriteRenderer로 프레임 이펙트를 재생한다. `delay`로 시차를 준다.
  - 연출은 `GameBootstrap.PlayEffects` 한 곳에서 정한다. 원소별 배치 이펙트 → 융합 이펙트 → 점유된 칸에 거리순으로 고리와 반짝임이 퍼진다.
  - 큰 반응(마그마, 백색, 흑색, 지뢰)은 화면을 흔든다(`_shake`).
- `Hud`는 IMGUI다. 스킨은 `Resources/Art/UI` 텍스처를 쓴다.
  - 기준 해상도는 540x960이다. `GUI.matrix`를 쓰지 않고 사각형과 글자 크기를 직접 키운다. 행렬로 확대하면 글자가 흐려지기 때문이다.
  - 한글은 `Resources/Fonts/Pretendard-Bold`로 그린다(OFL, `ThirdParty/Pretendard`). WebGL 기본 폰트에는 한글이 없다.
  - 버튼이 쓰지 않은 클릭만 보드로 보낸다.
- 씬(`Assets/Scenes/Main.unity`)과 PlayerSettings(세로 모드, WebGL 템플릿, 패키지 ID)는 `ProjectSetup.Run()`이 만들고 적용한다. ProjectSettings는 손으로 고치지 말고 여기에 추가한다.

## 아직 안 된 것 (PRD 대비)

- 공간 왜곡으로 옮긴 유닛이 그 자리에서 바로 융합하는 것. 지금은 옮기기만 하고, 융합은 다음에 놓는 유닛이 일으킨다.
- 융합 결과가 다시 다른 반응을 일으키는 다단 연쇄. 2단계 유닛을 재료로 쓰는 3차 조합은 구현했다.
- 덱 편집, 카드 해금 보상과 메타 성장(PRD §6), 사운드
- 스테이지 이름은 아직 "챕터명 + 번호"다(보스만 고유 이름).
- 돌풍(밀어내기)은 PRD 문장을 단순하게 해석했다. 주변 8칸의 내용을 한 칸 바깥으로 민 뒤 그 8칸을 점유한다.
