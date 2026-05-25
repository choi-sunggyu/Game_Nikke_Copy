using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BurstPhase
{
    Charging,    // 게이지 충전 중
    Step1Ready,  // 1버스트 대기 (초록, 20초)
    Step2Ready,  // 2버스트 대기 (노란, 20초)
    Step3Ready,  // 3버스트 대기 (빨간, 20초)
    FocusFire    // 집중사격 (빨간 소진)
}

public class BurstGaugeManager : MonoBehaviour
{
    public static BurstGaugeManager Instance { get; private set; }

    [SerializeField] private float maxGauge = 500f;
    [SerializeField] private float stepReadyTime = 20f;
    [SerializeField] private float focusFireDuration = 15f; // 집중사격 지속 시간
    [SerializeField] private float autoDelay = 0.7f;
    [SerializeField] private bool skipCutscene = false;
    private float lastBurstCoolTime;

    private float currentGauge = 0f;
    private BurstPhase currentPhase = BurstPhase.Charging;
    private bool isAutoMode = false;

    private Coroutine timerCoroutine;  // 단계 대기 타이머
    private Coroutine autoCoroutine;   // 자동 발동

    private CharacterManager characterManager;

    // 이벤트
    public static event Action<float> OnGaugeChanged;              // fillAmount (0~1)
    public static event Action<BurstPhase> OnPhaseChanged;         // 색상/상태 전환
    public static event Action<List<CharacterBase>> OnBurstReady;  // 슬롯 IN
    public static event Action OnBurstConsumed;                    // 슬롯 OUT
    public static event Action<bool> OnAutoModeChanged;
    public static event Action OnFocusFireStart;

    public static event Action OnFocusFireEnd;
    public static event Action<float> OnStepTimeChanged;

