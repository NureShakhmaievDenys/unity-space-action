using UnityEngine;

public class PlanetRotation : MonoBehaviour
{
    public float rotationSpeed = 2f;

    void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.Self);
    }
}