using UnityEngine;

/// <summary>
/// 위로 갈수록 옅어지는 검정 그라데이션 스프라이트 생성기.
///
/// 사용 예:
///   image.sprite = Black_Gradation.Create();                    // 기본값 (64 × 128, 아래 알파 1 → 위 알파 0)
///   image.sprite = Black_Gradation.Create(128, 256);            // 해상도만 조정
///   image.sprite = Black_Gradation.Create(64, 128, 0.8f, 0.1f); // 알파 범위 조정 (완전 검정 아님)
///
/// 텍스처는 y=0 이 아래쪽이므로 bottomAlpha → topAlpha 로 보간합니다.
/// (Texture2D 좌표계: 좌하단이 (0,0))
/// </summary>
public static class Black_Gradation
{
    /// <summary>
    /// 검정 세로 그라데이션 스프라이트 생성.
    /// </summary>
    /// <param name="width">텍스처 가로 픽셀 (가로는 단색이므로 작아도 됨)</param>
    /// <param name="height">텍스처 세로 픽셀 (그라데이션 부드러움 결정)</param>
    /// <param name="bottomAlpha">아래쪽 알파 (0 = 투명, 1 = 완전 검정)</param>
    /// <param name="topAlpha">위쪽 알파</param>
    public static Sprite Create(
        int width = 64,
        int height = 128,
        float bottomAlpha = 1f,
        float topAlpha = 0f)
    {
        Texture2D tex = new Texture2D(width, height);
        tex.wrapMode = TextureWrapMode.Clamp; // 가장자리 늘어남 방지

        for (int y = 0; y < height; y++)
        {
            // y=0 이 아래쪽이므로 t=0(아래) ~ t=1(위)
            float t = (height <= 1) ? 0f : (float)y / (height - 1);
            float alpha = Mathf.Lerp(bottomAlpha, topAlpha, t);
            Color color = new Color(0f, 0f, 0f, alpha);

            for (int x = 0; x < width; x++)
                tex.SetPixel(x, y, color);
        }

        tex.Apply();
        return Sprite.Create(
            tex,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f));
    }
}
