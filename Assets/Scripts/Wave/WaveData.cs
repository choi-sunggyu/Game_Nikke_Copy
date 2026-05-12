using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "Game/WaveData")]
public class WaveData : ScriptableObject
{
    [System.Serializable]
    public class EnemySpawnInfo
    {
        public GameObject enemyPrefab;
        public int count;
    }

    [System.Serializable]
    public class Wave
    {
        public List<EnemySpawnInfo> enemies;
    }

    public List<Wave> waves; // 웨이브 목록
}