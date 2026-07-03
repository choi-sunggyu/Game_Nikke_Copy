using UnityEngine;

/// <summary>
/// 크로스헤어 위치에 따라 캐릭터를 좌우로 살짝 트는(lean) 효과.
///
/// 등을 보이는 정적 스프라이트가 좌/우 사격 시 자세 불일치로 어색해지는 문제를
/// Y축(또는 선택적으로 Z축) 회전으로 보완.
///
/// 사용:
///   - 각 캐릭터 GameObject 에 CharacterBase 와 같이 부착.
///   - Active 캐릭터일 때만 크로스헤어 추적, 비활성은 정면 자세로 천천히 복귀.
///
/// 권장 값:
///   - maxLeanAngle 10~20도. 너무 크면 등 스프라이트의 perspective 가 무너짐.
///   - leanSpeed 5~10. 크로스헤어 움직임 추종 속도 (Lerp 계수).
/// </summary>
public class CharacterAimLean : MonoBehaviour
{
    public enum LeanAxis
    {
        Y, // 어깨 트는 느낌 (3D 회전, 권장)
        Z  // 좌우로 기울임 (옆으로 기울어진 느낌)
    }

    [Header("── 참조 ────────────────────────")]
    [Tooltip("이 캐릭터의 CharacterBase. 비우면 GetComponent 로 자동 탐색")]
    [SerializeField] private CharacterBase character;

    [Header("── Lean 설정 ────────────────────")]
    [Tooltip("회전 축 — Y(어깨 트는 yaw), Z(좌우로 기울임 roll)")]
    [SerializeField] private LeanAxis axis = LeanAxis.Y;

    [Tooltip("최대 회전 각도 (도). 크로스헤어가 화면 끝에 있을 때의 각도")]
    [Range(0f, 45f)]
    [SerializeField] private float maxLeanAngle = 15f;

    [Tooltip("회전 보간 속도. 클수록 빠르게 따라감")]
    [SerializeField] private float leanSpeed = 6f;

    [Tooltip("크로스헤어 X 위치를 죽은 영역(가운데) 처리할 비율 (0=없음, 0.1=화면 가운데 10%는 회전 0)")]
    [Range(0f, 0.3f)]
    [SerializeField] private float deadZoneRatio = 0.05f;

    [Tooltip("비활성 캐릭터일 때도 마지막 자세 유지(false) 또는 정면 복귀(true)")]
    [SerializeField] private bool resetWhenInactive = true;

    [Header("── Tilt (카메라 pitch) 설정 ───")]
    [Tooltip("카메라 pitch tilt 활성화. off 면 캐릭터 lean 만 동작.")]
    [SerializeField] private bool  enableCameraTilt = true;
    [Tooltip("최대 pitch offset (도). 크로스헤어가 화면 끝일 때. 사용자 명시 4.")]
    [Range(0f, 15f)]
    [SerializeField] private float maxTiltAngle = 4f;
    [Tooltip("크로스헤어 Y 정중앙 데드존 비율 (0.05 = 화면 가운데 5% 는 tilt 0)")]
    [Range(0f, 0.3f)]
    [SerializeField] private float tiltDeadZone = 0.05f;

    private float currentLeanAngle = 0f;
    private Quaternion baseRotation;

    void Awake()
    {
        if (character == null) character = GetComponent<CharacterBase>();
        baseRotation = transform.localRotation;
    }

    void LateUpdate()
    {
        // 캐릭터/크로스헤어 없으면 스킵 (NRE 방지)
        if (character == null || character.CrossHair == null)
        {
            ApplyAngle(0f);
            RequestTilt(0f);
            return;
        }

        // 비활성 캐릭터: 옵션에 따라 정면 복귀 또는 자세 유지
        if (!character.IsActiveCharacter)
        {
            if (resetWhenInactive)
                currentLeanAngle = Mathf.Lerp(currentLeanAngle, 0f, leanSpeed * Time.deltaTime);
            ApplyAngle(currentLeanAngle);
            // tilt 는 활성 캐릭터의 몫 — 비활성이면 요청 안 함 (다른 캐릭터가 처리 중일 수 있음)
            return;
        }

        // 사망 중에는 정면 복귀
        if (!character.IsAlive)
        {
            currentLeanAngle = Mathf.Lerp(currentLeanAngle, 0f, leanSpeed * Time.deltaTime);
            ApplyAngle(currentLeanAngle);
            RequestTilt(0f);
            return;
        }

        // ★ 사격 상태 가드 — Fire 가 아니면 lean & tilt 모두 원상복귀
        //   사격 시작 → Fire 상태 → 매 프레임 lean/tilt 갱신
        //   사격 종료 → Idle/Reload 등 → 부드럽게 0 으로 복귀
        if (character.CurrentState != CharacterState.Fire)
        {
            currentLeanAngle = Mathf.Lerp(currentLeanAngle, 0f, leanSpeed * Time.deltaTime);
            ApplyAngle(currentLeanAngle);
            RequestTilt(0f);
            return;
        }

        // ── 크로스헤어 X 위치를 [-1, +1] 로 정규화 (lean) ──
        Vector2 crossPos = character.CrossHair.CrossHairPosition;
        float screenHalfW = Screen.width  * 0.5f;
        float screenHalfH = Screen.height * 0.5f;
        float normX = Mathf.Clamp((crossPos.x - screenHalfW) / screenHalfW, -1f, 1f);

        // X 가운데 dead zone 처리
        if (Mathf.Abs(normX) < deadZoneRatio)
            normX = 0f;
        else
            normX = Mathf.Sign(normX) * (Mathf.Abs(normX) - deadZoneRatio) / (1f - deadZoneRatio);

        // ── lean 목표 각도 (기존 로직) ──
        float targetLean = normX * maxLeanAngle;
        currentLeanAngle = Mathf.Lerp(currentLeanAngle, targetLean, leanSpeed * Time.deltaTime);
        ApplyAngle(currentLeanAngle);

        // ── Y 정규화 + 카메라 pitch tilt ──
        if (enableCameraTilt)
        {
            float normY = Mathf.Clamp((crossPos.y - screenHalfH) / screenHalfH, -1f, 1f);

            // Y 가운데 dead zone 처리
            if (Mathf.Abs(normY) < tiltDeadZone)
                normY = 0f;
            else
                normY = Mathf.Sign(normY) * (Mathf.Abs(normY) - tiltDeadZone) / (1f - tiltDeadZone);

            // Unity 관례: rotation.x + = 아래 시선 / - = 위 시선
            // 크로스헤어가 위 (normY=+1) → 위 시선 → pitch = -maxTiltAngle
            float pitchOffset = -normY * maxTiltAngle;
            RequestTilt(pitchOffset);
        }
    }

    /// <summary>UIManager facade 를 통해 카메라 pitch offset 요청.</summary>
    private void RequestTilt(float pitchOffset)
    {
        if (!enableCameraTilt) return;
        if (UIManager.Instance == null) return;
        UIManager.Instance.TriggerCameraTilt(pitchOffset);
    }

    private void ApplyAngle(float angle)
    {
        Vector3 euler = axis == LeanAxis.Y
            ? new Vector3(0f, angle, 0f)
            : new Vector3(0f, 0f, angle);

        transform.localRotation = baseRotation * Quaternion.Euler(euler);
    }
}
