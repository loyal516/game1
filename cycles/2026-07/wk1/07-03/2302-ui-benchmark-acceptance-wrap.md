---
date: 2026-07-03
scope: [gdd, ui-benchmark, hud]
type: feature
---

## TL;DR
GDD `§9.6 UI 벤치마크 리서치`에 UI surface별 적용 수용 기준과 UI 2차 구현 체크리스트를 추가하였다. 기존 벤치마크 게임 목록을 반복하지 않고, 팀 상태 레일, objective panel, 핑, 관전 HUD, 이벤트 배너가 로컬-first 프로토타입에서 어떤 검증 기준을 만족해야 하는지 문서화했다.

## Keywords
`GDD.md` `UI benchmark` `HUD acceptance criteria` `Dead by Daylight` `Splatoon 3` `Overwatch 2` `THE FINALS` `Among Us` `LocalPingSystem` `PlayerHud`

## Context
사용자의 장기 목표는 GDD 기준으로 게임을 끝까지 진전시키되, 덜 된 부분은 명확히 남기고, 벤치마크할 게임들의 UI도 조사해 GDD에 추가하는 것이다. 이전 작업들에서 `§9.6`에는 이미 Dead by Daylight, Among Us, Splatoon 3, Overwatch 2, Apex/EA accessibility pledge, THE FINALS 기반 벤치마크 표와 확정 규칙이 들어가 있었다.

따라서 이번 작업은 레퍼런스 게임을 새로 많이 늘리는 것이 아니라, 이미 공식 출처 중심으로 정리된 벤치마크를 실제 UI 구현의 판단 기준으로 전환하는 것이었다. 특히 다음 UI 작업에서 "무엇을 만들면 완료라고 볼 수 있는가"가 흐려지지 않도록, 수용 기준과 로컬 검증 방식을 분리해 적었다.

## Investigation
최신 cycles 문서는 wiki 지침에 따라 Explore agent로 확인했다. 관련 문서는 다음과 같았다.

- `2228-footstep-surface-ui-benchmark-wrap.md`: UI 벤치마크를 공식 자료 중심으로 보강하고 GDD `§9.6`에 반영한 최신 문서.
- `0929-victory-conditions-ui-benchmark-wrap.md`: 승리 조건 구현과 함께 UI 벤치마크 표를 공식/준공식 출처 중심으로 정리한 문서.
- `0957-navmesh-bot-pathing-wrap.md`: UI 자체 구현은 아니지만 로컬 MVP에 HUD/미니맵/핑/관전이 들어가 있다는 상태를 재확인한 문서.
- `0845-overthrone-local-prototype-wrap.md`: GDD 재정렬과 Unity 로컬 프로토타입에 HUD/미니맵/핑/관전을 포함한 기반 문서.

외부 자료는 공식/준공식 자료를 우선 확인했다.
- Dead by Daylight Developer Update: Survivor Activity HUD와 chase 표시.
- Splatoon 3 공식 gameplay/beginner basics: 색상 기반 영역 점유와 맵 확인.
- Overwatch 2 공식 ping system: contextual ping과 hold wheel.
- EA accessibility patent pledge: mappable input 기반 contextual audio/visual communication.
- THE FINALS patch notes: spectator HUD/team HUD 개선.
- Among Us 공식 페이지: task bar, emergency meeting, sabotage, admin map/security camera.

## What Didn't Work
### 중복 벤치마크 표 추가
- Tried: 기존 `§9.6`에 참고 게임을 더 늘리는 방향을 고려했다.
- Problem: 이미 `§9.6.1`, `§9.6.2`, `§9.6.3`에 같은 원칙이 정리되어 있어 반복 설명이 되기 쉬웠다.
- Lesson: 추가 레퍼런스보다 "적용 수용 기준"과 "검증 방식"이 다음 구현에 더 도움이 된다.

### 완료처럼 읽히는 수용 기준
- Tried: 최초 문안에는 "0.5초 안에 읽을 수 있어야 한다", "팀원 응답은 같은 마커에 누적한다"처럼 완료 기준처럼 읽히는 문장이 있었다.
- Problem: 독립 리뷰에서 실제 검증 방식이 판독성 실험을 증명하지 못하고, 핑 응답 누적이 구현 완료와 polish backlog 사이에서 애매하게 읽힌다는 P3 지적이 나왔다.
- Lesson: 문서 수용 기준은 구현된 사실과 향후 검증 목표를 분리해 써야 한다.

