using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스폰 큐 생성 로직 — WaveManager 에서 추출한 정적 헬퍼.
/// 인스턴스 상태 없음 → EditMode 테스트 가능.
/// </summary>
/// 
public enum PrefabKind { Regular, Elite }

public static class SpawnQueueGenerator
{
    // ═══════════════════════════════════════════════════════
    //  패턴별 적 수
    // ═══════════════════════════════════════════════════════
    public static int GetPatternCount(SpawnPattern p)
    {
        switch (p)
        {
            case SpawnPattern.Single:       return 1;
            case SpawnPattern.Trio:         return 3;
            case SpawnPattern.LateralLeft:  return Random.Range(2, 4); // 2~3
            case SpawnPattern.LateralRight: return Random.Range(2, 4);
            case SpawnPattern.DualSide:     return Random.Range(4, 7); // 4~6 (양쪽 합쳐서)
            case SpawnPattern.TopRandom:    return Random.Range(2, 5); // 2~4
            default:                        return 1;
        }
    }    

    public static List<SpawnPattern> BuildPatternPool(int poolSize)
    {
        // 1. 가중치 정의 (배열 또는 dictionary)
        //    SpawnPattern 과 weight 를 짝지어 가중치 큰 순서로 정렬해두기
        SpawnPattern[] patterns = { SpawnPattern.Single, SpawnPattern.Trio, SpawnPattern.LateralLeft, SpawnPattern.LateralRight, SpawnPattern.DualSide, SpawnPattern.TopRandom };
        int[]          weights  = { 25, 20, 15, 15, 13, 12 };

        int[] counts = new int[patterns.Length];

        // 2. 각 패턴별 floor 카운트
        int sum = 0;
        for (int i = 0; i < patterns.Length; i++)
        {
            counts[i] = weights[i] * poolSize / 100;
            sum += counts[i];
        }

        // 3. 부족분 채우기 — 가중치 큰 순서대로 +1
        int shortage = poolSize - sum;
        for (int i = 0; i < shortage; i++)
        {
            counts[i % patterns.Length]++;
        }

        // 4. List 에 카운트만큼 추가
        var pool = new List<SpawnPattern>(poolSize);
        for (int i = 0; i < patterns.Length; i++)
        {
            for (int j = 0; j < counts[i]; j++)
                pool.Add(patterns[i]);
        }

        // 5. Fisher-Yates 셔플
        Shuffle(pool);

        return pool;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);   // [0, i] 포함 — Unity Random.Range int 는 두 번째 인자 exclusive
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// 한 판 그룹 수 만큼 일반/엘리트 분배를 결정적으로 만들고 셔플.
    /// eliteRatio 가 1% 같이 작아 round 결과가 0 이 되어도 안전.
    /// </summary>
    public static List<PrefabKind> BuildPrefabKindPool(int poolSize, float eliteRatio)
    {
        // 안전망 — eliteRatio 0~1 클램프
        eliteRatio = Mathf.Clamp01(eliteRatio);

        // eliteRatio = 0.1 → poolSize 10 중 1 이 Elite
        int eliteCount   = Mathf.RoundToInt(poolSize * eliteRatio);
        eliteCount       = Mathf.Clamp(eliteCount, 0, poolSize);
        int regularCount = poolSize - eliteCount;

        var pool = new List<PrefabKind>(poolSize);
        for (int i = 0; i < regularCount; i++) pool.Add(PrefabKind.Regular);
        for (int i = 0; i < eliteCount; i++)   pool.Add(PrefabKind.Elite);

        Shuffle(pool);   // 같은 셔플 헬퍼 재사용
        return pool;
    }
}
