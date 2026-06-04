using System;
using UnityEngine;

public class CharacterAI : MonoBehaviour
{
    [Header("조준 설정")]
    [SerializeField] private float aimSpeed = 5f;           // 크로스헤어 추적 속도
    [SerializeField] private float aimSpread = 30f;         // 목표 지점 최대 오차 반경 (픽셀)
    [SerializeField] private float viperFireThreshold = 30f; // Viper 발사 오차 허용 픽셀
    [SerializeField] private float lineEndMoveSpeed = 3f;    // 가이드라인 정속도 이동 속도

    private CharacterBase owner;
    private CharacterManager characterManager;
    private WaveManager waveManager;
    private EnemyBase currentTarget;

    private RectTransform crossHairRect;
    private LineRenderer lineRenderer;
    private bool isViper;
    private Vector2 lineEndScreenPos; 
    private bool lineEndInitialized = false;
    private bool _battleStarted = false;

    // [전역 관리] static 필드
    private static bool isAutoScopeMode = false; 
    public static bool IsAutoScopeMode => isAutoScopeMode;

    // ★ [외부 연동용 핵심 프로퍼티] 현재 플레이어 캐릭터의 크로스헤어를 AI가 제어해야 하는 상태인가?
    // 다른 마우스 조작 스크립트에서 이 값이 true일 때는 마우스 위치 고정을 꺼야 합니다.
    public static bool IsAiControllingCrosshair => isAutoScopeMode && !Input.GetMouseButton(0);

    // [디테일 연출] 유기적인 조준 오차 연출용 변수
    private Vector2 smoothSpreadOffset;
    private float spreadChangeTimer = 0f;

    // 마지막 사격 위치 기억용 (쓸어 쏘기용)
    private Vector2 lastFireScreenPos;

    public static event Action<bool> OnAutoScopeModeChanged;

    void Awake()
    {
        owner = GetComponent<CharacterBase>();
        characterManager = FindAnyObjectByType<CharacterManager>();
        waveManager = FindAnyObjectByType<WaveManager>();

        isViper = owner is Viper;
    }

    void OnEnable()
    {
        BattleIntroManager.OnBattleIntroComplete += OnIntroComplete;
        BurstGaugeManager.OnFocusFireEnd += DisableFocusFire;
    }

    void OnDisable()
    {
        BattleIntroManager.OnBattleIntroComplete -= OnIntroComplete;
        BurstGaugeManager.OnFocusFireEnd -= DisableFocusFire;
    }

    void OnIntroComplete() => _battleStarted = true;
    public static void ToggleAutoScopeMode()
    {
        isAutoScopeMode = !isAutoScopeMode;
        OnAutoScopeModeChanged?.Invoke(isAutoScopeMode);

        CharacterManager charManager = UnityEngine.Object.FindAnyObjectByType<CharacterManager>();
        
        if (charManager != null)
        {
            CharacterBase activeChar = charManager.CurrentCharacter;
            if (activeChar != null)
            {
                CharacterAI activeAI = activeChar.GetComponent<CharacterAI>();
                if (activeAI != null && activeAI.crossHairRect != null)
                {
                    if (isAutoScopeMode)
                    {
                        if (activeAI.currentTarget != null)
                        {
                            Vector2 targetScreenPos = Camera.main.WorldToScreenPoint(activeAI.currentTarget.transform.position);
                            activeAI.crossHairRect.position = targetScreenPos;
                        }
                    }
                    else
                    {
                        activeAI.crossHairRect.position = activeAI.lastFireScreenPos;
                    }
                }
            }
        }
    }

