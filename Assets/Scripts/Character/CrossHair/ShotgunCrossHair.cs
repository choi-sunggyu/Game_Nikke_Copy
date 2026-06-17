using UnityEngine;

/// <summary>
/// 샷건(SG) 전용 크로스헤어.
/// AR(RifleCrossHair) 과 동작 패턴이 동일 — 단발 사격 + sprite 토글.
/// 인스펙터에서 다른 sprite(산탄 패턴 형태) 를 할당하면 시각적 차별화 가능.
///
/// 향후 산탄 시스템(다중 탄환 발사) 도입 시 이 클래스에 산탄 시각화(X 자 4점 등) 추가.
/// </summary>
public class ShotgunCrossHair : RifleCrossHair
{
    // 동작은 RifleCrossHair 그대로 사용.
    // 인스펙터에서 crossHairSprite 슬롯에 SG 전용 모양 (예: 4점 X 형태) 드래그.
}
