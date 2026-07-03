---
date: 2026-07-03
scope: [game1, Overthrone, Unity, local-bot, AI-hearing]
type: feature
---

## TL;DR
로컬 봇이 `AIHearingSensor`가 기억한 적 팀 달리기 소음 위치를 조사하도록 연결하였다. 독립 리뷰에서 확인된 NPC 소음 생산자 누락과 아군 소음 overwrite edge case까지 수정하여, Unity EditMode 123/123 통과와 scene component count로 검증하였다.

## Keywords
`Overthrone` `LocalBotController` `AIHearingSensor` `PlayerNoiseEmitter` `NoiseSystem` `rememberedEnemyNoisePosition` `CreateRosterNpc` `Prototype.unity`

## Checkpoint
- Path: `.codex/checkpoints/20260703-0845-overthrone-local-prototype.checkpoint.json`
- Status: `in_progress`
- Replay: full checkpoint replay completed at 20:13
- Note: 기존 문서 정리, Three.js 검증, Unity 이식, 로컬 봇/승리 조건, NavMesh pathing까지의 전체 흐름을 확인한 뒤 이번 적 소음 조사 AI MVP를 추가하였다.

## Context
중간점검에서 가장 큰 문제는 Unity 씬이 "규칙 엔진은 좋지만 실제 게임플레이는 부족한" 상태라는 점이었다. 앞선 후속 작업으로 NPC 5명은 `LocalBotController`를 통해 점령, 추격, 덮치기, 구출, 왕 최종 포획 보조를 수행하게 되었고, 이후 NavMesh path corner 추종도 추가되었다.

남은 AI gap 중 하나는 소리 기반 행동이었다. 기존 `AIHearingSensor`는 `NoiseSystem` 이벤트를 듣고 마지막 소음 위치를 기억할 수 있었지만, 봇 행동 선택에는 연결되지 않았다. 따라서 달리기는 소음을 내고 걷기는 들키지 않는다는 GDD/README 설명이 실제 로컬 봇 의사결정으로 이어지지 않았다.

이번 작업의 목표는 완성형 수색/경계 AI가 아니라, 로컬 playable MVP에서 "보이는 적이 없고 더 급한 포획/구출 일이 없으면 적 소음 위치를 조사한다"는 최소 행동을 넣는 것이었다.

## Investigation
기존 행동 우선순위는 왕의 Held 적 최종 포획, Holding 정지, Held 아군 구출, visible enemy 추격/덮치기, capture point 이동 순서였다. 여기에 소음 조사를 넣을 때는 포획/구출 같은 핵심 루프를 방해하지 않아야 했다.

`NoiseSystem.Emit(...)`의 실제 생산자는 `PlayerNoiseEmitter`였다. 초기 구현에서는 NPC에 `AIHearingSensor`만 붙고 `PlayerNoiseEmitter`가 없어, 봇이 소리를 듣기는 해도 봇끼리의 달리기 소음은 발생하지 않는 문제가 독립 리뷰에서 확인되었다.

또한 `AIHearingSensor`는 마지막 소음 하나만 기억하므로, 적 소음을 들은 직후 아군 소음이 들어오면 `LocalBotController`가 같은 팀 source라며 조사를 중단하는 edge case가 있었다. 이 문제는 센서에 팀 정책을 넣기보다, 봇 컨트롤러가 적으로 판정된 소음만 별도 캐시에 보존하는 방식으로 해결하였다.

## What Didn't Work

### ❌ AIHearingSensor만 NPC에 붙이는 접근
- Tried: roster NPC에 `AIHearingSensor`를 붙이고 `LocalBotController`가 센서의 마지막 소음 source/team을 읽게 했다.
- Problem: NPC가 `PlayerNoiseEmitter`와 `AudioSource`를 갖지 않으면 실제 `NoiseSystem.Emit(...)` 경로가 없다. 이 경우 플레이어 소음은 들을 수 있어도 NPC 간 소음 조사는 동작하지 않는다.
- Lesson: hearing 기능은 listener와 emitter가 모두 scene component로 존재해야 한다. Bootstrap과 scene count를 함께 검증해야 한다.

### ❌ 테스트에서 private lifecycle을 반사 호출
- Tried: EditMode 테스트에서 `AIHearingSensor.OnEnable/OnDisable`을 reflection으로 호출해 `NoiseSystem` 구독을 강제로 만들었다.
- Problem: destroyed sensor가 정적 이벤트에 남는 테스트 오염 위험이 생겼고, private Unity lifecycle에 테스트가 과하게 결합되었다.
- Lesson: 센서의 기억 로직은 `TryRememberNoise(...)`로 공개 계약화하고, 이벤트 broadcast 자체는 기존 `NoiseSystemBroadcastsRunNoise` 테스트가 담당하게 나누는 편이 안정적이다.

