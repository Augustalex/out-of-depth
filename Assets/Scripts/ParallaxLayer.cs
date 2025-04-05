using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    public Transform cameraTransform;
    public float parallaxMultiplier = 0.5f;
    public float parallaxMultiplierX = 1f;
    public float parallaxMultiplierY = 1f;

    private Vector3 previousCamPos;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;

        previousCamPos = cameraTransform.position;
    }

    void LateUpdate()
    {
        Vector3 deltaMovement = cameraTransform.position - previousCamPos;
        transform.position += new Vector3(
            deltaMovement.x * parallaxMultiplier * parallaxMultiplierX,
            deltaMovement.y * parallaxMultiplier * parallaxMultiplierY,
            0);
        previousCamPos = cameraTransform.position;
    }
}
