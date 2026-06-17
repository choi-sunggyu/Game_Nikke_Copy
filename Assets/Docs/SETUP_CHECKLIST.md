# Unity 에디터 작업 체크리스트

> 코드 외에 **사용자가 Unity 에디터에서 직접 해야 할 작업** 목록.
> 우선순위: 🔴 즉시 / 🟡 중요 / 🟢 폴리싱

---

## 🔴 즉시 필요 — 게임이 정상 동작하려면

### A. 인스펙터 리스트 5명 모두 추가
다음 컴포넌트의 `characters` 리스트 (또는 동급) 크기를 **5** 로 늘리고 순서대로 할당:
- [ ] `CharacterManager.Characters` — Ghost / Trend / Titan / Viper / Astro
- [ ] `CameraController.Characters` — 같은 순서
- [ ] `BottomUI.characterBoxes` — 같은 순서 (각 박스 7개 슬롯 연결)

> ⚠️ 순서가 어긋나면 Z/X/C/V/B 키가 엉뚱한 캐릭터를 호출합니다.

### B. 5명 캐릭터 GameObject + 컴포넌트 확인
- [ ] Hierarchy 에 5개 캐릭터 GameObject 존재 (Ghost / Trend / Titan / Viper / Astro)
- [ ] 각 GameObject 에 해당 캐릭터 클래스 컴포넌트 부착 (Ghost.cs / Trend.cs / Titan.cs / Viper.cs / Astro.cs)
- [ ] 각 GameObject 에 `CharacterAI` 컴포넌트 부착
- [ ] 각 GameObject 에 SpriteRenderer 부착

### C. 캐릭터별 인스펙터 슬롯 할당 (각 캐릭터 5개씩)
공통:
- [ ] `MuzzlePoint` — 총구 Transform
- [ ] `BulletPool` — 씬의 PlayerBullet ObjectPool
- [ ] `CrossHair` — 무기별 크로스헤어 (아래 참고)
- [ ] `EnemyLayer` (CharacterBase 의 private) — "Enemy" 레이어
- [ ] `CharacterPortrait` — 캐릭터 초상화 sprite
- [ ] `CharacterSprite` — BottomUI 작은 아이콘

캐릭터별 무기-크로스헤어 매핑:
| 캐릭터 | WeaponType | CrossHair |
|---|---|---|
| Ghost | RL | **LauncherCrossHair** ★ 차지 UI 슬롯 추가 필요 |
| Trend | AR | RifleCrossHair |
| Titan | MG | MiniGunCrossHair |
| Viper | SR | ScopeCrossHair |
| Astro | SG | ShotgunCrossHair |

### D. LauncherCrossHair (Ghost RL) UI 슬롯 추가
- [ ] `chargeProgressBar` — Image (Filled, Radial 360 추천)
- [ ] `chargePercentText` — TMP_Text (0~100 % 표시)
- [ ] `chargeGlow` — Image (차지 완료 시 발광 효과)
- [ ] `maxChargeTime` — 2.0 (`CharacterAI.launcherChargeTime` 과 일치)

### E. EnemyC enemyType 확인
- [ ] `Assets/Prefabs/EnemyC.prefab` 의 `enemyType` 이 **Normal** 인지 확인
  - 만약 Boss 로 설정되어 있으면 UIManager NRE 의 원인

### F. Ghost 의 WeaponType = RL 확인
- [ ] Ghost 인스펙터의 `weaponType` 이 **RL** 로 설정 (코드 폴백 AR 이라 SO 또는 인스펙터로 덮어쓰기 필요)
- [ ] 또는 `Ghost_Data` SO 에 `weaponType = RL` 설정

---

## 🟡 중요 — 캐릭터별 데이터 정착

### G. CharacterData SO 에셋 5개 생성
**경로:** `Assets/ScriptableObjects/Characters/`

각 SO 우클릭 → Create → Character → Character Data:
- [ ] `Ghost_Data.asset`
- [ ] `Trend_Data.asset`
- [ ] `Titan_Data.asset`
- [ ] `Viper_Data.asset`
- [ ] `Astro_Data.asset`

각 SO 에 `BALANCE_TABLE.md` §2-4 의 값 입력:

**Ghost_Data:**
```
weaponType = RL
maxHp = 1000   maxShield = 500
maxBulletCount = 4   reloadTime = 3.0   attackDamage = 400
fireRate = (사용 안 함, RL)
bulletSpeed = 400
chargingBurstGauge = 120   burstCoolTime = 15
burstNumber = 1
```

