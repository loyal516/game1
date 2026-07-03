# Overthrone Unity Prototype

이 폴더는 브라우저 기반 Three.js 프로토타입을 Unity로 옮기기 위한 실제 Unity 프로젝트입니다.

## 목표 구조

- **Input System**: `Assets/Input/OverthroneControls.inputactions`
- **CharacterController 이동**: `Assets/Scripts/Movement/PlayerMotor.cs`
- **상태별 이동 제어**: `Assets/Scripts/States/PlayerStateController.cs`
- **Animator Blend Tree**: `Assets/Editor/OverthroneUnityBootstrap.cs`가 `Assets/Animations/Controllers/Player.controller` 생성
- **AudioSource 발소리**: `Assets/Scripts/Audio/PlayerNoiseEmitter.cs`
- **AI hearing**: `Assets/Scripts/AI/AIHearingSensor.cs`

## 생성/실행

Unity Editor 설치 후 프로젝트를 열거나 batchmode로 bootstrap을 실행합니다.

첫 실행 전 Unity Hub에서 다음 상태를 먼저 완료해야 합니다.

1. Unity Hub Software License Terms 확인 후 동의
2. Unity 계정 로그인
3. Personal/Pro 라이선스 활성화

```bash
/Applications/Unity/Hub/Editor/6000.0.78f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -quit \
  -projectPath /Users/wy/Github-repos/game1/unity/OverthroneUnity \
  -executeMethod OverthroneUnityBootstrap.BootstrapPrototypeScene \
  -logFile -
```

Editor에서 직접 실행할 때는 `Overthrone > Bootstrap Prototype Scene` 메뉴를 누릅니다.

EditMode 테스트는 다음처럼 실행합니다. 이 환경에서는 `-runTests` 실행 시 `-quit`을 붙이지 않아야 결과 XML이 정상 생성됩니다.

```bash
/Applications/Unity/Hub/Editor/6000.0.78f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -projectPath /Users/wy/Github-repos/game1/unity/OverthroneUnity \
  -runTests \
  -testPlatform EditMode \
  -testResults /Users/wy/Github-repos/game1/unity/OverthroneUnity/TestResults.xml \
  -logFile -
```

## 조작

| 입력 | 행동 |
| --- | --- |
| `WASD` / 방향키 | 이동 |
| 마우스 이동 | 시점 회전 |
| 마우스 클릭 | 커서 재잠금 |
| `Shift` | 달리기 |
| `Q` / `Shift` 더블탭 | 대시 |
| `Space` | 점프 |
| `Ctrl` | 앉기 |
| `R` | 슬라임 |
| `E` / 좌클릭 | 덮치기 |
| `F` 홀드 | 왕 상태에서 붙들린 적 2m 이내 최종 포획 진행 |
| `Esc` | 커서 잠금 해제 |

Gamepad 기본 바인딩은 왼쪽 스틱 이동, 오른쪽 스틱 시점, 왼쪽 스틱 클릭 달리기, West 대시, South 점프, East 앉기입니다.

시점 입력은 마우스와 게임패드를 분리해 처리합니다. 마우스는 픽셀 델타에 `mouseSensitivity`를 곱고, 게임패드 오른쪽 스틱은 `gamepadLookSensitivityDegreesPerSecond`와 `deltaTime`을 사용해 FPS와 무관한 초당 회전 속도로 보정합니다. 기본 right stick binding은 `ScaleVector2(x=6,y=6)`를 유지하고, 모터 기본값 `15`와 조합해 최대 약 90도/초 회전으로 시작합니다.

## Phase 1 HUD / 스태미나

`OverthroneUnityBootstrap`은 `Prototype` scene에 기본 HUD를 생성합니다.

- 중앙 크로스헤어
- 하단 스태미나 바
- 현재 상태/이동 상태/지상 여부/스태미나 텍스트
- 좌측 팀 상태 레일
- 중앙 포획 진행 링

