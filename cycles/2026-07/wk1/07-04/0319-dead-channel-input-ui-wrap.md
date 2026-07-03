---
date: 2026-07-04
scope: [unity, overthrone, hud, capture-loop, dead-channel]
type: feature
---

## TL;DR
Captured 상태의 로컬 플레이어가 HUD 입력 필드와 명시적 Send 버튼으로 같은 팀 dead channel에 메시지를 보낼 수 있는 입력 UI 계약을 추가했다. 기존 `LocalDeadChannel`의 저장/가시성 규칙은 유지하면서 `PlayerHud`와 bootstrap HUD 생성 경로에 input field/send button을 연결하고, EditMode 테스트로 captured/alive/blank message 경계를 검증했다.

## Keywords
`PlayerHud` `LocalDeadChannel` `InputField` `Button` `SubmitDeadChannelInput` `TrySubmitDeadChannelText` `OverthroneUnityBootstrap` `LocalSpectatorCameraTests`

## Context
GDD 현재 구현 상태에서 포획/구출 루프는 관전 overlay, dead channel 로그, 최종 포획 system join 메시지까지 있었지만, 실제 로컬 플레이어가 dead channel에 입력하는 UI 경로는 남은 일로 기록되어 있었다.

이번 목표는 온라인 채팅/moderation까지 확장하는 것이 아니라 local-first prototype에서 바로 검증 가능한 입력 표면을 만드는 것이다. Captured 상태가 된 플레이어만 같은 팀 dead channel을 읽고 쓸 수 있어야 하고, 살아 있는 플레이어가 dead channel을 읽거나 쓰면 안 된다. 기존 `LocalDeadChannel.TryPost`가 core 권한 규칙을 이미 갖고 있으므로, HUD는 이 규칙을 우회하지 않고 UI active/interactable 상태와 제출 경로만 담당하게 했다.

## Investigation
현재 구현을 확인했을 때 다음 상태였다.

- `LocalDeadChannel`은 Captured author만 `TryPost`를 허용하고, viewer team별로 `BuildVisibleLog`를 생성한다.
- `LocalCaptureSystem`은 최종 포획 시 `joined dead channel` system message를 남긴다.
- `PlayerHud.BuildSpectatorText`는 관전 overlay에 dead channel log를 붙인다.
- `OverthroneUnityBootstrap.CreatePrototypeHud`는 prototype HUD를 코드로 생성하므로, 실제 로컬 실행에서 입력 UI가 보이려면 runtime `PlayerHud`뿐 아니라 bootstrap 생성 경로도 함께 바뀌어야 했다.

## What Didn't Work
### 패키지 자동 업그레이드 diff
- Tried: Unity/UI 입력 경로를 확인하는 과정에서 `Packages/manifest.json`, `packages-lock.json`에 Unity 6000.5 계열 자동 업그레이드 diff가 생김.
- Problem: dead channel 입력 UI와 무관한 package version churn이며, URP/Test Framework/uGUI 버전까지 건드려 blast radius가 너무 큼.
- Lesson: Unity batch/editor 작업 뒤에는 package/project settings 부산물을 항상 별도 확인하고, 기능과 무관한 자동 재직렬화는 커밋 범위에서 제외한다.

### InputSystemUIInputModule 기본 actions 미할당 위험
- Tried: bootstrap에서 `InputSystemUIInputModule`만 추가.
- Problem: UI input module은 기본 UI actions가 없으면 클릭/텍스트 입력이 동작하지 않을 수 있다.
- Lesson: 코드 생성 EventSystem에서는 `AssignDefaultActions()`까지 호출해 local prototype에서 바로 선택/입력 가능한 상태로 만든다.

### InputField end-edit 전송 위험
- Tried: `InputField.onEndEdit`에 제출 경로를 연결하는 초기 구현.
- Problem: Unity `onEndEdit`는 Enter뿐 아니라 포커스 해제에서도 호출될 수 있어, 클릭/포커스 전환만으로 메시지가 전송되는 UX 위험이 있다.
- Lesson: prototype에서도 전송은 명시적 Send 버튼으로 고정하고, input field는 텍스트 입력만 담당하게 한다.

## Decision Rationale
### `PlayerHud`는 UI 상태와 제출 경로만 담당
`LocalDeadChannel`의 권한 규칙을 중복 구현하지 않도록 했다. HUD는 `captureAgent.Status == Captured`, `deadChannel != null`, `Team != None`일 때만 input field를 active/interactable로 만들고, 실제 post는 `deadChannel.TryPost(captureAgent, body)`로 위임한다.

