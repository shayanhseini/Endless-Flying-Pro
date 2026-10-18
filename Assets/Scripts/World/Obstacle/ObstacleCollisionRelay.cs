using UnityEngine;

[DisallowMultipleComponent]
public sealed class ObstacleCollisionRelay : MonoBehaviour
{
    private Obstacle owner;

    public void Initialize(Obstacle obstacle)
    {
        owner = obstacle;
    }

    private void OnTriggerEnter(Collider other)
    {
        owner?.HandleVariantTrigger(GetComponent<Collider>(), other);
    }
}
