---
date: 2026-07-03
scope: [game1, Overthrone, Unity, local-bot, NavMesh]
type: feature
---

## TL;DR
로컬 봇의 이동을 straight-line MVP에서 NavMesh complete path 기반 path corner 추종으로 보강하였다. 실제 이동 적용은 계속 `PlayerInputReader.SetManualInput(...)`과 `PlayerMotor`를 통과하므로 상태별 이동 SSOT는 유지되며, NavMeshData는 별도 asset으로 분리해 `Prototype.unity`를 text YAML로 유지하였다.

## Keywords
`Overthrone` `LocalBotController` `NavMesh.CalculatePath` `NavMeshSurface` `NavMeshAgent` `PlayerInputReader.SetManualInput` `PlayerMotor` `Prototype.unity` `NavMesh-MoonlitGardenBlockout.asset`

## Checkpoint
- Path: `.codex/checkpoints/20260703-0845-overthrone-local-prototype.checkpoint.json`
- Status: `in_progress`
- Replay: `full checkpoint replay completed at 09:57`
- Note: 체크포인트의 전체 여정(문서 정리, Three.js 검증, Unity 이식, 로컬 봇/승리 조건 후속, UI 벤치마크)을 다시 확인한 뒤 이번 wrap을 작성하였다.

## Full Journey Timeline

### Phase 1 - GDD 재정리와 Unity 방향 확정
초기 작업은 오래된 `GDD.md`를 최신 방향으로 재정리하는 것이었다. 게임은 점령과 집결로 공수 권한이 실시간 전환되는 3인칭 비대칭 잠입/추격 PvP로 정리되었고, 플랫폼은 Steam/PC 우선, 모바일은 후순위로 결정되었다.

### Phase 2 - Three.js 검증에서 Unity 로컬 MVP로 전환
Unity 설치 전에는 Three.js 브라우저 프로토타입으로 이동, AI, 점령, 추격, 포획 루프를 빠르게 확인했다. Unity가 설치된 뒤에는 `unity/OverthroneUnity`가 주 구현 대상이 되었고, Three.js는 규칙 검증과 비교용 참고 구현으로 남았다.

### Phase 3 - Unity 핵심 루프 구축
Unity 6 프로젝트에 입력, 3인칭 카메라, 상태별 `MovementProfile`, 점령/왕 승계, 포획/구출, 관전, HUD, 미니맵, 핑, 슬라임 MVP가 들어갔다. 핵심 규칙은 static rule class와 EditMode test로 분리되어 네트워크 권위화 전 단계의 검증 발판을 제공한다.

### Phase 4 - 중간점검 P0 대응
중간점검에서 NPC 5명이 실제로 행동하지 않는 문제가 확인되었다. 이후 `LocalBotController`와 `PlayerInputReader.SetManualInput(...)` 경로가 추가되어 NPC가 점령 지점 이동, Attacker/King 추격과 덮치기, Held 아군 구출, King 최종 포획 보조를 수행하게 되었다. 이때 상대 전원 포획 승리, 시작 스폰 겹침 제거, Slime 속도 보정도 함께 처리되었다.

### Phase 5 - 승리 조건과 UI 벤치마크 후속
GDD 승리 조건 중 시간 종료 생존과 포기 조건이 추가되었다. 시간 종료는 active non-Captured 생존자 수, owned capture point 수, draw 순서로 판정하고, 포기는 양 팀 roster가 모두 존재한 뒤 한 팀 active/enabled 참가자가 0명이 될 때만 적용한다. UI 벤치마크는 공식/준공식 출처 중심으로 갱신되었다.

### Phase 6 - NavMesh 봇 경로 보강
이번 작업에서는 로컬 봇이 단순 직선 이동에만 의존하던 한계를 줄였다. `LocalBotController`는 NavMesh sample과 complete path 계산이 가능하면 다음 path corner를 steering target으로 삼고, 실패하면 기존 direct target 이동으로 fallback한다. Bootstrap은 scene에 `NavMeshSurface`를 추가하고, baked data를 별도 asset으로 저장하며, NPC 5명에는 `NavMeshAgent`를 붙인다.

## Context
중간점검의 핵심 지적은 "규칙 엔진은 좋은데 실제 플레이되는 게임이 약하다"는 것이었다. 첫 후속 작업으로 NPC가 움직이고 포획/구출/승리 조건에 참여하도록 만들었지만, 그 구현은 목표까지 직선으로 이동하는 MVP였다. 현재 블록아웃 맵은 단순 평면이어서 직선 이동도 플레이 루프 검증에는 충분하지만, GDD의 추격/잠입/우회 플레이로 가려면 최소한 NavMesh 기반 경로 계산을 시작해야 한다.

