using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "Game/WaveData")]
public class WaveData : ScriptableObject
{
    [System.Serializable]
    public class EnemySpawnInfo
    {
        public GameObject enemyPrefab;
        public int        count;
    }

    [System.Serializable]
    public class Wave
    {
        public List<EnemySpawnInfo> enemies;

        [Header("엘리트 웨이브 설정")]
        public bool isEliteWave; // ← true면 엘리트 웨이브로 처리
    }

    public List<Wave> waves;
}