    public BurstPhase CurrentPhase => currentPhase;
    public bool IsAutoMode => isAutoMode;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        characterManager = FindAnyObjectByType<CharacterManager>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) ToggleAutoMode();
    }

    // BulletBase에서 호출
    public void AddGauge(float amount)
    {
        if (currentPhase != BurstPhase.Charging) return;

        currentGauge = Mathf.Min(currentGauge + amount, maxGauge);
        OnGaugeChanged?.Invoke(currentGauge / maxGauge);

        if (currentGauge >= maxGauge)
            EnterPhase(BurstPhase.Step1Ready);
    }

    private void EnterPhase(BurstPhase phase)
    {
        currentPhase = phase;
        OnPhaseChanged?.Invoke(phase);

        StopAllActiveCoroutines();

        switch (phase)
        {
            case BurstPhase.Step1Ready:
            case BurstPhase.Step2Ready:
            case BurstPhase.Step3Ready:
                int step = PhaseToStep(phase);
                List<CharacterBase> targets = GetBurstTargets(step);
                OnBurstReady?.Invoke(targets);

                float duration = lastBurstCoolTime > 0 ? lastBurstCoolTime : stepReadyTime;
                timerCoroutine = StartCoroutine(StepReadyCoroutine(duration, phase));
                if (isAutoMode)
                    autoCoroutine = StartCoroutine(AutoBurstCoroutine(targets));
                break;

            case BurstPhase.FocusFire:
                OnFocusFireStart?.Invoke();
                timerCoroutine = StartCoroutine(FocusFireCoroutine());
                break;
        }
    }

    private IEnumerator StepReadyCoroutine(float duration, BurstPhase phase)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            OnGaugeChanged?.Invoke(1f - (elapsed / duration));
            yield return null;
        }
        ResetToCharging();
    }

    // 0.7초 후 자동 발동
    private IEnumerator AutoBurstCoroutine(List<CharacterBase> targets)
    {
        yield return new WaitForSecondsRealtime(autoDelay);
        if (targets.Count > 0 && IsStepReady())
            ExecuteBurst(targets[0]);
    }

    // 집중사격 지속 타이머
    private IEnumerator FocusFireCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < focusFireDuration)
        {
            elapsed += Time.deltaTime;
            OnGaugeChanged?.Invoke(1f - (elapsed / focusFireDuration));
            yield return null;
        }
        OnFocusFireEnd?.Invoke();
        ResetToCharging();
    }

    // UI 슬롯 클릭 or 키 입력
    public void TryUseBurstBySlot(int slotIndex)
    {
        if (!IsStepReady()) return;
        List<CharacterBase> targets = GetBurstTargets(PhaseToStep(currentPhase));
        if (slotIndex >= targets.Count) return;
        ExecuteBurst(targets[slotIndex]);
    }

    public void TryUseBurstByCharacter(CharacterBase target)
    {
        if (!IsStepReady()) return;
        if (target.BurstNumber != PhaseToStep(currentPhase)) return;
        ExecuteBurst(target);
    }

    private void ExecuteBurst(CharacterBase target)
    {
        StopAllActiveCoroutines();
        OnBurstConsumed?.Invoke();

        BurstPhase capturedPhase = currentPhase; // 코루틴 진행 중 phase 변경 방지
        StartCoroutine(BurstSequence(target, capturedPhase));
    }

    private IEnumerator BurstSequence(CharacterBase target, BurstPhase phase)
    {
        if (!skipCutscene)
            Time.timeScale = 0f;

        target.UseBurst();

        if (!skipCutscene)
        {
            yield return new WaitForSecondsRealtime(target.BurstCutsceneDuration);
            Time.timeScale = 1f;
        }
        else
        {
            yield return null;
        }

        BurstPhase nextPhase = phase == BurstPhase.Step1Ready ? BurstPhase.Step2Ready :
                            phase == BurstPhase.Step2Ready ? BurstPhase.Step3Ready :
                            BurstPhase.FocusFire;

        lastBurstCoolTime = target.BurstCoolTime; // ← 다음 단계 타이머에 사용
        EnterPhase(nextPhase);
    }

    private void ResetToCharging()
    {
        StopAllActiveCoroutines();
        currentGauge = 0f;
        currentPhase = BurstPhase.Charging;
        OnPhaseChanged?.Invoke(BurstPhase.Charging);
        OnGaugeChanged?.Invoke(0f);
        OnBurstConsumed?.Invoke(); // 혹시 남은 슬롯 정리
    }

    public void ToggleAutoMode()
    {
        isAutoMode = !isAutoMode;
        OnAutoModeChanged?.Invoke(isAutoMode);

        // Auto ON 전환 시, 이미 버스트 대기 중이면 즉시 자동 발동 시작
        if (isAutoMode && IsStepReady())
        {
            List<CharacterBase> targets = GetBurstTargets(PhaseToStep(currentPhase));
            if (targets.Count > 0)
                autoCoroutine = StartCoroutine(AutoBurstCoroutine(targets));
        }
        // Auto OFF 전환 시, 진행 중인 자동 발동 취소
        else if (!isAutoMode && autoCoroutine != null)
        {
            StopCoroutine(autoCoroutine);
            autoCoroutine = null;
        }
    }

    private List<CharacterBase> GetBurstTargets(int step)
    {
        return characterManager.Characters
            .Where(c => c.IsAlive && c.BurstNumber == step)
            .OrderBy(c => c.transform.GetSiblingIndex())
            .ToList();
    }

    private bool IsStepReady() =>
        currentPhase == BurstPhase.Step1Ready ||
        currentPhase == BurstPhase.Step2Ready ||
        currentPhase == BurstPhase.Step3Ready;

    private int PhaseToStep(BurstPhase phase) =>
        phase == BurstPhase.Step1Ready ? 1 :
        phase == BurstPhase.Step2Ready ? 2 : 3;

    private void StopAllActiveCoroutines()
    {
        if (timerCoroutine != null) { StopCoroutine(timerCoroutine); timerCoroutine = null; }
        if (autoCoroutine != null)  { StopCoroutine(autoCoroutine);  autoCoroutine = null;  }
    }
}