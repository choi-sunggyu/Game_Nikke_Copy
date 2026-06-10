using System.Collections;
using UnityEngine;

public class ViperBeamEffect : MonoBehaviour
{
    [SerializeField] private TrailRenderer beamTrail;
    [SerializeField] private ParticleSystem hitParticle;
    [SerializeField] private float beamSpeed = 80f;

    public void Fire(Vector3 from, Vector3 to)
    {
        transform.position = from;
        StartCoroutine(BeamRoutine(from, to));
    }

    private IEnumerator BeamRoutine(Vector3 from, Vector3 to)
    {
        beamTrail.Clear();
        beamTrail.emitting = true;

        float dist = Vector3.Distance(from, to);
        float duration = dist / beamSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        transform.position = to;
        beamTrail.emitting = false;

        // 명중 파티클
        hitParticle.transform.position = to;
        hitParticle.Play();

        // Trail 사라질 때까지 대기 후 소멸
        yield return new WaitForSeconds(beamTrail.time + hitParticle.main.duration);
        Destroy(gameObject);
    }
}
