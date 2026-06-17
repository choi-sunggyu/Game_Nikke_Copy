# 미구현 스킬 — 설계 + 구현 가이드

> 5명 캐릭터의 PASSIVE / SKILL 1 / SKILL 2 (총 13개) 미구현 스킬을 한 곳에 정리.
> 각 스킬은 다음 4가지로 정리: **트리거 / 효과 / 코드 / 의존성**

---

## §0. 공통 인프라 — 먼저 추가해야 할 것

### 0-1. 트리거 시스템 (`SkillTriggerSystem`)
스킬 발동 조건을 일관되게 관리. 현재는 각 캐릭터가 자기 이벤트를 직접 구독해야 함.

**조건 종류:**
- `OnBattleStart` — 전투 시작 시 1회
- `OnNthShot(int n)` — N발 사격마다
- `OnEnemyKilled` — 적 처치 시
- `OnTimer(float sec)` — N초마다 반복
- `OnHpThreshold(float ratio)` — HP 임계치 도달 시

### 0-2. StatusEffect 시스템
README 약속의 상속 구조:
```
StatusEffect (abstract)
 ├── BuffEffect       (공격력/방어력/회피율 등 강화)
 ├── DebuffEffect     (이동속도/방어력 감소)
 └── DotEffect        (Damage Over Time)
```

EnemyBase / CharacterBase 가 `List<StatusEffect> activeEffects` 보유. 매 프레임 또는 틱마다 효과 갱신.

### 0-3. EnemyBase 확장 필드
미구현 스킬 다수가 필요로 함:
```csharp
// EnemyBase.cs 에 추가
[SerializeField] protected float damageVulnerabilityMultiplier = 1f; // 받는 피해 배율 (1.0 = 기본)

public void ApplyVulnerability(float multiplier, float duration, string id) { ... }
```

### 0-4. CharacterBase 확장 필드
```csharp
[SerializeField] protected float damageResistance = 0f;   // 받는 피해 감소율 (0~1)
[SerializeField] protected float evasionRate     = 0f;    // 회피율 (0~1)
[SerializeField] protected float sightBonusRange = 0f;    // 사거리 보너스 (m)
```

`TakeDamage` 안에서 적용:
```csharp
public void TakeDamage(float damage)
{
    if (Random.value < evasionRate) return; // 회피
    damage *= (1f - damageResistance);      // 받는 피해 감소
    // 기존 로직...
}
```

---

## §1. Astro — Space Combat Pilot (SG, 3버스트)

### 1-1. PASSIVE — Zero-G Adaptation
| 항목 | 내용 |
|---|---|
| **트리거** | 매 발 사격 시 |
| **효과** | 자기 회피율 +15% (3초간), 중첩 X |
| **컨셉** | 사격 직후 짧은 시간 동안 무중력 회피 |

**구현 위치:** `Astro.cs` 의 `TryFire` 끝에 호출
```csharp
public override void TryFire()
{
    // ... 기존 사격 코드 ...
    ActivateZeroGAdaptation();
}

private const float ZEROG_EVASION = 0.15f;
private const float ZEROG_DURATION = 3.0f;
private Coroutine zeroGCoroutine;

private void ActivateZeroGAdaptation()
{
    if (zeroGCoroutine != null) StopCoroutine(zeroGCoroutine);
    zeroGCoroutine = StartCoroutine(ZeroGRoutine());
}

private IEnumerator ZeroGRoutine()
{
    evasionRate = ZEROG_EVASION;
    yield return new WaitForSeconds(ZEROG_DURATION);
    evasionRate = 0f;
}
```

**의존성:** §0-4 의 `evasionRate` 필드 + `TakeDamage` 의 회피 분기

---

### 1-2. SKILL 1 — Gravity Collapse
| 항목 | 내용 |
|---|---|
| **트리거** | 10초마다 자동 (Timer) |
| **효과** | 전방 5유닛 반경 적에게 "Slowed" 표시 (이동속도 -50%, 받는 피해 +20%, 5초) |
| **컨셉** | 중력장으로 적을 끌어당기듯 약화 |

