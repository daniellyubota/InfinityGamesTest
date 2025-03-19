using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform cameraTransform;
    public float moveSpeed = 5f;
    public float rotateSpeed = 5f;
    public float zoomSpeed = 2f;
    public Vector3 originalPosition = new Vector3(0, 35, -44.5f);
    public Vector3 maxZoomIn = new Vector3(0, 20, -26.6f);
    public Vector3 maxZoomOut = new Vector3(0, 50, -62.4f);
    public Vector3 movementBoundsMin = new Vector3(-7.5f, 0f, -7.5f);
    public Vector3 movementBoundsMax = new Vector3(7.5f, 0f, 7.5f);

    private float zoomAmount = 0f;
    private Vector3 lastMousePosition;
    private Vector3 targetPosition;
    private float leeway = 1f;

    void Start()
    {
        targetPosition = transform.position;
    }

    void Update()
    {
        CameraMove();
        CameraRotate();
        CameraZoom();
    }

    void CameraMove()
    {
        if (Input.GetMouseButton(0))
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            float moveX = -mouseDelta.x * moveSpeed * Time.deltaTime * 0.1f;
            float moveZ = -mouseDelta.y * moveSpeed * Time.deltaTime * 0.1f;
            transform.Translate(moveX, 0f, moveZ);

            float clampedX = Mathf.Clamp(transform.position.x, movementBoundsMin.x - leeway, movementBoundsMax.x + leeway);
            float clampedZ = Mathf.Clamp(transform.position.z, movementBoundsMin.z - leeway, movementBoundsMax.z + leeway);

            transform.position = new Vector3(clampedX, transform.position.y, clampedZ);
            targetPosition = transform.position;
        }
        else
        {
            targetPosition = new Vector3(
                Mathf.Clamp(targetPosition.x, movementBoundsMin.x, movementBoundsMax.x),
                targetPosition.y,
                Mathf.Clamp(targetPosition.z, movementBoundsMin.z, movementBoundsMax.z)
            );
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);
        }

        lastMousePosition = Input.mousePosition;
    }

    void CameraRotate()
    {
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * rotateSpeed;
            transform.Rotate(0f, mouseX, 0f, Space.World);
        }
    }

    void CameraZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            zoomAmount -= scroll * zoomSpeed;
            zoomAmount = Mathf.Clamp01(zoomAmount);
            cameraTransform.localPosition = Vector3.Lerp(maxZoomIn, maxZoomOut, zoomAmount);
        }
    }
}
