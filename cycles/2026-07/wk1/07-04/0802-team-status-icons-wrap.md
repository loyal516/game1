---
date: 2026-07-04
scope: [unity, overthrone, hud, ui]
type: feature
---

## TL;DR
팀 상태 레일에 상태별 절차적 아이콘을 추가하여 GDD P0 `팀 상태 아이콘 아트`의 로컬 MVP를 구현했다. 기존 텍스트 레일은 유지하면서 Captured/Held/Holding/King/Attacker/Slime/Neutral 상태를 sprite shape와 색상으로 구분하고, `PlayerHudUiTests` 12개 및 전체 EditMode 150개 테스트 통과로 기존 HUD 회귀가 없음을 확인했다.

## Keywords
`PlayerHud` `teamStatusIconImages` `TeamStatusIcon_King` `TeamStatusIcon_Held` `OverthroneUnityBootstrap` `PlayerHudUiTests`

## Context
GDD §9.6.5에는 UI 2차 구현 체크리스트의 P0로 `팀 상태 아이콘 아트`가 남아 있었다. 기존 HUD는 좌측 팀 상태 레일을 텍스트로 표시했지만, 상태별 의미가 텍스트와 색상에 의존해 색각 접근성 및 빠른 판독성 측면에서 부족했다.

이번 세션의 목표는 온라인/RPC나 정식 아트 에셋을 도입하지 않고, 로컬 Unity prototype 범위 안에서 팀 상태 레일에 상태별 아이콘 표시 계약을 먼저 만드는 것이었다. 실제 아트 에셋은 아직 없으므로 정식 완료가 아니라 로컬 MVP로 기록했다.

## Investigation
최신 cycle 문서를 Explore로 역순 확인했다. 반복 gap은 크게 세 가지였다.

- 실제 씬 기반 장시간 PlayMode/occluder 검증
- 온라인 RPC/권위화
- UI icon art, compact mode, final polish

직전 커밋 `bda356f`에서 6봇 PlayMode soak fixture가 추가됐으므로, 이번 세션은 같은 AI 검증 축을 바로 반복하기보다 로컬에서 닫을 수 있는 HUD P0 항목을 줄이는 방향으로 정했다.

코드 확인 결과 `PlayerHud`는 이미 `teamRailText`를 통해 참가자별 팀/이름/state/capture status를 표시하고 있었고, `PlayerHudUiTests`도 HUD 동작을 촘촘하게 검증하고 있었다. 따라서 기존 텍스트 레일을 유지한 채 optional `Image[]` 아이콘 슬롯을 추가하는 것이 가장 작은 변경 범위였다.

## What Didn't Work
### 리뷰 subagent 사용량 제한
- Tried: 현재 diff에 대해 독립 리뷰 subagent를 실행했다.
- Problem: usage limit에 걸려 리뷰가 완료되지 못했다.
- Lesson: 리뷰가 막히더라도 메인 세션에서 diff/read/test evidence를 직접 확보해야 한다. 이번 세션에서는 `PlayerHudUiTests`와 전체 EditMode를 직접 실행해 회귀를 확인했다.

### 정식 아이콘 에셋 도입 보류
- Tried: GDD의 "아이콘 아트" 항목을 닫기 위해 실제 에셋을 추가하는 방안도 가능했다.
- Problem: 현재 프로젝트는 로컬 prototype이며 외부 asset pipeline/Git LFS/아트 스타일 확정이 없다. 정식 에셋을 급히 넣으면 임시 asset churn이 커질 수 있다.
- Lesson: 이번 단계에서는 절차적 sprite shape로 표시 계약과 테스트를 먼저 만들고, 정식 아트와 색각 모드 스크린샷 QA는 다음 작업으로 남긴다.

## Decision Rationale
`PlayerHud.Configure(...)`의 뒤쪽에 optional `Image[] teamStatusIconImages = null` 파라미터를 추가했다. 이렇게 하면 기존 호출부와 테스트를 깨지 않고, bootstrap에서만 아이콘 슬롯을 넘길 수 있다.

아이콘은 런타임 코드에서 16x16 `Texture2D`를 절차적으로 생성하고 `Sprite.Create`로 캐싱한다. 이유는 다음과 같다.

- 외부 asset 추가 없이 상태별 shape를 즉시 제공할 수 있다.
- sprite 이름이 `TeamStatusIcon_King`, `TeamStatusIcon_Held`처럼 안정적이라 테스트에서 검증 가능하다.
- 색상만 다른 표시가 아니라 pixel shape 자체가 다르므로 텍스트/색상 의존을 줄인다.

## Work Accomplished
### 1. PlayerHud 팀 상태 아이콘 표시
`PlayerHud`에 `teamStatusIconImages`를 추가하고, `Refresh()`에서 매 프레임 참가자 상태에 맞춰 아이콘 슬롯을 갱신한다.

