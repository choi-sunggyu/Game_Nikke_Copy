# TREND — Idol × Shooter

> *"Make it. Share it. Trend it."*

**Faction:** Lumina Stage  
**Class:** Idol × Shooter  
**Role:** Striker (Team Buffer)  
**Burst Number:** 2

---

## 1. 기본 정보

| 항목 | 값 |
|---|---|
| 코드명 | TREND (트렌드) |
| 나이 / 키 | 20세 / 168cm |
| 생일 | 05/20 |
| 의상 컨셉 | 무대 의상 (검정 재킷 + 핑크/레드 액센트, fishnet stockings, 십자가 체인) |
| 악세서리 | Star Hairpin / Earring / Idol Tag |
| 컬러 팔레트 | Primary: Black/White / Secondary: 핑크·레드·골드 / Accent: 핑크 글로우 |
| 태그 | IDOL · STAGE · COMBAT · RHYTHM |

---

## 2. 컨셉 / 배경

> "루미나 스테이지의 톱 아이돌이자 최정예 전술 요원. 무대 위의 그녀는 빛나는 스타. 전장에선 누구보다 빛나는 에이스."

낮에는 아이돌 그룹의 센터, 밤에는 Lumina Stage 의 작전 요원. 무대 퍼포먼스의 정확함과 화려함을 사격 스타일로 옮긴 캐릭터. 노래·춤·사격이 모두 "리듬" 으로 연결된다는 컨셉.

ULTIMATE 발동 시 거대 홀로그램 무대가 생성되며 아군이 그녀의 "공연" 으로부터 영감을 받아 강화됨. 단순 버퍼가 아닌 **무대 위의 카리스마로 팀을 끌어올리는 리더형 서포터**.

---

## 3. 무기 — Stage Breaker (커스텀 AR)

> "루미나 스테이지의 상징이 새겨진 커스텀 AR. 음파 증폭 모듈과 스마트 조준 시스템이 결합되어, 무대 위의 완벽한 퍼포먼스처럼 정확하고 화려한 전장을 만들어낸다."

| 모듈 | 역할 |
|---|---|
| Smart Scope | 스마트 조준 시스템 (정밀 사격) |
| Sound Amp | 음파 증폭 모듈 (공연 효과) |
| Stage Light | LED 라이트 (시각 강조) |
| Energy Mag | 에너지 탄창 |
| Star Emblem | 루미나 로고 (브랜드) |

---

## 4. 능력치 (코드 폴백 / `Trend.cs`)

| 항목 | 값 |
|---|---|
| MaxHP | 1100 |
| MaxShield | 500 |
| Bullet Count | 30 |
| Reload Time | 1.0s |
| Attack Damage | 20 (지원형이라 약간 낮음) |
| Fire Rate | 0.08s (AR 표준) |
| Bullet Speed | 500 |
| Charging Burst Gauge | 15 / 발 |
| Burst Cooltime | 18s |
| Skill Cooltime | 10s |
| Burst Number | **2** |

---

## 5. 스킬

| 단계 | 이름 | 효과 | 코드 상태 |
|---|---|---|---|
| PASSIVE | **Influencer** | 전투 시작 시 아군 전체 공격력 소폭 증가 | ❌ TODO |
| SKILL 1 | **Viral Shot** | 적 전체에 '바이럴 표식' 부여, 표식 적에게 가하는 피해 증가 | ❌ TODO |
| SKILL 2 | **Hashtag Boost** | 아군 전체 공격력 / 치명타 확률 증가 | ❌ TODO |
| ULTIMATE | **Trending Now** | 홀로그램 무대 생성, 아군 전체 공격력 ×1.5 + 치명타 +20% (10초) | ✅ 구현 완료 |

### ULTIMATE 코드 — Trending Now

