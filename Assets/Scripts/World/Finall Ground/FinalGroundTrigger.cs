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
    private Transform cachedPatriotTarget;
    private GameObject cachedHitEffect;


    private void Awake()
    {
        ResolveMissileReferences();

        cachedHitEffect = FindMissileHitEffect();

        if (cachedHitEffect != null)
            cachedHitEffect.SetActive(false);

        cachedPatriotTarget = FindPatriotTarget();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
            return;


        if (!other.CompareTag("Player"))
            return;


        triggered = true;

        if (planeController == null)
        {
            planeController = other.GetComponent<PlaneController>();
            if (planeController == null)
                planeController = other.GetComponentInParent<PlaneController>();
        }


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
        if (missile == null)
            ResolveMissileReferences();

        if (cachedPatriotTarget == null)
            cachedPatriotTarget = FindPatriotTarget();

        if (cachedHitEffect == null)
            cachedHitEffect = FindMissileHitEffect();

        if (missile == null ||
            cachedPatriotTarget == null ||
            planeController == null)
            return;

        if (gameAudioController == null)
        {
            gameAudioController =
                FindFirstObjectByType<GameAudioController>();
        }

        missile.Launch(
            cachedPatriotTarget,
            missileSpeed * 2f,
            cachedHitEffect,
            missileTarget != null ? missileTarget.gameObject : null,
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
