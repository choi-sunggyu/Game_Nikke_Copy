using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카메라 시스템 단일 진실(Single Source of Truth).
///
/// 설계 원칙:
///   1. 인스펙터 값은 Awake 에서 캐싱. Update 는 캐싱된 값만 사용 (빠른 Iteration).
///   2. CameraMode 상태 머신 — 누가 카메라를 제어 중인지 명확.
///   3. 모든 외부 진입점은 public API 한 곳에 모음 (FollowCharacter / FocusOn / Enter|ExitCinematic / Freeze).
///   4. 외부에서 cam.fieldOfView 같은 내부 필드를 직접 만지지 않는다 → SetTargetFov() 사용.
///
/// 모드 전환:
///   FollowCharacter ←→ FocusTransform ←→ Cinematic → Frozen(게임 종료)
/// </summary>
public class CameraController : MonoBehaviour
{
    public enum CameraMode
    {
        FollowCharacter, // 캐릭터 인덱스 추적 (기본)
        FocusTransform,  // 임의 Transform 추적 (엘리트 등장 등)
        Cinematic,       // 외부 코루틴이 위치/FOV 단독 제어 (보스 등장/사망)
        Frozen           // 완전 정지 (게임 종료)
    }

    // ═══════════════════════════════════════════════════════
    //  인스펙터 (Awake 에서 캐싱)
    // ═══════════════════════════════════════════════════════
    [Header("── 캐릭터 추적 ──────────────")]
    [SerializeField] private List<CharacterBase> characters;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private float cameraZOffset = -10f;

    [Header("── FOV ──────────────────────")]
    [SerializeField] private Camera cam;
    [SerializeField] private float defaultFov = 34f;
    [SerializeField] private float zoomSpeed = 5f;

    [Header("── Pitch (마우스 Y 로 시선 상하) ─")]
    [Tooltip("pitch 보간 속도 — lean 과 동일 6 권장")]
    [SerializeField] private float tiltSpeed = 6f;

    [Header("── 엄폐물 회전 ───────────────")]
    [SerializeField] private Transform blockPivot;
    [SerializeField] private float blockAnimDuration = 0.3f;

    // ═══════════════════════════════════════════════════════
    //  캐싱된 인스펙터 값 — Awake 에서 1회 복사, 이후 변경 안 함
    // ═══════════════════════════════════════════════════════
    private float   _cachedMoveSpeed;
    private Vector3 _cachedCameraOffset;
    private float   _cachedCameraZOffset;
    private float   _cachedDefaultFov;
    private float   _cachedZoomSpeed;
    private float   _cachedTiltSpeed;
    private float   _defaultPitch;      // 초기 rotation.x (씬 설정값 기억)

    // ═══════════════════════════════════════════════════════
    //  내부 상태
    // ═══════════════════════════════════════════════════════
    private CameraMode _mode = CameraMode.FollowCharacter;
    private int        _characterIndex = -1;
    private Transform  _focusTarget;
    private float      _targetFov;
    private Vector3    _targetPosition;
    private float      _targetPitch;    // rotation.x 목표값 (default + tilt offset)

    private Coroutine _blockCoroutine;
    private bool      _blocksVisible = true;

    // ═══════════════════════════════════════════════════════
    //  Public 프로퍼티 — 외부에서 읽기 전용 접근
    // ═══════════════════════════════════════════════════════
    public CameraMode Mode                   => _mode;
    public int        CurrentCharacterIndex  => _characterIndex;

    // ═══════════════════════════════════════════════════════
    //  Lifecycle
    // ═══════════════════════════════════════════════════════
    void Awake()
    {
        // 인스펙터 값 캐싱 — 이후 Update 는 캐싱된 값만 사용
        _cachedMoveSpeed     = moveSpeed;
        _cachedCameraOffset  = cameraOffset;
        _cachedCameraZOffset = cameraZOffset;
        _cachedDefaultFov    = defaultFov;
        _cachedZoomSpeed     = zoomSpeed;
        _cachedTiltSpeed     = tiltSpeed;
        _targetFov           = _cachedDefaultFov;

        // 씬 인스펙터에서 설정한 초기 rotation.x 를 기본값으로 기억
        // → tilt=0 요청 시 이 값으로 복귀
        _defaultPitch        = transform.eulerAngles.x;
        _targetPitch         = _defaultPitch;
    }

