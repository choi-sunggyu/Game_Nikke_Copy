using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 런처(RL) 전용 크로스헤어.
///
/// 컨셉:
///   - SR(ScopeCrossHair) 처럼 충전식이지만, scope 줌인은 없음 (RL 은 광역 무기).
///   - maxChargeTime 동안 마우스를 누르고 있으면 충전 게이지가 차오름.
///   - 충전 100% 도달 시 자동으로 owner.TryFire() 호출 (사용자가 떼지 않아도 발사됨).
///   - 발사 후 다시 충전하려면 마우스를 떼고 재클릭 (autoFired 플래그로 중복 방지).
///
/// RL 캐릭터(예: Anvil) 와의 분담:
///   - 이 크로스헤어: 차지 시간 관리 + 자동 발사 트리거 (UI 영역의 책임)
///   - RL 캐릭터:    TryFire() 호출 시 일반 사격과 동일하게 1발 발사 (탄 소비 / 이펙트)
/// </summary>
public class LauncherCrossHair : RifleCrossHair
{
    [Header("── Launcher Charge ──────────────")]
    [Tooltip("0 → 1 까지 차오르는 시간 (초)")]
    [SerializeField] private float maxChargeTime = 1.0f;

    [Tooltip("차지 게이지 진행 바 (fillAmount 0~1)")]
    [SerializeField] private Image chargeProgressBar;

    [Tooltip("차지 % 텍스트 (선택)")]
    [SerializeField] private TMP_Text chargePercentText;

    [Tooltip("차지 완료 시 강조 글로우 (선택)")]
    [SerializeField] private Image chargeGlow;

    [Tooltip("차지 글로우 페이드 시간 (초)")]
    [SerializeField] private float glowFadeDuration = 0.25f;

    // 내부 상태
    private float chargeStartTime = -1f;
    private bool  isCharging      = false;
    private bool  autoFired       = false; // 한 번 자동 발사 후 재차지 방지 (마우스 떼야 풀림)
    private Coroutine glowCoroutine;

    // ═══════════════════════════════════════════════════════
    //  입력 — 차지 시작 / 종료
    // ═══════════════════════════════════════════════════════
    protected override void OnFirePress()
    {
        base.OnFirePress();
        if (!isActive) return;

        // 새 차지 시작
        chargeStartTime = Time.time;
        isCharging      = true;
        autoFired       = false;
        UpdateChargeUI(0f);
    }

    protected override void OnFireRelease()
    {
        base.OnFireRelease();
        ResetCharge();
    }