**Trend_Data:**
```
weaponType = AR
maxHp = 1100   maxShield = 500
maxBulletCount = 30   reloadTime = 1.0   attackDamage = 20
fireRate = 0.08   bulletSpeed = 500
chargingBurstGauge = 15   burstCoolTime = 18
burstNumber = 2
```

**Titan_Data:**
```
weaponType = MG
maxHp = 1500   maxShield = 800
maxBulletCount = 300   reloadTime = 1.5   attackDamage = 15
fireRate = (사용 안 함)
bulletSpeed = 500
chargingBurstGauge = 2   burstCoolTime = 20
burstNumber = 2
```

**Viper_Data:**
```
weaponType = SR
maxHp = 800   maxShield = 400
maxBulletCount = 6   reloadTime = 2.0   attackDamage = 250
fireRate = 1.0   bulletSpeed = 1000
chargingBurstGauge = 80   burstCoolTime = 20
burstNumber = 3
```

**Astro_Data:**
```
weaponType = SG
maxHp = 1300   maxShield = 700
maxBulletCount = 8   reloadTime = 2.5   attackDamage = 150
fireRate = 1.0   bulletSpeed = 600
chargingBurstGauge = 60   burstCoolTime = 20
burstNumber = 3
```

### H. 각 캐릭터 인스펙터에 SO 할당
- [ ] Ghost 의 `data` 슬롯 ← `Ghost_Data`
- [ ] Trend ← `Trend_Data`
- [ ] Titan ← `Titan_Data`
- [ ] Viper ← `Viper_Data`
- [ ] Astro ← `Astro_Data`

> SO 할당 시 ApplyData() 가 코드 폴백을 덮어씁니다. SO 가 비어있으면 코드 폴백 유지.

---

## 🟡 중요 — 시트 애니메이션 (각 캐릭터 15장)

### I. 시트 이미지 임포트 설정
각 캐릭터별 5×3 (또는 15장) 시트 이미지 선택 → Inspector:
- [ ] Texture Type = **Sprite (2D and UI)**
- [ ] Sprite Mode = **Multiple**
- [ ] Pixels Per Unit = 100 (또는 캐릭터 크기에 맞게 조정)
- [ ] **Alpha Source = Input Texture Alpha** ★ (체크무늬 배경 문제 방지)
- [ ] Alpha Is Transparency = ✓
- [ ] Apply

### J. Sprite Editor 슬라이싱
각 시트:
- [ ] Sprite Editor 열기 → Slice
- [ ] Type = **Grid By Cell Count** (또는 Automatic 후 검증)
- [ ] Column × Row = **5 × 3** = 15장
- [ ] Pivot = **Bottom Center** ★ (모든 슬라이스 통일)
- [ ] Slice → Apply
- [ ] 슬라이스가 정확히 15개인지 확인

### K. animSprites 인스펙터 할당
각 캐릭터:
- [ ] CharacterBase 의 `animSpriteRenderer` 슬롯 — 비워두면 자동 탐색
- [ ] `animSprites` 배열 크기 = **15**
- [ ] 슬라이스 15개를 인덱스 0~14 순서대로 드래그 (좌→우, 위→아래)
- [ ] `frameDuration` = 0.05 (20fps)
- [ ] 인덱스 0 = idle 첫 자세 / 인덱스 14 = idle 복귀 마지막 자세 확인

---

## 🟢 폴리싱 — 사운드 / 이펙트

### L. 사운드 클립 할당 (각 캐릭터)
공통:
- [ ] `singleShotClip` — 사격 사운드
- [ ] `reloadClip` — 리로드 사운드

캐릭터별 추가:
- [ ] Titan: `spinUpClip` (스핀업), `fireLoopClip` (풀회전 루프)
- [ ] Viper: `chargingClip` (차지 음정 상승)

### M. ULTIMATE 이펙트 프리팹 할당
- [ ] **Astro**: `supernovaPrefab` — 인공 태양 폭발 이펙트
- [ ] **Trend**: `trendingStagePrefab` — 홀로그램 무대 이펙트
- [ ] **Titan**: `buffStarPrefab`, `attackStarPrefab` — 별 이펙트 (`Assets/Prefabs/`)
- [ ] **Viper**: `viperBeamPrefab` — 빔 이펙트 (`Assets/Prefabs/ViperBeam.prefab`)
- [ ] **Ghost**: FlashEffect (이미 씬에 있음)

