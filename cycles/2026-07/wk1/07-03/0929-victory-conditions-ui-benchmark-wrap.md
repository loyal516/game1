---
date: 2026-07-03
scope: [game1, Overthrone, Unity, victory-conditions, UI-benchmark]
type: feature
---

## TL;DR
GDD의 승리 조건 4개 중 남아 있던 시간 종료 생존과 포기 조건을 Unity 로컬 MVP에 추가하였다. 시간 종료는 생존자 수, 점령 수, draw 순서로 판정하고, 포기는 양 팀 roster가 모두 존재한 상태에서 한 팀 active/enabled 참가자가 0명이 될 때만 적용한다. UI 벤치마크 표도 공식/준공식 출처 중심으로 정리했으며, Unity EditMode 114/114 통과로 확인하였다.

## Keywords
`Overthrone` `GDD.md` `LocalMatchManager` `LocalMatchRules` `LocalMatchFlowPresenter` `LocalMatchFlowTests` `timeout victory` `forfeit victory` `draw banner` `UI benchmark`

## Checkpoint
- Path: `.codex/checkpoints/20260703-0845-overthrone-local-prototype.checkpoint.json`
- Status: `stopped`
- Replay: `full checkpoint replay completed at 09:29`
- Note: 체크포인트의 전체 여정(문서 정리, Three.js 검증, Unity 이식, 로컬 봇, 승리 조건 후속)을 다시 확인한 뒤 이번 wrap을 작성하였다.

## Full Journey Timeline

### Phase 1 - GDD 재정리와 제품 방향 결정
초기 작업은 오래된 `GDD.md`를 읽고 최신 방향으로 재정리하는 것이었다. 게임은 고정 술래 숨바꼭질이 아니라, 점령과 집결로 공수 권한이 실시간 전환되는 3인칭 비대칭 잠입/추격 게임으로 정리되었다. 플랫폼은 Steam/PC 우선, 모바일은 PC 안정화 후순위로 결정하였다.

### Phase 2 - Three.js 규칙 검증과 Unity 전환
Unity 설치 전에는 Three.js 브라우저 프로토타입으로 이동, 점령, AI, 추격, 포획 루프를 빠르게 검증했다. 이후 Unity가 설치되면서 최종 구현 기준은 `unity/OverthroneUnity`로 옮겼고, Three.js는 참고 구현으로 남겼다.

### Phase 3 - Unity 로컬 프로토타입 구축
Unity 6 프로젝트에 입력, 이동, 3인칭 카메라, 상태별 `MovementProfile`, 점령 포인트, 왕 승계, 포획/구출, 관전, HUD, 미니맵, 핑, 슬라임 MVP가 들어갔다. 핵심 규칙은 static rule class와 EditMode tests로 검증 가능한 형태를 유지했다.

### Phase 4 - 로컬 봇과 전원 포획 승리 보강
중간점검에서 NPC 5명이 실제 게임을 하지 않는 문제가 확인되었다. 이후 `LocalBotController`가 추가되어 NPC가 `PlayerInputReader.SetManualInput(...)`으로 기존 `PlayerMotor`/`LocalCaptureSystem` 경로를 타도록 수정되었다. 상대 전원 `Captured` 즉시 승리, 시작 스폰 겹침 제거, Slime 9.8m/s 보정도 함께 반영되었다.

### Phase 5 - 시간 종료/포기 승리와 UI 벤치마크 보강
이번 작업에서는 남은 GDD 승리 조건인 시간 종료 생존과 포기 처리를 구현하였다. 또한 GDD의 UI 벤치마크 표가 Fandom/일반 사이트 위주로 섞여 있던 것을 Dead by Daylight 공식 포럼, Innersloth, Nintendo, THE FINALS 공식 패치노트 중심으로 보강했다.

## Context
중간점검 이후 Overthrone Unity 프로토타입은 실제 봇이 움직이고 전원 포획/완전 점령 승리가 동작하는 상태까지 올라왔다. 그러나 GDD §3.2의 승리 조건 4개 중 시간 종료 생존과 포기가 여전히 미구현이었다. 이 상태에서는 매치가 장시간 끊기지 않거나, 한 팀이 모두 비활성화된 상황을 라운드 결과로 처리할 수 없었다.

UI 벤치마크도 같은 맥락에서 정리할 필요가 있었다. Overthrone은 추격 중 상태 판단이 빠르게 이루어져야 하는 게임이므로, 팀 상태 레일, objective panel, ping, 관전 HUD의 정보 구조가 참고작과 어떻게 연결되는지 GDD에 명확히 남겨야 했다.

## Investigation
현재 `LocalMatchManager`는 이미 라운드 결과의 SSOT였다. 전원 포획 승리와 3점령 30초 승리 카운트다운도 이 클래스에서 처리하고 있었으므로, 시간 종료와 포기도 같은 곳에서 resolve하는 것이 가장 작고 일관된 변경이었다.

