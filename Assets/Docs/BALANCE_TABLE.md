# Project BUSTER — Balance Table

> 게임 밸런스의 단일 진실(SSoT). 캐릭터 SO, EnemyBase Initialize, WeaponSpecs 등의 수치는 모두 이 문서를 기준으로 한다.
> 수치 조정 시 이 문서를 먼저 갱신하고 코드/SO 에 반영.
> 마지막 갱신: 2026-06

---

## §0. 설계 원칙

1. **적정 사거리 DPS 평균 ≈ 450** — 무기 종류가 달라도 적정 사거리에서의 가치는 비슷
2. **무기 특성 차별화** — 단발 강한 무기는 fire rate 느림, 다발 약한 무기는 fire rate 빠름
3. **한 클립 = 게이지 ~100%** — 한 사이클(클립 비울 시간) 안에 버스트 게이지 80~100% 차도록 역산
4. **캐릭터 HP는 사거리 반비례** — 근거리 무기 = 탱커 / 원거리 무기 = 딜러 (위치별 위험도 보상)
5. **적 체력 3티어** Normal / Elite / Boss = 1 / 10 / 50 비율

---

## §1. 무기 종류 데이터

### 1-1. 핵심 스펙

| 무기 | 데미지 | 탄창 | Fire Rate | Reload | 게이지/발 | Bullet Speed |
|---|---|---|---|---|---|---|
| **SG** (샷건) | 150 | 8 | 1.00 s | 2.5 s | 60 | 600 |
| **SMG** (기관단총) | 20 | 100 | 0.06 s | 1.5 s | 5 | 500 |
| **AR** (소총) | 25 | 30 | 0.08 s | 1.0 s | 15 | 500 |
| **MG** (머신건) | 15 | 300 | 0.02 ~ 0.33 s (스핀업) | 1.5 s | 1.5 | 500 |
| **SR** (저격) | 250 | 6 | 1.00 s (차지 1.13s) | 2.0 s | 80 | 1000 |
| **RL** (런처) | 400 | 4 | 2.50 s | 3.0 s | 120 | 400 |

### 1-2. 적정 사거리 + DPS 계산

| 무기 | 적정 zone | 기본 DPS | 적정 zone DPS (×1.5) | 비고 |
|---|---|---|---|---|
| **SG** | Close | 150 | **225** | 단발 광폭, 다중 산탄으로 보완 가능 (향후) |
| **SMG** | Close | 333 | **500** | 빠른 단순 연사 |
| **AR** | Mid | 313 | **470** | 균형형 표준 |
| **MG** | Mid | 750 (최대 시) | **1125** | 스핀업 필요 + 정조준 어려움 |
| **SR** | Far | 250 | **375** (차지 시 563) | 차지샷으로 보너스 가산 |
| **RL** | 무관 | 160 | **160** (단일) | 광역으로 다중 타격 시 ×N |

> **MG 가 가장 높은 DPS**: 스핀업 5발 필요 + 명중률 낮음 + 컨트롤 어려움이 패널티
> **RL 단일 DPS 낮음**: 다중 적 대상이면 가장 강력. NIKKE 의 보스 광역 폭딜 컨셉
> **SR 의 진짜 가치**: 차지샷 × 사거리 보너스 = 발당 567 → 빠른 처치

### 1-3. 버스트 사이클

| 무기 | 1 클립 사격 시간 | 1 클립 게이지 충전 | 게이지 80% 도달까지 |
|---|---|---|---|
| SG | 8.0 s | 480 | 6.7 s (1 클립으로 96%) |
| SMG | 6.0 s | 500 | 4.8 s (1 클립으로 100%) |
| AR | 2.4 s | 450 | 2.1 s |
| MG | ~6.0 s | 450 | ~5.3 s |
| SR | 6.0 s | 480 | 5.0 s |
| RL | 10.0 s | 480 | 8.3 s |

→ 모든 무기가 **5~10초 내 한 버스트** 가능. 팀 전체로는 더 빠르게.

### 1-4. 게이지 max = 500 기준

각 무기의 "1 클립 = 게이지 ~100%" 가 디자인 의도. 한 사이클 안에 자기 차례의 버스트를 한 번 발동할 수 있도록.

---

## §2. 캐릭터 (5명 확정 구성)

### 2-1. 팀 구성

