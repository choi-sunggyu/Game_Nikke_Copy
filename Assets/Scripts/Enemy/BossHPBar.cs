using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHPBar : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════
    //  Inspector 연결
    // ═══════════════════════════════════════════════════════
    [SerializeField] private GameObject  rootUI;
    [SerializeField] private Image       hpFill;
    [SerializeField] private Image       delayFill;
    [SerializeField] private Image       bossIcon;

    [Header("── 설정 ────────────────────────")]
    [SerializeField] private Color hpColor    = new Color(0.9f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color delayColor = new Color(1f,   0.5f, 0.2f, 1f);
    [SerializeField] private float delaySpeed = 1.0f;
    [SerializeField] private float delayWait  = 0.4f;

    // ═══════════════════════════════════════════════════════
    //  내부 상태
    // ═══════════════════════════════════════════════════════
    private EnemyBase _boss;
    private float     _targetFill;
    private Coroutine _delayCoroutine;

    // ═══════════════════════════════════════════════════════
    //  초기화
    // ═══════════════════════════════════════════════════════
    void Awake()
    {
        rootUI.SetActive(false);
    }

    // 보스 등장은 TopUIManager 가 전권으로 제어 (TopUIManager → bossHPBar.Show()).
    // BossHPBar 는 자체 UI 갱신과 처치 처리만 담당.
    void OnEnable()
    {
        EnemyBase.OnBossDefeated += HandleBossDefeated;
    }

    void OnDisable()
    {
        EnemyBase.OnBossDefeated -= HandleBossDefeated;

        if (_boss != null)
            _boss.OnHpChanged -= HandleHpChanged;
    }

    public void Show(EnemyBase boss)
    {
        // ⚠ 안전망 — 이전 보스 구독이 살아있으면 먼저 해제 (다중 Show 누수 차단)
        if (_boss != null && _boss != boss)
        {
            _boss.OnHpChanged -= HandleHpChanged;
            _boss.OnDied      -= HandleBossDied;
        }

        _boss = boss;
        _boss.OnHpChanged += HandleHpChanged;
        _boss.OnDied      += HandleBossDied;

        hpFill.color    = hpColor;
        delayFill.color = delayColor;

        _targetFill          = 1f;
        hpFill.fillAmount    = 1f;
        delayFill.fillAmount = 1f;

        rootUI.SetActive(true); // TopUIManager 호출 시점에 활성화
    }

        private void HandleBossDied()
    {
        if (_boss != null)
        {
            _boss.OnHpChanged -= HandleHpChanged;
            _boss.OnDied      -= HandleBossDied;
        }
        rootUI.SetActive(false);
    }

    // ═══════════════════════════════════════════════════════
    //  HP 변경
    // ═══════════════════════════════════════════════════════
    private void HandleHpChanged(float currentHp, float maxHp)
    {
        // ⚠ 안전망 — 비활성 상태에서 코루틴 시도 시 Unity 가 던지는 에러 방지.
        //   보스 사망 후 잔존 구독(구독 해제 직전 한 프레임)이나, 다른 보스의 미정리 구독에서 호출될 때 진입.
        if (!isActiveAndEnabled) return;
        if (rootUI == null || !rootUI.activeInHierarchy) return;

        _targetFill       = currentHp / maxHp;
        hpFill.fillAmount = _targetFill;

        if (_delayCoroutine != null)
            StopCoroutine(_delayCoroutine);
        _delayCoroutine = StartCoroutine(DelayedFill());
    }

    private IEnumerator DelayedFill()
    {
        yield return new WaitForSeconds(delayWait);

        while (delayFill.fillAmount > _targetFill + 0.001f)
        {
            delayFill.fillAmount = Mathf.MoveTowards(
                delayFill.fillAmount,
                _targetFill,
                delaySpeed * Time.deltaTime
            );
            yield return null;
        }

        delayFill.fillAmount = _targetFill;
        _delayCoroutine = null;
    }

    // ═══════════════════════════════════════════════════════
    //  보스 처치
    // ═══════════════════════════════════════════════════════
    private void HandleBossDefeated(EnemyBase boss)
    {
        if (_boss != null)
            _boss.OnHpChanged -= HandleHpChanged;

        rootUI.SetActive(false);
    }
}