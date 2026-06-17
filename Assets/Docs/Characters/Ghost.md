# GHOST — Student Reporter / RL Specialist

> *"The truth always survives the dust."*

**Faction:** Independent  
**Class:** Student Reporter / RL Specialist  
**Codename:** TRUTH SEEKER  
**Burst Number:** 1

---

## 1. 기본 정보

| 항목 | 값 |
|---|---|
| 코드명 | GHOST |
| 나이 / 키 | 17세 / 154cm |
| 생일 | 04/18 |
| Role | RL Specialist |
| Weapon | Reporter Launcher |
| 의상 컨셉 | 흰 PRESS 코트, 카메라, 마이크, 노란 PRESS 완장 |
| 부가 장비 | Field Notebook, Lens Cases, Microphone, Camera Strap |

---

## 2. 컨셉 / 배경

> "A fearless student reporter who documents the frontlines others can't reach. Equipped with a custom RL system that records, analyzes, and delivers the truth — no matter the cost. She believes every witness matters."

전장의 진실을 기록하는 학생 기자. 단순한 폭격형 RL 캐릭터가 아니라 **임팩트 분석 + 데이터 링크** 기능을 갖춘 정보전 특화 무기를 운용. 노란 PRESS 완장 + 카메라 스트랩은 그녀의 신념을 상징하며, 사격 후 적의 정보를 실시간으로 데이터링크로 전송.

작은 키(154cm)와 학생 컨셉이지만 강력한 RL 을 다루는 갭이 매력 포인트. 무릎보호대(knee pad)는 IDLE/SHOOTING/RELOADING 모든 자세에서 무릎을 꿇는 사격 스타일.

---

## 3. 무기 — EYEWITNESS RL-7

| 항목 | 값 |
|---|---|
| Type | Reporter Launcher (RL) |
| Caliber | 70mm Smart Capsule |
| Max Range | 1500m |
| Guidance | Imaging Lock / Manual |
| Recording | 4K 60fps / Telemetry |
| Weight | 8.7kg |
| Length | 1120mm |
| Special | Impact Analysis / Data Link |

### 모듈 분해
| 모듈 | 역할 |
|---|---|
| Front Lens Housing | Focus Ring / Stabilizer Ring |
| Loading / Ammo Chamber | Smart Capsule 장전 |
| Recording Sensor Module | 4K 60fps 영상 기록 |
| Rear Interface Screen | Target Data / Distance HUD |
| Loading Module | Insert / Lock 메커니즘 |

---

## 4. 능력치 (코드 폴백 / `Ghost.cs`)

| 항목 | 값 |
|---|---|
| MaxHP | 100 (현 코드, BALANCE_TABLE 권장 1000) |
| MaxShield | 50 (권장 500) |
| Bullet Count | 120 → RL 변경 시 4 권장 |
| Reload Time | 1.0s → RL 권장 3.0s |
| Attack Damage | 20 → RL 권장 400 |
| Fire Rate | 1/20 (0.05s) — RL 은 fireRate 무관 (차지 시간이 사격 간격) |
| Bullet Speed | 500 → RL 권장 400 |
| Charging Burst Gauge | 5 → RL 권장 120 |
| Burst Cooltime | 15s |
| Burst Number | 1 |

> ⚠️ Ghost 의 weaponType 만 RL 로 전환되었고 능력치는 AR 시절 값 그대로. BALANCE_TABLE §1-1 의 RL 행 기준으로 능력치 조정 필요.

---

## 5. 스킬

| 단계 | 이름 | 효과 | 코드 상태 |
|---|---|---|---|
| PASSIVE | **Field Vision** *(가칭)* | 보유 캐릭터 사거리 보너스 (Telemetry) | ❌ TODO |
| SKILL 1 | **Live Coverage** *(가칭)* | 적의 위치를 실시간 표시 / Hit Indicator | ❌ TODO |
| SKILL 2 | **Impact Analysis** | 직격 시 적 약점 노출 (다음 N초간 받는 피해 증가) | ❌ TODO |
| ULTIMATE | **Truth Bomb** | 적 전체 2초 스턴 + 팀 HP 20% 회복 + FlashEffect | ✅ 구현 완료 |

### ULTIMATE 코드 — Truth Bomb (UseBurst)

