# Project BUSTER — 파일 구조 & 책임 맵

> 새 채팅에서 컨텍스트를 빠르게 인계받기 위한 메타 문서.
> 새 대화 첫 메시지에 이 파일을 첨부하거나, 아래 **「§0 빠른 인계 프롬프트」** 를 복붙해서 사용.
> 마지막 갱신: 2026-06

---

## §0. 빠른 인계 프롬프트 (새 채팅 복붙용)

```
프로젝트: Project BUSTER — NIKKE 모작 (Unity, C#)
폴더: C:\Users\PC\OneDrive\바탕 화면\choi\coding\_NIKKE
주요 컨벤션:
- 시스템 간 통신은 정적 C# 이벤트 (InputManager, CharacterBase, BurstGaugeManager 가 발행자)
- 캐릭터 3명: Ghost(1버스트·AR), Titan(2버스트·MG), Viper(3버스트·차지샷)
- 적 베이스: EnemyBase 추상 → EnemyA/B/C + Boss 가능 (EnemyType: Normal/Elite/Boss)
- 카메라 권한: CameraController (Single Source of Truth)
- 풀링: ObjectPool + IPoolable (Bullet, EnemyBullet, DamagePopup)
- 인트로: BattleIntroManager (UI 전담)
- 통합 게임 진행: BattleManager 싱글톤 (캐릭터별 스킬 트리거 담당)

전체 파일 구조와 각 파일 책임은 Assets/Docs/PROJECT_STRUCTURE.md 참조.
README.md 에 NIKKE 원작 메커닉 재현 매트릭스가 정리됨.
Assets/Docs/event-topology.svg 에 이벤트 발행자→구독자 다이어그램 있음.
```

---

## §1. 디렉터리 트리 (Assets/Scripts)

