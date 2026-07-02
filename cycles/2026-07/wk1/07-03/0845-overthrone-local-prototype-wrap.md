---
date: 2026-07-03
scope: [game1, Overthrone, Unity, GDD, local-prototype]
type: feature
---

## TL;DR
Overthrone의 오래된 GDD를 최신 기준으로 재정리하고, Three.js 규칙 검증 프로토타입을 거쳐 Unity 6 로컬 프로토타입으로 핵심 루프를 이식하였다. 현재 로컬 Unity 프로젝트는 이동/카메라, 상태별 이동, 점령/왕 승계, 포획/구출, HUD/미니맵/핑, 슬라임 MVP까지 EditMode 테스트 102개로 검증된 상태이며, Photon/Supabase/정식 아트/온라인 기능은 다음 단계로 남아 있다.

## Keywords
`game1` `Overthrone` `GDD.md` `Unity 6` `Three.js prototype` `PlayerMotor` `PlayerStateController` `LocalMatchManager` `LocalCaptureSystem` `LocalPingSystem` `Slime`

## Checkpoint
- Path: `.codex/checkpoints/20260703-0845-overthrone-local-prototype.checkpoint.json`
- Status: `stopped`
- Replay: `full checkpoint replay completed at 08:45`
- Note: 세션 중 기존 checkpoint 파일이 없어, 현재까지의 전체 여정을 checkpoint JSON으로 재구성한 뒤 이 문서를 작성하였다.

## Full Journey Timeline

### Phase 1 - GDD 재검토와 제품 방향 정리
초기 요청은 프로젝트의 Markdown 문서들을 읽고 정리하는 것이었다. `GDD.md`는 오래된 상태였고, 1인칭 중심 설명, 플랫폼 우선순위, 상태/왕/승리 조건의 세부 규칙이 현재 구현 방향과 맞지 않았다. 문서를 전면 갱신하여 Overthrone을 "점령과 집결로 공수가 실시간 전환되는 3인칭 비대칭 잠입/추격 게임"으로 정리하였다.

이 과정에서 1인칭과 3인칭의 장단점을 비교했고, 최종적으로 Steam/PC 우선, 3인칭 추격 카메라 기본, 모바일은 후순위라는 방향을 잡았다. GitHub/오픈소스 참고 게임은 직접 도입 대상이 아니라 UI/구조/기술 방향 참고 대상으로만 문서화하였다.

### Phase 2 - Three.js 브라우저 프로토타입
Unity 설치 전에는 로컬에서 빠르게 규칙을 확인하기 위해 Three.js 기반 브라우저 프로토타입을 만들었다. 이 프로토타입은 메인 화면, 방 생성, 진영 선택, AI 수 선택, 달빛 정원 테스트 맵, 3인칭 카메라, 슬라임, 점령 포인트, 팀 목표, AI 이동/점령/추격/붙들기/포획을 구현했다.

이후 사용자가 "왜 Unity로 안 하냐"고 물었고, Unity가 설치되면서 Three.js 프로토타입은 최종 구현이 아니라 규칙 검증용 참고 구현으로 위치를 낮췄다.

### Phase 3 - Unity 6 로컬 프로젝트 생성
Unity 6 프로젝트를 `unity/OverthroneUnity`에 만들고, URP/Input System/Test Framework 기반으로 로컬-first 구조를 구성했다. `OverthroneUnityBootstrap`은 기본 씬, 플레이어, AI, 점령 포인트, HUD, 이동 프로필, placeholder 애니메이션, 테스트용 재질과 프리팹을 생성하는 역할을 맡는다.

외부 인프라는 아직 붙이지 않았다. Photon Fusion, Supabase, Steam SDK는 GDD의 장기 방향으로 남겼고, 현재 완료 조건은 Unity Editor에서 로컬 씬을 열어 플레이 가능하며 EditMode 테스트로 핵심 규칙을 증명하는 것이다.

### Phase 4 - 이동/카메라/입력 안정화
사용자가 마우스 시점 조작과 방향키 좌우 반전 문제를 지적하면서 입력 계층을 정리했다. `PlayerInputReader`는 마우스와 게임패드 look source를 구분하고, `PlayerMotor`는 마우스 pixel delta와 게임패드 degrees/sec 회전을 분리한다. 카메라는 클릭 없이 움직이도록 보편적인 PC TPS/FPS 조작에 맞췄고, `ThirdPersonCameraRig`는 `SphereCast` 기반 충돌 보정으로 벽 뒤에서 카메라가 끼는 문제를 줄였다.

Dash는 `Q`와 Shift 더블탭으로 연결되었고, Crouch/Jump/Sprint/Slime/Tackle/Capture/Ping/Spectate 입력도 Input Actions에 포함되었다.

