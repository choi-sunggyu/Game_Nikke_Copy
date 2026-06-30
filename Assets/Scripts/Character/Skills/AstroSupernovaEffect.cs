using System.Collections;
using UnityEngine;

public class AstroSupernovaEffect : MonoBehaviour
{
    [SerializeField] private float coreScale = 1.3f;
    [SerializeField] private float pulseRadius = 4.5f;
    [SerializeField] private int segments = 96;
    [Tooltip("프리팹 안에 3D 태양 MeshRenderer가 있으면 기본 구체를 만들지 않고 해당 모델을 사용합니다.")]
    [SerializeField] private bool useExisting3DVisual = true;

    private Transform[] visualCores;
    private Vector3[] visualCoreBaseScales;
    private LineRenderer orbitRing;
    private LineRenderer pulseRing;
    private SpriteRenderer glow;
    private float duration;
    private float elapsed;
    private bool built;

    public void Play(float effectDuration)
    {
        duration = Mathf.Max(0.1f, effectDuration);
        if (!built) Build();
        elapsed = 0f;
        StartCoroutine(LifeRoutine());
    }

    public void PlayTickPulse()
    {
        if (pulseRing == null) return;
        StopCoroutine(nameof(PulseRoutine));
        StartCoroutine(PulseRoutine());
    }

    private void Build()
    {
        if (!TryUseExistingVisuals())
            CreateFallbackSunVisual();

        orbitRing = CreateRing("SupernovaOrbit", 1.9f, 0.06f, new Color(1f, 0.9f, 0.25f, 0.75f));
        pulseRing = CreateRing("SupernovaPulse", 0.1f, 0.08f, new Color(1f, 0.32f, 0.04f, 0f));
        built = true;
    }

    private IEnumerator LifeRoutine()
    {
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float fade = Mathf.SmoothStep(1f, 0f, Mathf.Max(0f, normalized - 0.82f) / 0.18f);
            float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.08f;

            if (visualCores != null)
            {
                for (int i = 0; i < visualCores.Length; i++)
                {
                    if (visualCores[i] == null) continue;
                    visualCores[i].localScale = visualCoreBaseScales[i] * pulse;
                    visualCores[i].Rotate(0f, 80f * Time.deltaTime, 45f * Time.deltaTime, Space.Self);
                }
            }

            if (glow != null)
            {
                glow.transform.localScale = Vector3.one * (3.6f + Mathf.Sin(Time.time * 5f) * 0.35f);
                Color color = glow.color;
                color.a = 0.22f * fade;
                glow.color = color;
            }

            if (orbitRing != null)
            {
                orbitRing.transform.localRotation = Quaternion.Euler(65f, Time.time * 80f, 20f);
                ApplyAlpha(orbitRing, 0.75f * fade);
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private bool TryUseExistingVisuals()
    {
        if (!useExisting3DVisual) return false;

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
        if (renderers == null || renderers.Length == 0) return false;

        visualCores = new Transform[renderers.Length];
        visualCoreBaseScales = new Vector3[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            visualCores[i] = renderers[i].transform;
            visualCoreBaseScales[i] = visualCores[i].localScale;
        }

        return true;
    }

    private void CreateFallbackSunVisual()
    {
        GameObject coreObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coreObject.name = "SupernovaCore";
        coreObject.transform.SetParent(transform, false);
        coreObject.transform.localScale = Vector3.one * coreScale;

        Collider coreCollider = coreObject.GetComponent<Collider>();
        if (coreCollider != null) Destroy(coreCollider);

        Renderer coreRenderer = coreObject.GetComponent<Renderer>();
        coreRenderer.material = new Material(Shader.Find("Unlit/Color"));
        coreRenderer.material.color = new Color(1f, 0.72f, 0.12f, 1f);

        visualCores = new[] { coreObject.transform };
        visualCoreBaseScales = new[] { coreObject.transform.localScale };

        GameObject glowObject = new GameObject("SupernovaGlow");
        glowObject.transform.SetParent(transform, false);
        glowObject.transform.localScale = Vector3.one * 4f;
        glow = glowObject.AddComponent<SpriteRenderer>();
        glow.sprite = CreateSquareSprite();
        glow.color = new Color(1f, 0.42f, 0.05f, 0.22f);
    }

    private IEnumerator PulseRoutine()
    {
        float pulseElapsed = 0f;
        float pulseDuration = 0.28f;

        while (pulseElapsed < pulseDuration)
        {
            pulseElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(pulseElapsed / pulseDuration);
            SetRingRadius(pulseRing, Mathf.Lerp(0.4f, pulseRadius, t));
            ApplyAlpha(pulseRing, Mathf.Lerp(0.75f, 0f, t));
            yield return null;
        }

        ApplyAlpha(pulseRing, 0f);
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
        SetRingRadius(line, ringRadius);
        return line;
    }

    private void SetRingRadius(LineRenderer line, float ringRadius)
    {
        if (line == null) return;

        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * ringRadius, 0f, Mathf.Sin(angle) * ringRadius));
        }
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