    void DisableFocusFire()
    {
        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    void Start()
    {
        if (owner.CrossHair != null)
            crossHairRect = owner.CrossHair.GetComponent<RectTransform>();

        Vector2 centerPos = new Vector2(Screen.width / 2f, Screen.height / 2f);
        lastFireScreenPos = centerPos;

        if (crossHairRect != null)
            crossHairRect.position = centerPos;

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

        lineEndScreenPos = centerPos;
        lineEndInitialized = false;

        OnAutoScopeModeChanged?.Invoke(isAutoScopeMode);
    }

    void Update()
    {
        if (!_battleStarted) return;
        if (characterManager.CurrentCharacter == owner && Input.GetKeyDown(KeyCode.LeftShift))
        {
            ToggleAutoScopeMode();
        }

        // 현재 직접 조작 중인 메인 캐릭터의 제어 로직
        if (characterManager.CurrentCharacter == owner)
        {
            if (lineRenderer != null && lineRenderer.enabled)
                lineRenderer.enabled = false;

            if (!owner.IsAlive || owner.CurrentState == CharacterState.Reload)
                return;

            // 엄폐 중이어도 자동사격 모드면 AI 사격 계속
            if (characterManager.IsCovering && !isAutoScopeMode)
                return;

            bool isPlayerFiring = Input.GetMouseButton(0);

            if (isAutoScopeMode)
            {
                if (isPlayerFiring)
                {
                    HandleManualOverrideInput(); // 자동사격 중 클릭 시 유저 마우스 위치 오버라이드
                }
                else
                {
                    UpdateAIShot(); // 마우스 클릭 해제 시 AI가 적을 따라다니며 자동 사격
                }
            }
            else
            {
                if (isPlayerFiring)
                {
                    HandleManualOverrideInput(); // 수동 모드일 때는 클릭할 때만 그 위치로 사격
                }
                else
                {
                    owner.OnStopFiring(); // 클릭 해제 시 사격 중지 (단순 마우스 이동엔 반응 X)
                }
            }
            return; 
        }

        // 대기 중인 다른 팀원 AI들의 로직
        if (!owner.IsAlive || characterManager.IsCovering || owner.CurrentState == CharacterState.Reload) 
        {
            if (lineRenderer != null && lineRenderer.enabled)
            {
                lineRenderer.enabled = false;
                lineEndInitialized = false;
            }
            return;
        }

        bool isPlayerClicking = Input.GetMouseButton(0);
        bool isFocusActive = BurstGaugeManager.Instance != null && BurstGaugeManager.Instance.IsFinalBurstActive;

        if (isFocusActive)
        {
            if (isPlayerClicking) UpdateFocusFire();
            else UpdateAIShotWithLine();
            return;
        }

        if (lineRenderer != null && lineRenderer.enabled)
        {
            lineRenderer.enabled = false;
            lineEndInitialized = false;
        }
        UpdateAIShot();
    }

    private void HandleManualOverrideInput()
    {
        Vector2 mousePos = Input.mousePosition;

        if (crossHairRect != null)
            crossHairRect.position = mousePos;

        Vector3 worldTarget = owner.GetWorldTargetFromScreenPos(mousePos);
        lastFireScreenPos = mousePos;

        // Viper는 자체 입력 이벤트(HandleFirePress/HandleFireRelease)로 차지-발사를 처리하므로
        // CharacterAI에서 직접 발사 호출 시 차지 없이 즉발 사격되는 문제를 방지
        if (!isViper)
            FireWeapon(worldTarget, mousePos);
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
        MoveAimToward(targetScreenPos); // 크로스헤어를 적 위치로 자동 이동

        // AI 크로스헤어에도 에임 어시스트 적용
        Vector2 assisted = owner.CrossHair.ApplyAimAssist(crossHairRect.position , true);
        crossHairRect.position = assisted;

        Vector3 worldTarget = owner.GetWorldTargetFromScreenPos(crossHairRect.position);
        lastFireScreenPos = crossHairRect.position; 

        FireWeapon(worldTarget, targetScreenPos); // 해당 크로스헤어 위치로 자동 사격 실행
    }

    private void UpdateFocusFire()
    {
        CharacterBase activeCharacter = characterManager.CurrentCharacter;
        if (activeCharacter == null || activeCharacter.CrossHair == null) return;

        Vector2 focusScreenPos = activeCharacter.CrossHair.CrossHairPosition;
        Vector3 worldTarget = owner.GetWorldTargetFromScreenPos(focusScreenPos);
        
        if (crossHairRect != null)
            crossHairRect.position = focusScreenPos;

        lineEndScreenPos = focusScreenPos;
        lineEndInitialized = true;
        
        FireLine(focusScreenPos);
        lastFireScreenPos = focusScreenPos; 

        FireWeapon(worldTarget, focusScreenPos);
    }

    private void UpdateAIShotWithLine()
    {
        ValidateAndSelectTarget();

        if (currentTarget == null)
        {
            owner.OnStopFiring();
            owner.TryReload();
            if (lineEndInitialized) FireLine(lineEndScreenPos);
            return;
        }

        Vector2 targetScreenPos = Camera.main.WorldToScreenPoint(currentTarget.transform.position);
        MoveAimToward(targetScreenPos); 

        if (crossHairRect != null)
        {
            lineEndScreenPos = Vector2.Lerp(
                lineEndScreenPos,
                crossHairRect.position,
                lineEndMoveSpeed * Time.deltaTime
            );
        }

        FireLine(lineEndScreenPos);

        Vector3 worldTarget = owner.GetWorldTargetFromScreenPos(crossHairRect.position);
        lastFireScreenPos = crossHairRect.position; 

        FireWeapon(worldTarget, targetScreenPos);
    }

    public void FireLine(Vector2 targetScreenPos)
    {
        if (lineRenderer == null) return;

        bool shouldShow = owner.IsAlive &&
                          owner.CurrentState != CharacterState.Reload && 
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

    private void FireWeapon(Vector3 worldTarget, Vector2 targetScreenPos)
    {
        if (isViper)
        {
            float dist = Vector2.Distance(crossHairRect.position, targetScreenPos);
            if (dist <= viperFireThreshold)
            {
                owner.TryFireAtTarget(worldTarget);
            }
            else owner.TryFire();
        }
        else
        {
            owner.TryFireAtTarget(worldTarget);
        }
    }

    private void MoveAimToward(Vector2 targetScreenPos)
    {
        if (crossHairRect == null) return;

        spreadChangeTimer -= Time.deltaTime;
        if (spreadChangeTimer <= 0f)
        {
            smoothSpreadOffset = UnityEngine.Random.insideUnitCircle * aimSpread;
            spreadChangeTimer = UnityEngine.Random.Range(0.15f, 0.35f);
        }

        Vector2 desiredPos = targetScreenPos + smoothSpreadOffset;
        Vector2 current = crossHairRect.position;
        
        Vector2 next = Vector2.Lerp(current, desiredPos, 1f - Mathf.Exp(-aimSpeed * Time.deltaTime));
        crossHairRect.position = next;
    }

    private void ValidateAndSelectTarget()
    {
        bool isValid = currentTarget != null
                    && currentTarget.IsAlive
                    && currentTarget.gameObject.activeSelf;

        if (!isValid)
        {
            currentTarget = SelectRandomEnemy();

            if (currentTarget != null)
            {
                if (crossHairRect != null)
                {
                    crossHairRect.position = lastFireScreenPos;
                }
            }
        }
    }

    private EnemyBase SelectRandomEnemy()
    {
        var enemies = waveManager.ActiveEnemies;
        if (enemies == null || enemies.Count == 0) return null;
        return enemies[UnityEngine.Random.Range(0, enemies.Count)];
    }
}