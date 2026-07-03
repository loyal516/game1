---
date: 2026-07-03
scope: [unity, local-bot-ai, hearing]
type: feature
---

## TL;DR
로컬 봇이 적 발소리를 들은 뒤 해당 위치로만 이동하고 끝나는 문제를 보완했다. `LocalBotController`에 소리 도착 후 조용한 수색 waypoint 순환과 짧은 경계 상태를 추가했고, 같은 소리를 처리 완료한 뒤 즉시 재진입하지 않도록 소비 처리를 넣었다.

## Keywords
`LocalBotController` `AIHearingSensor` `RunNoiseSearch` `RunNoiseGuard` `rememberedEnemyNoiseTimer` `consumedEnemyNoisePosition`

## Context
이전 상태의 AI hearing은 `AIHearingSensor`가 적 팀 소음을 기억하고 `LocalBotController`가 그 위치로 이동하는 수준이었다. 최신 wrap과 GDD 모두 다음 갭으로 수색/경계 상태 고도화와 dynamic obstacle avoidance를 남기고 있었다.

이번 작업의 목표는 dynamic obstacle avoidance나 시야 기반 의심도까지 확장하지 않고, 로컬 게임 루프에서 바로 체감되는 최소 행동 전이를 추가하는 것이었다. 즉, 봇이 소리를 들으면 달려가고, 도착하면 즉시 점령지로 돌아가지 않고, 주변을 조용히 확인한 뒤 잠깐 멈춰 경계하고, 처리한 같은 소리에는 다시 끌려가지 않아야 한다.

## Investigation
기존 `LocalBotController.Tick` 흐름은 전투/구출 우선순위 뒤에 `TryGetEnemyNoisePosition`을 확인하고, 기억된 적 소음이 있으면 해당 위치로 달려가는 구조였다. 문제는 도착 이후 상태가 없어서 소리 위치에 도달한 다음 프레임부터 capture point 선택 루프로 돌아갈 수 있다는 점이었다.

최신 cycles 탐색 결과도 같은 방향을 가리켰다.
- `2013-hearing-investigation-bots-wrap.md`: 적 소음 위치 조사 MVP까지 구현, 수색/경계 미구현.
- `2246-cooperative-capture-policy-wrap.md`: 협동 포획 수정 이후에도 AI 수색/경계 FSM은 미구현으로 남음.
- `0957-navmesh-bot-pathing-wrap.md`: NavMesh corner steering은 있으나 dynamic obstacle과 수색/경계는 남은 일.

## Decision Rationale
완전한 FSM 클래스를 새로 만들기보다 `LocalBotController` 내부에 작은 상태 타이머를 추가했다. 현재 로컬 봇은 아직 단일 컨트롤러 중심이고, 상태 전이가 hearing 이후 한 경로에 국한되어 있어 별도 아키텍처 분리는 과했다.

전이 순서는 다음처럼 정했다.
1. 포획/구출/추격 같은 전투 행동은 기존처럼 소리보다 우선한다.
2. 이미 수색 또는 경계 중이면 기억된 소리 재조사보다 먼저 처리한다.
3. 새 적 소리를 들으면 위치로 이동한다.
4. 소리 위치 도착 시 수색을 시작한다.
5. 수색이 끝나면 짧게 경계하고, 완료 시 같은 소리를 소비 처리한다.

수색 중에는 `sprint=false`로 입력을 넣는다. 현재 `PlayerNoiseEmitter`는 sprint 중 이동할 때만 NoiseEvent를 내므로, 이 선택은 "걸어서 접근하면 소리가 나지 않음"이라는 기존 스텔스 규칙과 맞다.

## Work Accomplished
### 1. 적 소리 도착 후 수색/경계 상태 추가
`LocalBotController`에 search/guard 타이머와 waypoint 상태를 추가했다.
- 수색 시간: `noiseSearchDurationSeconds`
- 경계 시간: `noiseGuardDurationSeconds`
- 수색 반경: `noiseSearchRadius`
- waypoint 갱신 간격: `noiseSearchWaypointSeconds`

봇은 적 소리 위치에 도착하면 `StartNoiseSearch`로 중심점을 잡고, `RunNoiseSearch`에서 네 방향 waypoint를 순환한다. 수색이 끝나면 `RunNoiseGuard`가 이동 입력을 0으로 두고 짧은 경계 상태를 유지한다.

