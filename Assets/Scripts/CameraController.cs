using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    public Transform cameraTransform; // Camera transform reference for zooming.

    // Camera movement parameters.
    public float moveSpeed = 5f;
    public float rotateSpeed = 5f;
    public float zoomSpeed = 2f;

    // Zoom position settings.
    public Vector3 originalPosition = new Vector3(0, 35, -44.5f);
    public Vector3 maxZoomIn = new Vector3(0, 20, -26.6f);
    public Vector3 maxZoomOut = new Vector3(0, 50, -62.4f);

    // Movement boundaries.
    public Vector3 movementBoundsMin = new Vector3(-7.5f, 0f, -7.5f);
    public Vector3 movementBoundsMax = new Vector3(7.5f, 0f, 7.5f);

    private float zoomAmount = 0f; // Current zoom level.
    private Vector3 lastMousePosition; // Last recorded mouse position.
    private Vector3 targetPosition; // Target position for smooth movement.
    private float leeway = 1f;
    private bool canMove = false;

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

    // Moves the camera based on mouse drag.
    void CameraMove()
    {
        if (EditableObject.IsDraggingObject)
        {
            canMove = false;
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                canMove = false;
                return;
            }
            canMove = true;
        }

        if (canMove && Input.GetMouseButton(0))
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

    // Rotates the camera when right mouse is held.
    void CameraRotate()
    {
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * rotateSpeed;
            transform.Rotate(0f, mouseX, 0f, Space.World);
        }
    }

    // Zooms the camera based on the mouse scroll wheel.
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
