using System.Collections;
using UnityEngine;

public class AllyBuffAuraEffect : MonoBehaviour
{
    [SerializeField] private float radius = 0.85f;
    [SerializeField] private float heightOffset = 1.4f;
    [SerializeField] private int segments = 48;

    private Transform target;
    private LineRenderer auraRing;
    private LineRenderer haloRing;
    private SpriteRenderer sparkle;
    private float duration;
    private float elapsed;

    public void Play(Transform followTarget, float effectDuration)
    {
        target = followTarget;
        duration = Mathf.Max(0.1f, effectDuration);
        Build();
        StartCoroutine(LifeRoutine());
    }

    private void Build()
    {
        auraRing = CreateRing("BuffAuraRing", radius, 0.04f, new Color(0.25f, 1f, 0.55f, 0.75f));
        haloRing = CreateRing("BuffHaloRing", radius * 0.55f, 0.035f, new Color(1f, 0.95f, 0.25f, 0.7f));

        GameObject sparkleObject = new GameObject("BuffSparkle");
        sparkleObject.transform.SetParent(transform, false);
        sparkleObject.transform.localPosition = Vector3.up * heightOffset;
        sparkleObject.transform.localScale = Vector3.one * 0.2f;
        sparkle = sparkleObject.AddComponent<SpriteRenderer>();
        sparkle.sprite = CreateSquareSprite();
        sparkle.color = new Color(1f, 0.95f, 0.25f, 0.85f);
    }

    private IEnumerator LifeRoutine()
    {
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (target == null)
            {
                Destroy(gameObject);
                yield break;
            }

            float normalized = Mathf.Clamp01(elapsed / duration);
            float fade = Mathf.SmoothStep(1f, 0f, Mathf.Max(0f, normalized - 0.82f) / 0.18f);

            transform.position = target.position;
            auraRing.transform.localRotation = Quaternion.Euler(90f, Time.time * 120f, 0f);
            haloRing.transform.localPosition = Vector3.up * (heightOffset + Mathf.Sin(Time.time * 5f) * 0.08f);
            haloRing.transform.localRotation = Quaternion.Euler(90f, -Time.time * 160f, 0f);
            sparkle.transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * 240f);
            sparkle.transform.localScale = Vector3.one * (0.18f + Mathf.Sin(Time.time * 8f) * 0.04f);

            ApplyAlpha(auraRing, 0.75f * fade);
            ApplyAlpha(haloRing, 0.7f * fade);
            Color sparkleColor = sparkle.color;
            sparkleColor.a = 0.85f * fade;
            sparkle.color = sparkleColor;

            yield return null;
        }

        Destroy(gameObject);
    }

    private LineRenderer CreateRing(string objectName, float ringRadius, float width, Color color)
    {
        GameObject ringObject = new GameObject(objectName);
        ringObject.transform.SetParent(transform, false);

        LineRenderer line = ringObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = segments;
        line.startWidth = width;
        line.endWidth = width;
        line.startColor = color;
        line.endColor = color;
        line.material = new Material(Shader.Find("Sprites/Default"));

        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * ringRadius, 0f, Mathf.Sin(angle) * ringRadius));
        }

        return line;
    }

    private static void ApplyAlpha(LineRenderer line, float alpha)
    {
        if (line == null) return;
        Color start = line.startColor;
        Color end = line.endColor;
        start.a = alpha;
        end.a = alpha;
        line.startColor = start;
        line.endColor = end;
    }

    private static Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
    }
}
