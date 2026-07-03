---
date: 2026-07-04
scope: [unity, overthrone, playmode-tests, local-bot]
type: feature
---

## TL;DR
로컬 봇/AI hearing 기능에 대해 Unity PlayMode smoke 테스트 인프라를 추가했다. 기존 EditMode 규칙 검증만으로는 실제 프레임 루프에서 캐릭터 컨트롤러 이동, 시야 의심, 동적 장애물 회피, 소음 수색/경계 진입이 함께 도는지 확인하기 어려웠기 때문에 코드 기반 PlayMode fixture를 만들고 2개 테스트를 통과시켰다.

## Keywords
`LocalBotPlayModeSmokeTests` `Overthrone.PlayModeTests` `LocalBotController` `AIHearingSensor` `sight suspicion` `dynamic obstacle avoidance` `PlayMode`

## Context
최근 로컬 Unity prototype은 GDD의 핵심 루프 중 로컬 봇 행동을 계속 보강해 왔다. 이전 작업에서 NPC 정지 문제, 점령/추격/구출/포획 보조, 소음 수색, 동적 장애물 회피, 시야 기반 의심 기억까지 MVP가 들어갔고 EditMode 테스트로 규칙 단위는 검증되었다.

하지만 남은 위험은 "실제 플레이 프레임에서 함께 도는가"였다. EditMode 테스트는 순수 규칙과 일부 컴포넌트 계약을 빠르게 확인하기 좋지만, `CharacterController`, Physics transform sync, `MonoBehaviour.Update`, `LocalBotController.Configure`, `AIHearingSensor` 기억 소비가 같이 연결되는 경로는 PlayMode에서 한 번은 돌려봐야 한다. 이번 작업은 전체 장시간 soak의 대체가 아니라, 장시간 검증으로 가기 위한 최소 PlayMode 기반선을 마련하는 데 초점을 맞췄다.

## Investigation
최신 GDD와 cycles를 다시 확인했을 때 반복적으로 남은 항목은 로컬 봇/AI hearing의 PlayMode 장시간 검증이었다.

- `0240-bot-sight-suspicion-wrap.md`: 시야 의심 MVP 완료, 남은 일은 PlayMode 장시간 검증과 occluder/narrow path/dynamic obstacle 통합 검증.
- `2326-bot-dynamic-obstacle-avoidance-wrap.md`: dynamic obstacle avoidance 완료, 남은 일은 PlayMode 장시간 검증과 튜닝.
- `2256-noise-search-guard-wrap.md`: noise search/guard 완료, 이후 dynamic obstacle, sight, PlayMode, audio가 잔여로 남음.

따라서 이번 세션의 선택지는 새 기능을 더 얹는 것보다, 기존 AI 기능이 실제 Unity 프레임 루프에서 최소한 한 번 통합 실행되는지 검증하는 것이 더 값어치가 있었다.

## Decision Rationale
### PlayMode smoke부터 추가
대안은 실제 씬 `Prototype.unity`를 여는 장시간 테스트를 먼저 만드는 것이었다. 하지만 현재 씬 기반 테스트는 Unity 에디터 상태, 씬 오브젝트 배치, NavMesh bake 상태에 더 민감하다. 반면 코드 기반 fixture는 필요한 GameObject와 컴포넌트를 직접 만들기 때문에 실패 원인을 `LocalBotController`/`AIHearingSensor` 계약으로 좁히기 쉽다.

그래서 이번에는 코드 기반 PlayMode smoke를 먼저 추가했다. 이는 장시간 soak를 완료했다는 뜻이 아니라, 장시간 검증을 자동화하기 위한 첫 발판이다.

### NavMesh 의존보다 CharacterController/Physics 프레임 루프 우선
현재 로컬 봇은 NavMesh path corner 추종과 direct fallback을 모두 가진다. smoke 테스트에서는 씬/NavMesh bake에 묶이지 않도록 direct fallback과 CharacterController 이동이 실제 프레임에서 정상 작동하는지 본다. 추후에는 별도 PlayMode scene 또는 fixture를 통해 occluder, 좁은 길, 누적 장애물 상황을 추가해야 한다.

## Work Accomplished
### 1. PlayMode 테스트 어셈블리 추가
`Assets/Tests/PlayMode/Overthrone.PlayModeTests.asmdef`를 추가했다.

- `Overthrone.Runtime`을 참조해 런타임 컴포넌트를 직접 fixture로 구성한다.
- Unity Test Runner 호환을 위해 `TestAssemblies` optional reference를 설정했다.
- Input System/UI 참조는 기존 런타임 테스트 의존성과 맞췄다.

### 2. 로컬 봇 PlayMode smoke 테스트 2개 추가
`Assets/Tests/PlayMode/LocalBotPlayModeSmokeTests.cs`를 추가했다.

