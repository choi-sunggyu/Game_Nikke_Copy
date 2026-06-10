using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHPBar : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════
    //  Inspector 연결
    // ═══════════════════════════════════════════════════════
    [Header("── HP바 ────────────────────────")]
    [SerializeField] private GameObject hpBarRoot;      // HP바 전체 루트
    [SerializeField] private Image      hpFill;         // 현재 HP
    [SerializeField] private Image      delayFill;      // 지연 감소 (빨간색)
    [SerializeField] private GameObject hexIconRoot;    // 육각형 아이콘 루트

    [Header("── 색상 ────────────────────────")]
    [SerializeField] private Color normalHpColor  = new Color(0.2f, 0.9f, 0.2f, 1f); // 초록
    [SerializeField] private Color eliteHpColor   = new Color(0.9f, 0.7f, 0.1f, 1f); // 노란
    [SerializeField] private Color bossHpColor    = new Color(0.9f, 0.2f, 0.2f, 1f); // 빨간
    [SerializeField] private Color delayColor     = new Color(1f,   0.2f, 0.2f, 1f); // 지연 빨간

    [Header("── 설정 ────────────────────────")]
    [SerializeField] private float delaySpeed     = 1.5f;  // 지연 감소 속도
    [SerializeField] private float delayWait      = 0.4f;  // 지연 시작까지 대기 시간
    [SerializeField] private Sprite eliteIconSprite;       // 엘리트 아이콘
    [SerializeField] private Sprite bossIconSprite;        // 보스 아이콘

    // ═══════════════════════════════════════════════════════
    //  내부 상태
    // ═══════════════════════════════════════════════════════
    private EnemyBase _owner;
    private float     _targetFill;
    private Coroutine _delayCoroutine;

    // ═══════════════════════════════════════════════════════
    //  초기화
    // ═══════════════════════════════════════════════════════
    public void Init(EnemyBase owner)
    {
        _owner = owner;
        _owner.OnHpChanged += HandleHpChanged;

        // 타입별 초기화
        switch (owner.EnemyType)
        {
            case EnemyType.Normal:
                hpFill.color = normalHpColor;
                hexIconRoot?.SetActive(false);
                hpBarRoot.SetActive(false); // 피격 전 숨김
                break;

            case EnemyType.Elite:
                hpFill.color = eliteHpColor;
                hexIconRoot?.SetActive(true);
                hpBarRoot.SetActive(true); // 처음부터 표시
                break;

            case EnemyType.Boss:
                hpFill.color = bossHpColor;
                hexIconRoot?.SetActive(true);
                hpBarRoot.SetActive(true); // 처음부터 표시
                break;
        }

        delayFill.color   = delayColor;
        _targetFill       = 1f;
        hpFill.fillAmount = 1f;
        delayFill.fillAmount = 1f;
    }

    void OnDisable()
    {
        if (_owner != null)
            _owner.OnHpChanged -= HandleHpChanged;
    }

    // ═══════════════════════════════════════════════════════
    //  HP 변경 처리
    // ═══════════════════════════════════════════════════════
    private void HandleHpChanged(float currentHp, float maxHp)
    {
        // 일반 몬스터는 첫 피격 시 표시
        if (_owner.EnemyType == EnemyType.Normal)
            hpBarRoot.SetActive(true);

        _targetFill       = currentHp / maxHp;
        hpFill.fillAmount = _targetFill; // HP바 즉시 감소

        // 지연 감소 코루틴 재시작
        if (_delayCoroutine != null)
            StopCoroutine(_delayCoroutine);
        _delayCoroutine = StartCoroutine(DelayedFill());
    }

    // ═══════════════════════════════════════════════════════
    //  지연 감소 — 빨간 바가 천천히 따라옴
    // ═══════════════════════════════════════════════════════
    private IEnumerator DelayedFill()
    {
        // 잠시 대기 후 빨간 바 감소 시작
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
    //  월드 스페이스 — 항상 카메라를 향하도록
    // ═══════════════════════════════════════════════════════
    void LateUpdate()
    {
        if (Camera.main != null)
            transform.rotation = Camera.main.transform.rotation;
    }
}