- File: `unity/OverthroneUnity/Assets/Scripts/PlayerHud.cs`
- capture status 우선순위: `Captured` → `Held` → `Holding`
- movement state 우선순위: `King` → `Attacker` → `Slime` → `Neutral`
- 참가자가 없는 stale icon slot은 inactive 처리하고 sprite를 비운다.
- sprite는 상태별 안정 이름과 shape를 갖는 절차적 texture로 생성된다.

### 2. Bootstrap HUD 아이콘 슬롯 연결
`OverthroneUnityBootstrap.CreateHud`에서 match participant 수만큼 18px 아이콘 슬롯을 생성하고 `PlayerHud.Configure`에 연결했다.

- File: `unity/OverthroneUnity/Assets/Editor/OverthroneUnityBootstrap.cs`
- 기존 팀 레일 텍스트는 유지하되 폭/위치를 조정해 왼쪽에 아이콘 슬롯이 들어갈 공간을 만든다.

### 3. EditMode 테스트 추가
`PlayerHudShowsTeamStatusIconsForActiveParticipantStatesAndHidesStaleSlots` 테스트를 추가했다.

- File: `unity/OverthroneUnity/Assets/Tests/EditMode/PlayerHudUiTests.cs`
- King/Holding/Held/Captured 아이콘이 active되고 sprite/color가 stale 상태를 덮어쓰는지 확인한다.
- 상태별 sprite name/color가 서로 다른지 확인한다.
- participant보다 많은 icon slot이 있을 때 stale slot이 inactive 되는지 확인한다.

### 4. GDD 상태 갱신
GDD §9.6.5와 §19.4에 팀 상태 아이콘 로컬 MVP 완료를 반영했다.

- File: `GDD.md`
- 남은 일은 정식 아이콘 에셋, 색각 모드 스크린샷 QA, compact layout 실측, 온라인 RPC 동기화로 분리했다.

## Architecture Impact
이번 변경은 HUD 표시 계약을 넓히지만, 기존 텍스트 팀 레일을 제거하지 않는다. 따라서 현재 UI는 다음 두 계층을 동시에 가진다.

- 텍스트: 팀, 이름, movement state, capture status를 그대로 표시
- 아이콘: 빠른 판독을 위한 상태별 shape/color 표시

정식 UI 단계에서는 절차적 sprite를 실제 아트 asset으로 교체할 수 있다. 교체 시에도 `teamStatusIconImages` 배열과 상태 판정 계약은 유지할 수 있다.

주의할 점은 현재 아이콘은 코드에서 생성되는 임시 MVP이므로, 실제 색각 모드/저해상도 화면 검증은 아직 완료 증거가 아니다.

## Verification
- `git diff --check`
  - 통과
- `Unity -batchmode -runTests -testPlatform EditMode -testFilter PlayerHudUiTests`
  - Result XML: `total="12" passed="12" failed="0"`
- `Unity -batchmode -runTests -testPlatform EditMode`
  - Result XML: `total="150" passed="150" failed="0"`

## Current Session Summary
이번 큰 흐름에서 로컬 prototype 기준으로 다음 진전이 있었다.

- 6봇 PlayMode soak fixture 추가: direct fallback, choke, dynamic avoidance, match invariant 240프레임 검증.
- 팀 상태 아이콘 로컬 MVP 추가: 상태별 절차적 sprite와 stale slot 처리.
- GDD는 과장 없이 `로컬 MVP 완료`와 `실제 씬/정식 아트/온라인 권위화 미완`을 분리해 기록했다.

다음 세션에서 바로 이어갈 우선순위 후보는 다음과 같다.

1. 실제 씬 기반 장시간 PlayMode/occluder/narrow path 검증.
2. Objective panel/관전 HUD compact mode의 1280x576 실측.
3. 정식 포획 VFX/SFX 에셋 또는 AudioMixer/clip 교체.
4. 온라인 전환 전 RPC/권위 경계 설계 문서화.

## Files Changed
| File | Change |
|------|--------|
| `unity/OverthroneUnity/Assets/Scripts/PlayerHud.cs` | 팀 상태 아이콘 배열, 상태별 sprite/color 판정, 절차적 sprite 캐시 추가 |
| `unity/OverthroneUnity/Assets/Editor/OverthroneUnityBootstrap.cs` | 팀 상태 아이콘 슬롯 생성 및 HUD 연결 |
| `unity/OverthroneUnity/Assets/Tests/EditMode/PlayerHudUiTests.cs` | 상태별 아이콘 active/sprite/color/stale slot 테스트 추가 |
| `GDD.md` | 팀 상태 아이콘 로컬 MVP 및 남은 리스크 갱신 |
| `cycles/2026-07/wk1/07-04/0802-team-status-icons-wrap.md` | 세션 wrap 문서 추가 |

## Commit
feat(overthrone): add team status icons