첫 번째 테스트 `SightSuspicionAndDynamicAvoidanceMoveCharacterController`는 다음을 확인한다.

- Blue 봇, Red 적, Blue 동적 blocker를 코드로 생성한다.
- `LocalBotController.Configure`에 participants/agents를 주입한다.
- 90프레임 안에 시야 의심 상태와 동적 장애물 회피 상태가 관측되는지 확인한다.
- `LastAvoidanceOffset`, lateral displacement, blocker line clearance, blocker를 지나친 뒤 최소 clearance를 함께 확인해 회피 플래그만 켜지는 false-positive를 줄인다.
- 봇의 수평 이동 거리가 최소 0.25m 이상인지 확인한다.
- 매 프레임 위치가 NaN/Infinity로 깨지지 않고 지면 아래로 떨어지지 않는지 확인한다.

두 번째 테스트 `HeardEnemyNoiseEntersSearchOrGuardDuringFrameLoop`는 다음을 확인한다.

- Blue 봇과 Red 공격자를 코드로 생성한다.
- 봇에 `AIHearingSensor`를 붙이고 `NoiseSystem.Emit`으로 적 소음 `NoiseEvent`를 발생시킨다.
- 소음 위치를 봇 현재 위치가 아니라 떨어진 적 위치로 두어 hearing event path와 이동 경로를 함께 확인한다.
- 90프레임 안에 봇이 noise target을 잡고, 소음 위치 쪽으로 실제 이동하며, noise search 또는 guard 상태로 진입하는지 확인한다.
- 위치 안정성도 매 프레임 같이 확인한다.

### 3. GDD 구현 상태 갱신
`GDD.md`의 현재 구현 상태 날짜와 검증 기준을 2026-07-04 기준으로 갱신했다.

- Unity prototype 검증 기준에 PlayMode smoke 테스트를 추가했다.
- 로컬 봇/AI hearing 근거에 PlayMode smoke 테스트를 추가했다.
- 남은 일을 "PlayMode 장시간 검증"에서 "PlayMode 장시간 soak/좁은 경로 반복 검증"으로 구체화했다.

## Verification
Unity batchmode PlayMode 테스트를 실행했다.

```bash
/Applications/Unity/Hub/Editor/6000.0.78f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /Users/wy/Github-repos/game1/unity/OverthroneUnity \
  -runTests \
  -testPlatform PlayMode \
  -testFilter LocalBotPlayModeSmokeTests \
  -testResults /Users/wy/Github-repos/game1/unity/OverthroneUnity/PlayModeTestResults.xml \
  -logFile -
```

결과 XML 기준:

- total: 2
- passed: 2
- failed: 0
- skipped: 0
- fixture: `LocalBotPlayModeSmokeTests`

추가로 `git diff --check`를 통과했다.

## Architecture Impact
이번 변경은 런타임 동작을 바꾸지 않고 테스트 계층만 추가한다. PlayMode fixture는 현재 로컬 봇의 최소 통합 경로를 확인하므로, 이후 봇 AI를 리팩터링하거나 hearing/sight/dynamic avoidance를 튜닝할 때 빠른 회귀 감지점으로 쓸 수 있다.

다만 이 테스트는 장시간 soak가 아니다. 실제 맵, baked NavMesh, occluder, 좁은 통로, 다수 NPC가 서로 밀집하는 상황은 아직 별도 PlayMode/E2E 테스트로 확장해야 한다.

## Files Changed
| File | Change |
|------|--------|
| `GDD.md` | 현재 구현 상태를 2026-07-04로 갱신하고 PlayMode smoke 검증/잔여 작업을 반영 |
| `unity/OverthroneUnity/Assets/Tests/PlayMode/Overthrone.PlayModeTests.asmdef` | PlayMode 테스트 어셈블리 추가 |
| `unity/OverthroneUnity/Assets/Tests/PlayMode/LocalBotPlayModeSmokeTests.cs` | 로컬 봇 sight/dynamic avoidance/hearing PlayMode smoke 테스트 추가 |
| `unity/OverthroneUnity/Assets/Tests/PlayMode*.meta` | Unity asset metadata 추가 |

## Remaining Work
- PlayMode 장시간 soak: 실제 6인 로스터를 장시간 돌려 포획/구출/점령/시야/소음 행동이 누적 상태에서 깨지지 않는지 검증.
- 좁은 경로/occluder fixture: blocker 회피, LOS 차단, 추격 전환이 맵 지형과 함께 작동하는지 검증.
- 정식 표면별 footstep audio clip/mixing.
- UI compact 검증, 아이콘 아트, 온라인 RPC 동기화.

## Commit
test(overthrone): add bot playmode smoke coverage
