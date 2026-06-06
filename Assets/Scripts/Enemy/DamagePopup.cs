using System.Collections;
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════
    //  Inspector 연결
    // ═══════════════════════════════════════════════════════
    [SerializeField] private TextMeshProUGUI damageText;

    [Header("── 일반 설정 ──────────────────")]
    [SerializeField] private Color normalColor    = Color.white;
    [SerializeField] private float normalFontSize = 36f;
    [SerializeField] private float riseDuration   = 0.6f;
    [SerializeField] private float riseHeight     = 80f;
    [SerializeField] private float fadeDuration   = 0.3f;

    [Header("── 크리티컬 설정 ──────────────")]
    [SerializeField] private Color criticalColor    = new Color(1f, 0.85f, 0f, 1f);
    [SerializeField] private float criticalFontSize = 52f;
    [SerializeField] private float bounceHeight     = 130f;
    [SerializeField] private float bounceReturnRate = 0.3f;
    [SerializeField] private float bounceDuration   = 0.5f;

    // ═══════════════════════════════════════════════════════
    //  내부 상태
    // ═══════════════════════════════════════════════════════
    private Coroutine _animCoroutine;
    private ObjectPool _ownerPool;

    // ═══════════════════════════════════════════════════════
    //  초기화
    // ═══════════════════════════════════════════════════════
    void OnEnable()
    {
        _ownerPool = GetComponent<PoolObject>()?.OwnerPool;
    }

    void OnDisable()
    {
        if (_animCoroutine != null)
            StopCoroutine(_animCoroutine);
    }

    // ═══════════════════════════════════════════════════════
    //  초기화 — 기존 Init(damage) 호환 유지
    // ═══════════════════════════════════════════════════════
    public void Init(float damage)             => Init(damage, false);

    public void Init(float damage, bool isCritical)
    {
        if (isCritical)
        {
            damageText.text     = $"! {Mathf.RoundToInt(damage)}";
            damageText.color    = criticalColor;
            damageText.fontSize = criticalFontSize;
        }
        else
        {
            damageText.text     = Mathf.RoundToInt(damage).ToString();
            damageText.color    = normalColor;
            damageText.fontSize = normalFontSize;
        }

        if (_animCoroutine != null)
            StopCoroutine(_animCoroutine);

        _animCoroutine = isCritical
            ? StartCoroutine(CriticalAnim())
            : StartCoroutine(NormalAnim());

        damageText.color = new Color(
            damageText.color.r,
            damageText.color.g,
            damageText.color.b,
            1f);

        transform.localScale = Vector3.one;
    }

    // ═══════════════════════════════════════════════════════
    //  일반 애니메이션
    // ═══════════════════════════════════════════════════════
    private IEnumerator NormalAnim()
    {
        transform.localScale = Vector3.one * 1.3f;

        float popDuration = 0.08f;
        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;

            transform.localScale =
                Vector3.Lerp(
                    Vector3.one * 1.3f,
                    Vector3.one,
                    elapsed / popDuration);

            yield return null;
        }

        yield return new WaitForSeconds(0.15f);

        Vector3 startPos = transform.localPosition;
        Vector3 endPos = startPos + Vector3.up * 40f;

        elapsed = 0f;
        float duration = 0.18f;

        Color c = damageText.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / duration;
            float ease = EaseOutCubic(t);

            transform.localPosition =
                Vector3.Lerp(startPos, endPos, ease);

            transform.localScale =
                Vector3.Lerp(
                    Vector3.one,
                    new Vector3(0.15f, 0.15f, 1f),
                    ease);

            c.a = Mathf.Lerp(1f, 0f, ease);
            damageText.color = c;

            yield return null;
        }

        ReturnToPool();
    }

    // ═══════════════════════════════════════════════════════
    //  크리티컬 애니메이션
    // ═══════════════════════════════════════════════════════
    private IEnumerator CriticalAnim()
    {
        transform.localScale = Vector3.one * 1.8f;

        float popDuration = 0.08f;
        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;

            transform.localScale =
                Vector3.Lerp(
                    Vector3.one * 1.8f,
                    Vector3.one * 1.1f,
                    elapsed / popDuration);

            yield return null;
        }

        Vector3 originalPos = transform.localPosition;

        float shakeTime = 0.15f;
        elapsed = 0f;

        while (elapsed < shakeTime)
        {
            elapsed += Time.deltaTime;

            transform.localPosition =
                originalPos +
                (Vector3)Random.insideUnitCircle * 2.5f;

            yield return null;
        }

        transform.localPosition = originalPos;

        Vector3 endPos = originalPos + Vector3.up * 50f;

        elapsed = 0f;
        float duration = 0.22f;

        Color c = damageText.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / duration;
            float ease = EaseOutCubic(t);

            transform.localPosition =
                Vector3.Lerp(originalPos, endPos, ease);

            transform.localScale =
                Vector3.Lerp(
                    Vector3.one * 1.1f,
                    new Vector3(0.1f, 0.1f, 1f),
                    ease);

            c.a = Mathf.Lerp(1f, 0f, ease);
            damageText.color = c;

            yield return null;
        }

        ReturnToPool();
    }

    // ═══════════════════════════════════════════════════════
    //  페이드아웃
    // ═══════════════════════════════════════════════════════
    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        Color c       = damageText.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a      = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            damageText.color = c;
            yield return null;
        }

        c.a = 0f;
        damageText.color = c;
    }

    // ═══════════════════════════════════════════════════════
    //  이징
    // ═══════════════════════════════════════════════════════
    private float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private float EaseInCubic(float t)  => t * t * t;

    // ═══════════════════════════════════════════════════════
    // 풀 반환
    // ═══════════════════════════════════════════════════════
    private void ReturnToPool()
    {
        _animCoroutine = null;
        _ownerPool?.Return(gameObject);
    }
}