| 슬롯 | 캐릭터 | 무기 | 버스트 | HP | 쉴드 | 역할 / 컨셉 |
|---|---|---|---|---|---|---|
| 1 | **Ghost** | AR | 1 | 1000 | 500 | 균형 어태커 |
| 2 | **Trend** | AR | 2 | 1100 | 500 | **Supporter / Buffer** (Eclipse Union, 인플루언서) |
| 3 | **Titan** | MG | 2 | 1500 | 800 | 메인 딜러 / 탱커 |
| 4 | **Viper** | SR | 3 | 800 | 400 | 정밀 저격수 |
| 5 | **Astro** | SG | 3 | 1300 | 700 | **Close-Range DPS / Crowd Control** (Eclipse Union, 중력) |

### 2-2. 버스트 분포

| 버스트 | 인원 | 캐릭터 |
|---|---|---|
| 1버스트 | 1명 | Ghost |
| 2버스트 | 2명 | Trend, Titan |
| 3버스트 | 2명 | Viper, Astro |

> **버스트 사이클**: 1 → 2 (Trend 또는 Titan 선택) → 3 (Viper 또는 Astro 선택) 순서로 발동. 같은 단계에 2명 있으면 상황별 선택 폭.
> **HP/쉴드 분배 의도**: 근거리(SG, MG) 캐릭터가 적과 가까워 위험 → 더 두꺼움. 원거리(SR) 는 후방 안전 → 얇음. Trend 는 지원형이라 방어력 낮음(설정).

### 2-3. 캐릭터별 ULTIMATE (UseBurst)

| 캐릭터 | ULTIMATE | 효과 |
|---|---|---|
| Ghost | (기존) | 적 전체 2초 스턴 + 팀 HP 20% 회복 |
| Trend | **Trending Now** | 아군 전체 공격력 ×1.5 + 치명타 +20% (10초) |
| Titan | (기존) | 팀 공격력 ×1.2 (10초) + 별 자동 공격 |
| Viper | (기존) | HP 최고 적에게 단발 ×20 데미지 |
| Astro | **Supernova** | 광역 5초간 0.5초마다 적 전체에 attackDamage×2 피해 |

### 2-4. Astro / Trend 능력치 (코드 폴백 + SO 기준)

`Astro_Data` (SG, 3버스트):
```
weaponType         = SG
maxHp              = 1300
maxShield          = 700
maxBulletCount     = 8
reloadTime         = 2.5
attackDamage       = 150
fireRate           = 1.0
bulletSpeed        = 600
chargingBurstGauge = 60
burstCoolTime      = 20
burstNumber        = 3
```

`Trend_Data` (AR, 2버스트):
```
weaponType         = AR
maxHp              = 1100
maxShield          = 500
maxBulletCount     = 30
reloadTime         = 1.0
attackDamage       = 20
fireRate           = 0.08
bulletSpeed        = 500
chargingBurstGauge = 15
burstCoolTime      = 18
burstNumber        = 2
```

### 2-2. 크리티컬 (공통)

| 속성 | 값 |
|---|---|
| criticalRate | 15% (0.15) |
| criticalMultiplier | 1.5x |

향후 캐릭터별 차별화 가능 (Viper 만 25% 같은 식).

### 2-3. 각 캐릭터의 특수 메커닉

| 캐릭터 | 특수 필드 | 값 |
|---|---|---|
| Titan | minFireRate | 0.014 (1/70 RPS) |
| Titan | maxFireRate | 0.333 (1/3 RPS) — 스핀업 시작값 |
| Titan | 스핀업 도달 발수 | 5발 |
| Viper | maxChargeTime | 1.13 s |
| Viper | 차지 데미지 배율 | 1.0 → 2.5 |

---

## §3. 적 데이터

### 3-1. 적 등급별 체력 / 데미지 배율

| 등급 | HP 배율 | 데미지 배율 | 예상 처치 시간 (AR 적정 사거리) |
|---|---|---|---|
| **Normal** | ×1 | ×1 | 1~2초 |
| **Elite** | ×10 | ×1.5 | 10~15초 |
| **Boss** | ×50 | ×2.0 | 50~70초 |

### 3-2. 적 종류별 기본 스펙 (Normal 등급 기준)

| 적 | 행동 | HP | 데미지 | 공격 주기 | 비고 |
|---|---|---|---|---|---|
| **EnemyA** | 낙하 → 고정 → 레이저 | 500 | 80 | 4.0 s | 텔레그래프(경고원 1.5s) — 큰 한방 |
| **EnemyB** | 측면 진입 → 위치 변경 → 사격 | 600 | 25 | 0.5 s | 짧은 주기 연사 |
| **EnemyC** | 근접 돌진 추적 | 400 | 50 | 1.0 s | 접근 시 멜리 |