```csharp
public override void UseBurst()
{
    UsedBurstThisCycle = true;
    if (characterManager == null) characterManager = FindAnyObjectByType<CharacterManager>();
    if (characterManager == null) return;

    // 이펙트 (자기 위치에 홀로그램 무대)
    if (trendingStagePrefab != null)
    {
        GameObject stage = Instantiate(trendingStagePrefab, transform.position, Quaternion.identity);
        stage.transform.SetParent(transform);
    }

    // 아군 전체 버프 — 살아있는 캐릭터에게만
    foreach (var ally in characterManager.Characters)
    {
        if (ally == null || !ally.IsAlive) continue;

        // ① 공격력 ×1.5 (10초)
        ally.ApplyDamageBuff(
            BURST_DAMAGE_MULTIPLIER,    // 1.5f
            BURST_DURATION,             // 10f
            TREND_BURST_DAMAGE_BUFF_ID);

        // ② 치명타 확률 +20% (10초)
        ally.ApplyCriticalRateBuff(
            BURST_CRIT_RATE_BONUS,      // 0.2f
            BURST_DURATION,
            TREND_BURST_CRIT_BUFF_ID);
    }

    Debug.Log("[Trend UseBurst] Trending Now! 아군 전체 공격력 ×1.5 + 치명타 +20% (10초)");
}
```

**파라미터**
- `BURST_DAMAGE_MULTIPLIER = 1.5f` (공격력 ×50%)
- `BURST_CRIT_RATE_BONUS = 0.2f` (치명타 +20%p)
- `BURST_DURATION = 10.0f` (10초)
- `TREND_BURST_DAMAGE_BUFF_ID`, `TREND_BURST_CRIT_BUFF_ID` — 두 효과를 독립 ID 로 관리 (Skill 2 와 충돌 방지)

---

## 6. 개발자 노트

### 6-1. 파일 경로
- **코드:** `Assets/Scripts/Character/Unit/Trend.cs`
- **데이터:** `Assets/ScriptableObjects/Characters/Trend_Data.asset` (생성 필요)

### 6-2. 두 종류의 버프 동시 적용
- `ApplyDamageBuff` 와 `ApplyCriticalRateBuff` 는 CharacterBase 의 **서로 다른 Dictionary** (`activeBuffs`, `activeCriticalBuffs`) 로 관리
- 같은 buffId 를 써도 충돌 안 나지만 **두 ID 를 분리한 이유** — 향후 Skill 2 (Hashtag Boost) 도 비슷한 효과를 줄 때 Burst 와 Skill 2 가 서로의 버프를 덮어쓰지 않도록 명시적 분리

### 6-3. 버스트 분포 — 2번째 슬롯
- BurstGaugeManager 의 `BurstNumber == 2` 캐릭터 — Titan / Trend 동시 존재
- `BurstSlotUI` 가 2버스트 단계 진입 시 두 슬롯 모두 활성화
- 사용자가 둘 중 한 명 선택 → 다른 한 명은 다음 사이클 대기

### 6-4. 시트 애니메이션 (15장 5+5+5)
- IDOL × SHOOTER 컨셉에 맞춰 무대 댄스 모션의 사격 자세 (춤사위에서 사격으로 자연스러운 전환)
- `CharacterBase.animSprites` 15장 슬라이스 할당

### 6-5. 사운드
- 무기 모듈 "Sound Amp" 컨셉 반영 → 사격음에 약간의 음악적 톤 또는 비트 입히기
- ULTIMATE 발동 시 무대 사운드 (군중 환호 / 음악 인트로)

### 6-6. TODO
- [ ] **PASSIVE Influencer** — 전투 시작 시 아군 공격력 +10% (1회만)
- [ ] **SKILL 1 Viral Shot** — 적 전체 '바이럴 표식' 부여. StatusEffect.MarkedEnemy 같은 컴포넌트로 구현
- [ ] **SKILL 2 Hashtag Boost** — 아군 공격력/치명타 증가. ULTIMATE 와 다른 buffId 로 관리 필요
- [ ] **trendingStagePrefab** — 홀로그램 무대 이펙트 프리팹 (LED + 별 + 빛줄기)
- [ ] **이동속도 버프 추가** — 이미지 스킬 설명에 "이동속도" 포함, 현재 미구현 (CharacterBase 에 moveSpeed 필드 부재)
- [ ] **무기 이미지의 LED 라이트 컬러** — 사격 시 발광 효과로 시각화

### 6-7. 의존성
- `characterManager.Characters` — UseBurst 의 팀 버프 대상 순회
- `CharacterBase.ApplyDamageBuff(multiplier, duration, buffId)` — 공격력 버프
- `CharacterBase.ApplyCriticalRateBuff(amount, duration, buffId)` — 치명타 버프
- `trendingStagePrefab` — 홀로그램 무대 이펙트 (인스펙터 할당)
