using System.Collections;
using UnityEngine;

public sealed class FinalSequenceMissile : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private float impactDistance = 0.6f;

    private Transform target;
    private float speed;
    private GameObject hitEffect;
    private GameObject finalMissileTarget;
    private GameAudioController gameAudioController;
    private bool launched;


    public void Launch(
        Transform newTarget,
        float moveSpeed,
        GameObject existingHitEffect,
        GameObject objectToDisable,
        GameAudioController audioController)
    {
        target = newTarget;
        speed = Mathf.Max(0.1f, moveSpeed);
        hitEffect = existingHitEffect;
        finalMissileTarget = objectToDisable;
        gameAudioController = audioController;
        launched = target != null;
    }


    private void Update()
    {
        if (!launched || target == null)
            return;

        Vector3 toTarget =
            target.position - transform.position;

        float step =
            speed * Time.deltaTime;

        if (toTarget.sqrMagnitude <=
            Mathf.Max(impactDistance * impactDistance, step * step))
        {
            Impact();
            return;
        }

        Vector3 direction = toTarget.normalized;

        transform.position += direction * step;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
    }


    private void Impact()
    {
        launched = false;
        Vector3 impactPosition = target.position;

        if (hitEffect != null)
        {
            hitEffect.SetActive(true);

            ParticleSystem[] particles =
                hitEffect.GetComponentsInChildren<ParticleSystem>(true);

            foreach (ParticleSystem particle in particles)
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particle.Play(true);
            }

        }

        StartCoroutine(DisableFinalMissileTarget());

        if (gameAudioController != null)
        {
            gameAudioController.PlayExplosionSound(
                impactPosition
            );
        }

        foreach (Renderer missileRenderer in GetComponentsInChildren<Renderer>())
            missileRenderer.enabled = false;

        foreach (Collider missileCollider in GetComponentsInChildren<Collider>())
            missileCollider.enabled = false;
    }


    private IEnumerator DisableFinalMissileTarget()
    {
        yield return new WaitForSeconds(0.3f);

        if (finalMissileTarget != null)
            finalMissileTarget.SetActive(false);

        Destroy(gameObject);
    }
}
