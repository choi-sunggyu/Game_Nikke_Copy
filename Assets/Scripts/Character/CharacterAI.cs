using UnityEngine;

public class CharacterAI : MonoBehaviour
{
    [SerializeField] private float aimSpread = 1.0f; // 조준 오차 반경 (AI 바보 만들기)

    private CharacterBase owner;
    private CharacterManager characterManager;
    private WaveManager waveManager;
    private EnemyBase currentTarget;

    void Awake()
    {
        owner = GetComponent<CharacterBase>();
        characterManager = FindAnyObjectByType<CharacterManager>();
        waveManager = FindAnyObjectByType<WaveManager>();
    }

    void Update()
    {
        if (characterManager.CurrentCharacter == owner) return;
        if (!owner.IsAlive) return;

        ValidateAndSelectTarget();
        if (currentTarget == null)
        {
            owner.TryReload();
            return;
        }

        // 적 위치에 랜덤 오프셋 → 사람이 조준하는 것처럼 퍼짐
        Vector3 spread = Random.insideUnitSphere * aimSpread;
        Vector3 aimPoint = currentTarget.transform.position + spread;

        owner.TryFireAtTarget(aimPoint);
    }

    private void ValidateAndSelectTarget()
    {
        bool isValid = currentTarget != null
                    && currentTarget.IsAlive
                    && currentTarget.gameObject.activeSelf;

        if (!isValid)
            currentTarget = SelectRandomEnemy();
    }

    private EnemyBase SelectRandomEnemy()
    {
        var enemies = waveManager.ActiveEnemies;
        if (enemies == null || enemies.Count == 0) return null;

        return enemies[Random.Range(0, enemies.Count)];
    }
}