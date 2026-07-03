---
date: 2026-07-03
scope: [overthrone-unity, capture, ai, gdd]
type: fix
---

## TL;DR
GDD의 "붙들기 + 포획 분리" 협동 규칙에 맞춰 왕 단독 `Tackle -> Hold -> Capture` 경로를 막았다. 최종 포획은 이제 `Free` 상태의 왕이, 자신이 붙든 대상이 아닌 `Held` 적에게만 진행할 수 있으며, 로컬 봇 AI도 같은 정책을 따른다.

## Keywords
`OverthroneUnity` `CaptureInteractionRules` `PlayerCaptureAgent` `LocalCaptureSystem` `LocalBotController` `CanFinalCapture` `CompleteCapture` `cooperative capture` `King solo capture`

## Context
직전 점검에서 남은 회색지대 중 하나는 왕이 혼자 덮치기, 붙들기, 최종 포획까지 모두 수행할 수 있다는 점이었다. GDD는 핵심 재미를 "붙들기 + 포획 분리"와 "협동 필수"로 정의하고, 포획 흐름도에서도 덮치기 후 "왕 도착"을 별도 단계로 둔다.

기존 구현은 `PlayerCaptureAgent.CaptureAuthorityState`가 `Holding` 상태일 때도 붙들기 이전의 `King` 상태를 반환했기 때문에, 왕이 직접 붙든 대상에게 `CompleteCapture`를 호출하면 최종 포획이 성공했다. 이로 인해 포획 루프가 협동 게임이 아니라 왕 단독 처형 루프로 축소될 수 있었다.

## Investigation
현재 GDD와 Unity 코드를 함께 확인했다.

- GDD 핵심 원칙: "붙들기 + 포획 분리", "협동 필수"
- GDD 포획 흐름: 덮치기 성공 후 붙들림, 이후 왕 도착, 최종 포획
- 기존 코드: `CaptureInteractionRules.CanFinalCapture`가 `captorStatus != Captured`만 확인해 `Holding` 왕도 통과 가능
- 기존 테스트: `KingCanCompleteCaptureAfterHoldingTarget`가 왕 단독 포획을 성공 케이스로 고정

독립 리뷰 시도 중 `LocalBotController`의 추가 문제도 발견했다. 사람 입력 경로는 막히더라도, 봇은 `Holding` 상태를 처리하기 전에 `FindHeldEnemyForKing()`을 먼저 호출했다. 왕 봇이 직접 붙든 대상에게 매 tick `captureHeld` 입력을 넣고 `TickFinalCapture`를 시도할 수 있는 구조였다. 실제 최종 포획은 실패하더라도 AI 의사결정이 헛돌 수 있어 함께 수정했다.

## What Didn't Work
### 단순히 `PlayerCaptureAgent.CompleteCapture`만 막는 접근
- Tried: `target.HeldBy == this`일 때 `CanFinalCapture`를 실패시키는 방식.
- Problem: `LocalCaptureSystem.IsFinalCaptureTarget`가 기존 3인자 `CanFinalCapture`를 계속 쓰면, 왕이 직접 붙든 대상에 대해 포획 링이 차다가 완료 시점에만 실패할 수 있다.
- Fix: `LocalCaptureSystem`도 holder 분리 조건을 포함한 final capture rule을 사용하도록 변경했다.

### 사람 입력만 고치는 접근
- Tried: capture rule과 input capture target만 수정.
- Problem: `LocalBotController`는 `Holding` 상태 조기 반환보다 held enemy 탐색이 먼저라, 왕 봇이 자신의 hold 대상에게 최종 포획 입력을 계속 시도할 수 있었다.
- Fix: rescue 시도 이후 `Holding` 상태를 먼저 처리하고, `FindHeldEnemyForKing`는 `agent.Status == CaptureStatus.Free`일 때만 후보를 찾도록 잠갔다.

## Decision Rationale
최종 포획의 핵심 invariant를 다음처럼 정했다.

- captor는 `MovementState.King`
- captor는 `CaptureStatus.Free`
- target은 `CaptureStatus.Held`
- captor는 target의 holder가 아니어야 함
- 같은 팀/팀 없음/거리 조건은 기존 `PlayerCaptureAgent`와 `LocalCaptureSystem` 검증을 유지

이렇게 하면 왕이 직접 덮쳐 hold하는 행동은 여전히 가능하지만, 그 상태에서 최종 포획은 불가능하다. 왕의 직접 덮치기는 지연/압박/아군 재정렬용 행동으로 남고, 최종 포획은 반드시 다른 플레이어가 붙든 대상을 왕이 처리하는 협동 루프로 고정된다.

## Work Accomplished
### 1. Final capture rule 강화
`CaptureInteractionRules.CanFinalCapture`가 `captorStatus == CaptureStatus.Free`를 요구하도록 바꿨다. 4인자 overload는 `captorIsTargetHolder`를 추가로 확인해 자기 hold 대상 포획을 막는다.

Files:

- `unity/OverthroneUnity/Assets/Scripts/Capture/CaptureInteractionRules.cs`
- `unity/OverthroneUnity/Assets/Scripts/Capture/PlayerCaptureAgent.cs`
- `unity/OverthroneUnity/Assets/Scripts/Capture/LocalCaptureSystem.cs`

### 2. LocalBotController 정책 동기화
봇은 rescue를 먼저 시도한 뒤, 본인이 `Holding`이면 수동 입력을 비우고 즉시 반환한다. 또한 왕의 held enemy 탐색은 `Free` 상태에서만 동작한다.

