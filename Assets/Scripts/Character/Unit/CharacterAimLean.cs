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
            return;
        }

        // 비활성 캐릭터: 옵션에 따라 정면 복귀 또는 자세 유지
        if (!character.IsActiveCharacter)
        {
            if (resetWhenInactive)
                currentLeanAngle = Mathf.Lerp(currentLeanAngle, 0f, leanSpeed * Time.deltaTime);
            ApplyAngle(currentLeanAngle);
            return;
        }

        // 사망/리로드 중에는 정면 복귀 (선택적 — 자연스러움 위해)
        if (!character.IsAlive)
        {
            currentLeanAngle = Mathf.Lerp(currentLeanAngle, 0f, leanSpeed * Time.deltaTime);
            ApplyAngle(currentLeanAngle);
            return;
        }

        // ── 크로스헤어 X 위치를 [-1, +1] 로 정규화 ──
        Vector2 crossPos = character.CrossHair.CrossHairPosition;
        float screenHalf = Screen.width * 0.5f;
        float normX = (crossPos.x - screenHalf) / screenHalf;
        normX = Mathf.Clamp(normX, -1f, 1f);

        // 가운데 dead zone 처리
        if (Mathf.Abs(normX) < deadZoneRatio)
            normX = 0f;
        else
            normX = Mathf.Sign(normX) * (Mathf.Abs(normX) - deadZoneRatio) / (1f - deadZoneRatio);

        // ── 목표 각도 계산 ──
        // Y축은 부호 반대 (크로스헤어가 오른쪽 → 캐릭터가 오른쪽으로 어깨 틀기 → +Y 회전)
        // Z축은 부호 그대로 (크로스헤어가 오른쪽 → 오른쪽으로 기울임 → +Z 회전)
        float targetAngle = (axis == LeanAxis.Y ? +1f : +1f) * normX * maxLeanAngle;

        // ── Lerp 보간 ──
        currentLeanAngle = Mathf.Lerp(currentLeanAngle, targetAngle, leanSpeed * Time.deltaTime);
        ApplyAngle(currentLeanAngle);
    }

    private void ApplyAngle(float angle)
    {
        Vector3 euler = axis == LeanAxis.Y
            ? new Vector3(0f, angle, 0f)
            : new Vector3(0f, 0f, angle);

        transform.localRotation = baseRotation * Quaternion.Euler(euler);
    }
}