### Phase 5 - 상태별 이동, 점령, 왕 승계
GDD의 상태 규칙을 Unity `MovementProfile`과 상태 시스템으로 옮겼다. Neutral, Attacker, King, Held, Captured, Slime, Holding 상태별 이동 가능 여부, 스프린트 가능 여부, 속도, 소음 반경을 분리하였다. Held/Captured/Holding은 수평 속도를 즉시 0으로 만들도록 검증했다.

로컬 3v3 구성을 위해 Blue 3명, Red 3명 roster를 만들었고, 팀이 2개 이상 점령할 때 팀당 King 1명을 선정하도록 했다. 왕 승계 우선순위는 최종 포획 수, 점령 기여도, tie-breaker 순이다. Attacker는 3명 집결 기반이며, 이탈 후 5초 유예를 둔다.

### Phase 6 - 포획/구출/관전/피드백
`LocalCaptureSystem`과 `PlayerCaptureAgent`로 덮치기, 붙들기, 왕 최종 포획, 같은 팀 구출, enemy Holding 대상 interrupt를 구현하였다. 덮치기는 `TackleHitbox` 전방 물리 히트박스를 기본 경로로 사용하며, 히트박스 컴포넌트가 없는 레거시 상황에서만 agent 목록 스캔 fallback을 쓴다.

Captured 상태가 되면 `LocalSpectatorCamera`가 같은 팀 생존자를 따라가며, `Q`/`Tab`으로 관전 대상을 순환한다. `LocalDeadChannel`은 Captured 전용 같은 팀 로그를 제공한다. `CaptureFeedbackSystem`과 `CaptureFeedbackController`는 절차적 파티클과 오디오 원샷으로 로컬 VFX/SFX를 제공한다.

### Phase 7 - HUD, 미니맵, 핑
HUD는 스태미나, 상태 텍스트, Dash/Slime 쿨다운/스태미나 부족 표시, 점령 패널, 팀 상태 레일, 포획 진행 링, 붙들림/구출 링, 관전 overlay, 미니맵을 포함한다. 미니맵은 자신, 아군, 적 왕, 적 공격자, 점령 포인트, 로컬 핑 마커를 표시한다.

핑은 `G` 탭 컨텍스트 핑과 `G` 홀드 방사형 핑 휠을 제공하며, 로컬 응답 로그까지 연결하였다. 온라인 RPC 동기화와 아이콘 아트는 아직 남은 작업이다.

### Phase 8 - 슬라임 MVP 마감
마지막 작업 범위는 GDD의 슬라임 항목 중 "히트박스/마찰/변형 비주얼"이었다. 기존에는 R 입력 시 스태미나 50 소모, 15초 쿨다운, 3초 Slime 임시 상태, Held 상태에서 매치당 1회 자가 탈출까지만 있었다.

이번 마감에서 `PlayerMotor`에 Slime shape 설정을 추가했다. Slime 상태 진입 시 `CharacterController` height/radius를 줄이고, transform scale을 XZ 방향으로 넓히고 Y 방향으로 낮춰 placeholder squash/widen 변형을 만든다. 이동 입력이 없을 때는 `slimeGroundFrictionMultiplier`로 감속량을 낮춰 미끄러지는 느낌을 제공한다. `MovementProfileTests`에 히트박스/비주얼 복원 테스트와 no-input friction 테스트를 추가했다.

## Context
Overthrone은 한 명의 고정 술래가 있는 숨바꼭질이 아니라, 점령과 집결에 따라 공격/도주 권한이 실시간으로 바뀌는 게임이다. 핵심 재미는 "쫓기던 사람이 순식간에 추격자가 되는 역전감", "왕만 최종 포획할 수 있는 협동 강제", "슬라임으로 탈출/기동을 만드는 공간 플레이"다.

초기 문서는 오래된 초안에 가까웠고, 실제 구현은 없거나 브라우저 프로토타입에 머물러 있었다. 사용자는 로컬에서 직접 실행 가능한 Unity 프로젝트를 원했고, Unity 초보자이므로 프로젝트를 열었을 때 바로 확인 가능한 씬, 테스트, README가 필요했다.

## Investigation
초기에는 문서와 구현 사이의 간극을 확인했다. `GDD.md`의 오래된 1인칭 중심 방향은 현재 요구와 맞지 않았고, Steam/PC와 모바일의 우선순위도 명확히 정리되어야 했다. Unity 프로젝트 생성 후에는 기능별로 EditMode 테스트를 늘리면서 GDD의 "완료/부분 완료/미구현" 상태표와 실제 코드가 어긋나지 않게 관리했다.

마지막 슬라임 작업에서는 `PlayerStateController`가 이미 슬라임 입력/쿨다운/스태미나/탈출을 담당하고 있고, 실제 이동/히트박스/감속은 `PlayerMotor`가 소유하는 것이 가장 자연스럽다고 판단했다. 따라서 상태 전환에 따른 물리/비주얼 변화는 `PlayerMotor.SetState(...)`에서 처리하도록 했다.