이번 변경은 완성형 AI가 아니라 로컬 플레이 가능성을 한 단계 더 올리는 pathing 보강이다. 행동 트리, 수색/경계, dynamic obstacle avoidance는 아직 미구현이다. 대신 이동 적용 경로는 기존 플레이어와 동일하게 유지하여 봇이 상태별 속도, sprint, slime, holding/held/captured 제한을 우회하지 않도록 했다.

## Investigation
이전 `LocalBotController`는 target position과 현재 위치의 차이를 바로 local input으로 변환했다. 이 방식은 컴포넌트 경로가 단순하고 안전하지만, 장애물이 생기면 벽을 향해 계속 입력을 넣는 구조다.

Unity 프로젝트에는 이미 `com.unity.ai.navigation` 패키지가 들어와 있었으므로, 별도 라이브러리 추가 없이 `UnityEngine.AI.NavMesh.CalculatePath`와 `Unity.AI.Navigation.NavMeshSurface`를 사용할 수 있었다. 구현 방향은 `NavMeshAgent`가 직접 위치를 이동시키는 방식이 아니라, NavMesh를 "다음 steering target을 계산하는 path planner"로만 쓰는 쪽으로 잡았다. 이렇게 해야 기존 `PlayerMotor`가 상태별 이동과 스태미나, sprint 입력을 계속 통제한다.

Bootstrap 검증 중 `NavMeshSurface.BuildNavMesh()`가 NavMeshData를 scene 내부에 직접 포함하면 `Prototype.unity`가 binary data로 저장되는 문제가 확인되었다. 저장소에서 scene diff가 불가능해지므로, NavMeshData를 `Assets/Scenes/Prototype/NavMesh-MoonlitGardenBlockout.asset`으로 분리하고 scene은 해당 asset reference만 들도록 수정했다.

## What Didn't Work

### ❌ NavMeshData를 scene에 직접 내장
- Tried: `NavMeshSurface.BuildNavMesh()` 직후 scene을 저장했다.
- Problem: `Prototype.unity`가 binary data로 바뀌어 `git diff`와 리뷰가 어려워졌다.
- Lesson: Unity NavMeshData는 scene 내부에 직접 박지 말고 별도 asset으로 분리해야 한다.

### ❌ NavMeshSurface collectObjects를 Children으로 제한
- Tried: 처음에는 ground에 붙은 surface가 children만 수집하도록 구성했다.
- Problem: ground object 자체 collider를 수집하지 못해 빈 NavMesh가 될 수 있었다.
- Lesson: 이번 블록아웃에서는 `CollectObjects.All`로 scene geometry를 수집하고, `ignoreNavMeshAgent/Obstacle` 기본값을 활용한다.

## Decision Rationale
봇 이동은 NavMeshAgent가 직접 주도하지 않는다. Overthrone의 이동 SSOT는 `PlayerMotor`와 `MovementProfile`이며, 봇은 플레이어와 같은 입력 레이어를 통과해야 상태별 이동과 포획 상태 제한이 일관된다. 따라서 NavMesh는 path corner를 계산하는 보조 수단으로만 사용한다.

fallback은 유지한다. NavMesh sample 실패, target sample 실패, partial/invalid path는 로컬 테스트 환경이나 미베이크 scene에서 발생할 수 있으므로 기존 direct target 이동으로 내려간다. 이 fallback은 완성형 장애물 회피가 아니라 NavMesh가 없는 로컬 개발 상황에서도 봇이 완전히 멈추지 않게 하는 보조 경로다.

NavMeshData는 별도 asset으로 저장한다. Unity binary asset 자체는 diff가 어렵지만, scene 전체가 binary로 변하는 것보다 영향 범위가 훨씬 작고 안전하다. Scene은 text YAML로 남아 `NavMeshSurface`, `NavMeshAgent`, 참조 GUID를 리뷰할 수 있다.

## Work Accomplished

### 1. LocalBotController NavMesh path corner 추종
`LocalBotController`에 `LocalBotMoveMode`, `LastMoveMode`, `LastSteeringTarget`을 추가하고, NavMesh complete path가 있으면 현재 위치 이후 첫 번째 유효 corner를 steering target으로 사용하도록 했다. 목표 최종 위치에 이미 도달한 경우 manual input을 0으로 클리어한다.

