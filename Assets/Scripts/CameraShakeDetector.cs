using UnityEngine;

public class CameraShakeDetector : MonoBehaviour
{
    // Threshold to detect shaking.
    public float shakeThreshold = 2.0f;
    private Vector3 lastPosition;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        // Calculate movement delta and speed.
        Vector3 delta = transform.position - lastPosition;
        float speed = delta.magnitude / Time.deltaTime;

        // If speed exceeds threshold then mark as shake.
        if (speed > shakeThreshold)
        {
            SnowEventManager.clearSnowNow = true;
        }
        else
        {
            SnowEventManager.clearSnowNow = false;
        }
        lastPosition = transform.position;
    }
}
