---
date: 2026-07-03
scope: [unity, ai, navigation]
type: feature
---

## TL;DR
`LocalBotController`에 로컬 MVP용 dynamic obstacle avoidance를 추가했다. 봇은 NavMesh path corner 또는 direct fallback 목적지로 이동할 때 전방 근거리 participant를 피해서 steering을 보정하고, chase/rescue/final-capture 같은 상호작용 대상 자체는 회피 대상에서 제외한다.

## Keywords
`LocalBotController` `dynamicObstacleLookahead` `IsAvoidingDynamicObstacle` `LastAvoidanceOffset` `LocalBotControllerTests` `NavMeshPath` `DirectFallback`

## Context
GDD `§19.4`의 로컬 봇/AI hearing 항목은 이미 NavMesh path corner 추종, direct fallback, 점령 포인트 선택, 추격/덮치기, 구출, 적 소리 조사, 소리 위치 도착 후 search/guard까지 포함하고 있었다. 그러나 최근 wrap 문서들에서 반복적으로 `dynamic obstacle avoidance`가 미완으로 남아 있었다.

기존 봇 이동은 목적지 또는 NavMesh corner를 향한 방향을 그대로 local input으로 변환했다. 이 구조에서는 같은 팀 NPC나 다른 participant가 진행 방향 앞에 있어도 계속 직선으로 밀고 갈 수 있다. 로컬 플레이에서 봇들이 한 줄로 겹치거나 choke point에서 서로 비비는 문제가 생기기 쉬워, GDD 기준의 "실제로 플레이되는 게임"에 가까워지려면 최소한의 동적 회피가 필요했다.

## Investigation
최신 cycles 문서는 wiki 규칙에 따라 Explore agent가 `cycles/2026-07/wk1/07-03`을 최신순으로 확인했다.

관련 최신 문맥:
- `2309-ping-response-polish-wrap.md`: 핑 응답 polish는 로컬 MVP 기준 완료, 온라인 RPC는 남음.
- `2302-ui-benchmark-acceptance-wrap.md`: UI 2차와 PlayMode/E2E 검증이 남음.
- `2256-noise-search-guard-wrap.md`: 봇 hearing search/guard는 구현됐지만 dynamic obstacle avoidance가 명시적으로 남음.
- `2246-cooperative-capture-policy-wrap.md`: 협동 포획 정책은 반영됐지만 AI가 여전히 MVP 수준이라고 기록.
- `2228-footstep-surface-ui-benchmark-wrap.md`: NavMesh 동적 회피와 PlayMode 장시간 검증이 남은 리스크로 반복됨.

코드 확인 결과 `LocalBotController.MoveToward()`는 `ResolveSteeringTarget()`으로 NavMesh corner 또는 fallback 목적지를 고른 뒤 해당 방향을 즉시 `PlayerInputReader.SetManualInput()`에 넣고 있었다. 따라서 NavMesh와 fallback 양쪽에 모두 적용하려면 steering target 결정 이후, local input 변환 이전에 방향을 보정하는 것이 가장 작은 변경이었다.

## What Didn't Work
### 첫 구현의 상호작용 대상 회피
- Tried: 모든 participant를 dynamic obstacle 후보로 두고 전방 근거리 대상이면 lateral steering을 섞었다.
- Problem: 독립 리뷰에서 chase target 자체를 회피하면 Attacker/King이 덮치기 forward cone을 놓칠 수 있다는 P2가 나왔다. 실제로 `enemyToChase`는 `MoveToward()` 직후 `IsInsideTackleRange()`를 보므로, 대상 자체를 피하면 포획 루프가 깨질 수 있다.
- Lesson: navigation obstacle과 interaction target은 다르다. 이동 목적 대상 자체는 avoidance ignore로 넘겨야 한다.

### DirectFallback만 테스트한 검증
- Tried: direct fallback 경로에서 전방 blocker 회피와 뒤/범위 밖 blocker 무시만 테스트했다.
- Problem: GDD의 현재 봇 이동 근거는 NavMesh path corner 추종과 direct fallback 양쪽이다. 공통 적용 의도라면 NavMeshPath에서도 telemetry와 lateral input을 증명해야 한다는 P3가 나왔다.
- Lesson: 공통 steering 계층에 넣은 기능은 모든 주요 move mode에 대해 테스트해야 한다.

## Decision Rationale
이번 작업은 Unity NavMeshAgent를 새로 도입하지 않고, 기존 `PlayerInputReader` 기반 bot control을 유지했다. 현재 로컬 플레이어와 봇은 같은 movement/input 경로를 공유하고 있어, 봇만 별도 agent 이동으로 바꾸면 상태별 movement profile, sprint, tackle, capture timing과 어긋날 수 있기 때문이다.

