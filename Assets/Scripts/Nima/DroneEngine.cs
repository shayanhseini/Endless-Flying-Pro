using UnityEngine;

public class DroneEngine : MonoBehaviour
{
    public Transform propeller;
    public float rotationSpeed = 1500f;

    void Update()
    {
        propeller.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
    }
}