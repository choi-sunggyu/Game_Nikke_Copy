using UnityEngine;

/// <summary>
/// 한 번에 등장하는 적 그룹의 패턴.
///
/// Single / Trio: 시간차 없이 즉시 스폰
/// LateralLeft/Right: 시간차로 옆에서 쪼르르 등장
/// DualSide: 양쪽에서 동시에 쪼르르
/// TopRandom: 위에서 랜덤 X 좌표로 쪼르르
/// </summary>
public enum SpawnPattern
{
    Single,        // 1마리 단독
    Trio,          // 3마리 동시 (가까운 위치)
    LateralLeft,   // 왼쪽에서 시간차 쪼르르
    LateralRight,  // 오른쪽에서 시간차 쪼르르
    DualSide,      // 양쪽 동시 쪼르르
    TopRandom      // 위에서 랜덤 X 쪼르르
}

/// <summary>
/// 스폰 큐의 한 단위. 60마리를 N 개의 SpawnGroup 으로 분할해 처리.
/// </summary>
public class SpawnGroup
{
    public SpawnPattern Pattern;
    public GameObject   EnemyPrefab;
    public int          Count;
    public float        TrickleDelay; // 쪼르르 간격 (Single/Trio 는 0)

    public SpawnGroup(SpawnPattern pattern, GameObject prefab, int count, float trickleDelay = 0.3f)
    {
        Pattern      = pattern;
        EnemyPrefab  = prefab;
        Count        = count;
        TrickleDelay = trickleDelay;
    }
}
