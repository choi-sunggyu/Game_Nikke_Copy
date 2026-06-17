# VIPER — Venom Reptile Trainer / SR Sniper

> *"I don't miss. I just decide who survives."*

**Faction:** Serpentis Protocol  
**Class:** Venom Reptile Trainer / SR Sniper  
**Role:** Long Range / Control  
**Specialty:** Toxic Manipulation / Recon  
**Classified File:** SP-VPR-77  
**Burst Number:** 3

---

## 1. 기본 정보

| 항목 | 값 |
|---|---|
| 코드명 | VIPER S |
| 의상 컨셉 | 검정·녹색 뱀 비늘 패턴 바디슈트 (Reptile-Scale Bodysuit) |
| 액세서리 | Snake Hair Ornament, Shoulder Emblem, Venom Capsule Belt, Handler Tools |
| 컬러 | Black + 형광 Toxic Green |
| Theme | Toxic / Venom / Serpent |

---

## 2. 컨셉 / 배경

Serpentis Protocol 의 비밀 작전 요원. 독사 사육사 출신 + SR 저격수 — 사살할 대상과 살릴 대상을 본인이 결정한다는 캐릭터성. 매 발의 사격이 "독" 을 적에게 주입.

뱀의 시야(Recon)와 정밀 사격(Long Range) 의 결합. 멀리서 적의 약점을 분석하고, 한 발로 결정. 풀버스트 단계의 핵심 결정타 캐릭터.

---

## 3. 무기 — Venomous SR (Specialized Sniper Rifle)

### 모듈 구성
| 모듈 | 역할 |
|---|---|
| Suppressor Module | Muzzle Signature + Toxic Trace 감소 |
| Barrel | Carbon-Infused — 최대 사거리/안정성 |
| Venom Core | Toxic Energy Core — Venom 시스템 동력 |
| Magazine / Cartridge | Venom Cartridge 장전 |
| Stock | Adjustable Combat Stock — 정밀 컨트롤 |

### Venom Cartridge
> "Contains stabilized venom toxin. Enhances impact damage and applies toxic effect."

### Sniper Scope — Viper Custom Optics
- **8–16× Variable Zoom**
- Toxic Range Filter
- Target Analysis HUD
- Anti-Reflection Coating

---

## 4. 능력치 (코드 폴백 / `Viper.cs`)

| 항목 | 값 |
|---|---|
| MaxHP | 100 (BALANCE_TABLE 권장 800) |
| MaxShield | 50 (권장 400) |
| Bullet Count | 5 |
| Reload Time | 1.0s (권장 2.0s) |
| Attack Damage | 50 (권장 250) |
| **maxChargeTime** | 1.13s (차지샷 최대 시간) |
| 차지 데미지 배율 | 0~1.5× (Lerp) |
| Bullet Speed | 800 |
| Charging Burst Gauge | 20 / 발 |
| Burst Cooltime | 20s |
| Burst Number | 3 |

### 차지샷 메커니즘
```csharp
private void FireChargedBullet(float chargeRatio)
{
    // ... worldTarget 계산 ...

    Vector3 finalDir = (worldTarget - muzzlePoint.position).normalized;
    GameObject bullet = bulletPool.Get(muzzlePoint.position, Quaternion.identity);

    // 차지 비율 0 → 1 에 따라 데미지 0 → 1.5배 Lerp
    float chargedDamage = attackDamage * attackDamageMultiplier
                        * Mathf.Lerp(0f, 1.5f, chargeRatio);
    bullet.GetComponent<BulletBase>()?.Init(this, chargedDamage, bulletSpeed, finalDir, chargingBurstGauge);
}
```

차지 입력 처리:
- `InputManager.OnFirePress` → `HandleFirePress` → `isFireHeld = true`
- `Viper.TryFire` → 차지 시작 (`chargeStartTime = Time.time`)
- `InputManager.OnFireRelease` → `HandleFireRelease` → 차지 비율 계산 후 발사