    void Start()
    {
        if (characters == null || characters.Count == 0)
        {
            Debug.LogError("[CameraController] characters 리스트 미할당.");
            enabled = false;
            return;
        }

        // 가운데 캐릭터 추적 시작
        int mid = characters.Count / 2;
        FollowCharacter(mid);

        // 시작 위치 즉시 적용 (Lerp 없음)
        Vector3 charPos = characters[mid].transform.position;
        _targetPosition = ComputeCameraPositionFor(charPos);
        transform.position = _targetPosition;

        // 2.5D Sprite 렌더링 순서: Z축 기준 정렬 (Camera Depth 방향 투명도 정렬)
        // 같은 SortingLayer/Order 내에서 Z가 큰 오브젝트(적 방향)를 뒤, Z가 작은 것을 앞에 렌더링
        if (cam != null)
        {
            cam.transparencySortMode = TransparencySortMode.CustomAxis;
            cam.transparencySortAxis = Vector3.forward; // (0,0,1) — Z 클수록 뒤
        }
    }

    void OnEnable()
    {
        InputManager.OnSwitchCharacter += FollowCharacter;
    }

    void OnDisable()
    {
        InputManager.OnSwitchCharacter -= FollowCharacter;
    }

    void Update()
    {
        // 모드별 위치 업데이트
        switch (_mode)
        {
            case CameraMode.FollowCharacter:
                UpdateFollowCharacter();
                break;
            case CameraMode.FocusTransform:
                UpdateFocusTransform();
                break;
            case CameraMode.Cinematic:
            case CameraMode.Frozen:
                // 외부 코루틴 / 완전 정지 — Update 는 아무것도 안 함
                break;
        }

        // FOV 보간 (Cinematic / Frozen 제외)
        if (_mode != CameraMode.Cinematic && _mode != CameraMode.Frozen)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, _targetFov, _cachedZoomSpeed * Time.deltaTime);

