using System.Collections;
using UnityEngine;

public class AttackStar : MonoBehaviour
{
    private float damage;
    private float speed;
    private EnemyBase target;

    public void Init(float damage, float speed, EnemyBase target)
    {
        this.damage = damage;
        this.speed = speed;
        this.target = target;
        StartCoroutine(FlyToTarget());
    }

    private IEnumerator FlyToTarget()
    {
        float maxDuration = 3f;
        float elapsed = 0f;

        while (elapsed < maxDuration)
        {
            elapsed += Time.deltaTime;

            // 타겟이 사라지면 소멸
            if (target == null || !target.IsAlive)
            {
                Destroy(gameObject);
                yield break;
            }

            // 타겟 방향으로 이동
            Vector3 dir = (target.transform.position - transform.position).normalized;
            transform.position += dir * speed * Time.deltaTime;
            transform.Rotate(0f, 0f, 360f * Time.deltaTime); // 회전

            // 명중 체크
            if (Vector3.Distance(transform.position, target.transform.position) < 0.3f)
            {
                target.TakeDamage(damage);
                Destroy(gameObject);
                yield break;
            }

            yield return null;
        }

        Destroy(gameObject); // 시간 초과 시 소멸
    }
}