**짧게 톡톡 = 차지 0% (낮은 데미지) + Burst Gauge 빠르게 충전** ★ Viper 의 핵심 운용

---

## 5. 스킬

| 단계 | 이름 | 효과 | 코드 상태 |
|---|---|---|---|
| PASSIVE | **Venom Carrier** *(가칭)* | 사격 시 일정 확률로 적에게 독 누적 (DOT) | ❌ TODO |
| SKILL 1 | **Toxic Mark** *(가칭)* | 한 적을 표식, 그 적이 받는 피해 증가 | ❌ TODO |
| SKILL 2 | **Snake Eyes** *(가칭)* | 자기 자신 치명타율 일시 증가 + 사거리 확장 | ❌ TODO |
| ULTIMATE | **Apex Strike** | 살아있는 적 중 HP 최고 적에게 단발 ×20 데미지 빔 | ✅ 구현 완료 |

### ULTIMATE 코드 — Apex Strike (UseBurst)

```csharp
public override void UseBurst()
{
    if (waveManager == null) return;

    // HP 가장 높은 적 탐색 (동률 시 무작위 선택)
    EnemyBase target = null;
    float highestHp = float.MinValue;
    List<EnemyBase> topTargets = new List<EnemyBase>();

    foreach (var enemy in waveManager.ActiveEnemies)
    {
        if (enemy == null || !enemy.IsAlive) continue;
        if (enemy.Hp > highestHp)
        {
            highestHp = enemy.Hp;
            topTargets.Clear();
            topTargets.Add(enemy);
        }
        else if (Mathf.Approximately(enemy.Hp, highestHp))
        {
            topTargets.Add(enemy);
        }
    }

    if (topTargets.Count > 0)
        target = topTargets[Random.Range(0, topTargets.Count)];

    if (target == null) return;

    // 발당 데미지의 ★20배 단발 ★
    float burstDamage = attackDamage * attackDamageMultiplier * 20f;
    target.TakeDamage(burstDamage); // 사거리 보너스 없음 (직격기)

    // 빔 이펙트 — 총구에서 타겟까지
    GameObject beam = Instantiate(viperBeamPrefab, muzzlePoint.position, Quaternion.identity);
    beam.GetComponent<ViperBeamEffect>()?.Fire(muzzlePoint.position, target.transform.position);
}
```

> ULTIMATE 컨셉: **보스 / 엘리트 결정타** — HP 최고 적을 노리므로 보스전에서 가장 효과적.

---

## 6. 개발자 노트

### 6-1. 파일 경로
- **코드:** `Assets/Scripts/Character/Unit/Viper.cs`
- **데이터:** `Assets/ScriptableObjects/Characters/Viper_Data.asset` (생성 필요)
- **빔 이펙트:** `Assets/Scripts/Character/Skills/ViperBeamEffect.cs`
- **프리팹:** `Assets/Prefabs/ViperBeam.prefab`

### 6-2. 차지샷 시스템 — Viper 만의 고유 메커닉
- `InputManager.OnFirePress / OnFireRelease` 별도 구독
- `isCharging`, `chargeStartTime`, `hasPlayedCharging` 상태 변수
- `PlayChargingSound()` 는 클릭당 1회만 (`hasPlayedCharging` 가드)
- `OnReloadComplete()` 오버라이드 — 리로드 후 마우스 누른 상태면 즉시 사격 재개

### 6-3. Plan B 사격 보정 (허공 사격 시 시차 방지)
```csharp
// 적 명중하면 그 지점, 허공이면 가상 평면(Z=20) 으로 보정
float defaultEnemyZ = 20f;
Plane virtualPlane = new Plane(Vector3.back, new Vector3(0, 0, defaultEnemyZ));
if (virtualPlane.Raycast(camRay, out float dist))
    worldTarget = camRay.GetPoint(dist);
```

NIKKE 원작 처럼 적이 배치된 깊이 평면에 사격 방향 보정.

