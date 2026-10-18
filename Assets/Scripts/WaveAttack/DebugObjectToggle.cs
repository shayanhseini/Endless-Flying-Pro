using UnityEngine;

public class DebugObjectToggle : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private GameObject object1;
    [SerializeField] private GameObject object2;
    [SerializeField] private GameObject object3;
    [SerializeField] private GameObject object4;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            ToggleObject(object1);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            ToggleObject(object2);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            ToggleObject(object3);

        if (Input.GetKeyDown(KeyCode.Alpha4))
            ToggleObject(object4);
    }

    private void ToggleObject(GameObject target)
    {
        if (target != null)
            target.SetActive(!target.activeSelf);
    }
}