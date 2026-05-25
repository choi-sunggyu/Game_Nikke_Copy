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
    private Vector2 lineEndScreenPos; // 빨간 막대기 끝점 (스크린 좌표)
    private bool lineEndInitialized = false;
    [SerializeField] private float lineEndMoveSpeed = 3f; // 정속도 이동 속도 (Inspector 조절)

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

        lineEndScreenPos = new Vector2(Screen.width / 2f, Screen.height / 2f);
        lineEndInitialized = false;
    }

    void Update()
    {
        if (characterManager.CurrentCharacter == owner)
        {
            if (lineRenderer != null && lineRenderer.enabled)
                lineRenderer.enabled = false;
            return;
        }

        if (!owner.IsAlive) return;
        if (characterManager.IsCovering) return;

        bool isPlayerFiring = Input.GetMouseButton(0);
        bool isFocusActive = BurstGaugeManager.Instance != null &&
                            BurstGaugeManager.Instance.IsFinalBurstActive;

        if (isFocusActive)
        {
            if (isPlayerFiring)
            {
                // 클릭 중 → 즉각 집중사격
                UpdateFocusFire();
            }
            else
            {
                // 클릭 해제 → 가이드라인 유지 + AI 사격
                UpdateAIShotWithLine();
            }
            return;
        }

        // 집중사격 아닐 때 → 일반 AI
        if (lineRenderer != null && lineRenderer.enabled)
        {
            lineRenderer.enabled = false;
            lineEndInitialized = false;
        }
        UpdateAIShot();
    }

    private void UpdateAIShot()
    {
        ValidateAndSelectTarget();

        if (currentTarget == null)
        {
            owner.OnStopFiring();
            owner.TryReload();
            return;
        }

        Vector2 targetScreenPos = Camera.main.WorldToScreenPoint(currentTarget.transform.position);
        MoveAimToward(targetScreenPos);

        if (isViper)
        {
            float dist = Vector2.Distance(crossHairRect.position, targetScreenPos);
            if (dist <= viperFireThreshold) { owner.TryFire(); SimulateViperFire(); }
            else owner.TryFire();
        }
        else
        {
            owner.TryFire();
        }
    }

    public void FireLine(Vector2 targetScreenPos)
    {
        if (lineRenderer == null) return;

        bool shouldShow =
            owner.IsAlive &&
            BurstGaugeManager.Instance != null &&
            BurstGaugeManager.Instance.IsFinalBurstActive &&
            characterManager.CurrentCharacter != owner;

        if (!shouldShow)
        {
            lineRenderer.enabled = false;
            lineEndInitialized = false;
            return;
        }

        Vector3 worldEndPoint = owner.GetWorldTargetFromScreenPos(targetScreenPos);
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, owner.MuzzlePoint.position);
        lineRenderer.SetPosition(1, worldEndPoint);
    }

    private void UpdateFocusFire()
    {
        CharacterBase activeCharacter = characterManager.CurrentCharacter;
        if (activeCharacter == null || activeCharacter.CrossHair == null) return;

        Vector2 focusScreenPos = activeCharacter.CrossHair.CrossHairPosition;
        Vector3 worldTarget = owner.GetWorldTargetFromScreenPos(focusScreenPos);

        // 크로스헤어도 즉각 이동
        if (crossHairRect != null)
            crossHairRect.position = focusScreenPos;

        // 가이드라인 끝점 즉각 갱신
        lineEndScreenPos = focusScreenPos;
        lineEndInitialized = true;
        FireLine(focusScreenPos);

        if (isViper)
        {
            float dist = Vector2.Distance(crossHairRect.position, focusScreenPos);
            if (dist <= viperFireThreshold)
                SimulateViperFireAtTarget(worldTarget);
            else
                owner.TryFire();
        }
        else
        {
            owner.TryFireAtTarget(worldTarget);
        }
    }

    private void UpdateAIShotWithLine()
    {
        ValidateAndSelectTarget();

        if (currentTarget == null)
        {
            owner.OnStopFiring();
            owner.TryReload();
            // 가이드라인은 유지 (lineEndScreenPos 마지막 위치 그대로)
            if (lineEndInitialized) FireLine(lineEndScreenPos);
            return;
        }

        Vector2 targetScreenPos = Camera.main.WorldToScreenPoint(currentTarget.transform.position);
        MoveAimToward(targetScreenPos); // AI 크로스헤어는 정상 이동

        // 가이드라인 끝점은 AI 크로스헤어 현재 위치로 부드럽게 이동
        if (crossHairRect != null)
        {
            lineEndScreenPos = Vector2.Lerp(
                lineEndScreenPos,
                crossHairRect.position,
                Time.deltaTime * lineEndMoveSpeed
            );
        }

        FireLine(lineEndScreenPos);

        // AI 사격
        if (isViper)
        {
            float dist = Vector2.Distance(crossHairRect.position, targetScreenPos);
            if (dist <= viperFireThreshold) { owner.TryFire(); SimulateViperFire(); }
            else owner.TryFire();
        }
        else
        {
            owner.TryFire();
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