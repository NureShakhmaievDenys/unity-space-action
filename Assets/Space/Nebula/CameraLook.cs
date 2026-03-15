using UnityEngine;

public class CameraLook : MonoBehaviour
{
    public float mouseSensitivity = 3f;

    float rotationX = 0f;
    float rotationY = 0f;

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        rotationX -= mouseY;
        rotationY += mouseX;

        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0);
    }
}