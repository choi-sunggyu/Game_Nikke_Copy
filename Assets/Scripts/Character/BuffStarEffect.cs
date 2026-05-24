using System.Collections;
using UnityEngine;

public class BuffStarEffect : MonoBehaviour
{
    private SpriteRenderer starSprite;

    private Vector3 baseScale;

    void Awake()
    {
        starSprite = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
    }

    void Update() {}

    public void Show(float duration)
    {
        gameObject.SetActive(true);
        StartCoroutine(HideAfter(duration));
    }

    private IEnumerator HideAfter(float duration)
    {
        // 페이드인
        float fadeIn = 0.3f;
        float elapsed = 0f;
        while (elapsed < fadeIn)
        {
            elapsed += Time.deltaTime;
            starSprite.color = new Color(1f, 1f, 0f, Mathf.Lerp(0f, 0.6f, elapsed / fadeIn));
            yield return null;
        }

        yield return new WaitForSeconds(duration - fadeIn - 0.3f);

        // 페이드아웃
        elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            starSprite.color = new Color(1f, 1f, 0f, Mathf.Lerp(0.6f, 0f, elapsed / 0.3f));
            yield return null;
        }

        gameObject.SetActive(false);
    }
}