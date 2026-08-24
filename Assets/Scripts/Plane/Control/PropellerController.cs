using UnityEngine;

public class PropellerController : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 1500f;
    [SerializeField] private Transform propeller;
    private void Update()
    {
        propeller.Rotate(
            Vector3.forward,
            rotationSpeed * Time.deltaTime,
            Space.Self
        );
    }
}