```
Assets/Scripts/
├── Core/                  ← 전역 enum (어디서든 참조 가능)
│   ├── CharacterState.cs  ← Idle/Fire/Reload/Dead
│   ├── EnemyState.cs      ← Idle/Attack/Move/Jump/Dead
│   └── EnemyType.cs       ← Normal/Elite/Boss
│
├── Character/             ← 플레이어 캐릭터 시스템
│   ├── CharacterBase.cs        ← 추상 베이스 (HP/쉴드/리로드/버프/사격 공통)
│   ├── CharacterManager.cs     ← 활성 캐릭터 추적, 스위칭, 사망 처리
│   ├── CharacterAI.cs          ← 비활성 팀원 자동 사격 + 오토 스코프
│   ├── InputManager.cs         ← 입력 → 정적 이벤트 발행
│   ├── Ghost.cs                ← 1버스트 AR (120발/1.0s 리로드)
│   ├── Titan.cs                ← 2버스트 MG (400발/스핀업)
│   ├── Viper.cs                ← 3버스트 차지샷 (5발/1.13s 차지)
│   ├── AttackStar.cs           ← Titan 버스트 공격 별
│   ├── BuffStarEffect.cs       ← Titan 버스트 버프 시각화
│   ├── ViperBeamEffect.cs      ← Viper 버스트 빔
│   └── CrossHair/
│       ├── CrossHairBase.cs    ← 추상 크로스헤어
│       ├── MiniGunCrossHair.cs ← Titan 전용
│       ├── RifleCrossHair.cs   ← Ghost 전용
│       └── ScopeCrossHair.cs   ← Viper 전용
│
├── Battle/                ← 전투 통합 로직
│   ├── BattleManager.cs        ← 싱글톤. 팀 관리 + 캐릭터별 스킬 트리거 (탄 소모/마지막 탄/풀버스트 이벤트 구독)
│   ├── BurstGaugeManager.cs    ← 싱글톤. 3단계 버스트 시스템 (Charging→Step1/2/3Ready→FocusFire)
│   └── BurstSlotsController.cs ← 버스트 슬롯 UI 제어
│
├── Combat/                ← 탄환
│   ├── BulletBase.cs           ← 플레이어 탄 (Raycast 충돌 예측, IPoolable)
│   └── EnemyBulletBase.cs      ← 적 탄
│
├── Enemy/                 ← 적 시스템
│   ├── Unit/
│   │   ├── EnemyBase.cs        ← 추상 베이스 (HP/스턴/사망/타겟팅 전략)
│   │   ├── EnemyA.cs           ← 낙하 출현 + 레이저 (경고원→레이저)
│   │   ├── EnemyB.cs           ← 측면 진입 이동형
│   │   ├── EnemyC.cs           ← 근접 돌진형
│   │   └── DummyEnemy.cs       ← 테스트용
│   ├── Damage/
│   │   ├── DamagePopup.cs      ← 데미지 숫자 한 개
│   │   └── DamagePopupManager.cs ← 싱글톤. 풀링 관리
│   ├── BossHPBar.cs            ← 보스 HP 바 (delayFill 시각 효과)
│   ├── EnemyHPBar.cs           ← 일반/엘리트/보스 색상 분기 HP 바
│   ├── FlashEffect.cs          ← Ghost 버스트 시 화면 플래시
│   └── RandomTargetStrategy.cs ← ITargetStrategy 구현체
│
├── Camera/
│   └── CameraController.cs     ← 카메라 권한자. Lerp 이동 + 캐릭터 스위칭 연동
│
├── Wave/                  ← 웨이브 진행
│   ├── WaveManager.cs          ← 난이도별 적 스폰, 인트로 가드, OnStageClear 발행
│   ├── WaveData.cs             ← ScriptableObject 데이터
│   ├── GameSettings.cs         ← 정적 GameSettings.SelectedDifficulty
│   ├── MainMenuManager.cs      ← 메인 메뉴 진입
│   └── AudioManager.cs         ← BGM 재생
│
├── UI/
│   ├── HPShieldBarUI.cs        ← 캐릭터 HP/쉴드 바
│   ├── BottomUI.cs             ← 하단 탄창 표시
│   ├── ReloadProgressBarUI.cs  ← 리로드 진행 바
│   ├── AutoScopeButtonUI.cs    ← 오토 스코프 토글 버튼
│   ├── GameOverUI.cs           ← 게임 오버 화면
│   ├── MissionClearUI.cs       ← 미션 클리어 화면 (페이드인 + 펀치 텍스트)
│   ├── HexagonSpinner.cs       ← 로딩 회전 효과
│   ├── Burst/
│   │   ├── BurstGaugeUI.cs     ← 게이지/페이즈 표시
│   │   ├── BurstSlotUI.cs      ← 1/2/3 버스트 슬롯
│   │   └── AutoBurstButtonUI.cs ← 오토 버스트 토글
│   ├── Intro/
│   │   ├── BattleintroManager.cs ← 전투 시작 인트로 UI (다이아/크로스헤어/텍스트 시퀀스)
│   │   └── BattleIntroUI.cs     ← 단순 페이드 인트로 (별도 컴포넌트)
│   └── TopUI/
│       ├── TopUIManager.cs     ← 보스 HP바 + 엘리트 경고 + 카메라 줌 통합
│       └── WaveProgressBar.cs  ← 웨이브 진행도 다이아 마커
│
├── Scene/
│   └── LoadingSceneManager.cs  ← 씬 전환
│
└── Utility/               ← 풀링 인프라
    ├── IPoolable.cs            ← OnGet/OnReturn 인터페이스
    ├── ObjectPool.cs           ← Queue 기반 풀 + 확장 가능
    └── PoolObject.cs           ← 풀 객체에 owner 참조 부착
```

---

## §2. 핵심 이벤트 발행자 ↔ 구독자

자세한 다이어그램은 `Assets/Docs/event-topology.svg` 참조.

| 발행자 | 이벤트 | 주 구독자 |
|---|---|---|
| **InputManager** | OnFire / OnIdle / OnFirePress / OnFireRelease / OnSwitchCharacter / OnCoverToggle | CharacterManager, CameraController, Viper, Titan |
| **CharacterBase** | OnBulletCountChanged / OnStatChanged / OnReloadProgress / OnForcedReloadStart-End / OnCharacterDied / OnBulletConsumed | UI 전체, CharacterManager, BattleManager |
| **BurstGaugeManager** | OnGaugeChanged / OnPhaseChanged / OnBurstReady / OnBurstConsumed / OnAutoModeChanged / OnFocusFireStart-End / OnFullBurstStarted | BurstGaugeUI, BurstSlotUI, CharacterAI, BattleManager |
| **BulletBase** | OnLastBulletHit | BattleManager |
| **CharacterAI** | OnAutoScopeModeChanged | AutoScopeButtonUI |
| **CharacterManager** | OnGameOver / OnCharacterSwitchConfirmed | GameOverUI, InputManager |
| **WaveManager** | OnStageClear | InputManager, MissionClearUI |
| **BattleIntroManager** | OnBattleIntroComplete | InputManager(잠금 해제), WaveManager, CameraController, CharacterAI |

---

