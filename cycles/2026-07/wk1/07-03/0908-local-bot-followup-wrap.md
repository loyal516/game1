---
date: 2026-07-03
scope: [game1, Overthrone, Unity, local-bots, capture-loop]
type: follow-up
---

## TL;DR
중간점검에서 나온 핵심 결론은 정확했다. 이전 Unity 씬은 규칙 함수와 테스트는 좋았지만, NPC 5명이 실제로 게임을 하지 않는 "무저항 데모"에 가까웠다. 이번 후속 작업에서는 `LocalBotController`를 추가해 NPC가 기존 `PlayerMotor`/`LocalCaptureSystem` 경로를 타고 이동, 점령, 추격, 덮치기, 구출, 왕 최종 포획 보조를 하도록 만들었다. 또한 상대 전원 Captured 즉시 승리, 시작 스폰 겹침 제거, Slime 속도 9.8m/s 보정을 반영했다.

현재 검증은 Unity bootstrap exit 0, Unity EditMode 108/108 Passed, `git diff --check` Passed, inputactions JSON parse Passed다. 다만 NavMesh/pathfinding, PlayMode/E2E 장시간 검증, 시간 종료 생존/포기 승리, 왕 단독 최종 포획 정책 결정은 아직 남아 있다.

## Keywords
`game1` `Overthrone` `Unity` `LocalBotController` `PlayerInputReader.SetManualInput` `LocalMatchManager` `all captured victory` `LocalRosterSlot` `Slime 9.8`

## Checkpoint
- Path: `.codex/checkpoints/20260703-0845-overthrone-local-prototype.checkpoint.json`
- Status: `stopped`
- Update: P0~P2 중간점검 후속 내용, 검증 결과, 남은 설계 질문을 추가했다.
- Note: `codex-review` wrapper는 로컬에 없어 실행하지 못했다. 실패 메시지는 `zsh:1: command not found: codex-review`였다.

## Feedback Context
중간점검의 핵심 지적은 다음이었다.

- P0: NPC 5명이 입력을 받지 못해 동상처럼 서 있었다.
- P0: GDD 승리 조건 4개 중 Unity에는 3점령 30초만 구현되어 있었다.
- P1: Red 스폰 일부가 CapturePoint A 반경과 겹쳐 시작 직후 자동 점령이 발생했다.
- P2: Slime 속도가 6.3m/s라 일반 스프린트 7.2m/s보다 느렸다.
- P3: `LocalMatchManager`가 왕 후보 계산을 참가자마다 반복했다.
- P3: 왕이 혼자 Tackle -> Hold -> Capture까지 가능한지는 설계상 회색지대다.

이번 작업은 P0~P2와 P3 성능 항목을 우선 처리했다. 왕 단독 포획 정책은 의도 결정이 필요해서 변경하지 않았다.

## Work Accomplished

### 1. 로컬 봇 컨트롤러 추가
`LocalBotController`를 추가해 NPC가 매 프레임 목표를 선택하고 `PlayerInputReader.SetManualInput(...)`으로 이동 입력을 주입하게 했다. 이 방식은 Transform을 직접 움직이지 않기 때문에 기존 `PlayerMotor`, 상태별 이동 프로필, 스태미나, 덮치기 규칙을 그대로 탄다.

현재 봇 MVP 행동은 다음이다.

- Captured/Held 상태에서는 입력을 비운다.
- 가까운 Held 아군이 있으면 구출하러 간다.
- Attacker/King이 덮칠 수 있으면 적을 추격하고 범위/전방각 조건에서 덮치기를 시도한다.
- King이면 Held 적에게 접근해 최종 포획을 진행한다.
- 특별한 전투 목표가 없으면 점령 포인트를 고르고 이동한다.

Files:
- `unity/OverthroneUnity/Assets/Scripts/AI/LocalBotController.cs`
- `unity/OverthroneUnity/Assets/Scripts/Input/PlayerInputReader.cs`
- `unity/OverthroneUnity/Assets/Editor/OverthroneUnityBootstrap.cs`
- `unity/OverthroneUnity/Assets/Scenes/Prototype.unity`

### 2. 상대 전원 Captured 즉시 승리 추가
`LocalMatchManager`에 all-captured 승리 조건을 추가했다. 상대 팀 참가자가 모두 `CaptureStatus.Captured`가 되면 3점령 카운트다운과 별개로 즉시 `Winner`, `Phase = Result`, `RoundEnded` 이벤트가 설정된다.

이 과정에서 왕 후보 계산도 팀당 한 번만 수행하도록 정리했다. 기존에는 참가자마다 `ResolveKingCandidate(...)`가 전체 roster를 다시 훑어 P3 지적처럼 작은 규모에서도 불필요한 반복이 있었다.

