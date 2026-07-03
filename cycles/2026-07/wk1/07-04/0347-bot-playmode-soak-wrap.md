---
date: 2026-07-04
scope: [unity, overthrone, ai, playmode-test]
type: test
---

## TL;DR
로컬 봇 AI가 단순 EditMode 규칙과 짧은 PlayMode smoke를 넘어, 6봇 환경에서 240프레임 동안 direct fallback 이동, choke point 통과, dynamic obstacle avoidance, match invariant를 유지하는지 검증하는 PlayMode soak fixture를 추가했다. Unity PlayMode 단독 테스트와 `LocalBotPlayModeSmokeTests` 전체 3개가 모두 통과했으며, GDD에는 fixture 기반 soak 완료와 실제 씬 기반 장시간 soak 잔여 작업을 분리해 기록했다.

## Keywords
`LocalBotPlayModeSmokeTests` `LocalSixBotChokePointDirectFallbackSoakMaintainsMatchAndSteeringInvariants` `LocalBotController` `LocalMatchManager` `DirectFallback` `dynamic obstacle avoidance` `PlayMode soak`

## Context
이전 작업에서 로컬 봇은 NavMesh path corner 추종, direct fallback, hearing investigation, sight suspicion, dynamic obstacle avoidance까지 구현됐고, 짧은 PlayMode smoke 테스트도 추가됐다. 하지만 여러 wrap 문서에서 반복적으로 남은 리스크는 같았다.

- 실제 프레임 루프에서 여러 봇이 동시에 움직일 때 상태가 깨지는지 충분히 보지 못했다.
- 좁은 경로와 동적 장애물 상황에서 direct fallback과 avoidance가 함께 버티는지 검증이 약했다.
- GDD의 "게임처럼 플레이되는 로컬 프로토타입" 기준에서 AI가 멈추지 않고 목표/회피/매치 상태를 유지하는 증거가 더 필요했다.

이번 작업은 로컬-first 범위를 유지하면서도 온라인 인프라 도입 전 AI 안정성을 더 강하게 묶는 테스트 보강이다.

## Investigation
최신 cycle 문서 10개를 역순으로 읽어 반복 gap을 확인했다. `07-04/0253-bot-playmode-smoke-wrap.md`, `07-03/2326-bot-dynamic-obstacle-avoidance-wrap.md`, `07-04/0240-bot-sight-suspicion-wrap.md` 등에서 공통적으로 `PlayMode 장시간 soak`, `좁은 경로`, `occluder/dynamic obstacle` 검증이 남아 있었다.

구현 후 독립 리뷰에서 중요한 P2가 발견됐다.

- `LocalMatchManager.Update()`는 매 프레임 `ApplyMatchRules(Time.deltaTime)`를 호출한다.
- 새 soak 테스트도 `yield return null` 이후 `ApplyMatchRules(Time.captureDeltaTime)`를 수동 호출했다.
- 이 상태에서는 match timer, victory countdown, participant state가 한 프레임에 두 번 진행되어 실제 게임 루프와 다른 false positive/false negative가 생길 수 있었다.

해결은 soak 테스트에서 만든 `LocalMatchManager`의 자동 `Update()`만 끄고, 테스트의 deterministic 수동 tick을 단일 source로 삼는 방식으로 정리했다.

## What Didn't Work
### Double-tick 상태의 초기 테스트
- Tried: `yield return null` 뒤 `systems.MatchManager.ApplyMatchRules(Time.captureDeltaTime)`를 직접 호출했다.
- Problem: 생성된 `LocalMatchManager`가 enabled 상태라 Unity PlayMode lifecycle에서 `Update()`도 이미 한 번 호출된다.
- Lesson: PlayMode 테스트에서 MonoBehaviour의 자동 lifecycle과 수동 tick을 섞을 때는 테스트가 소유한 tick path를 하나로 고정해야 한다.

### Unity 자동 ProjectSettings 직렬화 변경
- Tried: Unity 6000.0.78f1 batchmode로 테스트를 실행했다.
- Problem: 로컬 Unity 버전이 `ProjectSettings.asset`을 serializedVersion 28 형태로 자동 재직렬화하고, `PlayModeTestResults.xml`을 생성했다.
- Lesson: 테스트 실행 산출물과 Unity 버전 자동 직렬화 diff는 이번 작업 범위가 아니므로 커밋 전 `ProjectSettings.asset` 복원과 결과 XML 제거가 필요하다.

## Decision Rationale
테스트는 실제 씬 asset을 수정하지 않는 code fixture 방식으로 추가했다. 이유는 다음과 같다.