## §3. 캐릭터 / 적 / 무기 스펙 (코드 실측)

| 캐릭터 | 버스트 | HP | 탄창 | 리로드 | 데미지 | 충전량 | 탄속 | 특이사항 |
|---|---|---|---|---|---|---|---|---|
| Ghost | 1 | 100 | 120 | 1.0s | 20 | +5 | 500 | 단발 AR |
| Titan | 2 | 200 | 400 | 1.5s | 10 | +10 | 500 | 스핀업 가속 |
| Viper | 3 | 100 | 5 | 1.0s | 50 | +20 | 800 | 차지 1.13s, 최대 1.5배 |

| 적 | 행동 | 공격 |
|---|---|---|
| EnemyA | 낙하 출현 → 고정 | 경고원(1.5s) → 레이저(0.5s) |
| EnemyB | 측면 진입 → 이동 후 | 일반 사격 |
| EnemyC | 근접 돌진 | 추적 공격 |
| Boss | (BossScene 별도) | 별도 패턴 |

---

## §4. 자주 손대는 파일 우선순위 (작업 시 참조)

| 작업 종류 | 우선 손볼 파일 |
|---|---|
| 캐릭터 능력치/사운드/스킬 | `Character/Ghost.cs` `Titan.cs` `Viper.cs` |
| 캐릭터 공통 동작 (피격/리로드) | `Character/CharacterBase.cs` |
| 캐릭터 스위칭/사망 | `Character/CharacterManager.cs` |
| 입력 추가/제거 | `Character/InputManager.cs` |
| 버스트 시스템 | `Battle/BurstGaugeManager.cs` |
| 카메라 이동/줌 | `Camera/CameraController.cs` (단일 진실) |
| 적 신규 추가 | `Enemy/Unit/EnemyBase.cs` 상속 + WaveData 등록 |
| 적 타겟팅 변경 | `Enemy/RandomTargetStrategy.cs` (ITargetStrategy 새 구현) |
| 인트로 연출 | `UI/Intro/BattleintroManager.cs` |
| HP바/데미지팝업 | `Enemy/EnemyHPBar.cs` `Enemy/BossHPBar.cs` `Enemy/Damage/DamagePopupManager.cs` |
| 웨이브 디자인 | `Wave/WaveManager.cs` + `WaveData.cs` ScriptableObject |
| UI 변경 | `UI/` 하위 (Burst/Intro/TopUI 그룹 확인) |

---

## §5. Prefabs / Scenes

### Prefabs (`Assets/Prefabs/`)
```
Characters.prefab     ← 3 캐릭터 묶음
Enenmies.prefab       ← (오타) 적 컨테이너
PlayerBullet.prefab   ← BulletBase 풀링 대상
EnemyBullet.prefab    ← EnemyBulletBase 풀링 대상
DamagePopup.prefab    ← DamagePopupManager 풀링 대상
AttackStar.prefab     ← Titan 버스트 공격 별
BuffStar.prefab       ← Titan 버스트 버프 별
ViperBeam.prefab      ← Viper 버스트 빔
EnemyA/B/C.prefab     ← 적 종류별
FlashEffect.prefab    ← Ghost 버스트 화면 플래시
Effects.prefab        ← 이펙트 컨테이너
UI.prefab             ← UI 루트
```

### Scenes (`Assets/Scenes/`)
```
MainMenuScene.unity   ← 시작 화면
LoadingScene.unity    ← 씬 전환용
BattleScene.unity     ← 일반 전투 (3명 + 적 웨이브)
BossScene.unity       ← 보스 전투
```

---

## §6. 미구현 / 빈 디렉터리

```
Assets/Scripts/StatusEffect/   ← README 약속 (BuffEffect/DebuffEffect/DotEffect 상속 구조) — 미구현
Assets/Scripts/Skill/          ← 스킬 데이터 분리 예정 — 미구현
Assets/ScriptableObjects/      ← WaveData 외 비어있음 — 데이터 외부화 작업 미진행
```

향후 손대면 README 의 「향후 개선 항목」 섹션과 동기화 필요.

---

## §7. 문서 / 산출물

```
README.md                       ← 포트폴리오용. NIKKE 메커닉 재현 매트릭스 + 코드 셀링 포인트
Assets/Docs/event-topology.svg  ← 이벤트 발행자→구독자 다이어그램 (1800×1200)
Assets/Docs/PROJECT_STRUCTURE.md ← (본 문서)
Project_NIKKE_v1.1_tracked.docx ← 초기 기획 문서
```
