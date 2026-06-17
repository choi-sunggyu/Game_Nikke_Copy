# TITAN — Heavy Gunner

> *"I AM THE WALL. THE ENEMY BREAKS."*

**Faction:** Northguard  
**Class:** Heavy Gunner  
**File ID:** NG-TTN-0721  
**Status:** ACTIVE  
**Clearance Level:** S-RANK  
**Burst Number:** 2

---

## 1. 기본 정보

| 항목 | 값 |
|---|---|
| 코드명 | TITAN |
| 키 / 몸무게 | 172cm / 65kg |
| 생일 | May. 17 |
| 의상 컨셉 | Navy/Blue tactical jacket + white crop top, fingerless gloves, tactical boots |
| Faction Logo | Northguard (방패 + 다이아 마크) |
| 컬러 | Navy/Blue + 형광 블루 발광 라인 |

---

## 2. 컨셉 / 배경

Northguard 최전선의 Heavy Gunner. 방어전의 "벽" 이라는 별명처럼 적의 진격을 단신으로 막아내는 전선 유지 캐릭터. 묵직한 Gatling Cannon 을 한 손으로 다루는 압도적 피지컬과 차분하고 절제된 표정의 갭이 매력 포인트.

POSE 시트의 한국어 디렉션:
- **Cover Idle:** 우측 45도 시점, 무기 낮게 잡고 대기, 적 방향 경계, 차분하고 경계된 표정
- **Shooting:** 우측 45도 시점, 개틀링 사격 모션, 총열 회전 / 탄피 배출 / 머즐 플래시, 강한 반동에 대응하는 자세, 집중되고 공격적인 표정
- **Reloading:** 무기를 낮게 두고 정비, 총열 냉각 모듈 교체 / 탄띠 점검, 시선은 무기에 집중, 몰입된 정비 표정

---

## 3. 무기 — M-03A1 "Juggernaut" Gatling Cannon

| 항목 | 값 |
|---|---|
| Type | MG (Gatling Cannon) |
| 모듈 | Barrel Cluster / Cooling Unit / Ammo Feed / Power Core |
| 한 손 운용 | Northguard 외골격 시스템 보조 |
| 특수 메커닉 | 스핀업 (5발 동안 발사속도 점진 가속) |

---

## 4. 능력치 (코드 폴백 / `Titan.cs`)

| 항목 | 값 |
|---|---|
| MaxHP | 200 (BALANCE_TABLE 권장 1500 — 탱커) |
| MaxShield | 50 (권장 800) |
| Bullet Count | 400 |
| Reload Time | 1.5s |
| Attack Damage | 10 |
| **minFireRate** | 1/70 ≈ 0.014s (최대 RPM, 스핀업 후) |
| **maxFireRate** | 1/3 ≈ 0.333s (초기 RPM, 스핀업 전) |
| LoopStartShots | 5 (이 발수 안에서 fireRate 가 max → min 으로 Lerp) |
| Bullet Speed | 500 |
| Charging Burst Gauge | 10 / 발 |
| Burst Cooltime | 20s |
| Burst Number | 2 |

### 스핀업 코드
```csharp
private void ProcessShot()
{
    bulletCount--;
    shotsFired++;

    // 5발 동안 maxFireRate (1/3s) → minFireRate (1/70s) 로 Lerp
    currentFireRate = Mathf.Lerp(maxFireRate, minFireRate,
                      Mathf.Clamp01(shotsFired / (float)LoopStartShots));
    nextTitanFireTime = Time.time + currentFireRate;
    ...
}
```

스핀업 사운드 분기:
```csharp
if (shotsFired <= LoopStartShots) {
    spinUpSource.Play();   // 도는 중 (가속 사운드)
    singleShotSource.PlayOneShot(singleShotClip); // 발당 + pitch Lerp
}
else if (!isLooping) {
    spinUpSource.Stop();
    loopSource.Play();     // 풀회전 (루프 사운드)
    isLooping = true;
}
```

---

## 5. 스킬

| 단계 | 이름 | 효과 | 코드 상태 |
|---|---|---|---|
| PASSIVE | **Reinforced Frame** *(가칭)* | 받는 피해 감소 (탱커 컨셉) | ❌ TODO |
| SKILL 1 | **Suppression Fire** *(가칭)* | 발사 시 적의 정확도/이동속도 감소 | ❌ TODO |
| SKILL 2 | **Damage Buff Trigger** | 동맹이 버스트 사용 시 자동 40% 공격력 버프 부여, 미사용 동맹엔 20% | ✅ `UseSkill` 구현 |
| ULTIMATE | **Star Barrage** | 팀 전체 공격력 ×1.2 (10초) + 별 자동 공격 코루틴 (10초 동안 0.25초마다 별 발사) | ✅ 구현 완료 |

### UseSkill 코드 — Damage Buff Trigger
```csharp
public override void UseSkill()
{
    foreach(var ally in BattleManager.Instance.Team)
    {
        if(ally == null || !ally.IsAlive) continue;

        if(ally.UsedBurstThisCycle)
            ally.ApplyDamageBuff(1.4f, 15f, TitanBuff40); // 버스트 쓴 동맹 → 40%
        else
            ally.ApplyDamageBuff(1.2f, 15f, TitanBuff20); // 안 쓴 동맹 → 20%
    }
}
```