`PlayerMotor`는 `maxStamina`, `currentStamina`, `NormalizedStamina`를 노출합니다. 스프린트 중에는 초당 10을 소모하고, 스프린트하지 않을 때는 정지 15/초, 이동 5/초, 앉기 10/초로 회복합니다. 스태미나가 0이면 스프린트 속도로 전환되지 않습니다. `Q` 또는 `Shift` 더블탭 대시는 스태미나 25를 소모하고 5초 쿨다운을 시작하며, 0.5초 동안 이동 방향 또는 전방으로 200% 가속합니다. 스태미나가 부족하거나 쿨다운 중이면 HUD 상태 텍스트에 `DASH STA` 또는 `DASH CD`가 표시됩니다.

`ThirdPersonCameraRig`는 목표 시점에서 기본 카메라 위치까지 `SphereCast`를 쏘고, 벽/오브젝트가 있으면 카메라를 장애물 앞쪽으로 당겨 플레이어 시야가 가려지지 않게 합니다. `collisionRadius`, `collisionBuffer`, `collisionMask`로 로컬 테스트맵에 맞춰 조절할 수 있습니다.

## Phase 2 로컬 점령 포인트

`Prototype` scene에는 Blue 3명/Red 3명의 로컬 3v3 roster와 `A/B/C` CapturePoint 3개가 삼각형으로 배치됩니다. 각 포인트는 기본 반경 5m trigger 안의 `LocalPlayerTeam` 인원을 Blue/Red로 집계하고, `CapturePointProgress`가 점령 게이지와 owner를 계산합니다.

점령 속도는 GDD 기준을 그대로 사용합니다.

| 상황 | 속도 |
| --- | --- |
| 1명 | 0.05/초 |
| 2명 | 0.08/초 |
| 3명 이상 | 0.12/초 |
| 양 팀 동시 | 0/초 |
| 점령 팀 부재 | 미완료 게이지 -0.03/초 |

`LocalRosterBuilder`는 테스트 가능한 기본 3v3 슬롯을 제공하고, `LocalMatchManager`는 씬의 CapturePoint 목록, 참가자, 팀별 owner count, 30초 승리 카운트다운, match duration/remaining을 노출하는 로컬 SSOT 기반입니다. `LocalMatchRules`는 2개 이상 점령 시 팀당 King 1명, 3명 이상 집결 시 Attacker, 집결 해제 후 5초 Attacker 유예를 계산합니다. King 후보는 최종 포획 수, 점령 기여도, tie-breaker 순으로 고릅니다. 세 포인트를 모두 점령하면 `LocalMatchPhase.VictoryCountdown`으로 전환되고, 상대 팀은 defender re-entry window 안에 점령을 끊어 카운트다운을 중단할 수 있습니다. 상대 팀 전원이 `Captured`가 되거나 한 팀 roster가 존재한 뒤 active/enabled 참가자가 0명이 되면 즉시 `LocalMatchPhase.Result`와 Winner가 확정됩니다. 시간 종료 시에는 active non-Captured 생존자 수, owned capture point 수 순서로 Winner를 고르고 둘 다 동률이면 Winner `None` draw로 종료합니다. HUD 우측 Objective 텍스트는 각 포인트의 owner, progress, capturing/contested 상태와 팀별 소유 수/승리 카운트다운, 현재 phase, defender re-entry 시간을 표시합니다. 정식 우측 점령 패널 MVP는 `A/B/C` row마다 point id, owner 색상, progress fill, state text, Blue/Red occupant count를 기본 `Text`/`Image`로 표시합니다. 로컬 핑 MVP는 `G`/게임패드 D-pad up 탭 입력으로 컨텍스트 핑을 생성하고, 적 Attacker/King 우선, 가까운 점령 포인트, 전방 주목 순서로 미니맵 마커와 짧은 `PING` 로그를 표시합니다. `G` 홀드 중에는 방사형 핑 휠 텍스트를 열고 이동 입력으로 Going/Defend/Capture/Help를 골라 로컬 응답 핑을 남깁니다. 미니맵은 적 King뿐 아니라 적 Attacker도 위협 마커로 노출합니다. `LocalMatchFlowPresenter`는 공수전환/중단/라운드 종료 배너와 화면 overlay를 표시하고, countdown 시작 시 defender 팀의 Free 플레이어를 re-entry spawn으로 이동시킨 뒤 짧은 Attacker timed state와 스태미나 회복을 부여합니다.

