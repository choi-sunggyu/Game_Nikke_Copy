using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashEffect : MonoBehaviour
{
    public static FlashEffect Instance { get; private set; }

    [SerializeField] private GameObject flashPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void TriggerEnemyFlash(List<EnemyBase> enemies)
    {
        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.IsAlive) continue;
            StartCoroutine(FlashAtPosition(enemy.transform.position));
        }
    }

    private IEnumerator FlashAtPosition(Vector3 worldPos)
    {
        GameObject flash = Instantiate(flashPrefab, worldPos, Quaternion.identity);
        SpriteRenderer sr = flash.GetComponent<SpriteRenderer>();

        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            sr.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        Destroy(flash);
    }
}