이로써 왕 봇이 직접 붙든 대상에게 capture hold 입력을 계속 넣는 헛도는 루프를 막았다.

File: `unity/OverthroneUnity/Assets/Scripts/AI/LocalBotController.cs`

### 3. 테스트 갱신 및 회귀 추가
기존 왕 단독 포획 성공 테스트를 실패 테스트로 바꾸고, 아군 Attacker가 붙든 대상을 왕이 포획하는 성공 테스트를 추가했다. 기존 테스트 helper들이 왕 직접 hold shortcut을 쓰던 부분은 아군 holder를 세우는 방식으로 갱신했다.

추가/변경된 주요 검증:

- 왕이 직접 붙든 대상은 `CompleteCapture` 실패
- 아군 holder가 붙든 적은 왕이 최종 포획 가능
- 자기 hold 대상은 포획 progress ring도 쌓이지 않음
- 팀 없음 target/captor는 포획 progress가 쌓이지 않음
- 왕 봇이 자신의 hold 대상에 `captureHeld`를 계속 시도하지 않음
- dead channel, spectator, match flow, king succession, HUD, feedback 테스트 fixture를 협동 포획 경로로 갱신

Files:

- `unity/OverthroneUnity/Assets/Tests/EditMode/CaptureInteractionTests.cs`
- `unity/OverthroneUnity/Assets/Tests/EditMode/LocalBotControllerTests.cs`
- `unity/OverthroneUnity/Assets/Tests/EditMode/CaptureFeedbackTests.cs`
- `unity/OverthroneUnity/Assets/Tests/EditMode/LocalDeadChannelTests.cs`
- `unity/OverthroneUnity/Assets/Tests/EditMode/LocalKingSuccessionTests.cs`
- `unity/OverthroneUnity/Assets/Tests/EditMode/LocalMatchFlowPresenterTests.cs`
- `unity/OverthroneUnity/Assets/Tests/EditMode/LocalMatchFlowTests.cs`
- `unity/OverthroneUnity/Assets/Tests/EditMode/LocalSpectatorCameraTests.cs`
- `unity/OverthroneUnity/Assets/Tests/EditMode/PlayerHudUiTests.cs`

### 4. 문서 업데이트
GDD와 Unity README에 협동 조건을 명시했다.

- GDD §6.4: 포획자는 해당 대상을 붙든 플레이어와 달라야 함
- GDD 현재 구현 상태: 붙든 자와 포획자 분리 규칙 반영
- README: 왕은 아군이 붙든 적만 최종 포획 가능
- README AI 설명: King bot은 아군이 붙든 Held 적만 최종 포획 보조

Files:

- `GDD.md`
- `unity/OverthroneUnity/README.md`

## Verification
실행한 검증:

- Unity EditMode tests
  - Command: `/Applications/Unity/Hub/Editor/6000.0.78f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/wy/Github-repos/game1/unity/OverthroneUnity -runTests -testPlatform EditMode -testResults /Users/wy/Github-repos/game1/unity/OverthroneUnity/TestResults.xml -logFile -`
  - Result: `total=134 passed=134 failed=0`

- Static diff check
  - Command: `git diff --check`
  - Result: no whitespace errors

- Review
  - `codex exec` review attempt found the AI ordering issue in `LocalBotController`.
  - The AI finding was fixed and covered by `KingBotHoldingOwnTargetDoesNotAttemptFinalCapture`.
  - A second short review attempt was interrupted after running too long; no additional P0-P2 finding was emitted before interruption.

## Architecture Impact
The capture invariant is now centralized in `CaptureInteractionRules` and consumed by both direct agent completion and local capture target selection. This reduces the chance that input UI progress and completion authority disagree.

The AI behavior now respects the same capture invariant as player input. This matters because local-first validation uses bots as the primary way to experience the 3v3 loop before online infrastructure exists.

## Remaining Risks
- PlayMode/E2E에서 실제 3v3 장시간 루프로 협동 포획 빈도와 답답함을 아직 검증하지 못했다.
- 왕이 직접 붙든 경우가 지연/압박 행동으로 충분히 재미있는지는 플레이테스트가 필요하다.
- AI는 여전히 수색/경계/동적 회피가 MVP 수준이다.
- 온라인 권위화 시 서버도 같은 final capture invariant를 가져야 한다.

## Files Changed
| File | Change |
|------|--------|
| `GDD.md` | 최종 포획 협동 조건과 현재 구현 상태 업데이트 |
| `unity/OverthroneUnity/README.md` | 협동 포획/봇 포획 설명 업데이트 |
| `unity/OverthroneUnity/Assets/Scripts/Capture/CaptureInteractionRules.cs` | `Free` king + holder 분리 final capture rule |
| `unity/OverthroneUnity/Assets/Scripts/Capture/PlayerCaptureAgent.cs` | 자기 hold 대상 최종 포획 거부 |
| `unity/OverthroneUnity/Assets/Scripts/Capture/LocalCaptureSystem.cs` | 포획 progress target selection에도 동일 규칙 적용 |
| `unity/OverthroneUnity/Assets/Scripts/AI/LocalBotController.cs` | Holding king bot의 final capture 시도 차단 |
| `unity/OverthroneUnity/Assets/Tests/EditMode/*.cs` | 협동 포획 테스트 fixture 갱신 및 회귀 추가 |

## Commit
fix(overthrone): enforce cooperative final capture

Co-Authored-By: Codex <noreply@openai.com>