## Phase 3 로컬 포획 루프

`LocalCaptureSystem`은 로컬 플레이어의 `E`/좌클릭 덮치기와 `F` 홀드 최종 포획을 처리합니다. `PlayerCaptureAgent`는 Free/Holding/Held/Captured 상태를 갖고, 덮치기에 성공하면 붙든 쪽은 `Holding`, 대상은 `Held`가 됩니다. `TackleHitbox`가 있는 캐릭터는 전방 `OverlapCapsule` 물리 히트박스로 대상을 찾고, 히트박스 컴포넌트가 없는 레거시 경로에서만 기존 로컬 agent 목록 스캔으로 fallback합니다. 덮치기 시도는 성공/실패와 관계없이 스태미나 30과 2초 쿨다운을 적용하고, 실패 시 공격자에게 0.5초 `Holding` 경직을 적용합니다. 왕은 붙들린 적 2m 이내에서 `F`를 1.5초 유지하면 `Captured`로 전환할 수 있고, 같은 팀 Free 플레이어가 1.5m 이내에 닿으면 구출됩니다. 구출, Held 상태의 첫 `R` 슬라임 탈출, 또는 enemy Holding 대상에 대한 덮치기 interrupt는 붙든 자에게 1초 `Holding` 경직을 적용하며, 슬라임 탈출은 로컬 매치 기준 1회만 사용할 수 있습니다. 각 포획 이벤트는 `CaptureFeedbackSystem`으로 TackleHit/TackleMiss/Rescue/FinalCapture/HolderInterrupted/SlimeEscape 피드백을 발행하고, `CaptureFeedbackController`가 절차적 파티클과 오디오 원샷으로 로컬 VFX/SFX를 재생합니다. `LocalSpectatorCamera`는 로컬 플레이어가 `Captured`가 되면 카메라를 기본 플레이어 pivot에서 분리해 살아있는 같은 팀 후보를 따라가며, 후보가 없으면 포획된 자신을 fallback 대상으로 관전합니다. 관전 중에는 `Q`/`Tab` 입력으로 이전/다음 같은 팀 생존자 후보를 순환하고, HUD 상단의 관전 오버레이가 현재 대상, 입력 힌트, `LocalDeadChannel`의 같은 팀 Captured 전용 로그를 표시합니다. `LocalDeadChannel`은 살아있는 플레이어의 글쓰기를 막고, 최종 포획 성공 시 팀별 system join 메시지를 남깁니다.

## 로컬 데이터 mock

`LocalDataStore`는 Supabase로 옮길 테이블명을 기준으로 `profiles.csv`, `matches.csv`, `match_players.csv`, `telemetry_events.csv`를 생성합니다. 현재는 외부 SDK 없이 `SaveCsvDirectory(...)`로 지정한 로컬 폴더에 CSV를 저장하며, CSV 헤더는 GDD의 `profiles`/`matches`/`match_players` 스케치와 텔레메트리 이벤트 계약에 맞춰 둡니다.

`LocalBotController`는 로컬 3v3의 NPC 5명을 실제로 움직입니다. 봇은 `PlayerInputReader.SetManualInput(...)`으로 `PlayerMotor`에 이동 입력을 주입하므로 플레이어와 같은 이동/상태 프로필을 통과합니다. 기본 행동은 점령 포인트 이동, Attacker/King 상태에서 적 추격 및 덮치기, 붙들린 아군 구출 이동, King 상태에서 Held 적 최종 포획 보조, `AIHearingSensor`가 기억한 적 달리기 소음 위치 조사입니다. NavMesh가 있으면 목표까지 complete path를 계산해 다음 path corner를 향해 이동하고, NavMesh sample/path 계산이 실패하면 기존 direct target 이동으로 fallback합니다.

## 상태별 이동

| 상태 | 이동 | 달리기 | 소음 |
| --- | --- | --- | --- |
| Neutral | 가능 | 가능 | 걷기 무음, 달리기 소음 발생 |
| Attacker | 가능 | 가능 | 달리기 소음 발생 |
| King | 가능 | 가능 | 달리기 소음 크게 발생 |
| Held | 불가 | 불가 | 없음 |
| Captured | 불가 | 불가 | 없음 |
| Slime | 가능 | 불가 | 낮은 소음 |
| Holding | 불가 | 불가 | 없음 |