**구현 위치:** `Astro.cs` 의 새 코루틴 (Initialize 에서 시작)
```csharp
private const float GRAVITY_COLLAPSE_INTERVAL = 10f;
private const float GRAVITY_COLLAPSE_RADIUS   = 5f;
private const float GRAVITY_COLLAPSE_VULN     = 1.2f; // 받는 피해 +20%
private const float GRAVITY_COLLAPSE_DURATION = 5f;

private IEnumerator GravityCollapseLoop()
{
    while (true)
    {
        yield return new WaitForSeconds(GRAVITY_COLLAPSE_INTERVAL);
        if (!IsAlive) continue;

        Vector3 forward = transform.forward; // 또는 muzzlePoint.forward
        Collider[] hits = Physics.OverlapSphere(
            transform.position + forward * 3f, // 전방 3유닛 중심
            GRAVITY_COLLAPSE_RADIUS,
            enemyLayer);

        foreach (var col in hits)
        {
            if (!col.TryGetComponent<EnemyBase>(out EnemyBase enemy)) continue;
            if (!enemy.IsAlive) continue;

            enemy.ApplyVulnerability(GRAVITY_COLLAPSE_VULN, GRAVITY_COLLAPSE_DURATION, "Astro_Gravity");
        }
    }
}
```

**의존성:** §0-3 의 `EnemyBase.ApplyVulnerability`

---

### 1-3. SKILL 2 — Singularity Blast
| 항목 | 내용 |
|---|---|
| **트리거** | Gravity Collapse (SKILL 1) 의 끝 시점 (5초 후) |
| **효과** | Slowed 상태 적에게 강력한 광역 피해 (attackDamage × 3) |
| **컨셉** | 끌어당긴 적을 폭발 |

**구현 위치:** Gravity Collapse 코루틴 끝에 연결
```csharp
private IEnumerator GravityCollapseLoop()
{
    while (true)
    {
        yield return new WaitForSeconds(GRAVITY_COLLAPSE_INTERVAL);
        var hits = ApplyGravityCollapse(); // 약화 적 List 반환
        
        // SKILL 2 — 5초 후 폭발
        yield return new WaitForSeconds(GRAVITY_COLLAPSE_DURATION);
        TriggerSingularityBlast(hits);
    }
}

private void TriggerSingularityBlast(List<EnemyBase> targets)
{
    float blastDamage = attackDamage * attackDamageMultiplier * 3f;
    foreach (var enemy in targets)
    {
        if (enemy == null || !enemy.IsAlive) continue;
        enemy.TakeDamage(blastDamage);
    }
}
```

**의존성:** SKILL 1 의 끌어당김 대상 List 추적

---

## §2. Ghost — Student Reporter (RL, 1버스트)

### 2-1. PASSIVE — Field Vision
| 항목 | 내용 |
|---|---|
| **트리거** | 항상 |
| **효과** | 자기 attackDamage +5% (Telemetry/Recording 컨셉 — 정확한 약점 분석) |
| **컨셉** | 적의 정보를 실시간 분석 |

**구현 위치:** `Ghost.cs` 의 `Initialize` 끝
```csharp
public override void Initialize()
{
    // ... 기존 코드 ...
    ApplyData();
    
    // PASSIVE — Field Vision: 항상 +5% 공격력
    ApplyDamageBuff(1.05f, float.MaxValue, "Ghost_FieldVision");
}
```

**의존성:** 기존 `ApplyDamageBuff` (영구 buffId)

---

### 2-2. SKILL 1 — Live Coverage
| 항목 | 내용 |
|---|---|
| **트리거** | 전투 시작 시 5초 동안 |
| **효과** | 모든 적 위치를 화면 가장자리에 마커로 표시 (UI) |
| **컨셉** | 라이브 방송으로 적의 모든 정보를 시청자(플레이어)에게 전달 |

**구현 위치:** UI 측 새 컴포넌트 `EnemyMarkerUI` 필요. Ghost 는 이벤트만 발화.
```csharp
// Ghost.cs
public static event Action<float> OnLiveCoverageStart; // duration 전달

protected override void OnEnable()
{
    base.OnEnable();
    BattleIntroManager.OnBattleIntroComplete += TriggerLiveCoverage;
}

private void TriggerLiveCoverage()
{
    OnLiveCoverageStart?.Invoke(5f);
}

// EnemyMarkerUI.cs (신규)
void OnEnable() => Ghost.OnLiveCoverageStart += ShowMarkers;
IEnumerator ShowMarkers(float duration) { /* 5초 동안 적 위치 마커 */ }
```

**의존성:** 새 UI 컴포넌트 `EnemyMarkerUI`

---

### 2-3. SKILL 2 — Impact Analysis
| 항목 | 내용 |
|---|---|
| **트리거** | 직격 (스플래시 아닌 직접 명중) 시 |
| **효과** | 명중 적이 다음 5초간 받는 피해 +30% |
| **컨셉** | 사격 데이터로 약점 분석 |

