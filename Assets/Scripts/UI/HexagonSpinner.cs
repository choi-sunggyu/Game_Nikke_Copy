using UnityEngine;
using UnityEngine.UI;

public class HexagonSpinner : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 120f;   // 외부 육각형 회전 속도
    [SerializeField] private float innerSpeed = -80f;    // 내부 육각형 반대 방향 회전
    [SerializeField] private Color hexColor = new Color(0f, 1f, 1f, 0.9f); // 청록색

    private GameObject outer;
    private GameObject inner;

    void Start()
    {
        outer = CreateHexagon("Outer", 80f, hexColor, 8f);
        inner = CreateHexagon("Inner", 50f, new Color(hexColor.r, hexColor.g, hexColor.b, 0.5f), 5f);
    }

    void Update()
    {
        outer.transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
        inner.transform.Rotate(0f, 0f, innerSpeed * Time.deltaTime);
    }

    private GameObject CreateHexagon(string name, float radius, Color color, float lineWidth)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);

        // LineRenderer로 육각형 그리기
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.positionCount = 6;
        lr.startColor = color;
        lr.endColor = color;
        lr.sortingOrder = 10;

        // 기본 Material 설정
        lr.material = new Material(Shader.Find("Sprites/Default"));

        // 육각형 꼭짓점 계산
        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.Deg2Rad * (60f * i - 30f);
            lr.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f
            ));
        }

        return go;
    }
}