- 기존 PlayMode smoke 테스트와 같은 파일 안에서 관리되어 검증 범위가 명확하다.
- scene/prefab/ProjectSettings diff 없이 AI steering과 match invariant를 검증할 수 있다.
- 아직 정식 레벨/occluder/온라인 권위화가 없으므로, fixture 기반 soak와 실제 씬 기반 장시간 soak를 구분해 GDD에 남기는 편이 과장 없는 상태 기록이다.

`NavMesh.RemoveAllNavMeshData()`는 테스트가 baked NavMesh 없이 direct fallback을 강제하기 위해 사용했다. 독립 리뷰에서 전역 NavMesh 상태를 바꾸는 P3 위험이 지적됐지만, 현재 `LocalBotPlayModeSmokeTests` 전체 3개를 같은 run에서 통과해 fixture 내부 순서 의존성은 확인되지 않았다. 실제 씬 기반 장시간 soak로 확장할 때는 테스트 소유 NavMeshData를 별도로 관리하거나 격리 씬 전제를 명확히 해야 한다.

## Work Accomplished
### 1. 6봇 PlayMode soak fixture 추가
`LocalBotPlayModeSmokeTests`에 `LocalSixBotChokePointDirectFallbackSoakMaintainsMatchAndSteeringInvariants`를 추가했다.

- File: `unity/OverthroneUnity/Assets/Tests/PlayMode/LocalBotPlayModeSmokeTests.cs`
- Blue 3 / Red 3 로컬 봇을 생성한다.
- 좌우 wall collider로 좁은 choke corridor를 만들고, 중앙 blocker를 배치한다.
- baked NavMesh를 제거해 `LocalBotMoveMode.DirectFallback`을 반드시 거치게 한다.
- 3개 capture point와 `LocalCaptureSystem`, `LocalMatchManager`를 fixture로 구성한다.
- 240프레임 동안 위치 NaN/Infinity, match phase/winner invariant, 이동 거리, objective/sight target 획득, dynamic avoidance, lateral avoidance, choke traversal을 검증한다.

### 2. Match rule tick 경로 단일화
soak 테스트에서 만든 `LocalMatchManager`를 `enabled = false`로 두어 자동 `Update()`를 끄고, 테스트 루프의 `ApplyMatchRules(Time.captureDeltaTime)`만 match state를 전진시키게 했다.

- File: `unity/OverthroneUnity/Assets/Tests/PlayMode/LocalBotPlayModeSmokeTests.cs`
- 리뷰 P2였던 double-tick 위험을 제거했다.

### 3. GDD 상태 업데이트
GDD의 현재 구현 상태에 6봇 choke/direct fallback/dynamic avoidance 240프레임 PlayMode soak fixture 테스트를 근거로 추가했다.

- File: `GDD.md`
- 남은 일은 `실제 씬 기반 장시간 soak/occluder 반복 검증`으로 좁혀 적었다.
- 이미 완료된 dead channel 입력 UI는 다음 우선순위에서 제거하고, 남은 항목을 온라인 dead channel 채팅/moderation으로 정리했다.

## Architecture Impact
이번 변경은 런타임 동작을 바꾸지 않는 테스트/문서 변경이다. 테스트가 검증하는 핵심 계약은 다음과 같다.

- local bot movement는 baked NavMesh가 없어도 direct fallback으로 meaningful movement를 만든다.
- dynamic obstacle avoidance는 좁은 통로에서 lateral offset을 만든다.
- 여러 봇이 동시에 움직여도 match phase/winner/countdown invariant가 즉시 깨지지 않는다.

남은 구조적 리스크는 실제 scene asset, camera/HUD, 정식 audio clip, occluder가 섞인 장시간 플레이에서는 아직 동일한 수준의 증거가 없다는 점이다.

## Verification
- `Unity -batchmode -runTests -testPlatform PlayMode -testFilter LocalBotPlayModeSmokeTests.LocalSixBotChokePointDirectFallbackSoakMaintainsMatchAndSteeringInvariants`
  - Result XML: `total="1" passed="1" failed="0"`
- `Unity -batchmode -runTests -testPlatform PlayMode -testFilter LocalBotPlayModeSmokeTests`
  - Result XML: `total="3" passed="3" failed="0"`
- `git diff --check`
  - 통과

## Files Changed
| File | Change |
|------|--------|
| `unity/OverthroneUnity/Assets/Tests/PlayMode/LocalBotPlayModeSmokeTests.cs` | 6봇 choke/direct fallback/dynamic avoidance 240프레임 PlayMode soak fixture 추가, match manager 자동 Update 비활성화 |
| `GDD.md` | 현재 구현 상태와 다음 우선순위 갱신 |
| `cycles/2026-07/wk1/07-04/0347-bot-playmode-soak-wrap.md` | 세션 wrap 문서 추가 |

## Commit
test(overthrone): add bot playmode soak coverage
