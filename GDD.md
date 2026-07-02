# Game Design Document (GDD)
# 프로젝트명: [가제] Overthrone

**버전:** 2.0
**최종 수정:** 2026-07-03
**상태:** Pre-Production
**문서 소유자:** -

---

## 목차

1. [게임 개요](#1-게임-개요)
2. [핵심 컨셉](#2-핵심-컨셉)
3. [게임플레이 메카닉](#3-게임플레이-메카닉)
4. [플레이어 시스템](#4-플레이어-시스템)
5. [점령 시스템](#5-점령-시스템)
6. [포획 시스템](#6-포획-시스템)
7. [스킬 시스템](#7-스킬-시스템)
8. [맵 & 레벨 디자인](#8-맵--레벨-디자인)
9. [UI/UX 디자인](#9-uiux-디자인)
10. [비주얼 & 아트 스타일](#10-비주얼--아트-스타일)
11. [사운드 디자인](#11-사운드-디자인)
12. [밸런싱 가이드](#12-밸런싱-가이드)
13. [네트워킹 & 멀티플레이](#13-네트워킹--멀티플레이)
14. [기술 사양](#14-기술-사양)
15. [백엔드 & 데이터](#15-백엔드--데이터)
16. [안티치트 & 보안](#16-안티치트--보안)
17. [텔레메트리 & 분석](#17-텔레메트리--분석)
18. [접근성](#18-접근성)
19. [개발 로드맵](#19-개발-로드맵)
20. [라이브 옵스 & 시즌](#20-라이브-옵스--시즌)
21. [수익화 전략](#21-수익화-전략)
22. [리스크 관리](#22-리스크-관리)
23. [부록](#23-부록)

---

## 1. 게임 개요

### 1.1 한 줄 설명 (High Concept)
> **"왕좌를 쥔 자만이 심판할 수 있다"** — 점령과 집결로 공수가 실시간 전환되는 비대칭 잠입/추격 게임

### 1.2 장르
- **메인:** 비대칭 멀티플레이어 PvP
- **서브:** 잠입(Stealth) + 추격(Chase) + 점령(Capture)
- **시점:** 3인칭 추격 카메라 기본 (1인칭 옵션 검토)
- **세션 길이:** 5–12분 (평균 8분)

### 1.3 타겟 플랫폼

| 플랫폼 | 우선순위 | 출시 시기 | 입력 |
|--------|----------|----------|------|
| PC (Steam) | 1순위 | 초기 출시 | 키보드+마우스, 게임패드 |
| 모바일 (iOS/Android) | 2순위 | PC 안정화 후 | 가상 스틱, 자동조준 보조 |
| 콘솔 (Switch/PS5/Xbox) | 3순위 | 모바일 이후 | 게임패드, 자이로 조준 |

### 1.4 타겟 유저

| 유저 타입 | 특징 | 어필 포인트 |
|-----------|------|------------|
| **코어 게이머** | 경쟁적, 숙련도 중시 | 지형 마스터리, 높은 스킬캡, 랭크 |
| **캐주얼 게이머** | 짧은 플레이, 파티 게임 | 빠른 매치(5분), 간단한 조작 |
| **스트리머** | 방송 콘텐츠 | 극적인 역전, 추격 장면, 관전 모드 |
| **소셜 게이머** | 친구와 함께 | 팀 플레이, VOIP, 친구 초대 |

### 1.5 경쟁작 분석

| 게임 | 공통점 | 우리의 차별점 |
|------|--------|-------------|
| **Dead by Daylight** | 비대칭, 추격 | 역할 고정 vs **우리는 실시간 역할 전환** |
| **Among Us** | 긴장감, 사회적 | 물리적 추격 없음 vs **우리는 액션 기반** |
| **Propnight** | 숨바꼭질, 캐주얼 | 변신 특화 vs **우리는 점령 중심** |
| **Midnight Ghost Hunt** | 팀 기반, 역할 전환 | 시간 기반 전환 vs **우리는 점령 기반 전환** |
| **The Finals** | 환경 활용, 시즌제 | 슈팅 vs **우리는 잠입/추격** |

### 1.6 USP (Unique Selling Points)

1. **실시간 공수전환** — 쫓기던 사람이 순식간에 추격자가 됨
2. **왕 = 상태** — 고정 캐릭터가 아닌 획득/박탈되는 권한
3. **붙들기 + 포획 분리** — 협동 강제 메카닉
4. **지형 마스터리** — 에임이 아닌 공간 이해가 실력
5. **슬라임 스킬** — 독특한 이동/탈출 메카닉
6. **5분 블리츠 모드** — 모바일 친화적 짧은 세션

---

## 2. 핵심 컨셉

### 2.1 게임 철학

```
"이 게임은 누가 더 세냐의 게임이 아니라,
 지금 이 공간에서 누가 유리하냐의 게임이다."
```

### 2.2 핵심 설계 원칙

| 원칙 | 설명 | 구현 방향 |
|------|------|----------|
| **동등한 출발** | 모든 플레이어 동일 능력 | 클래스/레벨 없음, 코스메틱만 |
| **상황적 강약** | 위치·상태가 힘을 결정 | 점령 기반 상태 변화 |
| **역전 가능성** | 항상 뒤집을 수 있음 | 구출, 점령 탈환, 슬라임 탈출 |
| **공포의 집중** | 왕에게 위협 집중 | 왕만 최종 포획 권한 |
| **협동 필수** | 혼자선 한계 | 붙들기 + 왕 포획 분리 |
| **No P2W** | 결제는 외형뿐 | 스탯 영향 결제 0 |

### 2.3 감정 루프 (Emotion Loop)

```
[은신] 넓은 시야로 루트 탐색, 심장 쿵쾅
    ↓
[발각] "들켰다!" 공포 폭발
    ↓
[추격] 전력 질주, 아드레날린
    ↓
[슬라임] 스킬 발동, 탈출 시도
    ↓
[성공/실패] 안도 또는 좌절
    ↓
[재정비] 팀 합류, 전략 수정
    ↓
[점령] 역전 기회, 희망
    ↓
[공수전환] "이제 내 차례" 복수심
    ↓
[반복]
```

### 2.4 핵심 경험 목표

| 순간 | 플레이어 감정 | 디자인 목표 |
|------|--------------|------------|
| 왕에게 쫓길 때 | 극도의 공포 | 심박수 상승, 손에 땀 |
| 붙들린 동료 구출 | 영웅적 쾌감 | "내가 살렸다!" |
| 점령 성공 후 공수전환 | 통쾌함 | "이제 복수다" |
| 슬라임으로 극적 탈출 | 짜릿함 | "ㅋㅋㅋ 못 잡지?" |
| 왕이 되어 포획 성공 | 지배감 | "내가 왕이다" |

---

## 3. 게임플레이 메카닉

### 3.1 매치 구조

```
[로비] 플레이어 대기 (3~10명)
    ↓
[매치메이킹] MMR 기반 페어링
    ↓
[캐릭터 선택] 코스메틱 확정 (15초)
    ↓
[매치 시작] 초기 왕 지정 (랜덤 + 가중치)
    ↓
[게임 진행] 점령 ↔ 추격 ↔ 포획 루프
    ↓
[승리 조건 달성]
    ↓
[결과 화면] 통계, MVP, 보상
    ↓
[다음 매치 또는 로비]
```

### 3.2 승리 조건

| 조건 | 설명 | 달성 주체 |
|------|------|----------|
| **전원 포획** | 상대 팀 전원 포획 | 공격 팀 |
| **완전 점령** | 모든 점령 포인트 장악 30초 유지 | 점령 팀 |
| **시간 종료 (생존)** | 제한 시간 내 1명 이상 생존 | 도주 팀 |
| **포기 (Forfeit)** | 한 팀 전원 이탈 | 잔존 팀 |

### 3.3 게임 모드

#### 3.3.1 클래식 모드 (기본)
- 3v3 ~ 5v5
- 점령 포인트 3개
- 제한 시간 10분
- 공수전환 무제한
- 랭크 매칭 지원

#### 3.3.2 블리츠 모드
- 3v3 고정
- 점령 포인트 1개
- 제한 시간 5분
- 모바일 친화적

#### 3.3.3 카오스 모드 (FFA)
- 개인전 6~10명
- 모두가 적
- 점령 시 개인 왕 상태 획득
- 마지막 1인 승리

#### 3.3.4 커스텀 모드
- 친구 초대, 비공개 방
- 룰 변경 가능 (시간, 인원, 점령 수)
- MMR 영향 없음

### 3.4 라운드 흐름 상세

```
[0:00] 게임 시작
  - 양 팀 스폰 (대각선 위치)
  - 초기 왕 표시 (양 팀 1명씩)
  - 10초 준비 시간 (이동 불가, 관찰 가능)

[0:10] 게임 본격 시작
  - 이동 가능
  - 점령 포인트 활성화

[진행 중]
  - 점령 → 상태 변화 → 추격 → 포획/구출 반복
  - 매 2분마다 미니맵 펄스 (모든 적 위치 0.5초 노출)

[종료 30초 전]
  - 화면 가장자리 붉은 비네팅
  - BGM 고조

[종료 조건 충족]
  - 슬로우 모션 연출 (3초)
  - 승리 팀 하이라이트

[결과]
  - MVP 선정 (포획수, 구출수, 점령기여도 가중)
  - 개인 통계
  - 보상 지급 (XP, 시즌패스 진행)
```

---

## 4. 플레이어 시스템

### 4.1 기본 스탯

| 스탯 | 기본값 | 설명 |
|------|--------|------|
| 이동속도 | 5 m/s | 기본 달리기 |
| 대시속도 | 8 m/s | 스프린트 |
| 점프력 | 2m | 수직 점프 |
| 체력 | 없음 | 체력 시스템 없음 (포획만 존재) |
| 스태미나 | 100 | 대시/스킬에 사용 |

### 4.2 상태 시스템 (State System)

#### 4.2.1 상태 종류

```
┌─────────────────────────────────────────────┐
│                  플레이어 상태               │
├─────────────┬─────────────┬─────────────────┤
│   중립      │   공격자    │      왕         │
│  (Neutral)  │ (Attacker)  │    (King)       │
├─────────────┼─────────────┼─────────────────┤
│ - 기본 상태 │ - 붙들기 ○  │ - 붙들기 ○     │
│ - 붙들기 ✕  │ - 포획 ✕    │ - 포획 ○       │
│ - 포획 ✕    │ - 위치 노출 │ - 강화 스탯    │
│ - 은신 용이 │             │ - 특수 스킬    │
└─────────────┴─────────────┴─────────────────┘
```

#### 4.2.2 중립 상태 (Neutral)

| 항목 | 내용 |
|------|------|
| **획득 조건** | 기본 상태, 집결 해제 시 |
| **특징** | 은신에 유리, 직접 교전 불리 |
| **가능 행동** | 이동, 점령 참여, 구출 |
| **불가 행동** | 붙들기, 포획 |
| **UI 표시** | 없음 (기본) |

#### 4.2.3 공격자 상태 (Attacker)

| 항목 | 내용 |
|------|------|
| **획득 조건** | 점령 포인트에 3명 이상 집결 |
| **해제 조건** | 집결 해제 (포인트 이탈 or 인원 감소) |
| **유예 시간** | 해제 조건 충족 후 5초 (도주 기회) |
| **특징** | 붙들기 가능, 위치 노출 증가 |
| **가능 행동** | 이동, 덮치기(붙들기), 점령 |
| **불가 행동** | 최종 포획 |
| **UI 표시** | 주황색 아이콘, 머리 위 표식 |

#### 4.2.4 왕 상태 (King)

| 항목 | 내용 |
|------|------|
| **획득 조건** | 게임 시작 시 지정 / 2개 이상 점령 시 팀 1명 자동 승계 |
| **박탈 조건** | 점령 포인트 1개 이하로 감소 |
| **승계 우선순위** | 최근 포획수 → 점령기여도 → 랜덤 |
| **스탯 변화** | 이동속도 +10%, 덮치기 범위 +20% |
| **가능 행동** | 모든 행동 + 최종 포획 |
| **특수 스킬** | 왕의 포효 (범위 슬로우) |
| **UI 표시** | 금색 왕관 아이콘, 항상 미니맵 표시 |

#### 4.2.5 상태 전환 다이어그램

```
                    ┌──────────┐
                    │  중립    │
                    └────┬─────┘
                         │
         ┌───────────────┼───────────────┐
         │ 3명+ 집결     │               │ 집결 해제 +5초
         ▼               │               ▼
    ┌──────────┐         │          ┌──────────┐
    │  공격자  │◄────────┘          │  중립    │
    └────┬─────┘                    └──────────┘
         │
         │ 팀 2개+ 점령 & 승계 우선순위 1위
         ▼
    ┌──────────┐
    │    왕    │
    └────┬─────┘
         │
         │ 점령 포인트 1개 이하
         ▼
    ┌──────────┐
    │  공격자  │ (또는 중립)
    └──────────┘
```

### 4.3 피격 상태 (Debuff States)

#### 4.3.1 붙들림 상태 (Held)

| 항목 | 내용 |
|------|------|
| **발생 조건** | 공격자/왕에게 덮치기 당함 |
| **효과** | 그 자리에 고정, 이동 불가, 시점 회전만 가능 |
| **해제 조건** | 동료 구출, 슬라임 스킬 탈출, 붙드는 자 피격 |
| **지속 시간** | 무제한 (해제까지) |
| **시각 효과** | 속박 이펙트, 캐릭터 발버둥 |
| **사운드** | 고통스러운 신음 (전 맵 청취 가능) |

#### 4.3.2 포획됨 상태 (Captured)

| 항목 | 내용 |
|------|------|
| **발생 조건** | 왕에게 최종 포획 당함 |
| **효과** | 게임에서 제외 (관전 모드) |
| **해제 조건** | 라운드 종료 |
| **시각 효과** | 감옥/구슬에 갇힘 |
| **관전** | 아군 자유 시점 관전 가능 |
| **채팅** | 죽은 자 채널 (아군에게만) |

### 4.4 이동 시스템

#### 4.4.1 기본 이동

| 조작 | 행동 | 속도 |
|------|------|------|
| WASD | 걷기/달리기 | 5 m/s |
| Shift + WASD | 스프린트 | 8 m/s |
| Space | 점프 | 2m 높이 |
| Ctrl | 앉기 | 2.5 m/s, 소음 -50% |
| Mouse | 시점 | 사용자 감도 |

#### 4.4.2 스태미나 시스템

```
최대 스태미나: 100

소모:
- 스프린트: 10/초
- 덮치기: 30
- 슬라임 스킬: 50
- 대시: 25

회복:
- 정지 시: 15/초
- 걷기 시: 5/초
- 앉기 시: 10/초
- 스프린트 시: 회복 없음
```

#### 4.4.3 상태별 이동 정책

| 상태 | 이동 | 스프린트 | 이동 배율 | 비고 |
|------|------|----------|-----------|------|
| **중립** | 가능 | 가능 | 1.00x | 기본 이동 |
| **공격자** | 가능 | 가능 | 1.05x | 추격 압박을 위한 소폭 보너스 |
| **왕** | 가능 | 가능 | 1.10x | 최종 포획 권한과 함께 기동성 보너스 |
| **붙들림** | 불가 | 불가 | 0.00x | 시점 회전과 슬라임 탈출만 허용 |
| **포획됨** | 불가 | 불가 | 0.00x | 관전 전환 |
| **붙든 자** | 불가 | 불가 | 0.00x | 포획 진행 또는 제3자 개입 대기 |
| **슬라임** | 가능 | 불가/제한 | 1.40x | 탈출/가속 특수 상태 |

Unity 구현 시 위 표는 `MovementProfile` 또는 `ScriptableObject`로 분리하고, 플레이어 상태 머신은 현재 상태에 해당하는 이동 프로필만 참조한다.

### 4.5 시점 시스템

#### 4.5.1 3인칭 추격 카메라 (기본)

| 항목 | 내용 |
|------|------|
| **사용 시점** | 기본 플레이 |
| **카메라 위치** | 캐릭터 후방 상단 |
| **시야각 (FOV)** | 76° 기본 (설정 가능 70°~100°) |
| **장점** | 넓은 시야, 캐릭터/스킨 가시성, 지형 파악 |
| **단점** | 1인칭 대비 몰입감 감소 |

#### 4.5.2 1인칭 (옵션 검토)

| 항목 | 내용 |
|------|------|
| **사용 시점** | 하드코어 모드/관전 옵션 검토 |
| **카메라 위치** | 캐릭터 눈높이 |
| **전환** | 설정에서 선택 |
| **장점** | 몰입감, 긴장감 |
| **단점** | 제한된 시야, 캐릭터 외형 노출 감소 |

---

## 5. 점령 시스템

### 5.1 점령 포인트 기본

| 항목 | 내용 |
|------|------|
| **개수** | 맵당 3개 (블리츠 1개) |
| **배치** | 삼각형 형태, 균등 거리 |
| **크기** | 반경 5m 원형 |
| **시각화** | 바닥 발광, 기둥 표시, 팀 색상 |

### 5.2 점령 메카닉

#### 5.2.1 점령 진행

```
[점령 포인트 진입]
    ↓
[점령 게이지 시작] ────────────────┐
    ↓                              │ 이탈 시
[게이지 100% 도달]                 │ 게이지 감소
    ↓                              │
[점령 완료]◄───────────────────────┘
    ↓
[소유권 변경]
    ↓
[공수전환 체크]
```

#### 5.2.2 점령 게이지

| 상황 | 게이지 변화 | 속도 |
|------|------------|------|
| 1명 점령 | 증가 | 5%/초 |
| 2명 점령 | 증가 | 8%/초 |
| 3명+ 점령 | 증가 | 12%/초 (캡) |
| 양 팀 동시 | 정지 | 0%/초 |
| 점령 팀 부재 | 감소 | 3%/초 |

#### 5.2.3 점령 효과

| 점령 개수 | 효과 |
|-----------|------|
| 0개 | 모든 아군 중립 상태 |
| 1개 | 해당 포인트 근처 공격자 전환 가능 |
| 2개 | 왕 유지 가능, 맵 시야 +15m |
| 3개 (전체) | 30초 카운트다운 → 승리 |

### 5.3 공수전환 시스템

#### 5.3.1 전환 조건

| 조건 | 결과 |
|------|------|
| 점령 포인트 뺏김 | 해당 팀 공격→도주 |
| 점령 포인트 획득 | 해당 팀 도주→공격 |
| 왕 박탈 | 해당 팀 공격력 약화 |

#### 5.3.2 전환 연출

```
[점령 완료 순간]
    ↓
[화면 플래시 (0.5초)]
    ↓
[슬로우 모션 (1초)]
    ↓
["공수전환!" 텍스트]
    ↓
[BGM 변경]
    ↓
[정상 속도 복귀]
```

### 5.4 점령 포인트 종류 (맵별 변형)

| 종류 | 특징 | 전략적 의미 |
|------|------|------------|
| **일반** | 기본 점령 포인트 | 균형 |
| **고지대** | 높은 곳에 위치 | 방어 유리, 시야 확보 |
| **지하** | 숨겨진 위치 | 기습 점령 가능 |
| **이동형** | 90초마다 위치 이동 | 예측 필요 (시즌2 도입) |

---

## 6. 포획 시스템

### 6.1 포획 단계

```
[도주자 발견]
    ↓
[추격]
    ↓
[덮치기 (공격자)] ─────── 성공 ──────┐
    ↓ 실패                           │
[재추격]                             ▼
                              [붙들림 상태]
                                     │
                    ┌────────────────┼────────────────┐
                    │                │                │
                    ▼                ▼                ▼
              [왕 도착]        [동료 구출]      [슬라임 탈출]
                    │                │                │
                    ▼                ▼                ▼
              [최종 포획]      [붙들림 해제]    [붙들림 해제]
                    │
                    ▼
              [게임 제외]
```

### 6.2 덮치기 (Tackle)

| 항목 | 내용 |
|------|------|
| **사용 조건** | 공격자/왕 상태 |
| **조작** | 마우스 좌클릭 또는 E키 |
| **범위** | 전방 3m (왕은 3.6m) |
| **각도** | 정면 60° |
| **스태미나 소모** | 30 |
| **쿨다운** | 2초 |
| **성공 시** | 대상 붙들림 상태 |
| **실패 시** | 0.5초 경직 |
| **네트워크** | 클라이언트 예측 + 서버 검증 |

### 6.3 붙들기 (Hold)

| 항목 | 내용 |
|------|------|
| **상태** | 대상이 그 자리에 고정됨 |
| **붙드는 자** | 덮치기 성공한 공격자/왕 |
| **이동** | 양쪽 모두 이동 불가 |
| **지속** | 해제 조건 충족까지 무제한 |
| **취약점** | 붙드는 자도 이동 불가 → 제3자에 취약 |

#### 6.3.1 붙들기 해제 조건

| 조건 | 결과 | 비고 |
|------|------|------|
| 동료가 터치 | 붙들림 해제 | 구출 성공 |
| 슬라임 스킬 사용 | 붙들림 해제 | 자가 탈출, 1회/매치 제한 |
| 붙드는 자가 피격 | 붙들림 해제 + 붙든 자 1초 경직 | 제3자 덮치기 개입 |
| 왕이 포획 실행 | 포획됨 상태로 전환 | 게임 제외 |

### 6.4 최종 포획 (Capture)

| 항목 | 내용 |
|------|------|
| **사용 조건** | 왕 상태 + 붙들린 대상 2m 이내 |
| **조작** | F키 (길게 누르기 1.5초) |
| **캔슬** | 피격, 키 해제, 대상 탈출 |
| **성공 시** | 대상 게임에서 제외 |

#### 6.4.1 포획 연출

```
[F키 홀드 시작]
    ↓
[원형 게이지 표시]
    ↓
[1.5초 후 완료]
    ↓
[대상 "감금" 이펙트]
    ↓
[대상 관전 모드 전환]
```

### 6.5 구출 (Rescue)

| 항목 | 내용 |
|------|------|
| **사용 조건** | 도주자 상태 |
| **조작** | 붙들린 동료에게 접촉 |
| **범위** | 1.5m 이내 |
| **시간** | 즉시 (터치 순간) |
| **효과** | 붙들림 해제, 붙드는 자 1초 경직 |
| **보상** | XP +50, MVP 가중치 |

---

## 7. 스킬 시스템

### 7.1 공용 스킬

#### 7.1.1 대시 (Dash)

| 항목 | 내용 |
|------|------|
| **조작** | Shift 더블탭 또는 Q키 |
| **효과** | 순간 이동속도 200% (0.5초) |
| **스태미나** | 25 소모 |
| **쿨다운** | 5초 |
| **용도** | 추격/도주 시 순간 가속 |

### 7.2 슬라임 스킬 (Slime Mode)

#### 7.2.1 기본 정보

| 항목 | 내용 |
|------|------|
| **조작** | R키 |
| **지속시간** | 3초 |
| **스태미나** | 50 소모 |
| **쿨다운** | 15초 |
| **시점** | 3인칭으로 전환 |
| **탈출 사용 제한** | 매치당 1회 (밸런스) |

#### 7.2.2 효과 상세

| 효과 | 수치 | 설명 |
|------|------|------|
| 이동속도 | +40% | 7 m/s → 9.8 m/s |
| 마찰 계수 | -70% | 미끄러지듯 이동 |
| 히트박스 | -30% | 좁은 틈 통과 가능 |
| 경사 한계 | +20° | 더 가파른 경사 등반 |
| 붙들림 탈출 | 가능 | 붙들림 상태에서 발동 시 탈출 (1회/매치) |
| 피격 무적 | 없음 | 덮치기 받을 수 있음 |

#### 7.2.3 슬라임 활용 예시

| 상황 | 활용법 |
|------|--------|
| 좁은 틈새 | 히트박스 감소로 통과 |
| 경사면 | 미끄러지며 빠른 하강 |
| 코너 | 관성으로 속도 유지하며 회전 |
| 붙들림 | 발동으로 즉시 탈출 |
| 추격당할 때 | 시야 확보 (3인칭) + 속도 증가 |

### 7.3 왕 전용 스킬

#### 7.3.1 왕의 포효 (King's Roar)

| 항목 | 내용 |
|------|------|
| **조작** | V키 |
| **범위** | 반경 8m |
| **효과** | 범위 내 적 30% 슬로우 (3초) |
| **쿨다운** | 30초 |
| **용도** | 도주자 추격, 팀 전투 지원 |

#### 7.3.2 왕의 시야 (King's Vision)

| 항목 | 내용 |
|------|------|
| **조작** | 패시브 (자동) |
| **효과** | 점령 포인트 주변 적 위치 표시 |
| **범위** | 점령 포인트 반경 15m |
| **표시** | 미니맵에 붉은 점, 2초마다 갱신 |

---

## 8. 맵 & 레벨 디자인

### 8.1 맵 디자인 철학

```
"처음 보면 막힌 곳, 숙련되면 핵심 루트"
```

| 원칙 | 설명 |
|------|------|
| **다층 구조** | 수직적 플레이 장려 |
| **숨겨진 통로** | 숙련자 보상 |
| **시야 차단물** | 긴장감 유지 |
| **점령 포인트 균형** | 어느 쪽도 유리하지 않음 |
| **루프 동선** | 막다른 길 최소화, 회피 경로 보장 |

### 8.2 맵 구성 요소

#### 8.2.1 지형 요소

| 요소 | 용도 | 예시 |
|------|------|------|
| **벽** | 시야 차단, 이동 제한 | 건물 벽, 컨테이너 |
| **경사** | 이동 속도 변화 | 계단, 슬로프 |
| **좁은 틈** | 슬라임 전용 통로 | 환기구, 하수구 |
| **높은 곳** | 시야 확보, 낙하 | 옥상, 발코니 |
| **장애물** | 은신, 이동 방해 | 상자, 차량 |

#### 8.2.2 특수 지형

| 요소 | 효과 | 위치 |
|------|------|------|
| **미끄러운 바닥** | 마찰 감소 | 젖은 바닥, 얼음 |
| **소음 바닥** | 발소리 증가 | 유리, 금속판 |
| **어두운 구역** | 시야 감소 | 지하, 창고 |
| **점프대** | 높이 점프 | 트램펄린, 스프링 |

### 8.3 맵 목록 (런칭 기준)

| 맵 이름 | 인원 | 크기 | 층수 | 특징 |
|---------|------|------|------|------|
| **폐공장** (Abandoned Factory) | 3v3~5v5 | 80×60m | 2층 | 컨베이어 벨트 (이동 장애물) |
| **항구** (Harbor) | 4v4~5v5 | 100×80m | 3층 | 물에 빠지면 리스폰 (5초 페널티) |
| **학교** (School) | 3v3~4v4 | 90×70m | 3층 | 교실 문 개폐, 창문 탈출 |

### 8.4 맵 밸런스 체크리스트

| 항목 | 기준 |
|------|------|
| 스폰→점령 거리 | 양 팀 동일 (±5%) |
| 점령 간 거리 | 균등 삼각형 |
| 은신 포인트 | 최소 10개/맵 |
| 슬라임 통로 | 최소 5개/맵 |
| 고지대 | 각 점령 포인트당 1개 |
| 막다른 길 | 0개 (모든 공간 루프 가능) |

---

## 9. UI/UX 디자인

### 9.1 HUD (인게임)

```
┌─────────────────────────────────────────────────────────┐
│ [팀 상태]                              [미니맵]        │
│ ● ● ● ○ ○                              ┌─────┐         │
│ 아군 생존                              │  N  │         │
│                                        │W ● E│         │
│                                        │  S  │         │
│                                        └─────┘         │
│                                                         │
│                    [크로스헤어]                         │
│                         +                               │
│                                                         │
│ [상태 아이콘]        [스태미나 바]                      │
│ ⚔️ 공격자           ████████░░ 80%                      │
│                                                         │
│ [스킬]                                 [점령 현황]      │
│ [Q] 대시 (준비됨)                      A: ■■■ 우리팀   │
│ [R] 슬라임 (12초)                      B: □□□ 경쟁중   │
│                                        C: ▣▣▣ 적팀     │
└─────────────────────────────────────────────────────────┘
```

### 9.2 UI 요소 상세

#### 9.2.1 미니맵

| 표시 | 아이콘 | 조건 |
|------|--------|------|
| 자신 | 흰색 삼각형 | 항상 |
| 아군 | 파란 점 | 항상 |
| 적 왕 | 붉은 왕관 | 항상 (왕은 숨길 수 없음) |
| 적 공격자 | 붉은 점 | 노출 시 |
| 점령 포인트 | 색상 원 | 항상 |

#### 9.2.2 상태 표시

| 상태 | 색상 | 아이콘 |
|------|------|--------|
| 중립 | 흰색 | 없음 |
| 공격자 | 주황 | 칼 |
| 왕 | 금색 | 왕관 |
| 붙들림 | 빨강 | 사슬 |
| 슬라임 | 초록 | 물방울 |

### 9.3 메뉴 구조

```
[메인 메뉴]
    ├── 빠른 매치
    │   ├── 클래식 (3v3~5v5)
    │   ├── 블리츠 (3v3)
    │   └── 카오스 (FFA)
    ├── 랭크 매치
    │   ├── 솔로/듀오 큐
    │   └── 풀팀 큐
    ├── 사용자 지정 게임
    │   ├── 방 만들기
    │   └── 방 찾기
    ├── 프로필
    │   ├── 통계
    │   ├── 업적
    │   └── 스킨/꾸미기
    ├── 시즌패스
    ├── 상점
    ├── 설정
    │   ├── 그래픽
    │   ├── 오디오
    │   ├── 조작
    │   ├── 접근성
    │   └── 계정
    └── 종료
```

### 9.4 알림 시스템

| 이벤트 | 표시 방법 | 사운드 |
|--------|----------|--------|
| 아군 붙들림 | 화면 가장자리 빨간 플래시 | 경고음 |
| 점령 완료 | 중앙 대형 텍스트 | 팡파르 |
| 공수전환 | 화면 전환 효과 | 드럼 |
| 아군 포획됨 | 킬 피드 | 비명 |
| 왕 박탈 | "왕좌 상실!" 텍스트 | 깨지는 소리 |

### 9.5 핑(Ping) 시스템

| 핑 종류 | 단축키 | 의미 |
|---------|--------|------|
| 컨텍스트 | G 탭 | 적 발견 > 점령 포인트 > 전방 주목 우선순위로 자동 핑 |
| 방사형 핑 휠 | G 홀드 + 이동 입력 | W: 간다, A: 방어, D: 점령, S: 도움 |
| 팀 응답 | 핑 선택/응답 | "간다/방어/도움/점령" 의도를 짧은 로그와 마커로 공유 |

### 9.6 UI 벤치마크 리서치

**조사 기준일:** 2026-07-03
**목표:** Overthrone의 UI는 "추격 중에도 0.5초 안에 읽히는 상태 정보"와 "보이스 없이도 협동 가능한 핑/상태 표시"를 우선한다.

| 참고 게임 | UI에서 배울 점 | Overthrone 적용 |
|-----------|----------------|-----------------|
| [Dead by Daylight](https://deadbydaylight.fandom.com/wiki/Status_HUD) | 생존자별 상태, 운반/갈고리/사망 같은 팀 상태를 한 영역에 고정해 보여준다. 활동 HUD는 진행 중 행동과 일부 진행률도 아이콘 주변 링으로 보여준다. | 좌측 팀 상태 레일에 `중립/공격자/왕/붙들림/포획됨`을 표시하고, 붙들림/구출/포획 진행률은 아이콘 링으로 표시한다. |
| [Splatoon 3](https://splatoon.nintendo.com/en/gameplay/) | 빠른 이동/잠입/자원 회복이 캐릭터 상태와 강하게 연결되어 있고, 팀 색상 대비가 전장 이해를 돕는다. | 스태미나와 슬라임 지속시간을 캐릭터 주변 또는 하단 HUD에 붙여 추격 중 시선 이동을 줄인다. 점령 포인트는 팀 색상+형태를 같이 써 색각 의존을 낮춘다. |
| [Overwatch 2](https://overwatch.blizzard.com/en-us/news/23785337/an-inside-look-at-the-ping-system-in-overwatch-2/) | 한 버튼 탭/홀드로 적 위치, 공격, 방어, 도움 요청을 빠르게 전달한다. 팀원이 핑에 응답할 수 있는 상호작용이 있다. | `G` 탭은 컨텍스트 핑, `G` 홀드는 방사형 핑 휠. 핑에는 "간다/방어/도움/점령" 응답을 붙인다. |
| [Apex Legends](https://apexlegends.fandom.com/wiki/Ping) | 보이스 없이도 위치, 적, 이동 방향, 상호작용 대상을 음성/텍스트/월드 마커로 동시에 전달한다. | 핑은 월드 마커+미니맵 마커+짧은 팀 로그를 동시에 생성한다. 솔로큐를 위해 자주 쓰는 핑은 음성 없이도 의미가 명확해야 한다. |
| [Fall Guys](https://interfaceingame.com/games/fall-guys-ultimate-knockout/) | 라운드 목표와 진행/탈락 여부를 큰 글자와 단순 카운터로 보여준다. | 블리츠/카오스 모드는 "남은 시간", "포획 수", "점령 유지 시간"을 큰 카운터로 단순화한다. |
| [The Finals](https://www.reachthefinals.com/patchnotes/10-00) | 목표물 중심 핑과 빠른 커뮤니케이션을 강화한다. | 점령 포인트 핑은 포인트 ID, 점령 상태, 예상 충돌 위험을 함께 보여준다. 핑은 전투 중 오입력 방지를 위해 우선순위를 `적 > 구조 요청 > 점령 > 이동`으로 둔다. |

#### 9.6.1 HUD 우선순위 규칙

| 우선순위 | 정보 | 표시 위치 | 이유 |
|----------|------|-----------|------|
| P0 | 내 상태, 스태미나, 크로스헤어 | 중앙 하단/중앙 | 조작 판단에 즉시 필요 |
| P0 | 붙들림/포획/구출 알림 | 화면 가장자리 + 팀 상태 레일 | 팀 협동 실패가 바로 패배로 이어짐 |
| P1 | 왕 위치/왕 박탈/공수전환 | 미니맵 + 중앙 배너 | 게임 국면 전환 |
| P1 | 점령 포인트 상태 | 우측 목표 패널 | 승리 조건 판단 |
| P2 | 스킬 쿨다운/핑 로그 | 하단 좌우 | 전투 중 필요하지만 시선 점유를 줄여야 함 |

#### 9.6.2 프로토타입 HUD 범위

| 단계 | 포함 | 제외 |
|------|------|------|
| Phase 1 | 크로스헤어, 스태미나 바, 현재 상태 텍스트 | 미니맵, 팀 상태 레일, 점령 패널 |
| Phase 2 | 점령 포인트 패널, 붙들림/포획 진행 링 | 랭크/상점/시즌 UI |
| Phase 3 | 슬라임 지속시간/쿨다운, 핑 휠 MVP | 고급 접근성 커스터마이징 |

---

## 10. 비주얼 & 아트 스타일

### 10.1 아트 방향

| 항목 | 방향 |
|------|------|
| **스타일** | 스타일라이즈드 (Stylized) |
| **색감** | 선명하고 대비 강한 색상 |
| **분위기** | 긴장감 + 유머러스 |
| **참고작** | Fortnite, Valorant, Splatoon 3 |
| **렌더 파이프라인** | URP, 셀 셰이딩 일부 |

### 10.2 캐릭터 디자인

#### 10.2.1 기본 캐릭터

| 항목 | 설명 |
|------|------|
| **체형** | 약간 과장된 비율 (머리 큼) |
| **표정** | 눈에서 감정 표현 |
| **의상** | 모듈식 (커스터마이징 슬롯 5개) |
| **특징** | 귀여움 + 날렵함 |

#### 10.2.2 슬라임 형태

| 항목 | 설명 |
|------|------|
| **형태** | 반투명 젤리 |
| **색상** | 플레이어 색상 유지 |
| **움직임** | 흐르듯, 찰랑찰랑 |
| **표정** | 단순한 눈 (점 2개) |

### 10.3 이펙트 가이드

| 상황 | 이펙트 |
|------|--------|
| 덮치기 | 잔상 + 충격파 |
| 붙들기 | 빨간 사슬/손 |
| 슬라임 변신 | 물방울 튀김 |
| 포획 | 감옥/구슬 생성 |
| 점령 완료 | 기둥에서 빛 폭발 |
| 공수전환 | 화면 전체 색상 반전 |

### 10.4 UI 아트

| 요소 | 스타일 |
|------|--------|
| **아이콘** | 심플, 2D 플랫 |
| **폰트** | 굵고 둥근 산세리프 (Pretendard / Noto Sans) |
| **버튼** | 부드러운 모서리, 호버 효과 |
| **색상** | 하이 콘트라스트 |

---

## 11. 사운드 디자인

### 11.1 BGM

| 상황 | 분위기 | BPM |
|------|--------|-----|
| 메인 메뉴 | 신비로운, 긴장 | 80 |
| 로비 대기 | 밝은, 기대 | 100 |
| 게임 시작 | 긴장 고조 | 120 |
| 추격 중 | 빠른, 긴박 | 150 |
| 공수전환 | 극적 전환 | 변동 |
| 승리 | 승리감, 화려 | 130 |
| 패배 | 아쉬움, 차분 | 70 |

### 11.2 SFX 목록

#### 11.2.1 캐릭터

| 행동 | 사운드 |
|------|--------|
| 발걸음 | 바닥 재질별 다름 (5종) |
| 점프 | 가벼운 "휙" |
| 착지 | "텅" |
| 스프린트 | 빠른 발소리 |
| 숨소리 | 추격 시 거칠게 |

#### 11.2.2 스킬

| 스킬 | 사운드 |
|------|--------|
| 대시 | "슉!" |
| 덮치기 | "펑!" + 충격음 |
| 슬라임 변신 | "뿌직" + 물 소리 |
| 슬라임 이동 | "찰랑찰랑" |
| 왕의 포효 | 우렁찬 고함 |

#### 11.2.3 시스템

| 이벤트 | 사운드 |
|--------|--------|
| 붙들림 | 쇠사슬 소리 |
| 구출 | 사슬 끊어지는 소리 |
| 포획 | "쾅" + 감옥 닫히는 |
| 점령 시작 | 차임벨 |
| 점령 완료 | 팡파르 |
| 공수전환 | 드럼 롤 + 심벌즈 |

### 11.3 3D 오디오

| 요소 | 적용 |
|------|------|
| 발소리 | 방향 + 거리 + HRTF |
| 스킬 | 방향 |
| 환경음 | 공간 반향 |
| 보이스 채팅 | 위치 기반 근접 채팅 (옵션) |

---

## 12. 밸런싱 가이드

### 12.1 수치 테이블

#### 12.1.1 이동 관련

| 항목 | 기본값 | 범위 |
|------|--------|------|
| 기본 이동속도 | 5 m/s | 4~6 |
| 스프린트 속도 | 8 m/s | 7~10 |
| 점프 높이 | 2 m | 1.5~2.5 |
| 슬라임 속도 배율 | 1.4x | 1.2~1.6 |

#### 12.1.2 스킬 관련

| 항목 | 기본값 | 범위 |
|------|--------|------|
| 대시 쿨다운 | 5초 | 3~8 |
| 슬라임 쿨다운 | 15초 | 10~20 |
| 슬라임 지속시간 | 3초 | 2~5 |
| 덮치기 범위 | 3 m | 2~4 |
| 덮치기 쿨다운 | 2초 | 1~3 |

#### 12.1.3 점령 관련

| 항목 | 기본값 | 범위 |
|------|--------|------|
| 1인 점령 속도 | 5%/초 | 3~8 |
| 집결 점령 속도 | 12%/초 | 8~15 |
| 게이지 감소 속도 | 3%/초 | 2~5 |

### 12.2 밸런스 지표 (KPI)

| 지표 | 목표 | 허용 범위 | 측정 |
|------|------|----------|------|
| 평균 매치 시간 | 8분 | 5~12분 | 텔레메트리 |
| 공수전환 횟수/매치 | 4회 | 2~8회 | 텔레메트리 |
| 포획률 (포획/추격) | 30% | 20~40% | 텔레메트리 |
| 슬라임 탈출 성공률 | 60% | 50~70% | 텔레메트리 |
| 왕 생존 시간 | 3분 | 2~5분 | 텔레메트리 |
| 매치 포기율 | <5% | <8% | 텔레메트리 |
| 첫 시간 이탈률 | <30% | <40% | 분석 |

### 12.3 ScriptableObject 구조

```csharp
[CreateAssetMenu(fileName = "GameBalance", menuName = "Game/Balance Data")]
public class GameBalanceData : ScriptableObject
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float jumpHeight = 2f;

    [Header("Skills")]
    public float dashCooldown = 5f;
    public float slimeCooldown = 15f;
    public float slimeDuration = 3f;
    public float slimeSpeedMultiplier = 1.4f;

    [Header("Combat")]
    public float tackleRange = 3f;
    public float tackleCooldown = 2f;
    public float captureTime = 1.5f;

    [Header("Capture Points")]
    public float soloCapRate = 5f;
    public float groupCapRate = 12f;
    public float decayRate = 3f;

    [Header("King")]
    public float kingSpeedBonus = 0.10f;
    public float kingTackleRangeBonus = 0.20f;
    public float roarSlowAmount = 0.30f;
    public float roarRadius = 8f;
}
```

### 12.4 원격 설정 (Remote Config)

핫픽스용 밸런스 패치는 Supabase Edge Functions로 원격 배포:
- 클라이언트는 매치 시작 시 최신 밸런스 페치
- 캐싱 + 폴백 (네트워크 실패 시 로컬 SO 사용)
- A/B 테스트 지원 (10% 트래픽 → 새 밸런스)

---

## 13. 네트워킹 & 멀티플레이

### 13.1 네트워크 아키텍처

| 항목 | 선택 | 비고 |
|------|------|------|
| **솔루션** | Photon Fusion 2 | Tick-based, 권위 서버 옵션 |
| **모델** | Host-authority + 클라이언트 예측 | 1순위 |
| **틱레이트** | 30 Hz (서버), 60 Hz (클라이언트 보간) | |
| **지역 서버** | KR, JP, NA-East, NA-West, EU, SEA | 6개 |
| **백필** | 매치 중 이탈자 자리 봇으로 대체 | 30초 후 |

### 13.2 동기화 항목

| 항목 | 동기화 방식 | 빈도 |
|------|-----------|------|
| 플레이어 위치 | 보간 + 예측 | 30Hz |
| 플레이어 입력 | RPC | 입력 시 |
| 상태 변경 | NetworkBehaviour | 변경 시 |
| 점령 게이지 | NetworkProperty | 30Hz |
| 스킬 발동 | RPC + 검증 | 사용 시 |
| 채팅/핑 | 이벤트 RPC | 발생 시 |

### 13.3 지연 보정

| 기법 | 적용 영역 |
|------|----------|
| **클라이언트 예측** | 이동, 점프 |
| **서버 권위 검증** | 덮치기 히트, 포획 |
| **롤백 보정** | 덮치기 히트박스 (최대 100ms) |
| **보간 (Interpolation)** | 다른 플레이어 표시 |
| **외삽 (Extrapolation)** | 짧은 패킷 손실 시 |

### 13.4 매치메이킹

```
[큐 진입]
    ↓
[MMR 검색 (±50)]
    ↓ 30초 후 확장 (±150)
    ↓ 60초 후 확장 (±300)
    ↓
[매치 발견]
    ↓
[수락 확인 (10초)]
    ↓
[서버 할당 + 로딩]
    ↓
[게임 시작]
```

| 항목 | 내용 |
|------|------|
| **레이팅 시스템** | Glicko-2 |
| **초기 MMR** | 1500 |
| **배치 매치** | 10판 (높은 변동) |
| **랭크 티어** | Bronze ~ Grandmaster (7티어) |
| **시즌 리셋** | 시즌마다 부분 리셋 (소프트) |

### 13.5 이탈 페널티

| 행동 | 페널티 |
|------|--------|
| 첫 이탈 (시즌) | 경고 |
| 2회 이탈 | 5분 큐 금지 |
| 3회 이탈 | 30분 큐 금지 + MMR -25 |
| 반복 이탈 | 24시간 큐 금지 |

---

## 14. 기술 사양

### 14.1 개발 환경

| 항목 | 선택 |
|------|------|
| **게임 엔진** | Unity 6 LTS (6000.0.x) |
| **렌더 파이프라인** | URP 17.x |
| **네트워크** | Photon Fusion 2 |
| **백엔드** | Supabase (PostgreSQL + Edge Functions) |
| **인증** | Supabase Auth (OAuth + Email) |
| **CDN/배포** | Steam, Cloudflare |
| **버전 관리** | Git + GitHub + Git LFS |
| **CI/CD** | GitHub Actions + Unity Cloud Build |
| **IDE** | JetBrains Rider / Visual Studio 2022 |
| **이슈 트래커** | Linear |
| **현재 인프라 모드** | Local-first prototype |

### 14.1.1 로컬 우선 인프라 원칙

초기 구현은 온라인 인프라를 바로 붙이지 않고, Unity Editor와 로컬 데이터만으로 핵심 재미를 검증한다. Photon/Supabase는 설계 계약을 문서화하되 실제 의존성은 Phase 4~5까지 지연한다.

| 영역 | 로컬 단계 | 온라인 전환 조건 |
|------|-----------|------------------|
| 게임 루프 | Unity Play Mode 단일 프로세스 | 점령/포획 루프가 1인 테스트에서 검증됨 |
| 플레이어 데이터 | ScriptableObject/JSON mock | 프로필, 전적, 보상이 실제 유지되어야 함 |
| 매치 상태 | 로컬 MatchManager | 2인 이상 동기화 테스트 시작 |
| 네트워크 | 없음, 인터페이스만 준비 | Phase 4에서 Photon Fusion Host-authority 실험 |
| 백엔드 | 없음, 로컬 mock service | Phase 5에서 Supabase Auth/PostgreSQL 연결 |
| 텔레메트리 | 로컬 로그/CSV | Alpha 전에 Edge Function 이벤트 수집 |

#### 14.1.2 로컬 실행 기준

| 항목 | 기준 |
|------|------|
| Unity 버전 | 6000.0.x LTS 계열 |
| 기본 씬 | `Assets/Scenes/Prototype.unity` |
| 테스트 | Unity EditMode 테스트가 로컬에서 통과해야 함 |
| 외부 서비스 | 기본 실행에 Photon/Supabase/Steam 로그인이 필요 없어야 함 |
| 데이터 | 로컬 mock 데이터는 나중에 Supabase 스키마로 옮길 수 있게 필드명을 맞춘다 |
| 검증 | GDD 기능 추가 시 `README.md`에 로컬 실행/테스트 명령을 갱신한다 |

### 14.1.3 GitHub 공개 레퍼런스 후보

**조사 기준일:** 2026-07-03
**사용 원칙:** 공개 repo는 구조/패턴 참고용이다. 라이선스가 명확하지 않은 repo, Photon SDK/3rd-party asset 포함 repo의 코드는 복사하지 않는다. 실제 도입 전에는 라이선스와 Unity/패키지 버전을 다시 확인한다.

| 영역 | 후보 repo | 라이선스 | 최근성/Unity | 참고 포인트 | 주의점 |
|------|-----------|----------|--------------|-------------|--------|
| Unity 6 멀티 샘플 | [Boss Room](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop) | Unity Companion | 2026-03, Unity 6000.0.x | Input System, GameData, action/UI flow | Netcode for GameObjects 기반, Fusion 아님 |
| Unity 6 3인칭 네트워크 | [Multiplayer Bitesize](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.bitesize) | Unity Companion | 2025-10, Unity 6000.0.x | StarterAssets third-person + Netcode 연결 | 점령/포획 objective는 약함 |
| Objective/Capture 구조 | [FPSSample](https://github.com/Unity-Technologies/FPSSample) | Unity Companion | 2025-10 push, Unity 2018.3 | CapturePoint, GameScore, team objective 구조 | Unity 버전 오래됨, 1인칭 중심 |
| CharacterController 연구 | [CharacterControllerSamples](https://github.com/Unity-Technologies/CharacterControllerSamples) | Unity Companion | 2025-11, Unity 6000.2.x | character/camera sample 구조 | DOTS/Entities 성격이라 직접 결합 비용 큼 |
| 3인칭 이동 | [simple-character-controller](https://github.com/PixelWizards/simple-character-controller) | MIT | 2024-10, Unity 2022.3 | third-person camera, jump/run animation | stamina 없음 |
| 스태미나/발소리/UI | [gold-player](https://github.com/Hertzole/gold-player) | MIT | 2026-06, Unity 6000.5.x | stamina, crouch, jump, footsteps, UI | 1인칭 controller라 카메라/이동 변환 필요 |
| 3인칭 crouch/jump 테스트 | [Erbium](https://github.com/mikhomak/Erbium) | BSD-3-Clause | 2025-05 | third-person movement, crouch, jump tests | 입력 구조가 현재 Unity Input System과 다를 수 있음 |
| Photon Fusion objective | [packet-panic-relay-arena](https://github.com/sayCHEESExD/packet-panic-relay-arena) | 미확인 | 2026-06, Unity 6000.0.x | Fusion GameState, packet/objective, timer | 라이선스 없음, 코드 복사 금지 |
| Photon Fusion scaffold | [multiplayer-ability-arena](https://github.com/jereor/multiplayer-ability-arena) | MIT | 2026-05, Unity 6000.4.x | NetworkBootstrap, PlayerSpawner | objective 구현은 약함 |
| CapturePoint 참고 | [Cityrunners](https://github.com/Soossie/Cityrunners) | 미확인 | 2026-05, Unity 6000.3.x | CapturePoint, match UI, NetworkRunner-style scripts | manifest 복원/라이선스 확인 필요 |
| Fusion dedicated server | [hathora-photon-fusion-dedicated-server](https://github.com/hathora/hathora-photon-fusion-dedicated-server) | MIT | 2024-01 | dedicated server build/deploy 구조 | 게임룰보다는 서버 인프라 참고 |
| 숨바꼭질/Photon | [hide-and-seek](https://github.com/owengretzinger/hide-and-seek) | 미확인 | Unity + Photon, 6 commits | 온라인 hide-and-seek 구조, seeker/hider 세션 감각 | repo 규모 작음, 실행 안정성/라이선스 확인 전 코드 복사 금지 |
| Prop Hunt/Mirror | [PropHunt-Mirror](https://github.com/nicholas-maltbie/PropHunt-Mirror) | MIT | Unity + Mirror, 51 commits | 숨는 팀/찾는 팀 분리, PropHunt식 역할 UI/네트워크 구조 | Mirror 기반이라 Fusion 도입 시 개념만 참고 |
| CTF/Mirror/Steam | [Capture-The-UdarFlag](https://github.com/OfirUdar/Capture-The-UdarFlag) | 미확인 | Unity 2020.3.6f1, 37 commits | 깃발/거점 objective, lobby/Steam 친구 join, revive/stand-near flow, README 이미지 | 외부 모델/패키지 포함 가능성, Unity 버전 오래됨 |
| 스텔스/hearing 게임 | [Seek_And_Steal_Unity3D-Game](https://github.com/Mansitos/Seek_And_Steal_Unity3D-Game) | 미확인 | Unity3D course project | 달리면 들리는 guard hearing, 장애물/수풀 hide loop | 오래된 input system, 싱글플레이 중심 |
| stealth/hearing | [stealth-ai-sandbox](https://github.com/malvinlh/stealth-ai-sandbox) | MIT | 2026-05, Unity 6000.3.x | hearing/noise, stance SO, event channel | 2D/샌드박스 성격 |
| AI perception | [Unity_AIBehavioursPackage](https://github.com/GameDevEducation/Unity_AIBehavioursPackage) | MIT(LICENSE 파일 확인) | 2026-06, Unity package 형태 | HearingSensor, sensor config, vision/proximity sensor 구조 | 패키지 성격, 게임 loop 샘플 아님 |
| 캐릭터 feel/URP | [stylised-character-controller](https://github.com/joebinns/stylised-character-controller) | MIT | Unity 3D, URP | floating capsule, squash/stretch, dither silhouette, dust/SFX로 캐릭터 감각 강화 | 물리 기반 이동이라 현재 CharacterController와 직접 결합하지 않음 |
| 슬라임 softbody | [Softbodies](https://github.com/Ideefixze/Softbodies) | MIT | Unity, 29 commits | jelly mesh deformation, spring/riggedbody 방식, 슬라임 비주얼 연구 | softbody는 성능/콜라이더 비용 큼, MVP는 shader/scale squash부터 |
| 슬라임 softbody 실험 | [SoftBodySimulation](https://github.com/chrismarch/SoftBodySimulation) | GPL-3.0 | Unity 2018.2+, 54 commits | jelly/slime mesh deformation, jump/compress procedural animation | GPL이라 코드 도입 금지, 아이디어/영상 레퍼런스만 |
| minimap/UGUI lightweight | [unity-minimap](https://github.com/pointcache/unity-minimap) | Unlicense(LICENSE 파일 확인) | 2026-05 | UGUI minimap, icon prefab, MinimapSystem/MinimapObject 구조 | GitHub license metadata는 비어 있어 LICENSE 파일 기준으로만 판단 |
| minimap/no camera | [UnitySimpleMiniMap](https://github.com/bilal-arikan/UnitySimpleMiniMap) | MIT(LICENSE 파일 확인) | 2026-06 | RenderTexture/별도 카메라 없이 map bounds와 marker를 계산하는 구조 | 기능은 가볍지만 아트/UI polish는 직접 필요 |
| minimap/waypoint | [DMMap](https://github.com/DMeville/DMMap) | MIT | 2025-08, Unity 2021.1 | minimap, icons, waypoint UI | 오래됨, HUD 디자인 재구성 필요 |
| ping marker | [Kaiju-Runner](https://github.com/Leetany/Kaiju-Runner) | 미확인 | 2025-08, Unity 6000.1.x | ping marker UI/RPC pattern | 라이선스 없음, 구조 관찰만 |

#### 14.1.4 레퍼런스 적용 순서

| 구현 단계 | 우선 참고 | 적용 방식 |
|-----------|-----------|-----------|
| Phase 2 점령/포획 | FPSSample, Cityrunners | CapturePoint/score 구조를 보고 Overthrone용 로컬 `CapturePoint`를 새로 작성 |
| Phase 2 상태 SSOT | Boss Room, FPSSample, PropHunt-Mirror | action/game state 데이터 경계를 참고하되 `LocalMatchManager` 중심으로 단일화 |
| Phase 2 숨기/듣기 | Seek_And_Steal, stealth-ai-sandbox | 걷기 무음/달리기 소음/수색 상태를 현재 `NoiseSystem` 기준으로 재구성 |
| Phase 3 슬라임/스태미나 | gold-player, CharacterControllerSamples, Softbodies, stylised-character-controller | stamina/crouch/footstep은 `PlayerMotor`, 슬라임 비주얼은 shader/squash 우선으로 재구성 |
| Phase 4 멀티 | Photon 공식 SDK, multiplayer-ability-arena, packet-panic-relay-arena, hide-and-seek | Fusion 2 버전 확인 후 별도 branch에서 networked prototype |
| UI 2차 | unity-minimap, UnitySimpleMiniMap, DMMap, Kaiju-Runner, Capture-The-UdarFlag, 벤치마크 UI 리서치 | minimap/ping/objective/lobby는 복사보다 정보 구조와 입력 흐름만 참고 |

#### 14.1.5 GitHub 검색 검증 메모

**확인 기준일:** 2026-07-03
**검색 범위:** GitHub 공개 repo와 GitHub API tree. 라이선스가 없거나 3rd-party asset/SDK가 포함된 repo는 구조 관찰만 허용한다.

| repo | 실제 확인한 단서 | Overthrone 적용 |
|------|------------------|-----------------|
| [Cityrunners](https://github.com/Soossie/Cityrunners) | `Assets/Scripts/Match Logic/CapturePoint.cs`, `Assets/Scripts/UI/MatchUI.cs`, `PlayerUI.cs`, `Assets/Scripts/Networking/NetworkRunnerHandler.cs`, 캐릭터별 `*NetworkController.cs` | 3v3 Photon Fusion FPS의 capture point, match UI, character/network script 분리 방식을 관찰한다. 코드 복사는 금지하고 `LocalMatchManager`/`PlayerHud` 계약을 유지한다. |
| [Kaiju-Runner](https://github.com/Leetany/Kaiju-Runner) | `Assets/Scripts/PingSystem/PingMarkerUI.cs`, `PingTarget.cs`, `PingTargetRPC.cs`, `RPCPingUIManager.cs`, `Resources/Prefabs/UI/Marker.prefab` | 핑 마커는 world marker, UI manager, RPC 이벤트로 나누는 구조를 참고한다. 라이선스가 확인되지 않았으므로 Overthrone용 `LocalPingSystem`을 새로 작성한다. |
| [unity-minimap](https://github.com/pointcache/unity-minimap) | `Assets/pointcache/Minimap/MinimapSystem.cs`, `MinimapObject.cs`, `MinimapIcon.cs`, `Resources/pointcache/Minimap/Icon.prefab`, `Minimap.prefab` | 현재 `PlayerHud`의 단순 `Image` 마커를 나중에 minimap system + icon prefab 구조로 확장할 때 참고한다. 라이선스는 LICENSE 파일 기준 Unlicense로 확인했지만, 도입 전 asset/meta 포함 범위를 다시 점검한다. |
| [UnitySimpleMiniMap](https://github.com/bilal-arikan/UnitySimpleMiniMap) | `Runtime/MiniMapView.cs`, `Runtime/MiniMapBounds.cs`, `Runtime/Utils.cs`, `Demo/MiniMap.prefab`, `Demo/uiDot1.prefab` | RenderTexture/별도 minimap camera 없이 bounds 기반으로 마커 위치를 계산하는 구조가 로컬 HUD MVP와 잘 맞는다. 현재 미니맵은 이 방향을 우선 비교한다. |
| [DMMap](https://github.com/DMeville/DMMap) | `Assets/DMMap/Scripts/DMMap.cs`, `DMMapIcon.cs`, `DMMapPoint.cs`, `DMMapShape.cs`, waypoint demo, icon assets | 미니맵은 마커/아이콘/웨이포인트를 분리하고, 로컬 MVP의 `Image` 마커를 나중에 icon/waypoint layer로 치환한다. MIT지만 현재는 전체 패키지 도입보다 구조만 참고한다. |
| [Capture-The-UdarFlag](https://github.com/OfirUdar/Capture-The-UdarFlag) | README의 CTF 승리 조건, stand-near revive/respawn 설명, `ScoreDisplay_UI`, `MapDisplay_UI`, `ObjectLocationsDisplay_UI`, `SprintStaminaDisplay_UI`, `CircleFiller` | 목표/점수/맵/스태미나/상호작용 링을 별도 UI 컴포넌트로 나누는 방향을 참고한다. Unity 2020 + Mirror/Steam/asset 의존이 있어 직접 도입하지 않는다. |
| [Boss Room](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop) | Unity Netcode 기반 co-op sample, action flow, replicated object, RPC, UGS/Auth flow | Fusion 전환 전에도 client UI와 game state 이벤트를 분리하는 교육용 reference로만 사용한다. 네트워크 스택은 직접 섞지 않는다. |
| [FPSSample](https://github.com/Unity-Technologies/FPSSample) | Unity 공식 fully functional multiplayer shooter, Git LFS 기반 대형 프로젝트, Unity 2018.3/HDRP/transport/ECS hybrid | objective/score 구조와 대형 프로젝트 툴링 관찰용이다. Unity 버전과 렌더 파이프라인이 달라 코드 도입보다 개념 비교에 제한한다. |
| [Unity_AIBehavioursPackage](https://github.com/GameDevEducation/Unity_AIBehavioursPackage) | `Assets/com.gamedeveducation.aibehaviours/Runtime/CommonCore/Perception/Scripts/Sensors/HearingSensor.cs`, `HearingSensorConfig.cs`, `SensorBase.cs`, `SensorConfigBase.cs`, `SensorConfig_Hearing.asset`, `package.json` | 현재 `NoiseSystem`/`AIHearingSensor`를 유지하되, sensor config를 데이터화하고 vision/proximity/hearing 인터페이스를 나누는 방향을 참고한다. |

### 14.2 최소/권장 사양 (PC)

| 항목 | 최소 | 권장 |
|------|------|------|
| **OS** | Windows 10 64bit | Windows 11 64bit |
| **CPU** | i5-8400 / Ryzen 5 2600 | i5-12400 / Ryzen 5 5600 |
| **RAM** | 8 GB | 16 GB |
| **GPU** | GTX 1060 6GB | RTX 3060 / RX 6600 |
| **VRAM** | 4 GB | 8 GB |
| **저장공간** | 8 GB SSD | 12 GB SSD |
| **네트워크** | 광대역 (Ping <100ms) | 광대역 (Ping <50ms) |

### 14.3 모바일 최소 사양

| 플랫폼 | 최소 사양 |
|--------|----------|
| iOS | iPhone XR 이상, iOS 16+ |
| Android | Snapdragon 778G / Dimensity 1100 이상, RAM 6GB+, Android 11+ |

### 14.4 프로젝트 구조

```
Assets/
├── _Project/
│   ├── Scripts/
│   │   ├── Core/           # 게임 매니저, 상태 머신
│   │   ├── Player/         # 플레이어 컨트롤러, 스킬
│   │   ├── Systems/        # 점령, 포획, 상태 시스템
│   │   ├── Network/        # Photon Fusion 관련
│   │   ├── Backend/        # Supabase 클라이언트
│   │   ├── UI/             # UI 스크립트
│   │   ├── Telemetry/      # 분석 이벤트
│   │   └── Utils/          # 유틸리티
│   ├── Prefabs/
│   │   ├── Player/
│   │   ├── Environment/
│   │   └── UI/
│   ├── Scenes/
│   │   ├── Boot.unity
│   │   ├── MainMenu.unity
│   │   ├── Lobby.unity
│   │   └── Maps/
│   ├── ScriptableObjects/
│   │   ├── GameBalance.asset
│   │   └── MapData/
│   ├── Materials/
│   ├── Textures/
│   ├── Audio/
│   │   ├── BGM/
│   │   └── SFX/
│   └── Animations/
├── Photon/                  # Photon Fusion 2 SDK
├── Supabase/                # Supabase Unity SDK
├── TextMeshPro/
└── Tests/
    ├── EditMode/
    └── PlayMode/
```

### 14.5 핵심 클래스 설계

```
┌─────────────────┐
│   GameManager   │ ◄── 게임 전체 관리 (싱글톤)
└────────┬────────┘
         │
    ┌────┴────┬────────────┐
    │         │            │
┌───▼───┐ ┌───▼────┐ ┌─────▼─────┐
│Match  │ │Network │ │ Backend   │ ◄── Supabase 클라이언트
│Manager│ │Manager │ │ Service   │
└───┬───┘ └────────┘ └───────────┘
    │
┌───▼────────────┐
│  PlayerManager │ ◄── 플레이어 스폰/관리
└───┬────────────┘
    │
┌───▼─────────────┐
│ PlayerController│ ◄── 개별 플레이어 (NetworkBehaviour)
├─────────────────┤
│ - Movement      │
│ - StateSystem   │
│ - SkillSystem   │
│ - InputHandler  │
│ - NetworkSync   │
└─────────────────┘

┌─────────────────┐
│ CaptureManager  │ ◄── 점령 시스템
├─────────────────┤
│ - CapturePoints │
│ - StateTracker  │
│ - SwitchLogic   │
└─────────────────┘

┌─────────────────┐
│ CombatSystem    │ ◄── 포획 시스템
├─────────────────┤
│ - Tackle        │
│ - Hold          │
│ - Capture       │
│ - Rescue        │
└─────────────────┘
```

### 14.6 성능 목표

| 플랫폼 | 해상도 | FPS 목표 |
|--------|--------|---------|
| PC (Low) | 1080p | 60 |
| PC (High) | 1440p | 144 |
| PC (Ultra) | 4K | 60 |
| Mobile (Low) | 720p | 30 |
| Mobile (High) | 1080p | 60 |
| Switch | 720p docked / 540p handheld | 30 |

---

## 15. 백엔드 & 데이터

### 15.1 백엔드 책임

| 도메인 | 책임 | 기술 |
|--------|------|------|
| **인증** | 로그인, 세션, 소셜 연동 | Supabase Auth |
| **프로필** | 닉네임, 통계, 인벤토리 | PostgreSQL |
| **매치 기록** | 전적, 리플레이 메타 | PostgreSQL |
| **상점/결제** | IAP 검증, 인벤토리 지급 | Edge Functions |
| **시즌패스** | 진행도, 보상 | PostgreSQL + Edge Functions |
| **친구/소셜** | 친구 목록, 초대 | PostgreSQL + Realtime |
| **리더보드** | 랭킹 | PostgreSQL (materialized view) |
| **밸런스 핫픽스** | Remote Config | Edge Functions |

### 15.2 주요 테이블 스케치

```sql
-- 프로필
profiles (
  id uuid pk,
  display_name text,
  mmr int,
  rank_tier text,
  created_at timestamptz,
  last_seen_at timestamptz
)

-- 매치 기록
matches (
  id uuid pk,
  mode text,
  map text,
  started_at timestamptz,
  ended_at timestamptz,
  winning_team int,
  duration_sec int
)

-- 매치 참가자
match_players (
  match_id uuid fk,
  profile_id uuid fk,
  team int,
  captures int,
  rescues int,
  captured_count int,
  point_contribution float,
  mmr_change int,
  was_mvp bool
)

-- 인벤토리
inventory (
  profile_id uuid fk,
  item_id text,
  acquired_at timestamptz,
  source text
)

-- 시즌 진행
season_progress (
  profile_id uuid fk,
  season_id int,
  xp int,
  pass_tier int,
  premium_unlocked bool
)
```

### 15.3 RLS (Row Level Security)

- `profiles`: 본인만 UPDATE, 모두 SELECT (display_name + mmr만)
- `inventory`: 본인만 SELECT/UPDATE
- `matches`, `match_players`: 본인 참가 매치만 SELECT
- 클라이언트는 **결제/지급 직접 쓰기 금지** → Edge Functions로만

---

## 16. 안티치트 & 보안

### 16.1 클라이언트 보호

| 위협 | 대응 |
|------|------|
| 메모리 조작 | EAC (Easy Anti-Cheat) 통합 |
| 패킷 위조 | Photon 권위 서버 + 검증 |
| 매크로/봇 | 행동 패턴 분석 (서버 측) |
| 디버거 | EAC 검사 |
| Speedhack | 서버 측 이동거리 검증 (>120% 시 플래그) |

### 16.2 서버 측 검증

- **모든 히트 판정 서버 권위**
- **이동 속도 sanity check** (틱당 최대 이동거리)
- **스킬 쿨다운 서버 추적**
- **점령 게이지 서버 권위**
- **결제는 항상 영수증 검증** (Steam / App Store / Google Play)

### 16.3 신고 시스템

| 신고 사유 | 처리 |
|-----------|------|
| 핵 의심 | 자동 리플레이 + 휴먼 리뷰 |
| 욕설/혐오 | 채팅 로그 수집 + 자동 필터 |
| AFK/트롤 | 매치 데이터 분석 |
| 부정 행위 | 다중 신고 누적 시 자동 검토 큐 |

### 16.4 제재 단계

1. 1회 위반: 경고
2. 2회 위반: 24시간 정지
3. 3회 위반: 7일 정지
4. 핵 확정: 영구 정지 + HWID/계정 차단

---

## 17. 텔레메트리 & 분석

### 17.1 핵심 이벤트

| 이벤트 | 속성 | 용도 |
|--------|------|------|
| `match_start` | mode, map, team_size | 모드/맵 인기도 |
| `match_end` | duration, winner, switches | 매치 길이 |
| `state_change` | from, to, trigger | 상태 시스템 검증 |
| `tackle_attempt` | success, range, lag | 덮치기 밸런스 |
| `capture_complete` | point_id, team_size | 점령 패턴 |
| `slime_use` | reason, escape_success | 슬라임 활용도 |
| `player_captured` | by_king, time_since_held | 포획 효율 |
| `quit_match` | reason, match_time | 이탈률 |
| `purchase` | item_id, price, currency | 매출 |

### 17.2 분석 파이프라인

```
[Unity Client] ──> [Supabase Edge Function]
                          │
                          ▼
                  [PostgreSQL events]
                          │
                          ├─> [BigQuery / DuckDB export]
                          │
                          └─> [Metabase 대시보드]
```

### 17.3 핵심 대시보드

1. **DAU/WAU/MAU**
2. **D1/D7/D30 리텐션**
3. **모드별 플레이 시간 분포**
4. **밸런스 KPI** (§12.2 기준)
5. **결제 퍼널**
6. **이탈 매치 비율**

---

## 18. 접근성

### 18.1 시각

| 옵션 | 설명 |
|------|------|
| **색맹 모드** | Deuteranopia, Protanopia, Tritanopia 프리셋 |
| **UI 스케일** | 80% ~ 150% |
| **자막** | 모든 음성/효과음 자막화 옵션 |
| **고대비 모드** | UI 콘트라스트 강화 |
| **카메라 흔들림** | 비활성화 옵션 |

### 18.2 청각

| 옵션 | 설명 |
|------|------|
| **시각적 사운드 표시** | 발소리 방향 화면 가장자리 표시 |
| **자막** | 모든 보이스 라인 |
| **모노 오디오** | 좌우 분리 무력화 |

### 18.3 조작

| 옵션 | 설명 |
|------|------|
| **키 리매핑** | 모든 키 변경 가능 |
| **토글/홀드** | 스프린트, 앉기 등 |
| **자동 점프** | 가장자리 자동 점프 (옵션) |
| **컨트롤러 진동** | 강도 조절 |
| **자이로 조준** | 모바일/콘솔 |

### 18.4 인지

| 옵션 | 설명 |
|------|------|
| **튜토리얼 반복** | 언제든 재실행 |
| **상황 알림 강화** | 중요 이벤트 화면 중앙 표시 |
| **속도 조절** | 튜토리얼 한정 |

---

## 19. 개발 로드맵

### 19.1 Phase 개요 (현재 위치: Phase 1)

```
[Phase 0] Pre-Production ───── 완료 (2026-02)
    ↓
[Phase 1] 싱글플레이어 기반 ── 진행 중 (2026-05~06)  ◄── 현재
    ↓
[Phase 2] 핵심 메카닉 ──────── 2026-07~08
    ↓
[Phase 3] 슬라임 스킬 ──────── 2026-09
    ↓
[Phase 4] 멀티플레이어 ─────── 2026-10~12
    ↓
[Phase 5] 백엔드 통합 ───────── 2027-01
    ↓
[Phase 6] 폴리싱 ────────────── 2027-02~03
    ↓
[Closed Alpha] 내부 테스트 ─── 2027-04
    ↓
[Closed Beta] 친구/인플루언서 ─ 2027-05~06
    ↓
[Open Beta] 일반 공개 ──────── 2027-07~08
    ↓
[Launch] Steam Early Access ── 2027-09
    ↓
[1.0 Release] 정식 출시 ────── 2028-Q1
```

### 19.2 Phase 상세

#### Phase 1: 싱글플레이어 기반 (2026-05 ~ 2026-06)
- 프로젝트 설정 (Unity 6 LTS, URP)
- 플레이어 이동 (CharacterController)
- 3인칭 추격 카메라 + 마우스룩
- 테스트 맵 (Blockout)
- 기본 HUD (스태미나, 크로스헤어)

#### Phase 2: 핵심 메카닉 (2026-07 ~ 2026-08)
- 점령 포인트 로직
- 상태 시스템 (중립/공격자/왕)
- 덮치기 + 붙들기
- 포획/구출
- 공수전환

#### Phase 3: 슬라임 스킬 (2026-09)
- 슬라임 변신
- 물리 변경 (마찰/속도/히트박스)
- 3인칭 카메라 전환
- 붙들림 탈출 로직

#### Phase 4: 멀티플레이어 (2026-10 ~ 2026-12)
- Photon Fusion 2 통합
- 플레이어 동기화
- 게임 상태 동기화
- 매치메이킹 (간소화)
- 로비 + 룸 시스템

#### Phase 5: 백엔드 통합 (2027-01)
- Supabase Auth
- 프로필 + 통계
- 인벤토리
- 시즌패스 기반
- 텔레메트리 파이프라인

#### Phase 6: 폴리싱 (2027-02 ~ 2027-03)
- UI 완성
- 사운드 (BGM + SFX)
- VFX
- 최적화 (60 FPS 보장)
- 버그 수정

### 19.3 마일스톤

| 마일스톤 | 목표 | 기준 | 예상 시점 |
|----------|------|------|----------|
| **M1** | 움직이는 캐릭터 | 이동/점프/카메라 | 2026-06 |
| **M2** | 점령 작동 | 점령 게이지, 완료 | 2026-07 |
| **M3** | 포획 루프 | 덮치기→붙들기→포획 | 2026-08 |
| **M4** | 슬라임 완성 | 변신, 물리, 탈출 | 2026-09 |
| **M5** | 멀티 2인 | 2명 동시 플레이 | 2026-11 |
| **M6** | 풀 매치 | 6명 매치 완료 | 2026-12 |
| **M7** | 백엔드 연동 | 프로필 영속 | 2027-01 |
| **M8** | Alpha | 내부 테스트 가능 | 2027-04 |
| **M9** | Beta | 외부 테스트 가능 | 2027-06 |
| **M10** | Early Access | Steam 출시 | 2027-09 |
| **M11** | 1.0 Launch | 정식 출시 | 2028-Q1 |

### 19.4 현재 구현 상태 (2026-07-03)

**현재 기준:** `unity/OverthroneUnity` 로컬 Unity prototype. 외부 Photon/Supabase/Steam 의존 없이 Unity Editor와 EditMode 테스트로 검증한다.

| 범위 | 상태 | 근거 | 남은 일 |
|------|------|------|---------|
| Unity 6 + URP 프로젝트 | 완료 | `unity/OverthroneUnity` 생성, `Prototype.unity` 기본 씬 | Git LFS/빌드 타깃 정리 |
| 입력 | 완료 | WASD/방향키, 마우스룩, Sprint, Dash, Jump, Crouch, Slime, Tackle, Capture raw hold, Ping | 키 리바인딩 UI, 모바일 가상 스틱 |
| 이동/카메라 | 완료 | CharacterController 이동, 점프, 스프린트, Dash 0.5초 200% 가속, 3인칭 카메라, `SphereCast` 기반 카메라 충돌 처리, 마우스/게임패드 Look 감도 분리와 게임패드 초당 회전 보정 | 콘솔별 감도 프리셋/자이로 조준 |
| 상태별 이동 | 완료 | Neutral/Attacker/King/Held/Captured/Slime/Holding MovementProfile, Held/Captured/Holding 수평 속도 즉시 0, 로컬 Blue 3/Red 3 roster, 점령 수 기반 팀당 King 1명, 최종 포획 수→점령 기여도→tie-breaker 왕 승계, 3명 집결 기반 Attacker, 5초 Attacker 유예 | Photon roster 동기화 |
| 스태미나 | 부분 완료 | Sprint 소모/회복, HUD 표시, Q 또는 Shift 더블탭 Dash 스태미나 25 소모 및 5초 쿨다운, 덮치기 시도 스태미나 30 소모 및 2초 쿨다운, Slime 스태미나 50 소모 및 15초 쿨다운 | 비용/쿨다운 밸런스 튜닝 |
| HUD/UI | 부분 완료 | 크로스헤어, 스태미나 바, 상태 텍스트, Dash/Slime 쿨다운/스태미나 부족 표시, 우측 Objective 점령/소유/승리 카운트다운/phase/defender 텍스트, 우측 점령 패널 MVP(A/B/C row, owner 색상, progress fill, state, Blue/Red count), 좌측 팀 상태 레일, 포획 진행 링, 붙들림/구출 전용 링, 관전 overlay, 로컬 미니맵 panel/자신/아군/적 왕/적 공격자/점령 포인트/핑 마커, `G` 탭 컨텍스트 핑, `G` 홀드 방사형 핑 휠, 로컬 핑 응답 로그, EditMode 테스트 | 아이콘 아트, 핑 응답 상호작용 polish, 온라인 RPC 동기화 |
| 사운드/AI hearing | 부분 완료 | 달리기 발소리 NoiseEvent, AIHearingSensor 소음 기억 | 바닥 재질별 소리, NavMesh 추적, 경계/수색 상태 |
| 애니메이션 | 부분 완료 | Placeholder Idle/Walk/Run Blend Tree | 실제 모델/모션 클립, 점프/앉기/덮치기/슬라임 애니메이션 |
| 점령 시스템 | 부분 완료 | 로컬 CapturePoint 3개, 반경 5m, 팀별 인원 집계, 1명 5%/초·2명 8%/초·3명+ 12%/초, 경합 정지, 빈 미완료 포인트 3%/초 decay, Attacker/King 자동 부여, 3점 점령 30초 승리 카운트다운, `LocalMatchPhase` Playing/VictoryCountdown/Result, defender re-entry window, 카운트다운 시작/중단/종료 `LocalMatchFlowEvent`, HUD phase/defender 표시, `LocalMatchFlowPresenter` 배너/overlay/defender re-entry spawn/임시 Attacker 버프, EditMode 테스트 | 정식 카메라 컷인, 공수전환 사운드/아트 polish, 네트워크 권위화 |
| 포획/구출 루프 | 부분 완료 | 로컬 `PlayerCaptureAgent`/`LocalCaptureSystem`, Attacker/King 덮치기 조건, `TackleHitbox` 전방 물리 히트박스, Holding/Held 상태, 덮치기 실패 시 0.5초 경직, 왕 1.5초 최종 포획, 같은 팀 접촉 구출, 구출/슬라임 탈출/붙드는 자 피격 입력 시 붙들림 해제 + 붙든 자 1초 경직, Captured 시 `LocalSpectatorCamera` 자동 관전 전환, `Q`/`Tab` 관전 대상 순환 입력, 관전 HUD overlay, `LocalDeadChannel` Captured 전용 같은 팀 로그/최종 포획 system join 메시지, `CaptureFeedbackController` 절차적 VFX/SFX, EditMode 테스트 | 정식 포획 VFX/SFX 에셋/믹싱, dead channel 입력 UI/온라인 채팅/moderation |
| 슬라임 스킬 | 부분 완료 | R 입력 시 스태미나 50을 소모하고 15초 쿨다운이 시작되는 임시 Slime movement state, Held 상태에서 매치당 1회 자가 탈출, `CharacterController` 히트박스 축소, no-input 감속 완화로 미끄러짐 처리, placeholder squash/widen 변형 비주얼, HUD 쿨다운/스태미나 gate 표시, EditMode 테스트 | 정식 shader/softbody VFX, 좁은 통로 레벨 검증, 라운드 재시작 시 탈출권 reset hook |
| 로컬 데이터 mock | 완료 | `LocalDataStore` profiles/matches/match_players/telemetry_events CSV writer, Supabase 스키마명 헤더, CSV escape, 로컬 디렉터리 저장, EditMode 테스트 | Supabase Auth/PostgreSQL 연결, Edge Function 이벤트 업로드 |
| 네트워크/백엔드 | 미구현 | Local-first 원칙으로 지연 | Photon Fusion, Supabase, Auth, 전적, 텔레메트리 |

#### 19.4.1 다음 구현 우선순위

1. **포획 루프 보강:** 정식 포획 VFX/SFX 에셋/믹싱, dead channel 입력 UI/온라인 채팅/moderation.
2. **UI 2차:** 아이콘 아트, 핑 응답 상호작용 polish, 온라인 RPC 동기화.
3. **공수전환 polish:** 정식 카메라 컷인, 사운드, 아트 polish.
4. **멀티 준비:** `LocalMatchRules`/`CaptureInteractionRules`를 Photon/server-authoritative tick으로 옮길 수 있게 입력/상태 이벤트 계약 분리.
5. **온라인 데이터 전환:** `LocalDataStore` CSV 계약을 Supabase Auth/PostgreSQL/Edge Function으로 연결.

---

## 20. 라이브 옵스 & 시즌

### 20.1 시즌 구조

| 항목 | 내용 |
|------|------|
| **시즌 길이** | 10주 |
| **시즌당 콘텐츠** | 신규 맵 1개 또는 신규 모드 1개 |
| **시즌패스** | 무료 50티어 + 프리미엄 50티어 |
| **랭크 리셋** | 시즌마다 소프트 리셋 (-200 MMR) |

### 20.2 콘텐츠 캘린더 (가안)

| 시즌 | 테마 | 신규 콘텐츠 |
|------|------|------------|
| S1 (런칭) | "왕좌의 시작" | 기본 3맵 + 3모드 |
| S2 | "심해" | 항구 확장맵, 이동형 점령 포인트 |
| S3 | "겨울 궁전" | 신규 맵 (눈), 미끄러운 바닥 강조 |
| S4 | "정글" | 신규 맵 (야간), 어두운 구역 |

### 20.3 이벤트

| 이벤트 | 빈도 |
|--------|------|
| 주말 2배 XP | 매주 |
| 한정 모드 (변형 룰) | 시즌 중간 |
| 콜라보 스킨 | 시즌당 1회 |
| 토너먼트 | 시즌 종료 시 |

### 20.4 핫픽스 SLA

| 심각도 | 대응 시간 |
|--------|----------|
| **Critical** (게임 진행 불가) | 24시간 |
| **High** (밸런스 붕괴) | 1주 |
| **Medium** (버그) | 다음 패치 (격주) |
| **Low** (UX) | 다음 시즌 |

---

## 21. 수익화 전략

### 21.1 비즈니스 모델

| 모델 | 설명 | 적용 |
|------|------|------|
| **프리미엄** | 유료 판매 | Steam Early Access ~ 1.0 |
| **F2P** | 무료 + 인앱 | 모바일 (Phase 7+) |
| **배틀패스** | 시즌 보상 | 모든 플랫폼 |

### 21.2 초기 가격 전략

| 플랫폼 | 가격 | 비고 |
|--------|------|------|
| Steam Early Access | $14.99 | 런칭 할인 20% → $11.99 |
| Steam 1.0 | $19.99 | EA 구매자 무료 업그레이드 |
| Mobile | F2P | 광고 옵션 + IAP |
| Console | $19.99 | 동시 출시 검토 |

### 21.3 인앱 구매

| 카테고리 | 예시 | 가격대 |
|----------|------|--------|
| **캐릭터 스킨** | 외형 | $3~10 |
| **슬라임 스킨** | 슬라임 형태 변경 | $2~5 |
| **이펙트** | 스킬 이펙트 | $1~3 |
| **이모트** | 승리 포즈, 댄스 | $2~4 |
| **배틀패스** | 시즌 보상 | $9.99/시즌 |
| **번들** | 패키지 | $15~30 |

### 21.4 원칙

| 원칙 | 설명 |
|------|------|
| **No P2W** | 스탯 영향 없는 꾸미기만 |
| **모든 맵/모드 무료** | 콘텐츠 분리 없음 |
| **드랍 확률 공개** | 뽑기 있을 시 명시 (한국법 준수) |
| **거래 가능 아이템 없음** | RMT 방지 |
| **환불 정책** | Steam/스토어 정책 준수 |

---

## 22. 리스크 관리

### 22.1 주요 리스크

| 리스크 | 영향 | 확률 | 대응 |
|--------|------|------|------|
| **밸런스 붕괴** (왕 OP/약화) | 높음 | 중 | Remote Config 즉시 패치 |
| **네트워크 지연 이슈** | 높음 | 중 | 지역 서버 확충, 롤백 보정 |
| **매치메이킹 풀 부족** | 높음 | 중 | 봇 백필, 모드 통합 |
| **핵/치팅** | 중 | 높음 | EAC + 서버 권위 |
| **장르 피로** (Among Us류) | 중 | 중 | USP 강조 마케팅 |
| **모바일 성능 미달** | 중 | 중 | 그래픽 옵션, 사양 분기 |
| **Photon 비용 폭증** | 중 | 낮음 | 자체 서버 마이그레이션 검토 |
| **인디 마케팅 실패** | 높음 | 중 | 스트리머 시드, 데모 노출 |

### 22.2 의존성 리스크

| 의존성 | 백업 계획 |
|--------|----------|
| Photon Fusion 2 | Mirror / Netcode for GameObjects 추상화 |
| Supabase | 표준 PostgreSQL → 셀프호스팅 가능 |
| Unity | 코드 모듈화로 엔진 전환 비용 최소화 |

---

## 23. 부록

### 23.1 용어 정리

| 용어 | 의미 |
|------|------|
| **점령 (Capture Point)** | 맵의 특정 지역을 차지하는 것 |
| **포획 (Capture Player)** | 적 플레이어를 게임에서 제외시키는 것 |
| **붙들기 (Hold)** | 적을 그 자리에 고정시키는 것 |
| **공수전환** | 공격↔도주 역할 전환 |
| **왕** | 최종 포획 권한을 가진 상태 |
| **슬라임** | 특수 이동 스킬 상태 |
| **MMR** | Matchmaking Rating (Glicko-2 기반) |
| **백필 (Backfill)** | 매치 중 이탈자 자리 대체 |
| **롤백 (Rollback)** | 지연 보정 위한 상태 되감기 |

### 23.2 참고 자료

| 자료 | 링크/출처 |
|------|----------|
| Unity 6 매뉴얼 | docs.unity3d.com |
| Photon Fusion 2 | doc.photonengine.com/fusion |
| Supabase | supabase.com/docs |
| Glicko-2 | glicko.net |
| Easy Anti-Cheat | easy.ac |

### 23.3 변경 이력

| 버전 | 날짜 | 변경 내용 |
|------|------|----------|
| 1.0 | 2026-02-09 | 초안 작성 |
| 2.0 | 2026-07-03 | 전면 개정 — Unity 6 LTS 전환, 네트워킹/백엔드/안티치트/텔레메트리/접근성/라이브옵스/리스크 섹션 신설, 로드맵 현실화 (2027 EA → 2028 1.0), 매치메이킹·이탈 페널티·핑 시스템·왕 승계·블리츠/카오스 세부화, 밸런스 KPI 측정 기준 명시 |

---

## 문서 끝

> 이 문서는 개발 진행에 따라 지속적으로 업데이트됩니다.
> 변경 시 §23.3 변경 이력에 반드시 기록.