**구현 위치:** `BulletBase.HandleCollision` 의 RL 분기 안
```csharp
if (owner != null && owner.WeaponType == WeaponType.RL)
{
    ApplyRocketSplash(enemy, finalDamage);
    
    // Ghost 의 PASSIVE 효과로 직격 적에 약점 부여
    if (owner is Ghost)
        enemy.ApplyVulnerability(1.3f, 5f, $"Ghost_Impact_{enemy.GetInstanceID()}");
}
```

**의존성:** §0-3 의 `EnemyBase.ApplyVulnerability`

---

## §3. Titan — Heavy Gunner (MG, 2버스트)

### 3-1. PASSIVE — Reinforced Frame
| 항목 | 내용 |
|---|---|
| **트리거** | 항상 |
| **효과** | 받는 피해 -20% (탱커 컨셉) |
| **컨셉** | Northguard 외골격으로 강화된 프레임 |

**구현 위치:** `Titan.cs` 의 `Initialize` 끝
```csharp
public override void Initialize()
{
    // ... 기존 코드 ...
    ApplyData();
    
    damageResistance = 0.20f; // 받는 피해 -20%
}
```

**의존성:** §0-4 의 `damageResistance` + `TakeDamage` 분기

---

### 3-2. SKILL 1 — Suppression Fire
| 항목 | 내용 |
|---|---|
| **트리거** | 스핀업 완료 후 매 10발마다 |
| **효과** | 대상 적 1마리 1초 stun |
| **컨셉** | 압도적 화력으로 적 제압 |

**구현 위치:** `Titan.ProcessShot` 안
```csharp
private const int SUPPRESSION_TRIGGER_SHOTS = 10;
private int suppressionCounter = 0;

private void ProcessShot()
{
    // ... 기존 스핀업 코드 ...
    
    // SKILL 1 — Suppression Fire
    if (shotsFired > LoopStartShots) // 스핀업 후
    {
        suppressionCounter++;
        if (suppressionCounter >= SUPPRESSION_TRIGGER_SHOTS)
        {
            suppressionCounter = 0;
            TryApplySuppression();
        }
    }
}

private void TryApplySuppression()
{
    var enemies = waveManager.ActiveEnemies;
    if (enemies == null || enemies.Count == 0) return;
    
    EnemyBase target = enemies[Random.Range(0, enemies.Count)];
    if (target != null && target.IsAlive)
        target.ApplyStun(1.0f); // 기존 ApplyStun 활용
}
```

**의존성:** 기존 `EnemyBase.ApplyStun`

---

### 3-3. SKILL 2 — Damage Buff Trigger (이미 구현됨)
`Titan.UseSkill()` 메서드에 코드 있음. 트리거만 명확화 필요.

**제안 트리거:** 동맹이 버스트 사용 시 `BurstGaugeManager.OnBurstConsumed` 구독
```csharp
// Titan.cs
protected override void OnEnable()
{
    base.OnEnable();
    BurstGaugeManager.OnBurstConsumed += OnAllyBurstConsumed;
}

private void OnAllyBurstConsumed()
{
    if (!IsAlive) return;
    UseSkill(); // 동맹 버프 적용
}
```

**의존성:** `BurstGaugeManager.OnBurstConsumed` 이벤트 (이미 존재)

---

## §4. Trend — Idol × Shooter (AR, 2버스트)

### 4-1. PASSIVE — Influencer
| 항목 | 내용 |
|---|---|
| **트리거** | 전투 시작 시 1회 (영구) |
| **효과** | 아군 전체 공격력 +10% |
| **컨셉** | 무대의 영향력 |

**구현 위치:** `Trend.cs`
```csharp
protected override void OnEnable()
{
    base.OnEnable();
    BattleIntroManager.OnBattleIntroComplete += ApplyInfluencerBuff;
}

private void ApplyInfluencerBuff()
{
    if (characterManager == null) characterManager = FindAnyObjectByType<CharacterManager>();
    if (characterManager == null) return;

    foreach (var ally in characterManager.Characters)
    {
        if (ally == null || !ally.IsAlive) continue;
        ally.ApplyDamageBuff(1.1f, float.MaxValue, "Trend_Influencer");
    }
}
```

**의존성:** 기존 `ApplyDamageBuff` (영구 buffId)

---

