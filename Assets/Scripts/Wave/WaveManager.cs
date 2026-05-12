using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Settings")]
    [SerializeField] private ObjectPool enemyBulletPool;
    [SerializeField] private WaveData easyData;
    [SerializeField] private WaveData normalData;
    [SerializeField] private WaveData hardData;
    [SerializeField] private Transform spawnPoint;

    private WaveData currentData;
    private int currentWaveIndex = 0;
    private List<EnemyBase> activeEnemies = new List<EnemyBase>();

    private float waveClearDelay = 2f;  // 웨이브 클리어 후 대기
    private float spawnInterval = 2f;   // 적 등장 간격

    public enum Difficulty { Easy, Normal, Hard }

    void Start()
    {
        StartGame(Difficulty.Easy); // 임시 기본값
    }

    public void StartGame(Difficulty difficulty)
    {
        currentData = difficulty switch
        {
            Difficulty.Easy   => easyData,
            Difficulty.Normal => normalData,
            Difficulty.Hard   => hardData,
            _                 => easyData
        };

        currentWaveIndex = 0;
        StartCoroutine(RunWave());
    }

    IEnumerator RunWave()
    {
        while (currentWaveIndex < currentData.waves.Count)
        {
            Debug.Log($"[WaveManager] Wave {currentWaveIndex + 1} 시작");

            // 웨이브 내 적 순차 등장
            yield return StartCoroutine(SpawnWave(currentData.waves[currentWaveIndex]));

            // 모든 적 전멸 대기
            yield return StartCoroutine(WaitForWaveClear());

            Debug.Log($"[WaveManager] Wave {currentWaveIndex + 1} 클리어");

            currentWaveIndex++;

            if (currentWaveIndex < currentData.waves.Count)
            {
                Debug.Log($"[WaveManager] 다음 웨이브까지 {waveClearDelay}초 대기");
                yield return new WaitForSeconds(waveClearDelay);
            }
        }

        Debug.Log("[WaveManager] 모든 웨이브 클리어!");
    }

    IEnumerator SpawnWave(WaveData.Wave wave)
    {
        foreach (var spawnInfo in wave.enemies)
        {
            for (int i = 0; i < spawnInfo.count; i++)
            {
                SpawnEnemy(spawnInfo.enemyPrefab);
                yield return new WaitForSeconds(spawnInterval);
            }
        }
    }

    void SpawnEnemy(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        EnemyBase enemy = obj.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.SetBulletPool(enemyBulletPool);
            activeEnemies.Add(enemy);
        }
    }

    IEnumerator WaitForWaveClear()
    {
        while (true)
        {
            // 사망한 적 제거
            activeEnemies.RemoveAll(e => e == null || !e.IsAlive);

            if (activeEnemies.Count == 0)
                yield break;

            yield return new WaitForSeconds(0.5f); // 0.5초마다 체크
        }
    }
}