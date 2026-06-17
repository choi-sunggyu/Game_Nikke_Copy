using UnityEngine;

/// <summary>
/// 캐릭터 정체성 데이터 — 능력치 / 스프라이트 / 버스트 메타데이터.
/// "어떤 캐릭터인가" 의 영속적인 정보를 담음 (씬에 종속된 muzzlePoint, bulletPool 등은 제외).
///
/// 사용 흐름:
///   1. Project 창 우클릭 → Create → Character → Character Data 로 .asset 파일 생성
///   2. 인스펙터에서 능력치/스프라이트 입력
///   3. Ghost/Titan/Viper 같은 CharacterBase 파생체의 인스펙터 슬롯에 할당
///   4. Initialize() 안에서 data 필드를 읽어 자기 자신 초기화
///
/// 캐릭터별 특수 능력치(Titan 의 minFireRate, Viper 의 maxChargeTime 등)는
/// 이 SO 가 아니라 각 캐릭터 클래스에 그대로 둠 — YAGNI 원칙.
/// 5명 추가하면서 공통 패턴이 굳어지면 그때 무기별 sub-SO 로 확장.
/// </summary>
[CreateAssetMenu(
    fileName = "NewCharacterData",
    menuName = "Character/Character Data",
    order    = 0)]
public class CharacterData : ScriptableObject
{
    [Header("── 식별 ─────────────────────────")]
    [Tooltip("코드/세이브 파일에서 사용할 고유 ID (예: ghost, titan, viper)")]
    public string characterId;

    [Tooltip("UI 에 표시할 이름")]
    public string displayName;

    [Tooltip("1버스트 / 2버스트 / 3버스트 — 1~3 사이")]
    [Range(1, 3)] public int burstNumber = 1;

    [Tooltip("무기 종류 — 적정 사거리에서 +50% 보너스 피해 (RL 제외, 거리 무관)")]
    public WeaponType weaponType = WeaponType.AR;

    [Header("── 체력 / 방어 ──────────────────")]
    public float maxHp     = 100f;
    public float maxShield = 50f;

    [Header("── 사격 ────────────────────────")]
    public int   maxBulletCount = 30;
    public float reloadTime     = 1.5f;
    public float attackDamage   = 20f;
    [Tooltip("발사 간 딜레이 (초). 1/RPM 으로 계산. 예: 20RPS → 1/20 = 0.05")]
    public float fireRate       = 0.05f;
    public float bulletSpeed    = 500f;

    [Header("── 버스트 / 스킬 ─────────────────")]
    [Tooltip("탄 1발이 적에게 명중할 때 차오르는 버스트 게이지 양")]
    public float chargingBurstGauge    = 5f;
    public float burstCoolTime         = 15f;
    public float skillCoolTime         = 10f;
    [Tooltip("버스트 컷씬 동안 Time.timeScale = 0 으로 잠기는 시간(초)")]
    public float burstCutsceneDuration = 0f;

    [Header("── 크리티컬 ────────────────────")]
    [Range(0f, 1f)] public float criticalRate       = 0.15f;
    public float                criticalMultiplier = 1.5f;

    [Header("── 시각 ────────────────────────")]
    [Tooltip("BottomUI / MissionClearUI 등에서 사용할 인물 포트레이트")]
    public Sprite characterPortrait;
    [Tooltip("BottomUI 박스 안의 작은 캐릭터 스프라이트")]
    public Sprite characterSprite;
    [Tooltip("기본 단일 sprite 시스템용 (Titan 처럼 시트 애니메이션을 쓰는 캐릭터는 무시)")]
    public Sprite idleSprite;
    public Sprite shootSprite;
    public Sprite reloadSprite;
}