선택한 방식:
- `ResolveSteeringTarget()`은 기존대로 NavMeshPath 또는 DirectFallback을 결정한다.
- 결정된 방향을 `ApplyDynamicObstacleAvoidance()`에서 보정한다.
- 전방 lookahead/radius 안에 있는 participant만 장애물로 본다.
- 자기 자신, null, 뒤쪽, 범위 밖, 현재 상호작용 target은 무시한다.
- `IsAvoidingDynamicObstacle`, `LastAvoidanceOffset` telemetry를 노출해 EditMode에서 회피 발생 여부를 검증한다.

## Work Accomplished
### 1. 동적 장애물 회피 steering 추가
`LocalBotController`에 다음 serialized parameter를 추가했다.

- `dynamicObstacleLookahead`
- `dynamicObstacleRadius`
- `dynamicObstacleAvoidanceStrength`

봇은 진행 방향 앞쪽의 participant를 검사해 lateral offset을 만들고, 기존 desired direction에 해당 offset을 섞어 local input으로 변환한다. 정면에 가깝게 있는 blocker는 deterministic하게 한쪽으로 피한다.

### 2. 상호작용 대상 ignore 처리
다음 이동 경로는 목적 대상 transform을 `ignoredDynamicObstacle`로 넘긴다.

- King이 Held enemy에게 최종 포획하러 이동
- Held ally를 구출하러 이동
- Attacker/King이 enemy를 추격해 tackle하러 이동

반대로 capture point 이동, noise position 이동, noise search waypoint 이동은 기존처럼 participants 회피가 적용된다.

### 3. EditMode 테스트 보강
`LocalBotControllerTests`에 다음 테스트를 추가했다.

- `DirectFallbackAvoidsParticipantBlockingForwardPath`
  - DirectFallback 이동 중 전방 participant를 피하고 lateral input/telemetry가 생긴다.
- `DirectFallbackIgnoresParticipantBehindOrOutsideLookahead`
  - 뒤쪽 또는 lookahead 밖 participant는 무시한다.
- `NavMeshPathAvoidsParticipantBlockingForwardPath`
  - NavMeshPath mode에서도 전방 participant 회피가 적용된다.
- `AttackerBotDoesNotAvoidChaseTargetIncludedInParticipants`
  - chase target이 participants 배열에 있어도 dynamic obstacle로 피하지 않고 tackle/hold가 성공한다.

### 4. GDD 상태 갱신
GDD `§19.4`의 로컬 봇/AI hearing 근거에 `NavMesh/direct fallback 공통 dynamic obstacle avoidance MVP`를 추가했다. 남은 일에서는 `dynamic obstacle avoidance`를 제거하고, `시야/의심도 결합`, `PlayMode 장시간 검증`, `정식 표면별 오디오 클립/믹싱`을 유지했다.

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
testcasecount=18 result=Passed total=18 passed=18 failed=0
Unity exit code 0 (Ok)
```

추가 검증:

```bash
git diff --check
```

결과:
- 출력 없음. whitespace/error 없음.

독립 리뷰:
- 첫 read-only 리뷰 결과: NO-GO.
- P2: chase/interaction target 자체를 회피할 수 있음.
- P3: NavMeshPath 회피 테스트가 없음.
- 조치: target ignore parameter와 NavMeshPath 회피 테스트, chase target 회귀 테스트를 추가했다.

## Architecture Impact
이번 변경은 `LocalBotController` 내부 steering 보정에 한정된다. 별도 NavMeshAgent, physics avoidance, online prediction은 도입하지 않았다.

영향:
- 기존 direct fallback/NavMeshPath 결정 로직은 유지된다.
- local input 주입 전에 방향만 보정하므로 movement profile, sprint, tackle, capture system과 같은 기존 경로를 계속 사용한다.
- participant 수가 적은 로컬 3v3 MVP에서는 O(N) scan으로 충분하다.

남은 리스크:
- PlayMode 장시간 루프에서 실제 CharacterController 이동, 여러 봇 밀집, narrow choke point 상황은 아직 검증하지 않았다.
- 시야/의심도와 결합된 AI 의사결정은 아직 없다.
- 실제 레벨/모델 크기에 맞춘 avoidance radius/strength 튜닝은 추후 필요할 수 있다.

## Files Changed
| File | Change |
|------|--------|
| `unity/OverthroneUnity/Assets/Scripts/AI/LocalBotController.cs` | dynamic obstacle avoidance parameter/telemetry/steering 보정과 상호작용 target ignore 추가 |
| `unity/OverthroneUnity/Assets/Tests/EditMode/LocalBotControllerTests.cs` | DirectFallback/NavMeshPath 회피, 뒤/범위 밖 무시, chase target ignore 회귀 테스트 추가 |
| `GDD.md` | 로컬 봇/AI hearing 상태에 dynamic obstacle avoidance MVP 반영 |
| `cycles/2026-07/wk1/07-03/2326-bot-dynamic-obstacle-avoidance-wrap.md` | 이번 구현, 리뷰 지적, 검증, 남은 리스크 기록 |

## Commit
Pending.
