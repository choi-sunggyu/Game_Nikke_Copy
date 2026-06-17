using UnityEngine;

/// <summary>
/// 기관단총(SMG) 전용 크로스헤어.
/// AR(RifleCrossHair) 과 동작 패턴이 동일 — 빠른 연사 + sprite 토글.
/// 인스펙터에서 작고 더 가벼운 sprite (점 4개나 작은 십자) 를 할당하면 SMG 느낌 살아남.
/// </summary>
public class SubMachineGunCrossHair : RifleCrossHair
{
    // 동작은 RifleCrossHair 그대로 사용.
    // 인스펙터에서 crossHairSprite 슬롯에 SMG 전용 모양 드래그.
}