### 테스트 가능한 public API 분리
UI Send button에 연결할 수 있는 `SubmitDeadChannelInput()`과 테스트에서 직접 호출 가능한 `TrySubmitDeadChannelText(string body)`를 제공했다. 이로써 실제 `InputField` text를 읽어 버튼 클릭으로 제출하는 경로와 core submit 경로를 모두 검증할 수 있다.

### Bootstrap까지 반영
씬/프리팹 YAML을 직접 수정하지 않고 `OverthroneUnityBootstrap`의 코드 생성 경로에 input field, hint/status text, EventSystem 생성을 추가했다. 이후 bootstrap을 다시 실행하면 Prototype HUD에 dead channel 입력 UI가 포함된다.

## Work Accomplished
### 1. Captured 전용 dead channel input UI 계약 추가
`PlayerHud`에 optional `InputField`, Send `Button`, status text, hint text 참조를 추가했다.

- Captured + valid team + channel 조건에서만 input field와 Send button active/interactable.
- alive/uncaptured 상태에서는 input UI inactive/non-interactable.
- 공백 메시지는 post하지 않고 `Not sent` 상태로 처리.
- 성공 제출 시 input field를 비우고 spectator overlay log를 즉시 갱신.

### 2. Prototype HUD bootstrap에 실제 입력 UI 생성 추가
`OverthroneUnityBootstrap.CreatePrototypeHud`에 다음 UI를 추가했다.

- `Dead Channel Hint Text`
- `Dead Channel Input`
- `Dead Channel Send Button`
- `Dead Channel Status Text`

또한 UI 선택/입력을 위해 `EventSystem`과 `InputSystemUIInputModule`을 생성하고 `AssignDefaultActions()`를 호출한다.

### 3. EditMode 테스트 보강
`LocalSpectatorCameraTests`에 dead channel input 경계를 추가했다.

- alive local player는 dead channel input 제출 실패.
- whitespace input은 실패하고 기존 field text를 유지.
- captured local player는 input field + Send button click으로 메시지를 제출하고 spectator overlay log에서 확인 가능.

## Verification
관련 EditMode 테스트를 Unity batchmode로 실행했다.

```bash
/Applications/Unity/Hub/Editor/6000.0.78f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /Users/wy/Github-repos/game1/unity/OverthroneUnity \
  -runTests \
  -testPlatform EditMode \
  -testFilter LocalSpectatorCameraTests \
  -testResults /Users/wy/Github-repos/game1/unity/OverthroneUnity/EditModeTestResults.xml \
  -logFile /tmp/overthrone-editmode-deadchannel.log
```

결과 XML 기준:

- total: 8
- passed: 8
- failed: 0
- fixture: `LocalSpectatorCameraTests`

추가로 `git diff --check`를 통과했다.

## Architecture Impact
이 변경은 온라인 채팅 시스템을 추가하지 않는다. 로컬 prototype에서 dead channel 입력/표시 UX를 검증할 수 있게 하는 HUD 레벨 계약이다. 이후 Photon/server-authoritative 채팅으로 전환할 때 `TrySubmitDeadChannelText`의 내부 post 대상만 네트워크 RPC/권위 서버 경로로 옮길 수 있다.

주의할 점은 현재 입력 UI가 bootstrap 코드 생성 경로에 추가되었으므로 기존 저장된 `Prototype.unity` 씬 YAML에는 즉시 반영되지 않을 수 있다는 것이다. 씬을 재생성하거나 bootstrap을 다시 실행하면 HUD에 포함된다.

## Files Changed
| File | Change |
|------|--------|
| `GDD.md` | 포획/구출 루프 구현 근거에 Captured 전용 dead channel 입력 UI 반영 |
| `unity/OverthroneUnity/Assets/Scripts/PlayerHud.cs` | dead channel input field/send button active/interactable 제어와 submit API 추가 |
| `unity/OverthroneUnity/Assets/Editor/OverthroneUnityBootstrap.cs` | Prototype HUD에 dead channel input/send/hint/status UI와 EventSystem 생성 추가 |
| `unity/OverthroneUnity/Assets/Tests/EditMode/LocalSpectatorCameraTests.cs` | captured/alive/blank dead channel input 경계 테스트 추가 |

## Remaining Work
- 온라인 dead channel 채팅/RPC 권위화.
- 채팅 moderation, rate limit, mute/report 정책.
- 정식 포획 VFX/SFX 에셋/믹싱.
- 저장된 `Prototype.unity` 씬을 bootstrap 결과와 동기화할지 별도 결정.

## Commit
feat(overthrone): add dead channel input ui
