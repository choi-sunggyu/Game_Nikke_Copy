using System.Collections;
using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;

    public void Init(float damage)
    {
        text.text = ((int)damage).ToString();
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        float duration = 0.8f;
        float elapsed = 0f;
        Vector3 startPos = transform.localPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 위로 떠오름
            transform.localPosition = startPos + Vector3.up * (50f * t);

            // 페이드아웃
            text.alpha = Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        Destroy(gameObject);
    }
}