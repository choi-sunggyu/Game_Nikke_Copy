using UnityEngine;

public class CharacterAI : MonoBehaviour
{
    [SerializeField] private float aimSpeed = 3f;      // 크로스헤어 이동 속도
    [SerializeField] private float aimSpread = 30f;    // 목표 지점 오차 반경 (픽셀)
    [SerializeField] private float viperFireThreshold = 30f; // Viper 발사 오차 허용 픽셀

    private CharacterBase owner;
    private CharacterManager characterManager;
    private WaveManager waveManager;
    private EnemyBase currentTarget;

    private RectTransform crossHairRect;
    private Vector2 aimTargetScreenPos; // 크로스헤어가 향할 목표 스크린 좌표
    private bool isViper;

    void Awake()
    {
        owner = GetComponent<CharacterBase>();
        characterManager = FindAnyObjectByType<CharacterManager>();
        waveManager = FindAnyObjectByType<WaveManager>();

        isViper = owner is Viper;
    }

    void Start()
    {
        // CrossHair rectTransform 참조
        if (owner.CrossHair != null)
            crossHairRect = owner.CrossHair.GetComponent<RectTransform>();

        // 초기 조준 위치 = 화면 중앙
        aimTargetScreenPos = new Vector2(Screen.width / 2f, Screen.height / 2f);
        if (crossHairRect != null)
            crossHairRect.position = aimTargetScreenPos;
    }

    void Update()
    {
        // 플레이어가 조작 중이면 AI 비활성
        if (characterManager.CurrentCharacter == owner) return;
        if (!owner.IsAlive) return;
        if (characterManager.IsCovering) return;

        ValidateAndSelectTarget();

        if (currentTarget == null)
        {
            owner.OnStopFiring();
            owner.TryReload();
            return;
        }

        // 적 월드좌표 → 스크린좌표
        Vector2 targetScreenPos = Camera.main.WorldToScreenPoint(currentTarget.transform.position);

        // 목표 스크린 좌표 = 적 위치 + aimSpread 오프셋 (타겟 선택 시 1회만 적용)
        MoveAimToward(targetScreenPos);

        // 크로스헤어와 목표 거리
        float dist = Vector2.Distance(crossHairRect.position, targetScreenPos);

        if (isViper)
        {
            // Viper: 목표 도달 시에만 발사
            if (dist <= viperFireThreshold)
            {
                // 클릭 해제로 인식 → HandleFireRelease 트리거
                owner.TryFire();          // Fire 상태 진입
                SimulateViperFire();      // 즉시 해제 → 발사
            }
            else
            {
                // 조준 중 → Fire 상태만 유지 (실제 발사 없음)
                owner.TryFire();
            }
        }
        else
        {
            // Ghost / Titan: 이동 중에도 계속 사격
            owner.TryFire();
        }
    }

    private void MoveAimToward(Vector2 targetScreenPos)
    {
        if (crossHairRect == null) return;

        // 현재 크로스헤어 위치를 목표로 Lerp 이동
        Vector2 current = crossHairRect.position;
        Vector2 next = Vector2.Lerp(current, aimTargetScreenPos, Time.deltaTime * aimSpeed);
        crossHairRect.position = next;

        // 목표 도달 시 새 aimTargetScreenPos 갱신 (aimSpread 오프셋 적용)
        if (Vector2.Distance(current, aimTargetScreenPos) < 5f)
        {
            Vector2 spread = Random.insideUnitCircle * aimSpread;
            aimTargetScreenPos = targetScreenPos + spread;
        }
    }

    private void SimulateViperFire()
    {
        // Viper의 HandleFireRelease를 AI에서 직접 트리거
        // Viper는 OnFireRelease 이벤트를 구독 중 → InputManager 우회
        var viper = owner as Viper;
        if (viper != null)
            viper.AIFire(); // ← Viper에 추가할 public 메서드
    }

    private void ValidateAndSelectTarget()
    {
        bool isValid = currentTarget != null
                    && currentTarget.IsAlive
                    && currentTarget.gameObject.activeSelf;

        if (!isValid)
        {
            currentTarget = SelectRandomEnemy();

            // 새 타겟 선택 시 aimSpread 즉시 반영
            if (currentTarget != null)
            {
                Vector2 targetScreenPos = Camera.main.WorldToScreenPoint(currentTarget.transform.position);
                Vector2 spread = Random.insideUnitCircle * aimSpread;
                aimTargetScreenPos = targetScreenPos + spread;
            }
        }
    }

    private EnemyBase SelectRandomEnemy()
    {
        var enemies = waveManager.ActiveEnemies;
        if (enemies == null || enemies.Count == 0) return null;
        return enemies[Random.Range(0, enemies.Count)];
    }
}