걷기는 AI에게 들리지 않고, 달리기 상태에서만 `PlayerNoiseEmitter`가 발소리를 재생하고 `NoiseSystem.Emit(...)`을 호출합니다. 상태별 `noiseRadius` 값으로 AI 감지 반경을 조절하고, 로컬 봇은 같은 GameObject의 `AIHearingSensor`가 기억한 적 팀 소음만 capture point보다 우선 조사합니다.

`PlayerStateController`는 입력/게임플레이 이벤트를 `PlayerMotor.SetState(...)`로 연결합니다. 현재 기본 연결은 `R` 입력 시 스태미나 50을 소모하고 15초 쿨다운이 시작되는 Slime 임시 상태입니다. 스태미나가 부족하거나 쿨다운 중이면 Slime은 발동하지 않고, HUD 상태 텍스트에 `SLIME STA` 또는 `SLIME CD`가 표시됩니다. Slime 상태에서는 `CharacterController` 높이/반지름이 줄어들고, placeholder 비주얼은 납작하게 넓어지며, 이동 입력이 끊겼을 때 `slimeGroundFrictionMultiplier`로 감속이 낮아져 미끄러지는 느낌을 냅니다. `LocalMatchManager`는 점령 상황과 팀 내 King 우선순위에 따라 Neutral/Attacker/King persistent state를 주입하고, `LocalCaptureSystem`은 Holding/Held/Captured 상태를 주입합니다. Slime 같은 임시 상태가 끝나면 기존 persistent state로 복귀합니다.

## 현재 한계

- 실제 캐릭터 모델과 모션 클립은 아직 외부 에셋을 임포트하지 않았습니다.
- Bootstrap은 placeholder `Idle/Walk/Run` clip과 Blend Tree를 생성합니다.
- 로컬 봇은 NavMesh path corner 추종 + direct fallback, 점령/추격/구출/포획/적 소음 조사 MVP까지이며, dynamic obstacle avoidance, 경계/수색 상태 고도화, PlayMode 장시간 검증, 바닥 재질별 소리는 다음 단계입니다.
- 앉기는 입력/HUD/저속 이동까지 연결되어 있지만, 전용 애니메이션과 카메라 높이 전환은 아직 없습니다.
- 슬라임 1회 탈출은 현재 `PlayerCaptureAgent` 인스턴스 수명 기준입니다. 한 씬에서 여러 라운드를 재시작하는 구조가 생기면 탈출권 reset hook이 필요합니다.
- 슬라임은 로컬 placeholder 히트박스 축소/감속/변형 비주얼까지이며, 정식 shader/softbody VFX와 좁은 통로 레벨 검증은 다음 단계입니다.
- 관전은 자동 대상 선택, `Q`/`Tab` 후보 순환 입력, 관전 전용 HUD overlay, 로컬 같은 팀 dead channel 로그까지입니다. 실제 텍스트 입력 UI, 음성/온라인 채팅, moderation은 아직 없습니다.
- 팀 상태 레일/포획 진행 링/붙들림·구출 링/미니맵/우측 점령 패널/핑 마커/방사형 핑 휠은 기본 `Text`/`Image` 기반 로컬 HUD입니다. 미니맵은 자신, 아군, 적 왕, 적 공격자, 점령 포인트, 현재 로컬 핑을 표시합니다. 아이콘 아트, 핑 응답 상호작용 polish, 온라인 RPC 동기화는 아직 없습니다.
- 로컬 데이터 mock은 CSV writer 계약까지이며, Supabase Auth/PostgreSQL/Edge Function 업로드는 아직 없습니다.
- 포획 VFX/SFX는 절차적 placeholder 피드백까지이며, 정식 아트/오디오 에셋과 믹싱은 아직 없습니다.
- 공수전환은 로컬 phase/result/defender re-entry window, 배너/overlay, defender re-entry spawn/임시 Attacker 버프까지입니다. 정식 카메라 컷인, 사운드, 아트 polish, 실제 네트워크 권위화는 다음 단계입니다.