구현 후 독립 리뷰에서 마지막 tick에 `matchTimeRemaining`이 0이 되면서 동시에 3점령 상태가 성립할 경우, `VictoryCountdownStarted` 이벤트와 defender re-entry가 먼저 발생한 뒤 같은 tick에서 timeout `RoundEnded`가 발생할 수 있다는 P2가 발견되었다. 이 피드백은 유효했다. 따라서 fresh countdown이 없는 상태에서 이번 tick에 시간이 0이 될 때는 countdown/re-entry를 시작하지 않고 timeout을 먼저 resolve하도록 보정했다.

재리뷰에서는 timeout 최종 판정이 같은 프레임의 최신 capture point owner를 못 볼 수 있다는 P2가 추가로 발견되었다. `CapturePoint.Update()`와 `LocalMatchManager.Update()`가 모두 기본 execution order면 manager가 먼저 실행될 수 있기 때문이다. 이에 `LocalMatchManager`에 `DefaultExecutionOrder(100)`을 부여하여 기본 Update 이후 라운드 판정을 수행하게 했고, reflection 기반 테스트로 이 계약을 잠갔다.

## What Didn't Work

### ❌ Countdown과 timeout을 단순 순서 처리
- Tried: 처음 구현은 all-captured, forfeit, victory countdown, timeout 순서였다.
- Problem: 마지막 tick에 3점령과 timeout이 동시에 성립하면 countdown started 이벤트가 먼저 나가고 같은 tick에 round ended가 나갈 수 있었다.
- Lesson: 라운드 종료 조건은 단순 우선순위뿐 아니라 이벤트 side effect를 고려해야 한다. 특히 presenter가 re-entry처럼 실제 위치/상태를 바꾸는 이벤트는 종료 직전 새로 발생하면 안 된다.

### ❌ GDD §3.2를 오래된 “도주 팀” 문구로 유지
- Tried: 구현 상태표에는 시간 종료 판정 세부 규칙을 반영했지만, 핵심 승리 조건 표는 그대로 두었다.
- Problem: 구현은 생존자 수 -> 점령 수 -> draw인데, GDD 본문은 “도주 팀”이라고 되어 있어 로컬 3v3 대칭 룰과 어긋났다.
- Lesson: 하단 구현 상태표만 갱신하면 SSOT가 갈라진다. 핵심 규칙 표도 함께 갱신해야 한다.

## Decision Rationale
시간 종료 승리는 로컬 3v3 대칭 MVP 기준으로 `active non-Captured 생존자 수 -> owned capture point 수 -> draw` 순서로 정했다. GDD의 “도주 팀” 표현은 초기 비대칭 초안에 가까웠고, 현재 Overthrone은 양 팀이 점령 상황에 따라 공격/도주 권한을 오가는 구조이기 때문이다.

포기 승리는 active/enabled 참가자 수로 판단하되, 양 팀 roster가 모두 존재할 때만 적용한다. 이렇게 해야 테스트나 초기 씬 구성 중 한 팀만 들어온 순간을 forfeit로 오판하지 않는다.

UI 벤치마크는 직접 도입이 아니라 정보 구조 참고로 제한했다. Overthrone의 실제 UI는 `PlayerHud`, `LocalPingSystem`, `LocalMatchFlowPresenter`가 이미 가지고 있는 계약을 유지한다.

## Work Accomplished

### 1. 시간 종료/포기 승리 추가
`LocalMatchRules`에 match duration, timeout tick, forfeit winner, timeout winner 계산을 추가했다. `LocalMatchManager`는 match duration/remaining을 노출하고, all-captured/forfeit/countdown/timeout을 라운드 결과 조건으로 처리한다.

- File: `unity/OverthroneUnity/Assets/Scripts/Match/LocalMatchRules.cs`
- File: `unity/OverthroneUnity/Assets/Scripts/Match/LocalMatchManager.cs`

### 2. Timeout과 fresh countdown 동시 tick 보정
마지막 tick에 아직 countdown이 없는 상태에서 시간이 0이 되는 경우, 새 `VictoryCountdownStarted` 이벤트를 만들지 않고 timeout을 먼저 resolve하도록 `ShouldResolveTimeoutBeforeStartingCountdown(...)`을 추가했다.

- File: `unity/OverthroneUnity/Assets/Scripts/Match/LocalMatchManager.cs`
- Test: `TimeoutDoesNotStartFreshVictoryCountdownInSameTick`

### 3. MatchManager 실행 순서 명시
timeout 타이브레이커가 최신 capture point owner를 읽도록 `LocalMatchManager`를 `DefaultExecutionOrder(100)`으로 지정했다. 기본 execution order인 `CapturePoint.Update()`가 먼저 owner/progress를 갱신하고, 그 뒤 manager가 승리 조건을 resolve한다.

- File: `unity/OverthroneUnity/Assets/Scripts/Match/LocalMatchManager.cs`
- Test: `MatchManagerRunsAfterCapturePointUpdates`

### 4. Draw 배너 처리
`Winner == TeamId.None`인 `RoundEnded` 이벤트는 `ROUND END\nDRAW`로 표시하도록 `LocalMatchFlowPresenter`를 보정했다.