### ULTIMATE 코드 — Star Barrage (UseBurst)
```csharp
public override void UseBurst()
{
    // ① 팀 전체 공격력 1.2배 버프 + 별 이펙트
    foreach (var character in characterManager.Characters)
    {
        if (character == null || !character.IsAlive) continue;
        character.ApplyDamageBuff(buffMultiplier, buffDuration, TitanBuffId); // ×1.2 (10초)

        GameObject star = Instantiate(buffStarPrefab, character.transform.position, Quaternion.identity);
        star.transform.SetParent(character.transform);
        star.GetComponent<BuffStarEffect>()?.Show(buffDuration);
    }

    // ② 10초간 0.25초마다 무작위 적에게 별 공격 발사
    StartCoroutine(StarAttackRoutine());
}

private IEnumerator StarAttackRoutine()
{
    float elapsed = 0f;
    while (elapsed < buffDuration) // 10초
    {
        elapsed += starFireInterval; // 0.25초
        yield return new WaitForSeconds(starFireInterval);

        var enemies = waveManager.ActiveEnemies;
        if (enemies == null || enemies.Count == 0) continue;

        EnemyBase target = enemies[Random.Range(0, enemies.Count)];
        if (target == null || !target.IsAlive) continue;

        Vector3 spawnPos = transform.position + (Vector3)(Random.insideUnitCircle * 1.5f);
        GameObject star = Instantiate(attackStarPrefab, spawnPos, Quaternion.identity);
        float starDamage = attackDamage * attackDamageMultiplier * starDamageRatio; // 40%
        star.GetComponent<AttackStar>()?.Init(starDamage, starSpeed, target);
    }
}
```

**파라미터**
- `buffMultiplier = 1.2f`, `buffDuration = 10f`
- `starDamageRatio = 0.4f` (attackDamage × 40%)
- `starFireInterval = 0.25f`, `starSpeed = 100f`
- 10초 / 0.25초 = 40회 별 공격 (다중 적 대응)

---

## 6. 개발자 노트

### 6-1. 파일 경로
- **코드:** `Assets/Scripts/Character/Unit/Titan.cs`
- **데이터:** `Assets/ScriptableObjects/Characters/Titan_Data.asset` (생성 필요)
- **스킬 이펙트:** `Assets/Scripts/Character/Skills/AttackStar.cs`, `BuffStarEffect.cs`
- **프리팹:** `Assets/Prefabs/AttackStar.prefab`, `BuffStar.prefab`

### 6-2. 스핀업 + 사운드 시스템
- AudioSource **3개** 동적 추가 (Initialize 시): `singleShotSource`, `spinUpSource`, `loopSource`, `reloadSource`
- 스핀업 동안:
  - `spinUpSource.loop = true` (가속 사운드 무한 반복)
  - `singleShotSource.PlayOneShot()` 매 발 + pitch 0.5 → 1.0 Lerp
- 5발 이상:
  - `spinUpSource.Stop()`, `loopSource.Play()` (풀회전 루프)
- `OnFireRelease` 시 `ResetFireRate()` 호출 → 모든 사운드 정지 + shotsFired = 0

### 6-3. 시트 애니메이션 (15장 5+5+5)
- `CharacterBase.animSprites` 15장 슬라이스 인스펙터 할당 (Titan 시트)
- 시퀀스는 `DEFAULT_*` 사용 (모든 캐릭터 공통)
- 마지막 프레임(인덱스 14) = idle 자세 / 인덱스 4,5 = ShootLoop

### 6-4. 동맹 버스트 사용 추적 — `ally.UsedBurstThisCycle`
- 각 캐릭터가 `UseBurst()` 호출 시 `UsedBurstThisCycle = true` 설정
- BurstGaugeManager 가 다음 Charging 페이즈 진입 시 모든 캐릭터 리셋
- Titan 의 `UseSkill` 이 이 플래그를 보고 버프 강도 분기 (40% vs 20%)

### 6-5. TODO
- [ ] **능력치 BALANCE_TABLE 반영** — MaxHP 200 → 1500, MaxShield 50 → 800
- [ ] **PASSIVE Reinforced Frame** — 받는 피해 -20%
- [ ] **SKILL 1 Suppression Fire** — 적 정확도/이속 감소 (StatusEffect 도입 후)
- [ ] **사격 시 카메라 흔들림** — 강한 반동 표현
- [ ] **머즐 플래시 이펙트** — 형광 블루로 (캐릭터 컬러와 통일)
- [ ] **탄피 배출 이펙트** — 시트 이미지 디렉션의 강조 요소

### 6-6. 의존성
- `BattleManager.Instance.Team` — UseSkill 의 동맹 순회
- `characterManager.Characters` — UseBurst 의 팀 버프 대상
- `WaveManager.ActiveEnemies` — StarAttackRoutine 의 무작위 적 선택
- `AttackStar` / `BuffStarEffect` — 이펙트 컴포넌트
- `CharacterBase.ApplyDamageBuff` — buffId 별 독립 관리
