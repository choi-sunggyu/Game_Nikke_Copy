using UnityEngine;

public class CharacterAI : MonoBehaviour
{
    [SerializeField] private float aimSpeed = 3f;           // 크로스헤어 이동 속도
    [SerializeField] private float aimSpread = 30f;         // 목표 지점 오차 반경 (픽셀)
    [SerializeField] private float viperFireThreshold = 30f; // Viper 발사 오차 허용 픽셀
    [SerializeField] private float lineEndMoveSpeed = 3f;    // 가이드라인 정속도 이동 속도

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

    // [개선] 마지막 사격 위치를 기억하여 쓸어 쏘기(밀고 가기) 구현용 변수
    private Vector2 lastFireScreenPos; 

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

    // 집중사격 종료 시 가이드라인 숨김
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

        // 초기 조준 위치 및 마지막 사격 위치 = 화면 중앙 초기화
        Vector2 centerPos = new Vector2(Screen.width / 2f, Screen.height / 2f);
        aimTargetScreenPos = centerPos;
        lastFireScreenPos = centerPos;

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

        lineEndScreenPos = centerPos;
        lineEndInitialized = false;
    }

    void Update()
    {
        // 플레이어가 현재 조작 중인 캐릭터라면 AI 로직 제외
        if (characterManager.CurrentCharacter == owner)
        {
            if (lineRenderer != null && lineRenderer.enabled)
                lineRenderer.enabled = false;
            return;
        }

        // [수정] owner.CurrentState를 체크하여 리로딩 중일 때 가이드라인 비활성화 및 사격 중지
        if (!owner.IsAlive || characterManager.IsCovering || owner.CurrentState == CharacterState.Reload) 
        {
            if (lineRenderer != null && lineRenderer.enabled)
            {
                lineRenderer.enabled = false;
                lineEndInitialized = false;
            }
            return;
        }

        bool isPlayerFiring = Input.GetMouseButton(0);
        bool isFocusActive = BurstGaugeManager.Instance != null &&
                            BurstGaugeManager.Instance.IsFinalBurstActive;

        if (isFocusActive)
        {
            if (isPlayerFiring)
            {
                UpdateFocusFire(); // 클릭 중 → 즉각 집중사격
            }
            else
            {
                UpdateAIShotWithLine(); // 클릭 해제 → 가이드라인 유지 + AI 사격
            }
            return;
        }

        // 집중사격 아닐 때 → 일반 AI (가이드라인 끔)
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

        // [수정] CharacterBase의 GetWorldTargetFromScreenPos 메서드 활용
        Vector3 worldTarget = owner.GetWorldTargetFromScreenPos(crossHairRect.position);
        lastFireScreenPos = crossHairRect.position; // 현재 발사 위치를 마지막 위치로 저장

        FireWeapon(worldTarget, targetScreenPos);
    }

    private void UpdateFocusFire()
    {
        CharacterBase activeCharacter = characterManager.CurrentCharacter;
        if (activeCharacter == null || activeCharacter.CrossHair == null) return;

        Vector2 focusScreenPos = activeCharacter.CrossHair.CrossHairPosition;

        // [수정] CharacterBase 내장 메서드로 정확한 히트 포인트 월드 좌표 획득
        Vector3 worldTarget = owner.GetWorldTargetFromScreenPos(focusScreenPos);
        
        // 크로스헤어도 즉각 이동
        if (crossHairRect != null)
            crossHairRect.position = focusScreenPos;

        // 가이드라인 끝점 즉각 갱신
        lineEndScreenPos = focusScreenPos;
        lineEndInitialized = true;
        
        FireLine(focusScreenPos);
        lastFireScreenPos = focusScreenPos; // 현재 발사 위치 저장

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
        MoveAimToward(targetScreenPos); // AI 크로스헤어 정상 이동

        // 가이드라인 끝점은 AI 크로스헤어 현재 위치로 부드럽게 이동
        if (crossHairRect != null)
        {
            lineEndScreenPos = Vector2.Lerp(
                lineEndScreenPos,
                crossHairRect.position,
                lineEndMoveSpeed * Time.deltaTime
            );
        }

        FireLine(lineEndScreenPos);

        // [수정] 실제 크로스헤어가 가리키는 화면의 월드 좌표 획득
        Vector3 worldTarget = owner.GetWorldTargetFromScreenPos(crossHairRect.position);
        lastFireScreenPos = crossHairRect.position; // 현재 발사 위치 저장

        FireWeapon(worldTarget, targetScreenPos);
    }

    public void FireLine(Vector2 targetScreenPos)
    {
        if (lineRenderer == null) return;

        // [수정] owner.CurrentState 체크 규칙 반영
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

        // 스크린 좌표 기준의 레이캐스트 지점을 받아와 Line 배정 (완벽한 입체감 실현)
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

        Vector2 current = crossHairRect.position;
        Vector2 next = Vector2.Lerp(current, aimTargetScreenPos, Time.deltaTime * aimSpeed);
        crossHairRect.position = next;

        if (Vector2.Distance(current, aimTargetScreenPos) < 5f)
        {
            Vector2 spread = Random.insideUnitCircle * aimSpread;
            aimTargetScreenPos = targetScreenPos + spread;
        }
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
                // [핵심 수정] 새 타겟 선택 시, 크로스헤어를 타겟으로 강제 텔레포트 시키지 않고 
                // 마지막으로 사격이 이루어졌던 화면 좌표(lastFireScreenPos)로 강제 고정합니다.
                if (crossHairRect != null)
                {
                    crossHairRect.position = lastFireScreenPos;
                }

                // 이동할 최종 목표 좌표만 새 적의 위치로 갱신하여, 마지막 위치에서부터 새 적까지 긁으면서 사격 이동하게 만듭니다.
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