- File: `unity/OverthroneUnity/Assets/Scripts/AI/LocalBotController.cs`

### 2. Direct fallback 유지
NavMesh sample/path 계산이 실패하거나 complete path가 아니면 기존 direct target 이동으로 fallback한다. fallback도 `PlayerInputReader.SetManualInput(...)`만 사용하므로 `PlayerMotor`와 상태별 이동 제한을 우회하지 않는다.

- File: `unity/OverthroneUnity/Assets/Scripts/AI/LocalBotController.cs`

### 3. Bootstrap NavMeshSurface / NavMeshAgent 생성
`OverthroneUnityBootstrap`은 ground에 `NavMeshSurface`를 붙이고 `CollectObjects.All`, physics collider geometry 기반으로 NavMesh를 빌드한다. NPC 5명에는 `NavMeshAgent`가 추가되지만 `updatePosition`, `updateRotation`, `updateUpAxis`를 끄고 obstacle avoidance도 비활성화하여 현재 이동 SSOT와 충돌하지 않게 했다.

- File: `unity/OverthroneUnity/Assets/Editor/OverthroneUnityBootstrap.cs`
- File: `unity/OverthroneUnity/Assets/Scenes/Prototype.unity`

### 4. NavMeshData 외부 asset 분리
Bootstrap이 생성한 NavMeshData를 `Assets/Scenes/Prototype/NavMesh-MoonlitGardenBlockout.asset`으로 저장하도록 했다. `Prototype.unity`는 해당 asset GUID를 참조하며 ASCII YAML 형식을 유지한다.

- File: `unity/OverthroneUnity/Assets/Editor/OverthroneUnityBootstrap.cs`
- File: `unity/OverthroneUnity/Assets/Scenes/Prototype/NavMesh-MoonlitGardenBlockout.asset`
- File: `unity/OverthroneUnity/Assets/Scenes/Prototype/NavMesh-MoonlitGardenBlockout.asset.meta`

### 5. EditMode 테스트 추가
NavMesh 경로, fallback, 도착 경계, diagonal input normalization, null configuration input clear를 검증하는 테스트를 추가했다.

- Test: `NavMeshPathUsesFirstCornerWhenCompletePathExists`
- Test: `DirectFallbackUsesDestinationVectorWhenNoNavMeshPathExists`
- Test: `DirectFallbackNormalizesDiagonalDestinationBeforeInjectingInput`
- Test: `DirectFallbackStopsAtArrivalDistanceBoundary`
- Test: `DirectFallbackMovesJustOutsideArrivalDistanceBoundary`
- Test: `TickClearsManualInputWhenNullConfigurationLeavesNoFallbackTarget`
- File: `unity/OverthroneUnity/Assets/Tests/EditMode/LocalBotControllerTests.cs`

### 6. GDD/README 상태 갱신
GDD와 Unity README의 로컬 봇 설명을 straight-line MVP에서 NavMesh path corner + direct fallback으로 갱신하고, 남은 작업은 dynamic obstacle avoidance, 수색/경계 상태, PlayMode 장시간 검증으로 정리했다.

- File: `GDD.md`
- File: `unity/OverthroneUnity/README.md`

## Architecture Impact
`LocalBotController`는 여전히 행동 선택과 입력 주입을 담당하고, 실제 이동/속도/상태 제한은 `PlayerMotor`가 담당한다. NavMesh 도입으로 pathing 계산 책임이 추가되었지만, 이동 결과를 직접 transform에 쓰지 않으므로 상태 시스템과 충돌하지 않는다.

`OverthroneUnityBootstrap`은 scene 생성 시 NavMeshSurface와 NavMeshData asset을 함께 생성한다. 앞으로 blockout obstacle이 추가되면 bootstrap에서 obstacle object가 생성된 뒤 `CreateNavigationSurface(...)`를 호출하거나, surface build 시점과 수집 대상 순서를 다시 점검해야 한다.

Scene에는 NPC 5개의 `NavMeshAgent`와 ground의 `NavMeshSurface`가 들어간다. 현재 `NavMeshAgent`는 직접 이동하지 않으며 obstacle avoidance도 꺼져 있으므로 dynamic avoidance는 아직 제공하지 않는다.

## Files Changed