### ❌ 센서의 마지막 소음을 그대로 행동에 사용
- Tried: `LocalBotController`가 `AIHearingSensor.LastHeardSource`와 `LastHeardPosition`을 직접 읽어 적이면 조사하고 아니면 무시하게 했다.
- Problem: 적 소음 뒤에 아군 소음이 들어오면 센서 memory가 아군 source로 덮여 직전 적 소음 조사가 사라진다.
- Lesson: 센서는 일반 perception memory로 두고, 봇 정책에 필요한 "마지막 적 소음"은 controller가 별도 캐시해야 한다.

## Decision Rationale
소음 조사는 행동 우선순위에서 capture point 이동보다만 우선한다. Held 적 포획, Held 아군 구출, visible enemy 추격/덮치기는 즉시 플레이 루프에 더 중요하므로 기존 우선순위를 유지하였다.

`AIHearingSensor`에는 팀 필터를 넣지 않았다. 센서는 어떤 GameObject의 소음이든 반경 안이면 기억하는 perception component이고, 팀 판별과 조사 정책은 `LocalBotController`의 책임이다. 이렇게 하면 향후 neutral object, 미끼, 환경 소음 같은 요소를 추가할 때 센서를 재사용하기 쉽다.

NPC에도 `PlayerNoiseEmitter`를 붙였다. 문서가 "로컬 봇은 적 팀 소음을 조사한다"고 말하려면 local player뿐 아니라 roster NPC도 소음 생산자가 되어야 하며, scene에서 3v3의 각 참가자가 같은 소음 규칙을 통과해야 한다.

## Work Accomplished

### 1. AIHearingSensor 기억 로직 공개 계약화
`AIHearingSensor.TryRememberNoise(...)`를 추가하여 self noise와 range 밖 noise를 거르고, 기억 성공 여부를 bool로 반환하게 했다. `OnNoiseEmitted(...)`는 이 메서드를 호출한다. `OnDestroy`에서도 정적 이벤트 구독을 해제해 테스트/scene 파괴 시 stale listener 위험을 낮췄다.

- File: `unity/OverthroneUnity/Assets/Scripts/AI/AIHearingSensor.cs`

### 2. LocalBotController 적 소음 조사 우선순위 추가
`LocalBotController`가 같은 GameObject의 optional `AIHearingSensor`를 resolve하고, 적으로 판정된 소음 위치를 `rememberedEnemyNoisePosition`에 캐시한다. 행동 선택에서는 visible enemy 추격 이후, capture point 선택 이전에 이 위치로 이동한다.

- File: `unity/OverthroneUnity/Assets/Scripts/AI/LocalBotController.cs`

### 3. 아군 소음 overwrite 방지
센서의 마지막 source가 아군이면 enemy memory를 갱신하지 않는다. 이미 저장된 적 소음 memory는 `enemyNoiseMemorySeconds` 동안 유지되어, 적 소음 직후 아군 소음이 들어와도 봇은 계속 적 위치를 조사한다.

- File: `unity/OverthroneUnity/Assets/Scripts/AI/LocalBotController.cs`
- Test: `FriendlyNoiseDoesNotOverwriteRememberedEnemyNoise`

### 4. NPC 소음 생산자 연결
`CreateRosterNpc(...)`가 NPC에 `AudioSource`, `PlayerNoiseEmitter`, `AIHearingSensor`, `LocalBotController`를 함께 붙인다. 이로써 NPC도 플레이어와 같은 `PlayerNoiseEmitter -> NoiseSystem.Emit(...)` 경로를 통해 달리기 소음을 낸다.

- File: `unity/OverthroneUnity/Assets/Editor/OverthroneUnityBootstrap.cs`
- File: `unity/OverthroneUnity/Assets/Scenes/Prototype.unity`

### 5. 테스트와 문서 갱신
`LocalBotControllerTests`에 적 소음이 capture point보다 우선되는지, 같은 팀 소음은 capture point 선택으로 fallback되는지, 아군 소음이 직전 적 소음 memory를 덮지 않는지 검증을 추가했다. GDD와 README의 로컬 봇/AI hearing 상태도 적 소음 조사 MVP 기준으로 갱신했다.

- File: `unity/OverthroneUnity/Assets/Tests/EditMode/LocalBotControllerTests.cs`
- File: `GDD.md`
- File: `unity/OverthroneUnity/README.md`

## Review Follow-up
독립 리뷰에서 최초 verdict는 Not ready였다.

- P2: roster NPC가 소리를 듣기만 하고 내지는 못했다. 수정 후 `CreateRosterNpc`에 `AudioSource`와 `PlayerNoiseEmitter`를 추가했고, scene count로 `AIHearingSensor=6`, `PlayerNoiseEmitter=6`, `AudioSource=7`을 확인했다.
- P3: friendly noise가 enemy noise memory를 덮어쓸 수 있었다. 수정 후 `LocalBotController`에 enemy-only memory cache를 추가했고, `FriendlyNoiseDoesNotOverwriteRememberedEnemyNoise` 테스트를 추가했다.

