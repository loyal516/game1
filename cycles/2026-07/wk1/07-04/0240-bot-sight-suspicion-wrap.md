---
date: 2026-07-04
scope: [unity, ai, perception]
type: feature
---

## TL;DR
`LocalBotController`에 로컬 MVP용 sight suspicion을 추가했다. 봇은 전방 FOV/range 안의 적을 line-of-sight로 확인하면 짧게 위치를 기억하고, capture point 선택보다 먼저 해당 지점을 조사한다.

## Keywords
`LocalBotController` `sightRange` `sightHorizontalFovDegrees` `sightSuspicionMemorySeconds` `HasSightSuspicion` `IsInvestigatingSight` `Physics.RaycastAll` `LocalBotControllerTests`

## Context
최근 GDD `§19.4`와 최신 wrap 문서들은 `LocalBotController`의 로컬 AI가 NavMesh path corner, direct fallback, 소리 조사, search/guard, dynamic obstacle avoidance까지 갖추었지만 `시야/의심도 결합`은 아직 남아 있다고 반복 기록하고 있었다.

기존 봇은 달리는 적의 소리는 들을 수 있었지만, "눈으로 봤는지", "벽에 가려져 못 봤는지", "시야각 밖이라 반응하지 않는지"를 판단하지 못했다. 이 상태에서는 은신/루트 선택/벽 뒤 긴장감 같은 GDD의 핵심 플레이 감각이 약해진다. 따라서 이번 작업은 온라인이나 정식 AI 프레임워크 없이 `LocalBotController` 안에서 로컬-first sight suspicion MVP를 닫는 데 집중했다.

## Investigation
Wiki-first 탐색으로 최신 `cycles/2026-07/wk1/07-03` 문서를 확인했다.

- `2326-bot-dynamic-obstacle-avoidance-wrap.md`: dynamic obstacle avoidance는 완료됐고, 남은 AI 리스크로 `시야/의심도`와 PlayMode 장시간 검증을 기록했다.
- `2256-noise-search-guard-wrap.md`: hearing search/guard는 완료됐지만, visual perception은 다음 단계로 남았다.
- `2246-cooperative-capture-policy-wrap.md`: 협동 포획 규칙은 정리됐지만, AI가 아직 MVP 수준이라고 남겼다.

현재 `LocalBotController.Tick()` 흐름은 combat/rescue/chase, active noise search/guard, remembered enemy noise, capture point 순서였다. sight suspicion은 포획/구출 같은 즉시 상호작용보다 앞서면 안 되고, 이미 시작된 search/guard도 갑자기 끊으면 안 된다. 반대로 단순 capture point 선택보다는 우선해야 한다.

## Decision Rationale
별도 perception system을 새로 만들지 않고 `LocalBotController` 내부에 작은 sight memory를 추가했다. 현재 로컬 AI는 단일 컨트롤러 중심이며, hearing memory/search/guard도 같은 파일 안에서 관리된다. 지금 단계에서 별도 sensor abstraction을 도입하면 테스트 표면이 커지고 GDD의 local-first 속도를 늦출 수 있다.

선택한 우선순위:

1. king final capture, ally rescue, enemy chase
2. active noise search/guard
3. fresh sight suspicion
4. remembered enemy noise
5. capture point

LOS는 `Physics.RaycastAll`을 사용했다. self/child collider는 무시하고, target/child collider를 먼저 맞으면 visible로 본다. 그 전에 다른 collider가 먼저 나오면 occluded로 판정한다.

## Work Accomplished
### 1. Sight suspicion 설정과 telemetry 추가
`LocalBotController`에 다음 설정을 추가했다.

- `sightRange`
- `sightHorizontalFovDegrees`
- `sightSuspicionMemorySeconds`
- `sightEyeHeight`
- `sightOcclusionLayers`

테스트와 후속 UI/디버깅을 위해 다음 telemetry를 노출했다.

- `IsInvestigatingSight`
- `HasSightSuspicion`
- `CurrentSightTarget`
- `CurrentSightAgent`

### 2. FOV/range/LOS 기반 enemy 감지
`TryFindVisibleEnemy()`는 다음 조건을 모두 만족하는 가장 가까운 적을 찾는다.