- File: `unity/OverthroneUnity/Assets/Scripts/Match/LocalMatchFlowPresenter.cs`
- Test: `TimeoutDrawKeepsWinnerNoneAndPresenterShowsDraw`

### 5. EditMode 테스트 확대
시간 종료/포기 조건을 검증하는 테스트가 추가되었다.

- `ForfeitRequiresBothRostersAndAwardsRemainingActiveTeam`
- `TimeoutSurvivorCountWinsBeforePointTiebreak`
- `TimeoutOwnedPointTiebreakWinsWhenSurvivorsAreEven`
- `TimeoutDoesNotStartFreshVictoryCountdownInSameTick`
- `MatchManagerRunsAfterCapturePointUpdates`
- `TimeoutDrawKeepsWinnerNoneAndPresenterShowsDraw`

File:
- `unity/OverthroneUnity/Assets/Tests/EditMode/LocalMatchFlowTests.cs`

### 6. GDD/README 갱신
`GDD.md`의 핵심 승리 조건 표, 구현 상태표, UI 벤치마크 표를 현재 구현과 맞췄다. `unity/OverthroneUnity/README.md`에는 match duration/remaining, forfeit, timeout 판정, draw 결과를 추가했다.

- File: `GDD.md`
- File: `unity/OverthroneUnity/README.md`

## Architecture Impact
라운드 종료 조건이 `LocalMatchManager`에 더 모였다. 이는 로컬 MVP에서는 장점이지만, 네트워크 권위화 시에는 같은 순서를 server-authoritative tick으로 옮겨야 한다. 현재 우선순위는 다음과 같다.

1. 이미 Result면 아무 것도 하지 않는다.
2. 상대 전원 Captured 승리.
3. Forfeit 승리.
4. 이번 tick에 fresh countdown을 시작하기 전에 timeout이 확정되면 timeout 종료.
5. 기존 또는 신규 3점령 countdown 처리.
6. timeout 처리.
7. 상태/왕/공격자 갱신.

`LocalMatchManager`는 `DefaultExecutionOrder(100)`으로 실행되어 기본 Update에서 capture point owner가 갱신된 뒤 승리 조건을 읽는다.

아직 PlayMode/E2E 장시간 검증은 없다. EditMode 규칙 검증은 통과했지만, 실제 Unity scene에서 봇이 몇 분 동안 움직이며 timeout/forfeit까지 자연스럽게 이어지는지는 다음 단계에서 봐야 한다.

## Files Changed

| File | Change |
|------|--------|
| `.codex/checkpoints/20260703-0845-overthrone-local-prototype.checkpoint.json` | 시간 종료/포기 승리, UI 벤치마크, 검증 결과 갱신 |
| `GDD.md` | §3.2 시간 종료 설명, §9.6 UI 벤치마크, 구현 상태표 갱신 |
| `cycles/2026-07/wk1/07-03/0929-victory-conditions-ui-benchmark-wrap.md` | 이번 후속 작업 wrap 문서 |
| `unity/OverthroneUnity/Assets/Scripts/Match/LocalMatchRules.cs` | match duration, timeout/forfeit winner rule 추가 |
| `unity/OverthroneUnity/Assets/Scripts/Match/LocalMatchManager.cs` | forfeit, timeout, draw, fresh countdown/timeout 충돌 처리 |
| `unity/OverthroneUnity/Assets/Scripts/Match/LocalMatchFlowPresenter.cs` | Winner None draw 배너 |
| `unity/OverthroneUnity/Assets/Tests/EditMode/LocalMatchFlowTests.cs` | forfeit/timeout/draw/countdown collision 테스트 |
| `unity/OverthroneUnity/README.md` | 로컬 승리 조건 설명 갱신 |

## Verification
- `git diff --check`: Passed
- `.codex/checkpoints/20260703-0845-overthrone-local-prototype.checkpoint.json` JSON parse: Passed
- `/Applications/Unity/Hub/Editor/6000.0.78f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/wy/Github-repos/game1/unity/OverthroneUnity -runTests -testPlatform EditMode -testResults /Users/wy/Github-repos/game1/unity/OverthroneUnity/TestResults.xml -logFile -`: Exit code 0
- `unity/OverthroneUnity/TestResults.xml`: `total="114" passed="114" failed="0" skipped="0"`

## Remaining Risks / Follow-up
- 로컬 봇은 아직 NavMesh/pathfinding/장애물 회피/수색/경계 행동이 없다.
- 왕 단독 `Tackle -> Hold -> Capture` 허용 여부는 설계 결정이 필요하다.
- PlayMode/E2E 장시간 플레이 검증은 아직 없다.
- 정식 UI 아이콘/아트, ping response polish, 온라인 RPC 동기화는 다음 단계다.
- Photon/Supabase/Steam SDK는 아직 붙이지 않았다.

## Commit Scope Check
- Included: victory condition code/tests, GDD/README/checkpoint/wrap documentation.
- Excluded: Unity generated caches, `TestResults.xml`, unrelated prototype/generated outputs.

## Commit
feat(overthrone): complete local victory conditions

Co-Authored-By: Codex <noreply@openai.com>