            // Pitch 보간 (rotation.x). Cinematic / Frozen 은 외부가 rotation 을 직접 제어할 수 있으므로 제외.
            Vector3 euler = transform.eulerAngles;
            euler.x = Mathf.LerpAngle(euler.x, _targetPitch, _cachedTiltSpeed * Time.deltaTime);
            transform.eulerAngles = euler;
        }
    }

    private void UpdateFollowCharacter()
    {
        if (_characterIndex < 0 || _characterIndex >= characters.Count) return;
        var c = characters[_characterIndex];
        if (c == null) return;

        _targetPosition = ComputeCameraPositionFor(c.transform.position);
        transform.position = Vector3.Lerp(transform.position, _targetPosition, _cachedMoveSpeed * Time.deltaTime);
    }

    private void UpdateFocusTransform()
    {
        if (_focusTarget == null)
        {
            // 타겟 사라짐 (예: 엘리트 적 사망) → 캐릭터 복귀
            FollowCharacter(_characterIndex);
            return;
        }

        _targetPosition = ComputeCameraPositionFor(_focusTarget.position);
        transform.position = Vector3.Lerp(transform.position, _targetPosition, _cachedMoveSpeed * Time.deltaTime);
    }

    // ═══════════════════════════════════════════════════════
    //  Public API — 외부에서 모드 진입
    // ═══════════════════════════════════════════════════════

    /// <summary>캐릭터 인덱스 추적. 키 입력 / 사망 자동 전환 등에서 호출.</summary>
    public void FollowCharacter(int index)
    {
        if (characters == null || index < 0 || index >= characters.Count)
        {
            Debug.LogWarning($"[CameraController] FollowCharacter({index}) — characters 크기 부족 ({characters?.Count ?? 0})");
            return;
        }
        if (!characters[index].IsAlive) return;

        _mode             = CameraMode.FollowCharacter;
        _characterIndex   = index;
        _focusTarget      = null;
        _targetFov        = _cachedDefaultFov;

        // 엄폐물 토글
        //   X(1), V(3) 키 캐릭터 = 후방 → 엄폐물 보임 (사격 라인이 엄폐물 위로 지나감)
        //   Z(0), C(2), B(4) 키 캐릭터 = 전방 → 엄폐물 내려감 (시야 확보)
        //   X ↔ V 간 이동 시 둘 다 후방이라 _blocksVisible 변화 없음 (가드로 자연 처리)
        bool isFront = (index != 1 && index != 3);
        if (isFront && _blocksVisible)
        {
            if (_blockCoroutine != null) StopCoroutine(_blockCoroutine);
            _blockCoroutine = StartCoroutine(RotateBlocks(false));
        }
        else if (!isFront && !_blocksVisible)
        {
            if (_blockCoroutine != null) StopCoroutine(_blockCoroutine);
            _blockCoroutine = StartCoroutine(RotateBlocks(true));
        }
    }

    /// <summary>임의 Transform 포커스 + FOV 조정. 타겟 null 되면 캐릭터 복귀.</summary>
    public void FocusOn(Transform target, float fov)
    {
        if (target == null) return;
        _mode        = CameraMode.FocusTransform;
        _focusTarget = target;
        _targetFov   = fov;
    }

    /// <summary>FOV 만 조정 (모드 유지). 엘리트 등장 줌인 등에서 사용.</summary>
    public void SetTargetFov(float fov) => _targetFov = fov;

    /// <summary>FOV 를 default 로 복귀.</summary>
    public void ResetFov() => _targetFov = _cachedDefaultFov;

    /// <summary>
    /// 카메라 pitch (rotation.x) 목표값 오프셋 지정. 기본 pitch + 인자만큼 조정.
    /// 예: pitchOffset=+4 → 카메라가 아래로 4도, pitchOffset=-4 → 위로 4도.
    /// 사격 중 CharacterAimLean 이 마우스 Y 위치 기반으로 매 프레임 호출.
    /// </summary>
    public void SetTargetPitch(float pitchOffset) => _targetPitch = _defaultPitch + pitchOffset;

    /// <summary>pitch 를 씬 초기값으로 복귀.</summary>
    public void ResetPitch() => _targetPitch = _defaultPitch;

    /// <summary>Cinematic 진입 — 외부 코루틴이 위치/FOV 단독 제어. Update 는 정지.</summary>
    public void EnterCinematic() => _mode = CameraMode.Cinematic;

    /// <summary>Cinematic 종료. resumeCharacterFollow=true 면 캐릭터 추적 복귀, false 면 Frozen.</summary>
    public void ExitCinematic(bool resumeCharacterFollow = true)
    {
        if (resumeCharacterFollow && _characterIndex >= 0)
            FollowCharacter(_characterIndex);
        else
            _mode = CameraMode.Frozen;
    }

    /// <summary>완전 정지 (게임 종료 시). 모든 입력 무시.</summary>
    public void Freeze()
    {
        _mode = CameraMode.Frozen;
        InputManager.OnSwitchCharacter -= FollowCharacter;
    }

    /// <summary>Cinematic 중 외부 코루틴이 직접 위치 설정.</summary>
    public void SetPositionImmediate(Vector3 pos) => transform.position = pos;

    /// <summary>Cinematic 중 외부 코루틴이 직접 FOV 설정.</summary>
    public void SetFovImmediate(float fov) => cam.fieldOfView = fov;

    /// <summary>worldPos 기준으로 카메라가 잡아야 할 위치 계산 (offset 적용).</summary>
    public Vector3 ComputeCameraPositionFor(Vector3 worldPos)
    {
        Vector3 pos = worldPos + _cachedCameraOffset;
        pos.z = worldPos.z + _cachedCameraZOffset;
        return pos;
    }

    private IEnumerator RotateBlocks(bool show)
    {
        _blocksVisible = show;
        float startAngle = show ? 90f : 0f;
        float endAngle   = show ? 0f  : 90f;

        float elapsed = 0f;
        while (elapsed < blockAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / blockAnimDuration);
            float angle = Mathf.Lerp(startAngle, endAngle, t);
            blockPivot.localEulerAngles = new Vector3(angle, 0f, 0f);
            yield return null;
        }
        blockPivot.localEulerAngles = new Vector3(endAngle, 0f, 0f);
    }

    // ═══════════════════════════════════════════════════════
    //  Cinematic 코루틴 (보스 등장 / 보스 사망)
    //   - 내부적으로 EnterCinematic → SetPositionImmediate → ExitCinematic / Freeze 흐름.
    //   - SetPositionImmediate / SetFovImmediate 만 사용 (Update 의 Lerp 와 충돌 없음).
    // ═══════════════════════════════════════════════════════

    public void StartBossAppearCinematic(EnemyBase boss, float duration, float zoomFov, Action onComplete)
    {
        StartCoroutine(BossAppearRoutine(boss, duration, zoomFov, onComplete));
    }

    public void StartBossDeathCinematic(float zoomFov, float zoomDuration, Vector3 bossPosition, Action onComplete)
    {
        StartCoroutine(BossDeathRoutine(zoomFov, zoomDuration, bossPosition, onComplete));
    }

    private IEnumerator BossAppearRoutine(EnemyBase boss, float duration, float zoomFov, Action onComplete)
    {
        EnterCinematic();

        // duration 을 2등분 — 절반은 줌인, 절반은 줌아웃.
        // 보스 1초 보여주는 효과 ★ 슬로우 느낌 제거
        float halfDuration = duration * 0.5f;

        float   startFov   = cam.fieldOfView;
        Vector3 startPos   = transform.position;
        Vector3 bossTarget = ComputeCameraPositionFor(boss.transform.position);

        // ── Phase 1: 줌인 (캐릭터 → 보스) ──
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / halfDuration);
            float eased = EaseInOutCubic(t);

            SetPositionImmediate(Vector3.Lerp(startPos, bossTarget, eased));
            SetFovImmediate(Mathf.Lerp(startFov, zoomFov, eased));
            yield return null;
        }

        // ── Phase 2: 줌아웃 (보스 → 활성 캐릭터, FOV 복귀) ──
        Vector3 zoomedPos = transform.position;
        float   zoomedFov = cam.fieldOfView;

        // 활성 캐릭터 위치를 phase2 시작 시점에 계산 (캐릭터가 이동 중일 가능성 대비)
        Vector3 charTarget = (_characterIndex >= 0 && _characterIndex < characters.Count)
            ? ComputeCameraPositionFor(characters[_characterIndex].transform.position)
            : startPos;

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / halfDuration);
            float eased = EaseInOutCubic(t);

            SetPositionImmediate(Vector3.Lerp(zoomedPos, charTarget, eased));
            SetFovImmediate(Mathf.Lerp(zoomedFov, _cachedDefaultFov, eased));
            yield return null;
        }

        // 캐릭터 추적 모드로 복귀 — 카메라가 이미 캐릭터 위치에 있으므로 점프 없음
        ExitCinematic(resumeCharacterFollow: true);

        onComplete?.Invoke();
    }

    private static float EaseInOutCubic(float t)
    {
        return t < 0.5f
            ? 4f * t * t * t
            : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
    }

    private IEnumerator BossDeathRoutine(float zoomFov, float zoomDuration, Vector3 bossPosition, Action onComplete)
    {
        EnterCinematic();

        Vector3 deathTarget = ComputeCameraPositionFor(bossPosition);
        float startFov = cam.fieldOfView;

        float elapsed = 0f;
        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / zoomDuration;

            SetPositionImmediate(Vector3.Lerp(transform.position, deathTarget, 8f * Time.deltaTime));
            SetFovImmediate(Mathf.Lerp(startFov, zoomFov, t));
            yield return null;
        }

        // ★ 보스 위치에서 동결 (게임 종료 UI 표시 동안 카메라 유지)
        Freeze();

        onComplete?.Invoke();
    }
}
