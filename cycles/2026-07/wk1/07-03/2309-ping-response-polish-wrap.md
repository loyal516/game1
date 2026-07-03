---
date: 2026-07-03
scope: [unity, hud, ping]
type: feature
---

## TL;DR
`LocalPingSystem`에 활성 핑 대상 응답 누적 API를 추가하고, `PlayerHud` 팀 로그에서 응답 묶음과 최신 응답자를 표시하도록 구현했다. GDD `§9.6.5`의 P0 핑 응답 polish는 로컬 MVP 기준 완료로 갱신했고, 온라인 RPC/권위 경계는 계속 남은 일로 유지했다.

## Keywords
`LocalPingSystem` `LocalPingResponse` `SubmitResponse` `PlayerHud` `PlayerHudUiTests` `GDD.md` `ping response` `LATEST`

## Context
이전 문서 작업에서 GDD `§9.6.4`와 `§9.6.5`에 UI 벤치마크 수용 기준과 UI 2차 체크리스트를 분리했다. 그중 P0로 남아 있던 항목은 "같은 핑에 누적된 간다/방어/도움/점령 응답을 시각적으로 묶고, 최신 응답자가 팀 로그에 표시됨"이었다.

기존 Unity 구현은 `G` 탭 컨텍스트 핑, `G` 홀드 방사형 핑 휠, 미니맵 핑 마커, 팀 로그 TTL 표시까지는 있었지만, 같은 핑에 대한 팀원 응답을 축적하거나 최신 응답자를 노출하는 계약은 없었다.

## Investigation
확인한 최신 문맥:

- `cycles/2026-07/wk1/07-03/2302-ui-benchmark-acceptance-wrap.md`: 핑 응답 polish가 UI 2차 P0 미완 항목으로 남아 있음.
- `GDD.md §9.6.4`: 핑 데이터는 월드 마커, 미니맵 마커, 팀 로그를 동시에 만들고 같은 마커에 팀 응답을 누적할 수 있어야 한다고 명시.
- `GDD.md §19.4`: HUD/UI 남은 일에 핑 응답 상호작용 polish가 남아 있었음.

코드에서는 `LocalPingSystem.BuildVisibleLog()`가 단일 `PING {label} {seconds}s`만 반환하고, 응답 상태를 보관하는 타입이나 API가 없었다.

## Work Accomplished
### 1. 로컬 핑 응답 데이터 추가
`LocalPingSystem`에 `LocalPingResponse`와 응답 목록을 추가했다.

동작:
- `SubmitResponse(responderName, type, team)`은 활성 핑이 있을 때만 `true`를 반환하고 응답을 누적한다.
- `ResponseCount`와 `LatestResponse`로 테스트/후속 UI가 현재 응답 상태를 읽을 수 있다.
- `Attention`, `Defend`, `Help`, `Objective`는 각각 `Going`, `Defend`, `Help`, `Capture` 라벨로 표시된다.

### 2. HUD 팀 로그 표시 강화
`BuildVisibleLog()`는 기존 `PING` 줄을 유지하면서 응답이 있으면 아래 정보를 추가한다.

```text
RESPONSES  Blue Runner: Going | Blue Anchor: Defend
LATEST  Blue Anchor: Defend
```

새 핑이 등록되거나 TTL이 만료되면 응답 목록은 초기화된다.

### 3. 테스트 추가
`PlayerHudUiTests`에 다음 검증을 추가했다.

- `PlayerHudPingLogAccumulatesResponsesAndShowsLatestResponder`
  - 같은 핑에 Going/Defend/Help/Capture 응답 4개가 누적됨.
  - HUD 로그가 모든 응답자와 응답 라벨을 포함함.
  - 최신 응답자 `Blue Caller: Capture`가 `LATEST`로 표시됨.
- `LocalPingSystemClearsResponsesWhenNewPingStartsOrExpires`
  - 활성 핑이 없으면 응답이 거부됨.
  - 새 핑 등록 시 이전 응답이 제거됨.
  - TTL 만료 후 응답 목록과 로그가 비워짐.

### 4. GDD 상태 갱신
GDD `§9.6.5`의 핑 응답 항목을 로컬 MVP 완료로 바꾸고, `§19.4` HUD/UI 남은 일에서 핑 응답 polish를 제거했다. 온라인 RPC/권위 경계는 아직 구현하지 않았으므로 남은 리스크로 유지했다.

## Verification
Unity EditMode 테스트를 `PlayerHudUiTests` 필터로 실행했다.

```bash
/Applications/Unity/Hub/Editor/6000.0.78f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /Users/wy/Github-repos/game1/unity/OverthroneUnity \
  -runTests \
  -testPlatform EditMode \
  -testFilter PlayerHudUiTests \
  -testResults /Users/wy/Github-repos/game1/unity/OverthroneUnity/TestResults.xml \
  -logFile -
```

결과:

```text
testcasecount=11 result=Passed total=11 passed=11 failed=0
Unity exit code 0 (Ok)
```

추가 확인:

```bash
git diff --check
```

결과:
- 출력 없음. whitespace/error 없음.

## Architecture Impact
핑 응답은 아직 로컬 `LocalPingSystem` 안의 MVP 계약이다. 온라인 전환 시에는 다음 경계를 다시 설계해야 한다.

- 누가 응답 이벤트의 권위를 가지는지
- 같은 핑에 대한 중복 응답/수정/취소를 어떻게 동기화할지
- 팀 로그, 월드 마커, 미니맵 마커를 RPC 이벤트 하나로 묶을지 분리할지
- 스팸 방지 cooldown과 accessibility 옵션을 어디에서 적용할지

이번 작업은 네트워크 구조를 선점하지 않고, UI와 규칙 테스트가 기대하는 로컬 데이터 계약만 닫았다.

## Files Changed
| File | Change |
|------|--------|
| `unity/OverthroneUnity/Assets/Scripts/Communication/LocalPingSystem.cs` | `LocalPingResponse`, `SubmitResponse`, 응답 누적/최신 응답자 로그, 새 핑/TTL 만료 clearing 추가 |
| `unity/OverthroneUnity/Assets/Tests/EditMode/PlayerHudUiTests.cs` | 핑 응답 누적/최신 응답자/clearing EditMode 테스트 추가 |
| `GDD.md` | 핑 응답 polish 로컬 완료 상태와 HUD/UI 남은 일 갱신 |
| `cycles/2026-07/wk1/07-03/2309-ping-response-polish-wrap.md` | 이번 구현/검증/남은 리스크 기록 |

## Commit
Pending.