재리뷰 verdict는 Ready/GO였고, 남은 P0-P2는 없다고 확인되었다.

## Architecture Impact
소리 기반 AI는 아직 수색/경계 상태머신이 아니다. 현재는 "마지막 적 소음 위치로 이동"하는 MVP이며, 도착 후 주변 탐색, 경계 모드, 의심도, 시야 센서 결합은 다음 단계로 남는다.

이동 적용 경로는 여전히 `MoveToward(...) -> PlayerInputReader.SetManualInput(...) -> PlayerMotor`이다. 따라서 봇은 PlayerMotor의 상태별 속도, sprint 가능 여부, Holding/Held/Captured 정지 규칙을 우회하지 않는다.

`AIHearingSensor`는 특정 팀을 알지 못한다. 팀/정책 판단은 `LocalBotController`가 맡는다. 이 분리는 향후 센서 데이터화나 vision/proximity/hearing perception interface 도입에 유리하다.

## Files Changed
| File | Change |
|------|--------|
| `.codex/checkpoints/20260703-0845-overthrone-local-prototype.checkpoint.json` | 적 소음 조사 AI MVP phase, 리뷰 후속, 검증 상태 갱신 |
| `GDD.md` | 로컬 봇/AI hearing 진행 상태에 적 팀 소음 위치 조사 MVP 반영 |
| `cycles/2026-07/wk1/07-03/2013-hearing-investigation-bots-wrap.md` | 이번 세션 wrap 문서 |
| `unity/OverthroneUnity/Assets/Editor/OverthroneUnityBootstrap.cs` | NPC에 AudioSource, PlayerNoiseEmitter, AIHearingSensor 연결 |
| `unity/OverthroneUnity/Assets/Scenes/Prototype.unity` | Bootstrap 재생성 결과로 NPC/플레이어 소음/센서 컴포넌트 반영 |
| `unity/OverthroneUnity/Assets/Scripts/AI/AIHearingSensor.cs` | TryRememberNoise 공개 계약, OnDestroy unsubscribe 추가 |
| `unity/OverthroneUnity/Assets/Scripts/AI/LocalBotController.cs` | 적 소음 memory cache와 조사 우선순위 추가 |
| `unity/OverthroneUnity/Assets/Tests/EditMode/LocalBotControllerTests.cs` | 적 소음 우선순위, 같은 팀 소음 무시, friendly overwrite 방지 테스트 추가 |
| `unity/OverthroneUnity/README.md` | 로컬 봇/AI hearing 설명과 한계 갱신 |

## Verification
- Unity bootstrap: `/Applications/Unity/Hub/Editor/6000.0.78f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/wy/Github-repos/game1/unity/OverthroneUnity -executeMethod OverthroneUnityBootstrap.BootstrapPrototypeScene -logFile -` exit code 0
- Unity EditMode: `/Applications/Unity/Hub/Editor/6000.0.78f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/wy/Github-repos/game1/unity/OverthroneUnity -runTests -testPlatform EditMode -testResults /Users/wy/Github-repos/game1/unity/OverthroneUnity/TestResults.xml -logFile -` exit code 0
- `unity/OverthroneUnity/TestResults.xml`: `total="123" passed="123" failed="0"`
- `git diff --check`: Passed
- Scene count: `AIHearingSensor=6`, `PlayerNoiseEmitter=6`, `AudioSource=7`
- Independent review: first verdict Not ready with P2/P3; follow-up review verdict Ready/GO with no remaining P0-P2

## Remaining Risks / Follow-up
- PlayMode/E2E 장시간 검증은 아직 없다. EditMode와 scene component count는 통과했지만, 실제 3v3 루프에서 소음 조사 체감이 자연스러운지는 다음 단계에서 확인해야 한다.
- dynamic obstacle avoidance는 아직 없다. NavMesh path corner 추종은 있지만 agent avoidance는 꺼져 있다.
- 수색/경계 상태 고도화, 의심도, 시야 센서와 hearing sensor 통합은 아직 없다.
- 왕 단독 `Tackle -> Hold -> Capture` 허용 여부는 설계 결정이 필요하다.
- Photon/Supabase/Steam SDK, 정식 캐릭터 모델/모션, 정식 VFX/SFX는 아직 붙이지 않았다.

## Commit Scope Check
- Included: AI hearing behavior code/tests, NPC noise emitter bootstrap/scene, GDD/README/checkpoint/wrap documentation.
- Excluded: Unity generated caches, `TestResults.xml`, `Player.controller` generated fileID churn, unrelated prototype outputs.

## Commit
feat(overthrone): add hearing investigation bots

Co-Authored-By: Codex <noreply@openai.com>
