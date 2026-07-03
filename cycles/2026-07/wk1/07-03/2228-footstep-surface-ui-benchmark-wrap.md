---
date: 2026-07-03
scope: [overthrone-unity, audio, gdd, ui-benchmark]
type: feature
---

## TL;DR
Unity 로컬 프로토타입에 `FootstepSurface` 기반 표면별 발소리 multiplier를 추가했다. GDD에는 공식 자료 기반 UI 벤치마크 규칙을 반영했고, EditMode 130개 테스트와 독립 리뷰로 이번 변경의 P0-P2 이슈가 없음을 확인했다.

## Keywords
`OverthroneUnity` `FootstepSurface` `PlayerNoiseEmitter` `NoiseSystem` `GDD` `UI benchmark` `Dead by Daylight` `Overwatch 2` `Splatoon 3` `THE FINALS`

## Context
이번 세션의 상위 목표는 GDD 기준으로 로컬 Unity 프로토타입을 계속 실제 게임에 가깝게 밀어붙이는 것이다. 직전 점검에서 AI 부재, 승리 조건 누락, 스폰/점령 겹침, 슬라임 밸런스, 성능/협동 규칙 회색지대 등이 P0-P3로 정리되었고, 이후 로컬 봇 이동/점령/추격/포획과 일부 승리 조건은 보강되었다.

이번 증분은 두 축이었다.

1. GDD의 UI 벤치마크를 공식 자료 중심으로 보강해 이후 HUD/핑/관전 UI 설계의 기준을 명확히 한다.
2. "걸을 때는 소리가 안 나고, 뛸 때는 소리가 나서 들킬 수 있다"는 은신/청각 플레이를 표면별로 확장할 수 있게 만든다.

로컬 인프라 우선 원칙은 유지했다. Photon, Supabase, Steam 같은 온라인/배포 인프라는 이번 범위에서 건드리지 않았다.

## Investigation
문서 연속성을 위해 `wiki` 흐름으로 최신 `cycles/2026-07/wk1/07-03` wrap들을 역순 확인했다. 반복적으로 남은 위험은 다음과 같았다.

- PlayMode/E2E 장시간 검증 부족
- NavMesh 동적 회피, 수색/경계 상태 고도화 필요
- 왕 단독 포획 정책의 기획 확정 필요
- Photon/Supabase/Steam 연동은 아직 로컬 이후 단계

UI 벤치마크는 웹 공식 자료를 기준으로 다시 잡았다.

- Dead by Daylight: 솔로 큐 정보 격차를 줄이되 위치 정보를 과도하게 노출하지 않는 HUD 방향
- Overwatch 2: 탭 컨텍스트 핑과 홀드 휠을 통한 빠른 의사소통
- Splatoon 3: 색상만으로 점령 상태를 전달하지 않는 전장 정보 설계
- THE FINALS: 관전자도 경기 판도를 읽을 수 있는 HUD
- EA Accessibility Patent Pledge: 핑 시스템을 접근성 기능으로 바라보는 근거

발소리 구현은 `PlayerNoiseEmitter`의 기존 계약을 확인하고 진행했다. 기존 구조는 이미 "스프린트 중이고 이동 속도가 기준 이상일 때만 `NoiseSystem.Emit` + `AudioSource.PlayOneShot`" 흐름을 갖고 있었다. 따라서 걷기 무음/뛰기 발소리 계약은 유지하고, 표면별 volume/pitch/noise radius 배율만 얹는 방식이 가장 작은 변경이었다.

## What Didn't Work
### First review finding: airborne surface lookup
- Tried: 단순 하향 raycast로 아래 표면의 `FootstepSurface`를 찾는 방식.
- Problem: 캐릭터가 공중에 있어도 아래 멀리 있는 표면 multiplier가 적용될 수 있었다.
- Fix: `CharacterController.isGrounded`가 false면 표면 lookup을 하지 않도록 했다.

### Second review finding: lower surface leaks through plain ground
- Tried: raycast hit 목록에서 `FootstepSurface`가 붙은 collider를 찾아 반환하는 방식.
- Problem: 플레이어가 표면 정보 없는 일반 바닥에 서 있고 그 아래에 표면 collider가 겹쳐 있으면, 아래 표면 multiplier가 적용될 수 있었다.
- Fix: 가장 가까운 non-self collider를 먼저 고른 뒤, 그 collider의 parent에서만 `FootstepSurface`를 찾도록 바꿨다.