- self가 아님
- 같은 팀이 아님
- `TeamId.None`이 아님
- `Captured` 또는 `Held` 상태가 아님
- range 안에 있음
- horizontal FOV 안에 있음
- line-of-sight가 막히지 않음

감지된 적 위치는 `rememberedSightPosition`에 저장되고, `sightSuspicionMemorySeconds` 동안 유지된다.

### 3. Capture point보다 sight suspicion 우선 적용
active search/guard가 없고 sight suspicion이 있으면 bot은 capture point 대신 마지막으로 본 위치를 향해 이동한다. 기존 hearing memory보다 sight suspicion을 먼저 처리하므로, 눈으로 본 정보가 단순 소리 기억보다 우선한다.

### 4. EditMode 테스트 추가
`LocalBotControllerTests`에 다음 테스트를 추가했다.

- `VisibleEnemyTakesPriorityOverCapturePoint`
  - 전방 visible enemy가 있으면 capture point 대신 sight target으로 이동한다.
- `OccludedEnemyIsIgnoredForCapturePointSelection`
  - cube wall이 LOS를 막으면 enemy를 무시하고 capture point를 선택한다.
- `EnemyOutsideSightAngleIsIgnoredForCapturePointSelection`
  - FOV 밖 적은 무시한다.
- `SightSuspicionKeepsLastSeenPositionBriefly`
  - 적이 즉시 시야 밖으로 이동해도 짧은 시간 last seen position을 조사한다.

## Verification
Unity EditMode 테스트를 `LocalBotControllerTests` 필터로 실행했다.

```bash
/Applications/Unity/Hub/Editor/6000.0.78f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /Users/wy/Github-repos/game1/unity/OverthroneUnity \
  -runTests \
  -testPlatform EditMode \
  -testFilter LocalBotControllerTests \
  -testResults /Users/wy/Github-repos/game1/unity/OverthroneUnity/TestResults.xml \
  -logFile -
```

결과:

```text
testcasecount=22 result=Passed total=22 passed=22 failed=0
Unity exit code 0 (Ok)
```

추가 검증:

```bash
git diff --check
```

결과:
- 출력 없음. whitespace/error 없음.

독립 read-only 리뷰:
- Verdict: GO
- P0/P1/P2 없음
- P3: same-team/self/Captured/Held 제외와 active search/guard 우선순위는 코드상 확인되지만 직접 테스트는 아직 없음.

## Architecture Impact
이번 변경은 `LocalBotController` 내부 perception MVP에 한정된다. 별도 `VisionSensor`나 ScriptableObject config는 아직 만들지 않았다.

영향:
- hearing memory와 search/guard 로직은 유지된다.
- sight suspicion은 capture point 선택보다 우선하지만, combat/rescue/chase와 active search/guard보다 앞서지 않는다.
- LOS는 Unity physics collider에 의존하므로, 실제 레벨 art/props의 collider 설정이 들어오면 PlayMode에서 다시 검증해야 한다.

남은 리스크:
- PlayMode 장시간 루프에서 실제 봇 5명, 좁은 길, occluder, dynamic obstacle이 섞인 상황은 아직 검증하지 않았다.
- same-team/self/Captured/Held 제외와 active search/guard 우선순위는 현재 코드 리뷰로 확인했지만, 추가 회귀 테스트를 넣으면 더 단단해진다.
- 정식 표면별 오디오 클립/믹싱은 아직 남아 있다.

## Files Changed
| File | Change |
|------|--------|
| `unity/OverthroneUnity/Assets/Scripts/AI/LocalBotController.cs` | sight suspicion 설정, FOV/range/LOS 감지, sight memory, sight 조사 우선순위, telemetry 추가 |
| `unity/OverthroneUnity/Assets/Tests/EditMode/LocalBotControllerTests.cs` | visible/occluded/outside FOV/sight memory EditMode 테스트 추가 |
| `GDD.md` | 로컬 봇/AI hearing 상태에 sight suspicion MVP 반영 |
| `cycles/2026-07/wk1/07-04/0240-bot-sight-suspicion-wrap.md` | 이번 구현, 검증, 리뷰, 남은 리스크 기록 |

## Commit
Pending.