## What Didn't Work

### ❌ Three.js를 최종 구현으로 밀고 가는 방향
- Tried: Unity 설치 전 브라우저 기반 Three.js 프로토타입으로 핵심 루프를 빠르게 구현했다.
- Problem: 캐릭터 컨트롤러, 애니메이션, 포획 히트박스, 플랫폼 출시, 장기 멀티플레이 설계는 Unity 쪽이 훨씬 적합했다.
- Lesson: Three.js는 규칙 검증용으로 유용하지만, 이 프로젝트의 최종 구현/로컬 실행 기준은 Unity로 두는 것이 맞다.

### ❌ 마우스 클릭 후 시점 조작
- Tried: 브라우저 prototype 초기 방식에서는 클릭/잠금 이후 시점 조작이 강조되었다.
- Problem: 사용자가 기대한 일반 PC 게임 조작은 마우스 이동만으로 시점이 움직이는 방식이었다.
- Lesson: Overthrone의 PC 기본값은 WASD/방향키 + 항상 반응하는 mouse look이 맞다. Cursor lock은 편의 장치이지 시점 입력의 전제 조건이 아니다.

### ❌ 슬라임을 상태 이름만으로 처리
- Tried: 초기 Unity 구현은 Slime movement state와 속도/쿨다운만 갖고 있었다.
- Problem: GDD의 슬라임은 히트박스 축소, 낮은 마찰, 형태 변형이 핵심인데 상태 이름만으로는 체감이 부족했다.
- Lesson: MVP라도 CharacterController 크기와 감속, placeholder shape 변화까지 넣어야 GDD 항목을 "부분 완료" 근거로 기록할 수 있다.

## Decision Rationale
Unity 전환은 장기적으로 맞는 결정이다. Photon/Supabase/Steam 출시 방향, 애니메이션/히트박스/카메라/입력/테스트 구조를 고려하면 웹 프로토타입은 빠른 규칙 실험에는 좋지만 최종 제품 기반으로는 부족하다.

슬라임 MVP는 softbody나 shader부터 만들지 않았다. 정식 비주얼은 에셋과 렌더링 파이프라인 결정이 필요하고, 지금 단계에서는 "상태가 실제 물리와 조작감에 영향을 준다"는 증거가 더 중요했다. 그래서 `CharacterController`와 transform scale, 감속 multiplier를 사용했다.

커밋 범위는 현재 프로젝트 전체의 로컬 프로토타입 스냅샷으로 잡았다. Unity generated `Library/`, `Logs/`, `UserSettings/`, Node `node_modules/`, Vite `dist/`, prototype screenshots는 제외하고, GDD, web prototype source, Unity source/assets/project settings, checkpoint, wrap 문서를 포함한다.

## Work Accomplished

### 1. GDD 현대화 및 구현 상태표 갱신
GDD를 2026-07-03 기준 문서로 갱신하고, 현재 Unity 로컬 prototype의 완료/부분 완료/미구현 상태를 표로 정리했다. 슬라임 항목은 이번 작업 후 `CharacterController` 히트박스 축소, no-input 감속 완화, placeholder squash/widen 변형 비주얼을 완료 근거에 추가했다.
- File: `GDD.md`

### 2. Browser prototype 보존
Three.js 기반 prototype은 Unity 전환 전 규칙 검증용으로 남겼다. `npm run verify`는 Vite build, 규칙 검사, 로컬 서버 smoke check를 묶는다.
- File: `package.json`
- File: `prototype/src/game.js`
- File: `prototype/README.md`

### 3. Unity 로컬 프로젝트 구성
Unity 6 프로젝트, package manifest, ProjectSettings, URP 기본 자산, 씬, 프리팹, 이동 프로필, placeholder 애니메이션을 포함했다. `OverthroneUnityBootstrap`이 로컬 씬과 기본 자산을 재생성할 수 있는 중심 도구다.
- File: `unity/OverthroneUnity/Packages/manifest.json`
- File: `unity/OverthroneUnity/ProjectSettings/ProjectVersion.txt`
- File: `unity/OverthroneUnity/Assets/Editor/OverthroneUnityBootstrap.cs`
- File: `unity/OverthroneUnity/Assets/Scenes/Prototype.unity`

### 4. Unity runtime systems
입력, 이동, 상태, 카메라, 점령, 왕 승계, 포획, 구출, 관전, dead channel, ping, local CSV data mock, HUD, AI hearing, noise emitter를 로컬 runtime으로 구현했다.
- File: `unity/OverthroneUnity/Assets/Scripts/Input/PlayerInputReader.cs`
- File: `unity/OverthroneUnity/Assets/Scripts/Movement/PlayerMotor.cs`
- File: `unity/OverthroneUnity/Assets/Scripts/States/PlayerStateController.cs`
- File: `unity/OverthroneUnity/Assets/Scripts/Match/LocalMatchManager.cs`
- File: `unity/OverthroneUnity/Assets/Scripts/Capture/LocalCaptureSystem.cs`
- File: `unity/OverthroneUnity/Assets/Scripts/PlayerHud.cs`

