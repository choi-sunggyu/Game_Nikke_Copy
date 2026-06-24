using UnityEngine;

/// <summary>
/// 적 정체성 데이터 — 능력치 / 메타데이터.
/// "어떤 적인가" 의 영속적인 정보를 담음 (씬에 종속된 muzzlePoint, bulletPool 등은 제외).
///
/// 사용 흐름:
///   1. Project 창 우클릭 → Create → Enemy → Enemy Data 로 .asset 파일 생성
///   2. 인스펙터에서 능력치 입력
///   3. EnemyA/B/C/D 같은 EnemyBase 파생체의 인스펙터 슬롯에 할당
///   4. Initialize() 안에서 data 필드를 읽어 자기 자신 초기화
///
/// 적별 특수 능력치(EnemyA 의 레이저 경고 시간, EnemyD 의 점프 인터벌 등)는
/// 이 SO 가 아니라 각 적 클래스에 그대로 둠 — YAGNI 원칙.
/// </summary>
[CreateAssetMenu(
    fileName = "NewEnemyData",
    menuName = "Enemy/Enemy Data",
    order    = 0)]
public class EnemyData : ScriptableObject
{
    [Header("── 식별 ─────────────────────────")]
    [Tooltip("코드/로그에서 사용할 고유 ID (예: enemyA, enemyB, enemyD_boss)")]
    public string enemyId;

    [Tooltip("UI 에 표시할 이름")]
    public string displayName;

    [Tooltip("적 분류 — Normal / Elite / Boss")]
    public EnemyType enemyType = EnemyType.Normal;

    [Tooltip("공중 적인가? true=공중(낙하/부유), false=지상(바닥 collider 위)")]
    public bool isAirborne = false;

    [Header("── 체력 / 공격 ──────────────────")]
    public float maxHp        = 100f;
    public float attackDamage = 10f;
    [Tooltip("기본 공격 (일반 총알) 간격 (초)")]
    public float attackDelay  = 2f;
    [Tooltip("이동 속도 — 옆 이동 / 추적 등에서 사용")]
    public float speed        = 2f;

    [Header("── 일반 총알 ───────────────────")]
    public float bulletSpeed  = 15f;

    [Header("── 미사일 (보스 전용) ───────────")]
    [Tooltip("미사일 발사 간격 (초). 0 이하면 미사일 미사용")]
    public float missileDelay   = 5f;
    public float missileDamage  = 15f;
    public float missileSpeed   = 10f;
    [Tooltip("1회 발사 시 동시에 나가는 미사일 수. EnemyD 는 muzzle 4개 풀세트 = 4")]
    public int   missileCountPerSalvo = 4;
    [Tooltip("미사일 추적 강도 — 0=직진, 1=완전 추적")]
    [Range(0f, 1f)]
    public float missileHoming  = 0.5f;

    [Header("── 시각 (옵션) ─────────────────")]
    public Sprite enemyPortrait;
}