### 4-2. SKILL 1 — Viral Shot
| 항목 | 내용 |
|---|---|
| **트리거** | 30발 사격마다 |
| **효과** | 모든 활성 적에게 "Viral Mark" 부여, 8초간 받는 피해 +15% |
| **컨셉** | 바이럴 표식 전파 |

**구현 위치:** `Trend.cs`
```csharp
private const int VIRAL_TRIGGER_SHOTS = 30;
private int viralCounter = 0;

public override void TryFire()
{
    // ... 기존 사격 코드 ...
    
    viralCounter++;
    if (viralCounter >= VIRAL_TRIGGER_SHOTS)
    {
        viralCounter = 0;
        TriggerViralShot();
    }
}

private void TriggerViralShot()
{
    var waveManager = FindAnyObjectByType<WaveManager>();
    if (waveManager == null) return;
    
    foreach (var enemy in waveManager.ActiveEnemies)
    {
        if (enemy == null || !enemy.IsAlive) continue;
        enemy.ApplyVulnerability(1.15f, 8f, "Trend_Viral");
    }
}
```

**의존성:** §0-3 의 `EnemyBase.ApplyVulnerability`

---

### 4-3. SKILL 2 — Hashtag Boost
| 항목 | 내용 |
|---|---|
| **트리거** | 20초마다 자동 |
| **효과** | 아군 전체 공격력 +20% + 치명타 +10% (5초) |
| **컨셉** | 해시태그가 트렌드를 만든다 |

**구현 위치:** `Trend.cs`
```csharp
private const float HASHTAG_INTERVAL = 20f;
private const float HASHTAG_DURATION = 5f;

void Start()
{
    Initialize();
    characterManager = FindAnyObjectByType<CharacterManager>();
    StartCoroutine(HashtagBoostLoop());
}

private IEnumerator HashtagBoostLoop()
{
    while (true)
    {
        yield return new WaitForSeconds(HASHTAG_INTERVAL);
        if (!IsAlive) continue;

        foreach (var ally in characterManager.Characters)
        {
            if (ally == null || !ally.IsAlive) continue;
            ally.ApplyDamageBuff(1.2f, HASHTAG_DURATION, "Trend_Hashtag_Dmg");
            ally.ApplyCriticalRateBuff(0.1f, HASHTAG_DURATION, "Trend_Hashtag_Crit");
        }
    }
}
```

**의존성:** 기존 `ApplyDamageBuff` + `ApplyCriticalRateBuff`

---

## §5. Viper — Venom Sniper (SR, 3버스트)

### 5-1. PASSIVE — Venom Carrier
| 항목 | 내용 |
|---|---|
| **트리거** | 사격 명중 시 (확률) |
| **효과** | 명중 적에게 DOT 부여 (3초간 0.5초마다 attackDamage × 0.2 피해) |
| **컨셉** | 모든 탄환이 독을 운반 |

**구현 위치:** `BulletBase.HandleCollision` Viper 분기 추가
```csharp
if (owner is Viper && enemy.IsAlive)
{
    if (Random.value < 0.5f) // 50% 확률
    {
        StartCoroutine(VenomDotRoutine(enemy, owner));
    }
}

private IEnumerator VenomDotRoutine(EnemyBase target, CharacterBase poisoner)
{
    float dotDamage = poisoner.FinalAttackDamage * 0.2f;
    int ticks = 6; // 3초 / 0.5초
    for (int i = 0; i < ticks; i++)
    {
        yield return new WaitForSeconds(0.5f);
        if (target == null || !target.IsAlive) yield break;
        target.TakeDamage(dotDamage);
    }
}
```

**의존성:** 향후 `DotEffect` SO 또는 컴포넌트로 추상화 가능

---

### 5-2. SKILL 1 — Toxic Mark
| 항목 | 내용 |
|---|---|
| **트리거** | 5발 사격마다 (한 클립 = 1회) |
| **효과** | HP 최고 적 1마리에 "Toxic Mark" 부여 — 10초간 받는 피해 +40% |
| **컨셉** | 표적 결정 |