### Performance review finding
- Tried: `Physics.SyncTransforms()`와 `Physics.RaycastAll()`을 생산 코드에서 사용하는 방식.
- Problem: 발소리 틱마다 transform sync와 배열 할당/정렬 위험이 생긴다.
- Fix: 생산 코드는 `Physics.RaycastNonAlloc()`만 사용한다. `Physics.SyncTransforms()`는 EditMode 테스트 fixture의 물리 동기화 용도로만 남겼다.

## Decision Rationale
표면별 발소리는 클립 교체 시스템까지 한 번에 넣지 않고 multiplier만 추가했다.

- 클립 교체까지 넣으면 AudioMixer, clip library, surface taxonomy까지 같이 정해야 한다.
- 현재 필요한 게임플레이 검증은 "뛸 때 들키는 소리의 크기/거리/톤이 표면에 따라 달라진다"는 규칙이다.
- 따라서 `FootstepSurface` 컴포넌트가 volume, pitch, noise radius 배율만 제공하고, 기존 `PlayerNoiseEmitter`가 이를 소비하는 구조로 제한했다.

이 구조는 나중에 정식 표면별 오디오 클립을 추가할 때도 확장 가능하다. `FootstepSurface`에 clip set 또는 surface id를 추가하고, `PlayerNoiseEmitter`가 현재 clip 선택을 위임하면 된다.

## Work Accomplished
### 1. GDD UI benchmark update
GDD의 UI 벤치마크 섹션을 공식 자료 링크 중심으로 정리하고, 확정 규칙을 별도 섹션으로 남겼다.

- 솔로 큐 정보 격차 완화
- 탭/홀드 기반 핑 구조
- 점령 정보의 비색상 의존 표현
- 관전 HUD 기준
- 핑을 접근성 기능으로 다루는 원칙

File: `GDD.md`

### 2. FootstepSurface component
새 `FootstepSurface` 컴포넌트를 추가했다. 각 표면은 다음 multiplier를 제공한다.

- `VolumeMultiplier`
- `PitchMultiplier`
- `NoiseRadiusMultiplier`

값은 런타임에서 음수/무음 pitch가 되지 않도록 `FootstepSurfaceMultipliers`에서 clamp된다.

File: `unity/OverthroneUnity/Assets/Scripts/Audio/FootstepSurface.cs`

### 3. PlayerNoiseEmitter surface-aware footsteps
`PlayerNoiseEmitter`는 기존 스프린트 발소리 조건을 유지하면서, 접지된 가장 가까운 바닥 collider의 `FootstepSurface`만 읽는다.

핵심 규칙:

- 걷기: 발소리/소음 없음
- 뛰기: 기존 interval에 맞춰 발소리와 `NoiseSystem.Emit`
- 공중: 표면 multiplier 적용 안 함
- 일반 바닥 위: 아래에 표면 collider가 있어도 multiplier 적용 안 함
- 표면 바닥 위: volume/pitch/noise radius multiplier 적용

File: `unity/OverthroneUnity/Assets/Scripts/Audio/PlayerNoiseEmitter.cs`

### 4. Bootstrap scene and prefab wiring
프로토타입 씬의 GardenGrass 바닥에 `FootstepSurface`를 붙였다.

- volume: `0.75x`
- pitch: `1.08x`
- noise radius: `0.8x`

`Player.prefab`과 `Prototype.unity`에는 `groundProbeOriginOffset=0.2`, `groundProbeDistance=0.35`가 저장되었다.

Files:

- `unity/OverthroneUnity/Assets/Editor/OverthroneUnityBootstrap.cs`
- `unity/OverthroneUnity/Assets/Prefabs/Player.prefab`
- `unity/OverthroneUnity/Assets/Scenes/Prototype.unity`

### 5. Tests
`PlayerNoiseEmitterFootstepTests`를 추가해 발소리/소음 계약을 EditMode에서 검증했다.

주요 테스트:

