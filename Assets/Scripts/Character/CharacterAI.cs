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
    private LineRenderer lineRenderer;
    private bool isViper;

    void Awake()
    {
        owner = GetComponent<CharacterBase>();
        characterManager = FindAnyObjectByType<CharacterManager>();
        waveManager = FindAnyObjectByType<WaveManager>();

        isViper = owner is Viper;
    }

    void OnEnable()
    {
        BurstGaugeManager.OnFocusFireEnd += DisableFocusFire;
    }

    void OnDisable()
    {
        BurstGaugeManager.OnFocusFireEnd -= DisableFocusFire;
    }

    void DisableFocusFire()
    {
        if (lineRenderer != null)
            lineRenderer.enabled = false;
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

        lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;

            Color lineColor = new Color(1f, 0f, 0f, 0.6f);

            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;

            lineRenderer.startWidth = 0.08f;
            lineRenderer.endWidth = 0.02f;

            lineRenderer.sortingOrder = 100;
        }
    }

    void Update()
    {
        // 플레이어가 조작 중이면 가이드라인을 끄고 AI 비활성
        if (characterManager.CurrentCharacter == owner)
        {
            if (lineRenderer != null && lineRenderer.enabled)
                lineRenderer.enabled = false;
            return;
        }

        if (!owner.IsAlive) return;
        if (characterManager.IsCovering) return;

        // 플레이어 클릭(사격) 여부 확인 (※ InputManager 등 실제 게임의 입력 상태 변수로 교체하세요)
        bool isPlayerFiring = Input.GetMouseButton(0);

        // 이벤트 변수 대신 실시간으로 집중사격 상태 확인
        if (BurstGaugeManager.Instance.IsFinalBurstActive && isPlayerFiring)
        {
            UpdateFocusFire();
            return;
        }

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

        // 집중사격 단계지만 플레이어가 클릭 안 할 때: AI 타겟을 향해 가이드라인 유지
        if (BurstGaugeManager.Instance.IsFinalBurstActive)
        {
            Vector3 aiWorldTarget = owner.GetWorldTargetFromScreenPos(crossHairRect.position);
            FireLine(aiWorldTarget);
        }
        else
        {
            if (lineRenderer != null && lineRenderer.enabled)
                lineRenderer.enabled = false;
        }

        // 크로스헤어와 목표 거리
        float dist = Vector2.Distance(crossHairRect.position, targetScreenPos);

        if (isViper)
        {
            // Viper: 조준이 충분히 정확할 때만 발사 (실제 발사는 HandleFireRelease에서)
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

    public void FireLine(Vector3 worldTarget)
    {
        if (lineRenderer == null)
            return;

        // 포커스 파이어 활성 & 자신이 조작 중인 캐릭터가 아닐 때만 라인 렌더러 활성화 & 최종 버스트 단계 후 사격 위치 업데이트
        bool shouldShow =
            owner.IsAlive &&
            BurstGaugeManager.Instance != null &&
            BurstGaugeManager.Instance.IsFinalBurstActive &&
            characterManager.CurrentCharacter != owner;

        if (!shouldShow)
        {
            lineRenderer.enabled = false;
            return;
        }

        lineRenderer.enabled = true;

        lineRenderer.SetPosition(0, owner.MuzzlePoint.position);
        lineRenderer.SetPosition(1, worldTarget);
    }

    private void UpdateFocusFire()
    {
        CharacterBase activeCharacter = characterManager.CurrentCharacter;
        if (activeCharacter == null || activeCharacter.CrossHair == null) return;

        Vector2 focusScreenPos = activeCharacter.CrossHair.CrossHairPosition;

        Vector3 worldTarget =
            owner.GetWorldTargetFromScreenPos(focusScreenPos);

        // 크로스헤어 Lerp 이동은 유지 (시각적 표현용)
        MoveAimToward(focusScreenPos);
        FireLine(worldTarget);

        // 자신의 크로스헤어 무시하고 플레이어 조준점으로 직접 발사
        if (isViper)
        {
            float dist = Vector2.Distance(crossHairRect.position, focusScreenPos);
            if (dist <= viperFireThreshold)
                SimulateViperFireAtTarget(worldTarget);
            else
                owner.TryFire(); // 조준 중 상태 유지
        }
        else
        {
            owner.TryFireAtTarget(worldTarget);
        }
    }

    private void SimulateViperFireAtTarget(Vector3 worldTarget)
    {
        var viper = owner as Viper;
        if (viper != null)
        {
            owner.TryFire();
            viper.AIFire();
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