### 3-3. Elite 등급 (×10 HP, ×1.5 데미지)

| 적 | HP | 데미지 |
|---|---|---|
| Elite EnemyA | 5,000 | 120 |
| Elite EnemyB | 6,000 | 38 |
| Elite EnemyC | 4,000 | 75 |

### 3-4. Boss 등급 (×50 HP, ×2.0 데미지)

| 적 | HP | 데미지 |
|---|---|---|
| Boss (모든 종류) | 25,000 ~ 30,000 | 160 |

**보스 페이즈 분기**: HP 100% / 50% / 25% 시점에 패턴 변화 (향후 구현).

---

## §4. 검증 — 한 사이클 시뮬레이션

가정: Ghost(AR) 가 Mid zone 의 Normal 적(HP 500) 사격.

```
fireRate    = 0.08 s
damage/발   = 25
사거리 보너스 = ×1.5 (Mid 일치)
크리티컬 15% × ×1.5 = 평균 1.075
실효 발당 데미지 = 25 × 1.5 × 1.075 ≈ 40

500 HP / 40 = 12.5 발
12.5 × 0.08 s = 1.0 초 처치  ✓
```

Elite (HP 5000) 의 경우 → 10초 처치. 의도와 일치.

Titan(MG) 스핀업 풀가동 후 Boss(HP 25000):
```
fireRate = 0.014 s
damage  = 15 × 1.5 (사거리) × 1.075 (크리) ≈ 24
DPS    ≈ 1715
25000 / 1715 = 14.6 초  ★ Titan 단일로 보스 14초 처치
```

→ **너무 빠름**. 팀 5명 동시 사격 시 1~3초. 보스가 컷씬/패턴 전환 시간 줄 여유 없음.

**조정 제안**:
- Boss HP × 4 → 100,000 으로 (Titan 단일 60초)
- 또는 Titan damage 15 → 10 으로 (Titan 단일 25초 → 팀 5초)

→ **결정**: Boss HP = 100,000 으로 조정.

---

## §5. 적용 체크리스트

### 5-1. 캐릭터 SO 에 입력할 값

`Ghost_Data.asset`:
```
weaponType         = AR
maxHp              = 1000
maxShield          = 500
maxBulletCount     = 30
reloadTime         = 1.0
attackDamage       = 25
fireRate           = 0.08
bulletSpeed        = 500
chargingBurstGauge = 15
burstNumber        = 1
criticalRate       = 0.15
criticalMultiplier = 1.5
```

`Titan_Data.asset`:
```
weaponType         = MG
maxHp              = 1500
maxShield          = 800
maxBulletCount     = 300
reloadTime         = 1.5
attackDamage       = 15
fireRate           = (사용 안 함 — Titan 의 min/maxFireRate 가 우선)
bulletSpeed        = 500
chargingBurstGauge = 1.5  (또는 2 — 인스펙터 정수 제약 시)
burstNumber        = 2
```

`Viper_Data.asset`:
```
weaponType         = SR
maxHp              = 800
maxShield          = 400
maxBulletCount     = 6
reloadTime         = 2.0
attackDamage       = 250
fireRate           = 1.0
bulletSpeed        = 1000
chargingBurstGauge = 80
burstNumber        = 3
```

### 5-2. 적 능력치는?

현재 `EnemyA.cs`, `EnemyB.cs`, `EnemyC.cs` 의 `Initialize()` 안 하드코딩. 다음 작업:

1. **`EnemyData.cs` SO 도입** (캐릭터처럼)
2. **`EnemyType` enum 활용** — Normal/Elite/Boss 에 따라 SO 내부에서 배율 자동 적용
3. **WaveData (ScriptableObject) 확장** — 어느 적의 어떤 등급을 스폰할지

이건 **다음 작업** 으로 분리. 이번엔 캐릭터 데이터부터 정착시키고.

---

## §6. 향후 조정 포인트

- **SG 산탄 시스템** — 1발이 다중 탄환을 발사하도록 (NIKKE 컨벤션)
- **RL 폭발 범위** — `Physics.OverlapSphere` 로 광역 피해
- **SR 헤드샷 보너스** — 약점 부위 노릴 때 ×3 같은 보너스
- **Boss 페이즈 분기** — HP % 별 다른 행동 패턴
- **밸런스 조정 로그** — 이 문서 상단에 변경 이력 기록

---

## §7. 변경 이력

| 날짜 | 변경 | 이유 |
|---|---|---|
| 2026-06 | 초기 작성 | 5명 확장 + 사거리 시스템 도입 기반 |