- 걷기는 footstep noise를 emit하지 않음
- 표면 정보가 없으면 1x multiplier 사용
- interval 안에서는 중복 footstep 없음
- 표면의 noise radius multiplier 적용
- 공중에서는 아래 표면을 resolve하지 않음
- 일반 바닥 위에서는 아래 표면을 resolve하지 않음
- multiplier clamp 동작

File: `unity/OverthroneUnity/Assets/Tests/EditMode/PlayerNoiseEmitterFootstepTests.cs`

## Verification
실행한 검증:

- Unity EditMode tests
  - Command: `/Applications/Unity/Hub/Editor/6000.0.78f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/wy/Github-repos/game1/unity/OverthroneUnity -runTests -testPlatform EditMode -testResults /Users/wy/Github-repos/game1/unity/OverthroneUnity/TestResults.xml -logFile -`
  - Result: `total=130 passed=130 failed=0`

- Bootstrap scene regeneration
  - Command: `/Applications/Unity/Hub/Editor/6000.0.78f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/wy/Github-repos/game1/unity/OverthroneUnity -executeMethod OverthroneUnityBootstrap.BootstrapPrototypeScene -logFile -`
  - Result: exit code 0

- Scene/prefab evidence
  - `FootstepSurface` guid appears once in `Prototype.unity`
  - `Player.prefab` and all scene player instances include `groundProbeDistance: 0.35`
  - GardenGrass includes `volumeMultiplier: 0.75`, `pitchMultiplier: 1.08`, `noiseRadiusMultiplier: 0.8`

- Static diff check
  - Command: `git diff --check`
  - Result: no whitespace errors

- Independent review
  - Tool: `codex exec` review on `PlayerNoiseEmitter.cs` and `PlayerNoiseEmitterFootstepTests.cs`
  - Final verdict: `Ready`
  - Findings: none

## Architecture Impact
이번 변경은 로컬 Unity 프로토타입의 청각 시스템에만 영향을 준다.

- `NoiseSystem` 이벤트 계약은 유지된다.
- AI hearing은 기존 `NoiseEvent.Radius`를 그대로 소비하므로, 표면별 stealth tuning이 가능해졌다.
- 생산 코드에는 per-footstep allocation을 피하기 위해 `RaycastNonAlloc`을 사용한다.
- 정식 오디오 품질은 아직 placeholder clip 기반이다. 표면별 실제 clip library, AudioMixer, occlusion/reverb는 후속 작업이다.

## Remaining Risks
- PlayMode/E2E에서 실제 플레이 중 표면 전환, 봇 반응, 장시간 소음 누적을 아직 검증하지 못했다.
- NavMesh 동적 회피, 수색/경계 상태 고도화는 여전히 남아 있다.
- 왕 단독 포획 허용 여부는 기획 정책 결정이 필요하다.
- Photon/Supabase/Steam 등 온라인/배포 인프라는 아직 로컬 이후 단계다.

## Files Changed
| File | Change |
|------|--------|
| `GDD.md` | 공식 자료 기반 UI 벤치마크 규칙과 현재 구현 상태 업데이트 |
| `unity/OverthroneUnity/Assets/Scripts/Audio/FootstepSurface.cs` | 표면별 발소리/소음 multiplier 컴포넌트 추가 |
| `unity/OverthroneUnity/Assets/Scripts/Audio/PlayerNoiseEmitter.cs` | 접지 바닥 surface lookup 및 multiplier 적용 |
| `unity/OverthroneUnity/Assets/Tests/EditMode/PlayerNoiseEmitterFootstepTests.cs` | 발소리 표면/접지/중복 emit 테스트 추가 |
| `unity/OverthroneUnity/Assets/Editor/OverthroneUnityBootstrap.cs` | GardenGrass 바닥에 `FootstepSurface` 부착 |
| `unity/OverthroneUnity/Assets/Prefabs/Player.prefab` | surface probe serialized field 저장 |
| `unity/OverthroneUnity/Assets/Scenes/Prototype.unity` | bootstrap 결과 반영 |
| `unity/OverthroneUnity/README.md` | footstep surface 동작 문서화 |

## Commit
feat(overthrone): add surface-aware footsteps

Co-Authored-By: Codex <noreply@openai.com>