### N. 무기별 크로스헤어 sprite 차별화
- [ ] **ShotgunCrossHair**: 산탄 패턴 (4점 X 형태 추천)
- [ ] **SubMachineGunCrossHair**: 작은 십자 (빠른 연사 느낌)
- [ ] **MiniGunCrossHair**: 큰 원 (분산 느낌)
- [ ] **LauncherCrossHair**: 큰 십자 + 차지 게이지 원

---

## 🟢 폴리싱 — UI / 시각 효과

### O. BurstSlotUI 슬롯 추가 (2버스트 / 3버스트 각 2명)
- [ ] 2버스트 슬롯 — Trend 와 Titan 동시 표시 가능하도록 슬롯 2개
- [ ] 3버스트 슬롯 — Viper 와 Astro 동시 표시 가능하도록 슬롯 2개

### P. CharacterAimLean 컴포넌트 부착
각 캐릭터 GameObject:
- [ ] `CharacterAimLean` 컴포넌트 추가
- [ ] Axis = Y (어깨 트는 yaw)
- [ ] maxLeanAngle = 15
- [ ] leanSpeed = 6

### Q. 캐릭터 위치 배치 (5명)
씬에서 5명 캐릭터 GameObject 의 transform.position 설정:
- [ ] 5명이 가로로 일정 간격 배치 (예: x = -8, -4, 0, +4, +8)
- [ ] 가운데 캐릭터 (인덱스 2 = Titan) 가 시작 시 카메라 중앙에 오도록

---

## 🟢 폴리싱 — Astro 무기 결정

### R. Astro 의 무기 컨셉 통합
- [ ] 이미지(AR-7A Stellarhunter) vs 코드(SG NOVA-12) 중 채택할 무기 결정
- [ ] 결정 후 `Astro.cs` 의 `weaponType` 변경 (필요 시)
- [ ] 무기 변경 시 능력치 + 크로스헤어 일괄 교체
- [ ] BALANCE_TABLE, Astro.md 갱신

---

## 🟢 폴리싱 — 검증 절차

### S. 게임 실행 후 한 사이클 검증
- [ ] Z/X/C/V/B 키로 5명 전환 정상 작동
- [ ] 카메라가 각 캐릭터로 부드럽게 이동
- [ ] BottomUI 박스가 5명 표시 + 활성 박스 높이 변화
- [ ] BurstSlot UI 가 1→2→3 단계 정상 진행
- [ ] Ghost(RL) — 마우스 누른 채 차지→발사→차지 자동 사이클
- [ ] Ghost(RL) — 발사 후 마우스 떼도 강제 reload 안 발생
- [ ] Astro(SG) — 산탄 5발 발사 + 가까운 적 다중 타격
- [ ] Ghost(RL) — 직격 + 주변 스플래시 (3유닛 반경)
- [ ] Viper(SR) — 톡톡 클릭으로 Burst Gauge 빠르게 충전
- [ ] Titan(MG) — 스핀업 후 풀 연사
- [ ] Trend(AR) — 표준 연사
- [ ] 각 캐릭터 시트 애니메이션 — idle→shoot→reload→idle 사이클 자연스러움
- [ ] 캐릭터별 ULTIMATE 발동 시 이펙트 + 효과 정상

### T. Console 에러 검증
- [ ] MissingReferenceException 없음
- [ ] IndexOutOfRangeException 없음
- [ ] NullReferenceException 없음
- [ ] 경고 로그 정상 (의도된 디버그 메시지 외)

---

## 📋 작업 추정 시간

| 단계 | 추정 시간 |
|---|---|
| 🔴 즉시 (A~F) | 30분 |
| 🟡 SO + 인스펙터 (G, H) | 40분 |
| 🟡 시트 임포트 + 슬라이싱 (I, J, K) | 1시간 (5명) |
| 🟢 사운드 + 이펙트 (L, M, N) | 1시간 |
| 🟢 UI + 위치 (O, P, Q) | 30분 |
| 🟢 무기 결정 (R) | 10분 |
| 🟢 검증 (S, T) | 30분 |
| **합계** | **약 4~5시간** |

> SKILLS_TODO.md 의 미구현 스킬 작업 시간 (4~5시간) 과 합치면 **총 8~10시간** 정도면 모든 작업 완료 가능.