```csharp
public override void UseBurst()
{
    var waveManager = FindAnyObjectByType<WaveManager>();
    var characterManager = FindAnyObjectByType<CharacterManager>();
    if (waveManager == null || characterManager == null) return;

    // ① 화면 플래시 이펙트
    var enemies = new List<EnemyBase>(waveManager.ActiveEnemies);
    FlashEffect.Instance?.TriggerEnemyFlash(enemies);

    // ② 전체 적 2초 스턴
    foreach (var enemy in enemies)
    {
        if (enemy != null && enemy.IsAlive)
            enemy.ApplyStun(2f);
    }

    // ③ 팀 전체 HP 20% 회복
    foreach (var character in characterManager.Characters)
    {
        if (character != null && character.IsAlive)
            character.Heal(character.MaxHp * 0.2f);
    }
}
```

---

## 6. 개발자 노트

### 6-1. 파일 경로
- **코드:** `Assets/Scripts/Character/Unit/Ghost.cs`
- **데이터:** `Assets/ScriptableObjects/Characters/Ghost_Data.asset` (생성 필요)

### 6-2. RL 메커닉 — Ghost 가 RL 의 유일한 캐릭터
- **`TryFire / TryFireAtTarget` 의 fireRate 우회 분기**
  ```csharp
  // RL 은 차지 시간이 사격 간격 역할 → fireRate 무관
  if (weaponType != WeaponType.RL && Time.time < NextFireTime) return;
  ```
- **`CharacterManager.HandleFire`** 에서 RL 이벤트 우회 (매 프레임 발사 방지)
- **`CharacterManager.HandleIdle`** 에서 RL 이벤트 우회 (강제 reload 방지)
- **`LauncherCrossHair`** 가 차지 사이클 + 자동 발사 단독 관리
- **`BulletBase.HandleCollision`** 에서 RL 스플래시 데미지 (`Physics.OverlapSphere` 반경 3유닛, 70% 데미지)

### 6-3. 크로스헤어 — LauncherCrossHair
- 차지 게이지 UI (`chargeProgressBar`, `chargePercentText`, `chargeGlow`)
- `maxChargeTime = 2.0f` (LauncherCrossHair 인스펙터)
- 자동 재차지: reload 종료 시점에 마우스 누른 상태면 즉시 차지 시작
- 차지 시작 시 `owner.ChangeState(Fire)` 호출 → 사격 자세 즉시 잡기
- ResetCharge 시 Fire→Idle 자동 복귀

### 6-4. AI 차지 시뮬레이션 — `CharacterAI.HandleLauncherCharge`
- `launcherChargeTime = 2.0f` (CharacterAI 인스펙터)
- 첫 진입 시 발사 방향 잠금 (`launcherChargedTarget`)
- 차지 도중 reload/사망 시 자동 취소

### 6-5. 사운드
- 사용자가 의도한 RL 사운드 (장전 클릭 + 발사 폭발음) 별도 할당 필요
- 현재는 Ghost AR 시절의 `singleShotClip`, `reloadClip` 유지

### 6-6. TODO
- [ ] **능력치 RL 기준으로 재조정** — HP 1000+, 탄창 4, attackDamage 400 등 BALANCE_TABLE §1-1 반영
- [ ] **PASSIVE / SKILL 1 / SKILL 2 구현** — Field Vision, Live Coverage, Impact Analysis
- [ ] **스플래시 이펙트 프리팹** — `Physics.OverlapSphere` 시각화 (폭발 + 충격파)
- [ ] **데이터 링크 UI** — Recording / Telemetry / Target Data 컨셉을 RL 사격 시 시각 피드백으로
- [ ] **차지 시간 통합** — `LauncherCrossHair.maxChargeTime` ↔ `CharacterAI.launcherChargeTime` 동기화 또는 SO 로 이전

### 6-7. 의존성
- `LauncherCrossHair` — 수동 차지 사이클
- `CharacterAI` (`isLauncher`, `HandleLauncherCharge`) — 비활성 시 자동 사격
- `BulletBase.ApplyRocketSplash` — 폭발 광역 피해
- `CharacterManager.HandleFire/HandleIdle` — RL 우회
- `FlashEffect` — Truth Bomb 화면 플래시