**구현 위치:** `Viper.cs` 의 `HandleFireRelease` 끝
```csharp
private const int TOXIC_MARK_TRIGGER_SHOTS = 5;
private int toxicCounter = 0;

void HandleFireRelease()
{
    // ... 기존 발사 코드 ...
    
    toxicCounter++;
    if (toxicCounter >= TOXIC_MARK_TRIGGER_SHOTS)
    {
        toxicCounter = 0;
        TriggerToxicMark();
    }
}

private void TriggerToxicMark()
{
    if (waveManager == null) return;
    
    EnemyBase target = null;
    float highestHp = float.MinValue;
    foreach (var enemy in waveManager.ActiveEnemies)
    {
        if (enemy == null || !enemy.IsAlive) continue;
        if (enemy.Hp > highestHp) { highestHp = enemy.Hp; target = enemy; }
    }
    
    if (target != null)
        target.ApplyVulnerability(1.4f, 10f, "Viper_ToxicMark");
}
```

**의존성:** §0-3 의 `EnemyBase.ApplyVulnerability`

---

### 5-3. SKILL 2 — Snake Eyes
| 항목 | 내용 |
|---|---|
| **트리거** | 15초마다 자동 |
| **효과** | 자기 치명타율 +25% (5초) |
| **컨셉** | 뱀의 시야로 약점 포착 |

**구현 위치:** `Viper.cs`
```csharp
private const float SNAKE_EYES_INTERVAL = 15f;
private const float SNAKE_EYES_BONUS    = 0.25f;
private const float SNAKE_EYES_DURATION = 5f;

void Start()
{
    Initialize();
    waveManager = FindAnyObjectByType<WaveManager>();
    StartCoroutine(SnakeEyesLoop());
}

private IEnumerator SnakeEyesLoop()
{
    while (true)
    {
        yield return new WaitForSeconds(SNAKE_EYES_INTERVAL);
        if (!IsAlive) continue;
        ApplyCriticalRateBuff(SNAKE_EYES_BONUS, SNAKE_EYES_DURATION, "Viper_SnakeEyes");
    }
}
```

**의존성:** 기존 `ApplyCriticalRateBuff`

---

## §6. 구현 우선순위 — 의존성 그래프

```
[Level 1 — 즉시 가능, 의존성 없음]
  ├─ Ghost.Field Vision (§2-1) — Initialize 한 줄
  ├─ Trend.Influencer (§4-1) — BattleIntro 이벤트 구독
  ├─ Trend.Hashtag Boost (§4-3) — 코루틴 + 기존 buff 활용
  ├─ Viper.Snake Eyes (§5-3) — 코루틴 + 기존 buff 활용
  └─ Astro.Zero-G Adaptation (§1-1) — evasionRate 필드 + 회피 분기

[Level 2 — §0-4 (CharacterBase 필드 확장) 필요]
  ├─ Titan.Reinforced Frame (§3-1) — damageResistance
  ├─ Astro.Zero-G Adaptation (§1-1) — evasionRate
  └─ Ghost.Field Vision (§2-1) 가능 — 단순 ApplyDamageBuff 면 불필요

[Level 3 — §0-3 (EnemyBase.ApplyVulnerability) 필요]
  ├─ Astro.Gravity Collapse + Singularity Blast (§1-2, §1-3)
  ├─ Ghost.Impact Analysis (§2-3)
  ├─ Trend.Viral Shot (§4-2)
  └─ Viper.Toxic Mark (§5-2)

[Level 4 — 기존 시스템 (Stun) 활용 가능]
  └─ Titan.Suppression Fire (§3-2)

[Level 5 — DOT 시스템 (가벼운 코루틴 버전)]
  └─ Viper.Venom Carrier (§5-1)

[Level 6 — UI 컴포넌트 신규 필요]
  └─ Ghost.Live Coverage (§2-2) — EnemyMarkerUI 컴포넌트
```

**추천 순서:**
1. **§0-4 CharacterBase 필드 확장** (한 번 추가하면 5개 스킬에 활용)
2. **§0-3 EnemyBase.ApplyVulnerability** (5개 스킬에 활용)
3. **Level 1** 5개 스킬 한꺼번에 구현 (의존성 없음)
4. **Level 2~5** 순차 진행
5. **Live Coverage** 는 UI 작업 시간 별도 확보 후 마지막

---

## §7. 작업 추정 시간

| 스킬 | 추정 시간 |
|---|---|
| Level 1 (5개) | 1시간 (각 10분) |
| Level 2 (Reinforced/Zero-G) | 30분 (필드 추가 포함) |
| Level 3 (Vulnerability 4개) | 1시간 (시스템 추가 + 4개 적용) |
| Level 4 (Suppression) | 15분 |
| Level 5 (Venom DOT) | 30분 |
| Live Coverage UI | 1.5시간 |
| **합계** | **약 4~5시간** |
