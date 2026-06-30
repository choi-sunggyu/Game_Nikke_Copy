using System.Collections;
using UnityEngine;

public class TrendHologramStageEffect : MonoBehaviour
{
    [SerializeField] private float radius = 2.4f;
    [SerializeField] private int segments = 96;
    [SerializeField] private float verticalHeight = 3.2f;

    private LineRenderer outerRing;
    private LineRenderer innerRing;
    private LineRenderer equalizerLine;
    private LineRenderer[] beams;
    private float duration;
    private float elapsed;

    public void Play(float effectDuration)
    {
        duration = Mathf.Max(0.1f, effectDuration);
        Build();
        StartCoroutine(LifeRoutine());
    }

    private void Build()
    {
        outerRing = CreateRing("OuterRing", radius, 0.06f, new Color(0.15f, 1f, 1f, 0.75f));
        innerRing = CreateRing("InnerRing", radius * 0.58f, 0.035f, new Color(1f, 0.2f, 0.9f, 0.65f));
        equalizerLine = CreateEqualizerLine();

        beams = new LineRenderer[6];
        for (int i = 0; i < beams.Length; i++)
        {
            float angle = Mathf.PI * 2f * i / beams.Length;
            Vector3 basePos = new Vector3(Mathf.Cos(angle) * radius * 0.8f, 0f, Mathf.Sin(angle) * radius * 0.8f);
            beams[i] = CreateBeam("Beam" + i, basePos, new Color(0.35f, 0.9f, 1f, 0.45f));
        }
    }

    private IEnumerator LifeRoutine()
    {
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float fade = Mathf.SmoothStep(1f, 0f, Mathf.Max(0f, normalized - 0.75f) / 0.25f);

            transform.Rotate(0f, 35f * Time.deltaTime, 0f, Space.World);
            AnimateEqualizer();
            ApplyAlpha(outerRing, 0.75f * fade);
            ApplyAlpha(innerRing, 0.65f * fade);
            ApplyAlpha(equalizerLine, 0.8f * fade);

            for (int i = 0; i < beams.Length; i++)
            {
                float pulse = 0.28f + 0.22f * Mathf.Sin(Time.time * 7f + i);
                ApplyAlpha(beams[i], pulse * fade);
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private LineRenderer CreateRing(string objectName, float ringRadius, float width, Color color)
    {
        GameObject ringObject = new GameObject(objectName);
        ringObject.transform.SetParent(transform, false);

        LineRenderer line = ringObject.AddComponent<LineRenderer>();
        ConfigureLine(line, width, color);
        line.loop = true;
        line.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * ringRadius, 0f, Mathf.Sin(angle) * ringRadius));
        }

        return line;
    }

    private LineRenderer CreateEqualizerLine()
    {
        GameObject lineObject = new GameObject("HologramEqualizer");
        lineObject.transform.SetParent(transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        ConfigureLine(line, 0.045f, new Color(1f, 0.95f, 0.2f, 0.8f));
        line.positionCount = 16;
        return line;
    }

    private LineRenderer CreateBeam(string objectName, Vector3 basePos, Color color)
    {
        GameObject beamObject = new GameObject(objectName);
        beamObject.transform.SetParent(transform, false);

        LineRenderer line = beamObject.AddComponent<LineRenderer>();
        ConfigureLine(line, 0.035f, color);
        line.positionCount = 2;
        line.SetPosition(0, basePos);
        line.SetPosition(1, basePos + Vector3.up * verticalHeight);
        return line;
    }

    private void AnimateEqualizer()
    {
        if (equalizerLine == null) return;

        int count = equalizerLine.positionCount;
        float width = radius * 1.5f;
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0f : (float)i / (count - 1);
            float x = Mathf.Lerp(-width * 0.5f, width * 0.5f, t);
            float y = 0.2f + Mathf.Abs(Mathf.Sin(Time.time * 8f + i * 0.7f)) * 0.75f;
            equalizerLine.SetPosition(i, new Vector3(x, y, -radius * 0.2f));
        }
    }

    private static void ConfigureLine(LineRenderer line, float width, Color color)
    {
        line.useWorldSpace = false;
        line.startWidth = width;
        line.endWidth = width;
        line.startColor = color;
        line.endColor = color;
        line.material = new Material(Shader.Find("Sprites/Default"));
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
}