| File | Change |
|------|--------|
| `.codex/checkpoints/20260703-0845-overthrone-local-prototype.checkpoint.json` | NavMesh 봇 경로 보강 phase, 결정, 남은 리스크, 검증 결과 갱신 |
| `GDD.md` | 로컬 봇/AI hearing 구현 상태를 NavMesh path corner + direct fallback 기준으로 갱신 |
| `cycles/2026-07/wk1/07-03/0957-navmesh-bot-pathing-wrap.md` | 이번 후속 작업 wrap 문서 |
| `unity/OverthroneUnity/Assets/Editor/OverthroneUnityBootstrap.cs` | text serialization 보장, NavMeshSurface 생성, NavMeshData asset 저장, NPC NavMeshAgent 추가 |
| `unity/OverthroneUnity/Assets/Scenes/Prototype.unity` | ground NavMeshSurface, NPC 5명 NavMeshAgent, 외부 NavMeshData 참조 반영 |
| `unity/OverthroneUnity/Assets/Scenes/Prototype.meta` | NavMeshData asset 폴더 meta |
| `unity/OverthroneUnity/Assets/Scenes/Prototype/NavMesh-MoonlitGardenBlockout.asset` | Prototype scene용 baked NavMeshData |
| `unity/OverthroneUnity/Assets/Scenes/Prototype/NavMesh-MoonlitGardenBlockout.asset.meta` | NavMeshData asset GUID |
| `unity/OverthroneUnity/Assets/Scripts/AI/LocalBotController.cs` | NavMesh complete path corner 추종, direct fallback, steering telemetry |
| `unity/OverthroneUnity/Assets/Tests/EditMode/LocalBotControllerTests.cs` | NavMesh/fallback/arrival/null configuration 테스트 추가 |
| `unity/OverthroneUnity/README.md` | 로컬 봇 설명과 남은 작업 갱신 |

## Verification
- Unity bootstrap: `/Applications/Unity/Hub/Editor/6000.0.78f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/wy/Github-repos/game1/unity/OverthroneUnity -executeMethod OverthroneUnityBootstrap.BootstrapPrototypeScene -quit -logFile -` exit code 0
- Unity EditMode: `/Applications/Unity/Hub/Editor/6000.0.78f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/wy/Github-repos/game1/unity/OverthroneUnity -runTests -testPlatform EditMode -testResults /Users/wy/Github-repos/game1/unity/OverthroneUnity/TestResults.xml -logFile -` exit code 0
- `unity/OverthroneUnity/TestResults.xml`: 10:04 KST 기준 `total="120" passed="120" failed="0" skipped="0"`
- `git diff --check`: Passed
- Checkpoint JSON parse: Passed
- `Prototype.unity`: ASCII text 확인
- Scene evidence: `NavMeshSurface`가 `NavMesh-MoonlitGardenBlockout.asset` GUID `10352c7990d43416c936ae80f31dbd77`를 참조하고, NPC 5명에 `NavMeshAgent`가 존재하며 `m_BaseOffset: 0`임을 확인
- Independent review gate: reviewer role은 계정 모델 제한으로 실패했고, default subagent 리뷰는 2분 이상 timeout되어 완료 전 커밋에는 반영하지 못했다. 대신 main session에서 scene asset/text serialization, fallback, baseOffset, generated controller churn을 직접 재검토하였다.

## Remaining Risks / Follow-up
- Dynamic obstacle avoidance는 아직 없다. `NavMeshAgent`의 obstacle avoidance는 꺼져 있고, 현재 봇은 path corner만 따라간다.
- 수색/경계/소리 기반 행동 전환은 아직 없다. `AIHearingSensor`는 기억만 제공하며 bot behavior planner와 직접 연결되지 않았다.
- PlayMode/E2E 장시간 플레이 검증은 아직 없다. EditMode 규칙 검증은 통과했지만 실제 scene에서 몇 분 동안 3v3 루프가 자연스럽게 유지되는지는 다음 단계에서 확인해야 한다.
- 왕 단독 `Tackle -> Hold -> Capture` 허용 여부는 설계 결정이 필요하다.
- Photon/Supabase/Steam SDK, 정식 캐릭터 모델/모션, 정식 VFX/SFX는 아직 붙이지 않았다.

## Commit Scope Check
- Included: NavMesh bot pathing code/tests, Unity bootstrap/scene/NavMeshData asset, GDD/README/checkpoint/wrap documentation.
- Excluded: Unity generated caches, `TestResults.xml`, unrelated prototype outputs.

## Commit
feat(overthrone): add navmesh bot pathing

Co-Authored-By: Codex <noreply@openai.com>
