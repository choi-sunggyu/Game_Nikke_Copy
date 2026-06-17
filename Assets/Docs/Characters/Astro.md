# ASTRO — Space Combat Pilot

> *"Gravity never lies. It pulls everything back to the truth."*

**Faction:** Nova Alliance  
**Class:** Space Combat Pilot  
**Burst Number:** 3

---

## 1. 기본 정보

| 항목 | 값 |
|---|---|
| 코드명 | ASTRO |
| 나이 / 키 | 22세 / 168cm |
| Race | Asian / L7 |
| Specialty | Piloting, Advance |
| Vang | SQ / C4 |
| Color Palette | `#FFFFFF` `#E0E6EA` `#A7B1BF` `#1C2636` `#2B6CFF` |
| Affiliation Logo | Nova Alliance (별 5각형) |

---

## 2. 컨셉 / 배경

Nova Alliance 의 최정예 우주 전투 파일럿. 백·청 톤의 우주 전투복은 무중력 환경 적응형 설계로 Zero-G 환경에서도 안정적인 사격 자세 유지. 차분하고 절제된 표정, 길게 흩날리는 흑발이 특징.

캐릭터 배경상 "중력 조작" 능력을 가진 특수 파일럿. ULTIMATE 스킬 "Supernova" 가 이를 시각적으로 표현 — 작은 인공 태양을 생성해 적을 광역 화상 상태로 만듦.

---

## 3. 무기

### 3-A. 이미지 설정 — AR-7A "Stellarhunter" (캐릭터 시트)
| 항목 | 값 |
|---|---|
| Type | Assault Rifle |
| Caliber | 7.62×36mm FMJ |
| Operation | Electric Maglev |
| Effective Range | 10–1200m |
| Magazine | 36+1 Smart Cell |
| 모듈 | Barrel Assembly / Energy Core / Smart Scope / Modular Magazine / Stock Unit |
| 악세서리 | Holo Sight, Silencer, Angled Grip, Tactical Light, Extended Mag |

### 3-B. 게임 내 설정 — SG (Shotgun) ★ 채택
> 이미지의 AR 컨셉과 다른 SG 메커닉 사용. 향후 통합 시 결정 필요.

| 항목 | 값 |
|---|---|
| WeaponType | `WeaponType.SG` |
| 산탄 수 | 5 (`WeaponSpecs.SG_PELLET_COUNT`) |
| Spread 각도 | ±10° (`SG_SPREAD_ANGLE`) |
| 적정 사거리 | Close zone (×1.5 보너스) |

---

## 4. 능력치 (코드 폴백 / `Astro.cs`)

| 항목 | 값 |
|---|---|
| MaxHP | 1300 |
| MaxShield | 700 |
| Bullet Count | 8 |
| Reload Time | 2.5s |
| Attack Damage | 150 (탄당 = 150/5 = 30, 모두 명중 시 직격 150) |
| Fire Rate | 1.0s |
| Bullet Speed | 600 |
| Charging Burst Gauge | 60 / 발 |
| Burst Cooltime | 20s |
| Skill Cooltime | 10s |
| Critical Rate | 0.15 (15%) |
| Critical Multiplier | 1.5× |

---

## 5. 스킬

| 단계 | 이름 | 효과 | 코드 상태 |
|---|---|---|---|
| PASSIVE | **Zero-G Adaptation** | 이동 시 짧은 시간 동안 이동속도/회피율 증가 | ❌ TODO |
| SKILL 1 | **Gravity Collapse** | 전방 범위 적 끌어당기고 이동속도/방어력 감소 | ❌ TODO |
| SKILL 2 | **Singularity Blast** | 중력 핵 폭발, 끌어당긴 적에게 강타 | ❌ TODO |
| ULTIMATE | **Supernova** | 화면 중앙 인공 태양 5초 동안 0.5초마다 모든 적에게 광역 피해 + 화상 상태 | ✅ 구현 완료 |

### ULTIMATE 코드 — Supernova