    // ═══════════════════════════════════════════════════════
    //  Update — 자동 재차지 + 차지 진행 + 자동 발사
    // ═══════════════════════════════════════════════════════
    protected override void Update()
    {
        base.Update();

        if (!isActive) return;
        if (owner == null || !owner.IsAlive)
        {
            ResetCharge();
            return;
        }

        // ── 자동 재차지: 사용자가 마우스 누른 채로 있을 때
        //    (1) reload 가 끝나는 순간
        //    (2) 직전 발사 후 (autoFired 해제 후) 다음 사이클
        //    위 두 경우에 OnFirePress 를 기다리지 않고 즉시 차지 시작.
        bool mousePressed = Input.GetMouseButton(0);
        bool canStartCharge = mousePressed
                           && !isCharging
                           && !autoFired
                           && owner.CurrentState != CharacterState.Reload
                           && owner.CurrentBulletCount > 0;
        if (canStartCharge)
        {
            chargeStartTime = Time.time;
            isCharging      = true;
            UpdateChargeUI(0f);

            // ★ 사격 자세 전환 — idle→shoot 시퀀스(5장) + ShootLoop 1회 재생 후 사격 자세 5번에서 정지.
            //    차지 동안에는 정지 자세 유지. 차지 완료 시 owner.TryFire 가 ShootLoop 한 사이클 추가 재생.
            owner.ChangeState(CharacterState.Fire);

            // 차지 사운드 재생 (Ghost 만 PlayChargingSound 메서드 구현됨)
            if (owner is Ghost ghost) ghost.PlayChargingSound();
        }

        if (!isCharging) return;
        if (autoFired)   return;

        // 리로드 중에는 차지 멈춤 (자연스러움). 마우스 누른 상태면 reload 종료 직후 위 canStartCharge 가 재차지.
        if (owner.CurrentState == CharacterState.Reload)
        {
            ResetCharge();
            return;
        }

        // 마우스가 떨어졌다면 차지 취소 (OnFireRelease 가 이미 처리하지만 안전망)
        if (!mousePressed)
        {
            ResetCharge();
            return;
        }

        float ratio = Mathf.Clamp01((Time.time - chargeStartTime) / maxChargeTime);
        UpdateChargeUI(ratio);

        if (ratio >= 1f)
        {
            // ★ 충전 완료 — 자동 발사 (Ghost.TryFire 가 RL 이라 fireRate 우회로 즉시 발사)
            owner.TryFire();

            // 마우스 누른 상태 유지 시 다음 차지 사이클 자동 시작 (NIKKE 런처 컨벤션).
            isCharging      = false;
            chargeStartTime = -1f;
            autoFired       = false;
            UpdateChargeUI(0f);

            // 차지 사운드 다음 사이클 준비 (한 번에 1회 재생 가드 해제)
            if (owner is Ghost ghost) ghost.StopChargingSound();
        }
    }

    // ═══════════════════════════════════════════════════════
    //  Charge UI
    // ═══════════════════════════════════════════════════════
    private void UpdateChargeUI(float ratio)
    {
        if (chargeProgressBar != null)
            chargeProgressBar.fillAmount = ratio;

        if (chargePercentText != null)
            chargePercentText.text = Mathf.RoundToInt(ratio * 100f).ToString();

        // 글로우 — 100% 도달 시 페이드인, 그 외엔 페이드아웃
        if (chargeGlow != null)
        {
            if (ratio >= 1f && glowCoroutine == null)
                glowCoroutine = StartCoroutine(FadeGlow(true));
            else if (ratio < 1f)
            {
                if (glowCoroutine != null) { StopCoroutine(glowCoroutine); glowCoroutine = null; }
                Color c = chargeGlow.color;
                c.a = 0f;
                chargeGlow.color = c;
            }
        }
    }

    private IEnumerator FadeGlow(bool fadeIn)
    {
        float elapsed = 0f;
        Color c       = chargeGlow.color;
        float fromA   = c.a;
        float toA     = fadeIn ? 1f : 0f;

        while (elapsed < glowFadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a      = Mathf.Lerp(fromA, toA, elapsed / glowFadeDuration);
            chargeGlow.color = c;
            yield return null;
        }
        c.a              = toA;
        chargeGlow.color = c;
        glowCoroutine    = null;
    }

    private void ResetCharge()
    {
        isCharging      = false;
        chargeStartTime = -1f;
        autoFired       = false;
        UpdateChargeUI(0f);

        // 차지 사운드 정지 (다음 사이클 준비)
        if (owner is Ghost ghost) ghost.StopChargingSound();

        // 사격 자세였다면 Idle 복귀 (마우스 떼기 / 차지 취소 케이스).
        // Reload 상태에서 호출된 경우는 ReloadDelay 가 이미 Reload 상태 설정해놓아서 이 가드가 false → 영향 없음.
        if (owner != null && owner.IsAlive && owner.CurrentState == CharacterState.Fire)
        {
            owner.ChangeState(CharacterState.Idle);
        }
    }

    // ═══════════════════════════════════════════════════════
    //  캐릭터 전환 — 비활성 시 차지 초기화
    // ═══════════════════════════════════════════════════════
    protected override void OnSwitchCharacter(int index)
    {
        base.OnSwitchCharacter(index);
        if (!isActive) ResetCharge();
    }
}