### 5. 슬라임 히트박스/마찰/변형 MVP
`PlayerMotor`에 Slime shape 설정을 추가했다. Slime 상태에서 controller height/radius를 줄이고, transform scale을 squash/widen 형태로 바꾸며, 입력이 없을 때 감속을 낮춰 미끄러짐을 만든다. Neutral 등 다른 상태로 돌아오면 기본 controller shape과 scale을 복원한다.
- File: `unity/OverthroneUnity/Assets/Scripts/Movement/PlayerMotor.cs`

### 6. EditMode 테스트 확대
Unity EditMode 테스트는 102개까지 확장되었다. 마지막으로 추가한 테스트는 `SlimeStateShrinksControllerAndSquashesVisual`, `SlimeStateReducesNoInputGroundFriction`이다.
- File: `unity/OverthroneUnity/Assets/Tests/EditMode/MovementProfileTests.cs`

## Architecture Impact
현재 구조는 로컬-first 단일 플레이/로컬 3v3 검증용이다. 핵심 규칙은 C# runtime class와 EditMode tests에 있으므로 Photon/server-authoritative tick으로 옮길 수 있는 발판은 있지만, 아직 네트워크 권위화는 되어 있지 않다.

`PlayerMotor`는 이동/스태미나/Dash/Slime shape를 함께 소유한다. 장기적으로 애니메이션, collider shape, visual mesh가 분리되면 Slime shape 적용을 별도 `PlayerFormController` 또는 visual child 기준으로 분리할 수 있다. 지금은 placeholder capsule/root transform 구조라 `PlayerMotor`에 두는 것이 가장 작고 검증 가능하다.

## Files Changed

| File | Change |
|------|--------|
| `.gitignore` | Node/Vite/Unity generated file 제외 규칙과 `TestResults.xml` 제외 추가 |
| `.codex/checkpoints/20260703-0845-overthrone-local-prototype.checkpoint.json` | 전체 여정 checkpoint 재구성 |
| `cycles/2026-07/wk1/07-03/0845-overthrone-local-prototype-wrap.md` | 세션 wrap 문서 |
| `GDD.md` | 최신 GDD, 구현 상태표, 벤치마크/로드맵/현재 범위 정리 |
| `package.json`, `package-lock.json`, `index.html` | Browser prototype 실행/검증 진입점 |
| `prototype/**` | Three.js 규칙 검증 prototype source |
| `unity/OverthroneUnity/**` | Unity 6 로컬 prototype source/assets/settings/tests |

## Verification
- `node -e "JSON.parse(require('fs').readFileSync('unity/OverthroneUnity/Assets/Input/OverthroneControls.inputactions','utf8')); console.log('inputactions json ok')"`: Passed
- `git diff --check`: Passed
- `/Applications/Unity/Hub/Editor/6000.0.78f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/wy/Github-repos/game1/unity/OverthroneUnity -runTests -testPlatform EditMode -testResults /Users/wy/Github-repos/game1/unity/OverthroneUnity/TestResults.xml -logFile -`: Exit code 0
- `unity/OverthroneUnity/TestResults.xml`: `total="102" passed="102" failed="0" skipped="0"`

## Remaining Risks / Follow-up
- Photon Fusion 2, Supabase Auth/PostgreSQL/Edge Function, Steam SDK는 아직 미연결이다.
- 정식 캐릭터 모델/모션 클립, 점프/앉기/덮치기/슬라임 애니메이션이 필요하다.
- 슬라임은 placeholder shape까지이며 shader/softbody VFX와 좁은 통로 레벨 검증은 남았다.
- 라운드 재시작 구조가 생기면 `PlayerCaptureAgent`의 매치당 슬라임 탈출권 reset hook이 필요하다.
- dead channel 실제 입력 UI, 온라인 채팅, moderation 설계가 필요하다.
- 현재 테스트는 EditMode 중심이다. PlayMode/E2E와 실제 빌드 검증은 다음 단계다.

## Commit Scope Check
- Included: GDD, local browser prototype source, Unity source/assets/project settings/tests, checkpoint, wrap documentation.
- Excluded: `node_modules/`, `dist/`, `prototype/artifacts/`, Unity `Library/`, `Temp/`, `Logs/`, `UserSettings/`, generated `TestResults.xml`.

## Commit
feat(overthrone): add local Unity prototype

Co-Authored-By: Codex <noreply@openai.com>