### 6-4. 사거리 메커닉 — SR 의 적정 사거리
- WeaponType: `SR` → 적정 사거리 = `DistanceZone.Far` (×1.5 보너스)
- 차지샷 × 사거리 보너스 조합 = **발당 최대 250 × 1.5 × 1.5 = 562.5** 데미지

### 6-5. CharacterAI 의 Viper 분기
- `isViper = owner is Viper` — Awake 시 판별
- AI 자동 사격 시 일반 단발과 다른 처리:
  ```csharp
  if (isViper)
  {
      float dist = Vector2.Distance(crossHairRect.position, targetScreenPos);
      if (dist <= viperFireThreshold) owner.TryFireAtTarget(worldTarget);
      else owner.TryFire(); // 차지 누적
  }
  ```

### 6-6. ULTIMATE 의 단일 인자 TakeDamage
- `target.TakeDamage(burstDamage)` — `TakeDamage(damage, weaponType)` 가 아닌 단일 인자
- **사거리 보너스 적용 안 함** (이미 ×20 배율이라 보너스 중복 방지)
- ViperBeamEffect 가 시각 피드백 단독 처리

### 6-7. 시트 애니메이션 (15장 5+5+5)
- 차지 자세 (sprite 4 또는 5) 가 핵심 — 짧게 톡톡 시 자주 보이는 자세
- 무릎 꿇은 정밀 사격 자세 (IDLE) → 일어서서 사격 (SHOOTING)

### 6-8. 사운드
- `singleShotClip` — 단발음 (suppressor 효과로 약한 폭발음)
- `chargingClip` — 차지 시 ↑ 음정 상승
- `reloadClip` — Venom Cartridge 교체 사운드
- `OnFireRelease` 시 차지 음악 정지 + 발사음 재생

### 6-9. TODO
- [ ] **능력치 BALANCE_TABLE 반영** — MaxHP 800, MaxShield 400, Attack 250, Reload 2.0s
- [ ] **PASSIVE Venom Carrier** — DOT (Damage Over Time) 시스템 필요. StatusEffect.DotEffect 도입 후
- [ ] **SKILL 1 Toxic Mark** — 표식 효과 (StatusEffect.MarkedEnemy)
- [ ] **SKILL 2 Snake Eyes** — 자기 자신 buffId 로 임시 치명타율 증가
- [ ] **Apex Strike 빔 이펙트 강화** — Venom Green 컬러 + 독액 잔여 효과
- [ ] **스코프 줌인 시 화면 효과** — Toxic Range Filter (녹색 비네팅) + Target Analysis HUD (적 HP 표시)

### 6-10. 의존성
- `WaveManager.ActiveEnemies` — UseBurst 의 HP 최고 적 탐색
- `ViperBeamEffect` — 빔 이펙트 컴포넌트
- `viperBeamPrefab` — 인스펙터 할당
- `ScopeCrossHair` — 스코프 줌인 + 차지 게이지 UI
- `BulletBase` — 차지 비율 반영된 데미지로 Init

---

## 부록 — Viper 운용 가이드 (밸런스 참고)

### 톡톡 사격 (수동 운용)
- 짧게 클릭 → 차지 0~10% → 발당 ~25 데미지
- **장점:** Burst Gauge 빠르게 충전 (발당 +20)
- **단점:** 데미지 낮음 (DPS = 25 × 5발 ÷ (리로드 포함 6초) ≈ 21)

### 풀차지 사격 (AI / 의도된 운용)
- 1.13s 차지 → 차지 1.5× × 사거리 1.5× = 발당 562 데미지 (적정 zone)
- DPS = 562 × 5 ÷ (1.13 × 5 + 2.0 리로드) ≈ 370
- **장점:** 강력한 단발
- **단점:** 차지 시간 동안 사격 불가

→ 사용자가 상황에 따라 두 운용을 전환할 수 있는 게 Viper 의 핵심 매력.
