using UnityEngine;

public class FinalGroundTrigger : MonoBehaviour
{
    [Header("Plane")]
    [SerializeField] private PlaneController planeController;
    [SerializeField] private Transform finalTarget;

    [Header("Final Missile")]
    [SerializeField] private FinalSequenceMissile missile;
    [SerializeField] private Transform missileTarget;
    [SerializeField] private GameAudioController gameAudioController;
    [SerializeField] private float missileSpeed = 35f;

    private bool triggered;


    private void Awake()
    {
        ResolveMissileReferences();

        GameObject hitEffect = FindMissileHitEffect();

        if (hitEffect != null)
            hitEffect.SetActive(false);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
            return;


        if (!other.CompareTag("Player"))
            return;


        triggered = true;


        if (planeController != null)
        {
            planeController.StartFinalAutoPilot(
                finalTarget
            );

            LaunchFinalMissile();
        }
        
        Debug.Log("FinalGroundTrigger entered");
    }


    private void LaunchFinalMissile()
    {
        ResolveMissileReferences();

        Transform patriotTarget = FindPatriotTarget();
        GameObject hitEffect = FindMissileHitEffect();

        if (missile == null ||
            patriotTarget == null ||
            planeController == null)
            return;

        if (gameAudioController == null)
        {
            gameAudioController =
                FindFirstObjectByType<GameAudioController>();
        }

        missile.Launch(
            patriotTarget,
            missileSpeed * 2f,
            hitEffect,
            missileTarget.gameObject,
            gameAudioController
        );
    }


    private Transform FindPatriotTarget()
    {
        if (missileTarget == null)
            return null;

        foreach (Transform child in
                 missileTarget.GetComponentsInChildren<Transform>(true))
        {
            if (child == missileTarget)
                continue;

            if (child.name.StartsWith("Patriot", System.StringComparison.OrdinalIgnoreCase))
                return child;
        }

        return missileTarget.childCount > 0
            ? missileTarget.GetChild(0)
            : missileTarget;
    }


    private GameObject FindMissileHitEffect()
    {
        Transform searchRoot = GetSearchRoot();

        foreach (Transform child in
                 searchRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "Missile Hit Effect")
                return child.gameObject;
        }

        return null;
    }


    private void ResolveMissileReferences()
    {
        Transform searchRoot = GetSearchRoot();

        if (missile == null)
        {
            foreach (Transform child in
                     searchRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != "Missile")
                    continue;

                missile =
                    child.GetComponent<FinalSequenceMissile>();

                if (missile == null)
                {
                    missile =
                        child.gameObject.AddComponent<FinalSequenceMissile>();
                }

                break;
            }
        }

        if (missileTarget == null)
        {
            foreach (Transform child in
                     searchRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Final Missile Target")
                {
                    missileTarget = child;
                    break;
                }
            }
        }
    }


    private Transform GetSearchRoot()
    {
        return transform.parent != null
            ? transform.parent
            : transform;
    }
}