Files:
- `unity/OverthroneUnity/Assets/Scripts/Match/LocalMatchManager.cs`
- `unity/OverthroneUnity/Assets/Tests/EditMode/LocalMatchFlowTests.cs`

### 3. 시작 스폰과 Slime 속도 보정
Red 기본 스폰이 CapturePoint A 반경과 겹치지 않도록 Z축을 뒤로 옮겼다. 이제 씬 시작 직후 플레이어가 아무 행동을 하지 않았는데도 A가 자동으로 Red 점령되는 상태를 피한다.

Slime은 GDD 기준의 고기동 탈출 스킬이어야 하므로 `walkSpeed/runSpeed`를 9.8m/s로 올렸다. 일반 스프린트 7.2m/s보다 빠르도록 테스트도 추가했다.

Files:
- `unity/OverthroneUnity/Assets/Scripts/Match/LocalRosterSlot.cs`
- `unity/OverthroneUnity/Assets/Profiles/DefaultMovementProfiles.asset`
- `unity/OverthroneUnity/Assets/Tests/EditMode/LocalRosterBuilderTests.cs`
- `unity/OverthroneUnity/Assets/Tests/EditMode/MovementProfileTests.cs`

### 4. 문서 업데이트
GDD와 Unity README에 로컬 봇 MVP, all-captured 승리, Slime 속도 보정, 남은 AI 한계를 반영했다. "로컬 봇이 생겼다"와 "완성형 AI가 생겼다"는 다르므로, NavMesh/pathfinding/장애물 회피/수색/경계 행동은 명확히 남은 작업으로 적었다.

Files:
- `GDD.md`
- `unity/OverthroneUnity/README.md`

## Verification
- `node -e "JSON.parse(require('fs').readFileSync('unity/OverthroneUnity/Assets/Input/OverthroneControls.inputactions','utf8')); console.log('inputactions json ok')"`: Passed
- `/Applications/Unity/Hub/Editor/6000.0.78f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/wy/Github-repos/game1/unity/OverthroneUnity -executeMethod OverthroneUnityBootstrap.BootstrapPrototypeScene -quit -logFile -`: Exit code 0
- `/Applications/Unity/Hub/Editor/6000.0.78f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/wy/Github-repos/game1/unity/OverthroneUnity -runTests -testPlatform EditMode -testResults /Users/wy/Github-repos/game1/unity/OverthroneUnity/TestResults.xml -logFile -`: Exit code 0
- `unity/OverthroneUnity/TestResults.xml`: `total="108" passed="108" failed="0" skipped="0"`
- `git diff --check`: Passed

## What Didn't Work

### codex-review wrapper
- Tried: 중간점검 후 독립 리뷰 커맨드를 실행하려 했다.
- Problem: 로컬에 `codex-review` 명령이 없어 실행할 수 없었다.
- Evidence: `zsh:1: command not found: codex-review`
- Follow-up: 이후에는 지적사항을 직접 checklist로 추적하고, 수정 범위별 EditMode 테스트를 추가했다.

### NavMesh 없는 봇
- Tried: 지금 단계에서 NavMesh/pathfinding 없이 straight-line local bot을 만들었다.
- Problem: 장애물 회피, 수색, 경계, 포위 같은 AI 품질은 아직 없다.
- Lesson: 그래도 이전처럼 NPC가 0 입력으로 서 있는 상태보다는 핵심 루프 검증 가능성이 크게 올라갔다. 다음 AI 단계는 NavMeshAgent 또는 별도 steering/path layer가 필요하다.

## Remaining Risks / Follow-up
- 실제 PlayMode/E2E로 몇 분 이상 돌려보는 통합 플레이 검증이 아직 없다.
- GDD 승리 조건 중 시간 종료 생존, 포기 처리는 아직 미구현이다.
- 왕이 혼자 Tackle -> Hold -> Capture까지 가능한 현재 규칙을 의도된 예외로 둘지, 협동 강제를 위해 막을지 결정해야 한다.
- 로컬 봇은 MVP다. NavMesh, 장애물 회피, guard/search behavior, hearing 기반 추적은 다음 단계다.
- 정식 애니메이션과 사운드 에셋이 붙으면 봇 상태 전환과 이동 소리/은신 밸런스를 다시 검증해야 한다.

## Commit Scope Check
- Included: P0~P2 후속 Unity source/assets/tests, GDD/README 문서, checkpoint/wrap 문서.
- Excluded: Unity generated cache, `TestResults.xml`, Node/브라우저 prototype generated outputs.

## Suggested Commit
fix(overthrone): add local bot gameplay loop

Co-Authored-By: Codex <noreply@openai.com>