### 2. 같은 소리 재진입 방지
`ClearNoiseMemory`에서 마지막으로 처리한 적 소리 위치를 `consumedEnemyNoisePosition`에 저장하고, `TryReadCurrentEnemyNoise`가 같은 위치의 아직 남아있는 sensor memory를 다시 읽지 않게 했다. 이 처리가 없으면 `AIHearingSensor`의 자체 memory timer가 남아있는 동안 같은 이벤트를 매 프레임 새 소리처럼 읽을 수 있다.

### 3. 리뷰 피드백 P2 수정
1차 독립 리뷰에서 P2가 나왔다. 원인은 `TryGetEnemyNoisePosition`이 `RunNoiseSearch`/`RunNoiseGuard`보다 먼저 실행되면, 실제 프레임 단위에서는 guard가 기억된 소리 분기 뒤로 밀릴 수 있다는 점이었다.

수정은 진행 중인 search/guard를 remembered noise 재조사보다 먼저 처리하도록 순서를 바꾸는 방식으로 했다. 이후 재리뷰에서 P0-P2 없음 판정을 받았다.

### 4. GDD 현재 구현 상태 업데이트
`GDD.md`의 로컬 봇/AI hearing 구현 근거에 도착 후 조용한 수색 waypoint 순환, 짧은 경계, 같은 소리 소비 처리 MVP를 추가했다. 남은 일은 dynamic obstacle avoidance, 시야/의심도 결합, PlayMode 장시간 검증, 정식 표면별 오디오 클립/믹싱으로 좁혔다.

## Verification
Unity EditMode 전체 테스트를 실행했다.

```bash
/Applications/Unity/Hub/Editor/6000.0.78f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /Users/wy/Github-repos/game1/unity/OverthroneUnity \
  -runTests \
  -testPlatform EditMode \
  -testResults /Users/wy/Github-repos/game1/unity/OverthroneUnity/TestResults.xml \
  -logFile -
```

결과:
- `136 passed / 136 total / 0 failed`

추가된 테스트:
- `HeardEnemyNoiseAtCurrentPositionStartsQuietSearch`: 소리 위치가 현재 위치일 때 capture point로 돌아가지 않고 조용한 수색을 시작하는지 검증.
- `NoiseSearchAndGuardReturnToCapturePointAfterConsumedNoise`: `0.1s` 반복 tick으로 search -> guard -> same noise consume -> capture point 복귀를 검증.

독립 리뷰:
- 1차: P2 발견. search/guard가 remembered noise보다 뒤에 있어 frame-by-frame runtime에서 guard가 밀릴 수 있음.
- 후속 패치 후 재리뷰: `VERDICT: Ready`, P0-P2 없음.

## Architecture Impact
이 변경은 로컬 봇의 hearing branch에만 영향을 준다. 전투/구출/추격 우선순위는 유지했고, search/guard는 그 다음 우선순위로 둔다. 따라서 적 발견, held ally rescue, held enemy final capture assist는 소리 수색보다 먼저 처리된다.

남은 주요 리스크:
- dynamic obstacle avoidance는 여전히 미구현이다.
- 시야 기반 perception, 의심도 누적/감쇠, 마지막 목격 위치 추적은 아직 없다.
- EditMode 규칙 검증은 통과했지만 PlayMode 장시간 루프에서 NavMesh/CharacterController 실제 이동까지 본 것은 아니다.

## Files Changed
| File | Change |
|------|--------|
| `unity/OverthroneUnity/Assets/Scripts/AI/LocalBotController.cs` | 적 소리 도착 후 search/guard 상태, 같은 소리 소비 처리, 진행 중 search/guard 우선순위 추가 |
| `unity/OverthroneUnity/Assets/Tests/EditMode/LocalBotControllerTests.cs` | 조용한 수색 시작 및 반복 tick 기반 search/guard/복귀 테스트 추가 |
| `GDD.md` | 로컬 봇/AI hearing 구현 상태 및 남은 일 업데이트 |

## Commit
feat(overthrone): add bot noise search guard

Co-Authored-By: Codex <noreply@openai.com>