## Decision Rationale
`§9.6.4 UI 벤치마크 적용 수용 기준`은 UI surface별로 분리했다. 팀 상태 레일은 Dead by Daylight, objective panel은 Splatoon 3, 핑은 Overwatch 2/EA pledge, 관전 HUD는 THE FINALS, 이벤트 배너는 Among Us를 기준으로 삼았다.

`§9.6.5 UI 2차 구현 체크리스트`는 GDD `§19.4`의 남은 일과 연결되도록 작성했다. 아이콘 아트, 핑 응답 상호작용 polish, compact layout, 관전 HUD compact mode, 접근성/스팸 보호 옵션을 P0/P1/P2로 나눴다.

문구는 과장되지 않도록 다음 경계를 두었다.
- 0.5초 판독은 즉시 증명된 구현 사실이 아니라 PlayMode 관찰/플레이테스트 체크 항목으로 분리.
- 핑 응답은 데이터 누적/TTL 검증과 시각 polish/최신 응답자 표시를 구분.
- 결과 배너는 모든 이벤트를 같은 문구로 쓰는 것이 아니라, 각 이벤트명이 surface 간 동일한 용어로 반복되어야 한다고 명확히 표현.

## Work Accomplished
### 1. UI surface별 수용 기준 추가
`GDD.md`에 `9.6.4 UI 벤치마크 적용 수용 기준`을 추가했다.

포함된 surface:
- 팀 상태 레일
- Objective panel
- 핑/커뮤니케이션
- 관전 HUD
- 긴급 이벤트/결과 배너

각 항목에는 벤치마크 근거, Overthrone 수용 기준, 로컬 검증 방식을 함께 적었다.

### 2. UI 2차 구현 체크리스트 추가
`GDD.md`에 `9.6.5 UI 2차 구현 체크리스트`를 추가했다.

남은 작업을 다음 항목으로 정리했다.
- 팀 상태 아이콘 아트
- 핑 응답 상호작용 polish
- Objective panel compact mode
- 관전 HUD compact mode
- 접근성/스팸 보호 옵션

### 3. 리뷰 피드백 반영
문서 diff만 대상으로 독립 리뷰를 실행했다. 결과는 P0/P1/P2 없음, P3/P4 문구 정리였다. 지적된 세부 내용은 모두 반영했다.

## Verification
문서 변경 검증:

```bash
git diff --check
```

결과:
- 출력 없음. whitespace/error 없음.

독립 리뷰:
- `codex exec --sandbox read-only`로 `GDD.md` diff만 검토.
- 결과: P0/P1/P2 없음.
- P3/P4 문구 정리 반영 완료.

## Architecture Impact
코드 변경은 없다. 이번 변경은 GDD의 UI 계약을 구체화하는 문서 작업이다. 다만 이후 UI 구현에서는 `PlayerHud`, `LocalPingSystem`, `LocalSpectatorCamera`, `LocalMatchFlowPresenter`가 이 기준을 검증 대상으로 삼게 된다.

남은 리스크:
- PlayMode/E2E 장시간 UI 검증은 아직 없다.
- 1280x576 같은 낮은 viewport에서 실제 겹침 여부는 코드/씬 실행 후 스크린샷으로 별도 확인해야 한다.
- 온라인 전환 시 핑/미니맵/관전 정보 노출 범위는 RPC/권위 경계에 맞춰 재검토해야 한다.

## Files Changed
| File | Change |
|------|--------|
| `GDD.md` | `§9.6.4` UI benchmark acceptance criteria와 `§9.6.5` UI 2차 구현 체크리스트 추가 |
| `cycles/2026-07/wk1/07-03/2302-ui-benchmark-acceptance-wrap.md` | 이번 GDD 보강 작업의 배경, 결정, 검증 기록 |

## Commit
docs(gdd): add ui benchmark acceptance criteria

Co-Authored-By: Codex <noreply@openai.com>