```csharp
public override void UseBurst()
{
    UsedBurstThisCycle = true;
    if (waveManager == null) waveManager = FindAnyObjectByType<WaveManager>();
    if (waveManager == null) return;

    // 이펙트 (화면 중앙)
    if (supernovaPrefab != null)
    {
        Vector3 centerScreen = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 25f);
        Vector3 centerWorld  = Camera.main.ScreenToWorldPoint(centerScreen);
        Instantiate(supernovaPrefab, centerWorld, Quaternion.identity);
    }

    StartCoroutine(SupernovaRoutine());
}

private IEnumerator SupernovaRoutine()
{
    float elapsed = 0f;
    float tickDamage = attackDamage * attackDamageMultiplier * SUPERNOVA_TICK_MULTIPLIER;

    while (elapsed < SUPERNOVA_DURATION) // 5초
    {
        // ★ 매 틱마다 살아있는 적 스냅샷 후 데미지 적용 (열거 중 변경 방지)
        var snapshot = new List<EnemyBase>(waveManager.ActiveEnemies);
        foreach (var enemy in snapshot)
        {
            if (enemy == null || !enemy.IsAlive) continue;
            enemy.TakeDamage(tickDamage); // 거리 무관 단순 피해
        }
        elapsed += SUPERNOVA_TICK_INTERVAL; // 0.5초
        yield return new WaitForSeconds(SUPERNOVA_TICK_INTERVAL);
    }
}
```

**파라미터**
- `SUPERNOVA_DURATION = 5.0f` (지속 5초)
- `SUPERNOVA_TICK_INTERVAL = 0.5f` (0.5초 틱)
- `SUPERNOVA_TICK_MULTIPLIER = 2.0f` (틱당 attackDamage × 2)
- 총 데미지: 10틱 × (attackDamage × 2 × attackDamageMultiplier) = 약 3000+

---

## 6. 개발자 노트

### 6-1. 파일 경로
- **코드:** `Assets/Scripts/Character/Unit/Astro.cs`
- **데이터:** `Assets/ScriptableObjects/Characters/Astro_Data.asset` (생성 필요)
- **프리팹:** `Assets/Prefabs/Characters.prefab` 내부 Astro GameObject

### 6-2. 무기 메커닉
- **`CharacterBase.FireBullet`** 안의 SG 분기로 산탄 발사 처리
  ```csharp
  if (weaponType == WeaponType.SG) {
      SpawnShotgunSpread(fireDir); // 5발 cone
      return;
  }
  ```
- **탄당 데미지 = `attackDamage / SG_PELLET_COUNT` = 30**
- Close zone 적이 1마리 모두 명중 시 = 30 × 5 × 1.5 (사거리 보너스) = **225 dmg/발**
- 겹친 적은 동시 다중 타격 가능 (펠릿이 각자 충돌 처리)

### 6-3. 크로스헤어
- **`ShotgunCrossHair`** (`Assets/Scripts/Character/CrossHair/ShotgunCrossHair.cs`)
- RifleCrossHair 상속, sprite 만 SG 전용으로 인스펙터 할당

### 6-4. 시트 애니메이션 (15장 5+5+5)
- `CharacterBase.animSprites` 인스펙터에 15장 슬라이스 할당
- 시퀀스는 `DEFAULT_IDLE_TO_SHOOT` / `DEFAULT_SHOOT_TO_RELOAD` / `DEFAULT_RELOAD_TO_IDLE` 사용
- Fire 진입 시 ShootLoop 1회 재생 후 정지 (사격 자세 유지)

### 6-5. 사운드
- `singleShotClip` — SG 한 발음
- `reloadClip` — 펌프액션 (NIKKE 컨벤션)
- AudioSource 두 개 동적 추가 (Initialize 시)

### 6-6. TODO
- [ ] **PASSIVE Zero-G Adaptation** — 이동속도/회피율 버프. StatusEffect 시스템 도입 후 구현
- [ ] **SKILL 1 Gravity Collapse** — 적 끌어당김. `Physics.OverlapSphere` + Rigidbody 또는 transform 보간
- [ ] **SKILL 2 Singularity Blast** — 끌어당긴 적에 한정 광역 피해. SKILL 1 의 끌어당김 대상 추적 필요
- [ ] **Supernova 이펙트 프리팹** — `supernovaPrefab` 슬롯 미할당, 작은 인공 태양 시각 효과
- [ ] **무기 통합** — 이미지 AR vs 코드 SG 결정 시 능력치 + 크로스헤어 일괄 변경

### 6-7. 의존성
- `WaveManager.ActiveEnemies` — UseBurst 의 광역 피해 대상 조회
- `WeaponSpecs.SG_PELLET_COUNT`, `SG_SPREAD_ANGLE` — 산탄 파라미터
- `CharacterBase.ApplyData()` — `Astro_Data.asset` 의 능력치 자동 적용
