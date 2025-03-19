using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_Button : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public GameObject objectPrefab; // Prefab to spawn when dropping the object
    private GameObject dragIcon;    // UI icon that follows the cursor
    private RectTransform canvasTransform;
    private Camera mainCamera;
    private Image buttonImage;      // Reference to button image to modify opacity
    private GameObject placementIndicator; // Visual indicator for object placement
    private bool canPlace = true;   // Determines if the object can be placed

    void Start()
    {
        canvasTransform = GetComponentInParent<Canvas>().transform as RectTransform;
        mainCamera = Camera.main;
        buttonImage = GetComponent<Image>();
    }

    // Called when the user left-clicks the button and starts dragging the UI image
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        // Reduce button opacity while dragging
        if (buttonImage != null)
        {
            buttonImage.color = new Color(buttonImage.color.r, buttonImage.color.g, buttonImage.color.b, 0.3f);
        }

        // Create a draggable UI icon at the cursor position with 0.7 opacity
        dragIcon = new GameObject("DragIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dragIcon.transform.SetParent(canvasTransform, false);
        dragIcon.GetComponent<RectTransform>().sizeDelta = GetComponent<RectTransform>().sizeDelta;
        Image dragImage = dragIcon.GetComponent<Image>();
        dragImage.sprite = GetComponent<Image>().sprite;
        dragImage.color = new Color(dragImage.color.r, dragImage.color.g, dragImage.color.b, 0.7f);
        dragIcon.transform.position = Input.mousePosition;

        // Create placement indicator (a Quad rotated flat)
        placementIndicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
        placementIndicator.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        placementIndicator.transform.localScale = Vector3.one; // Temporary, will adjust below

        // Scale the indicator to match the prefab's footprint
        Renderer objRenderer = objectPrefab.GetComponentInChildren<Renderer>();
        if (objRenderer != null)
        {
            placementIndicator.transform.localScale = new Vector3(
                objRenderer.bounds.size.x,
                objRenderer.bounds.size.z,
                1f
            );
        }

        // Add a BoxCollider for collision detection
        BoxCollider indicatorCollider = placementIndicator.AddComponent<BoxCollider>();
        indicatorCollider.size = placementIndicator.transform.localScale;
        indicatorCollider.isTrigger = true;

        placementIndicator.GetComponent<Renderer>().material.color = Color.blue;
        placementIndicator.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    // Updates the icon position while dragging, and moves the placement indicator
    // so it always follows the mouse on y=0.1 (regardless of bounds).
    public void OnDrag(PointerEventData eventData)
    {
        // Move the drag icon in screen-space
        if (dragIcon != null)
        {
            dragIcon.transform.position = Input.mousePosition;
        }

        // Project the mouse onto the plane y=0 to get a 3D position
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, 0f); // plane at y=0
        float distance;

        if (groundPlane.Raycast(ray, out distance))
        {
            Vector3 placementPosition = ray.GetPoint(distance);
            placementPosition.y = 0.1f; // keep indicator slightly above the ground
            placementIndicator.transform.position = placementPosition;

            // Make sure the indicator is always visible
            placementIndicator.SetActive(true);

            // --- Same collision logic as before ---
            // Check for overlap with placed objects
            Collider[] colliders = Physics.OverlapBox(
                placementIndicator.transform.position,
                placementIndicator.transform.localScale / 2,
                placementIndicator.transform.rotation
            );

            bool isColliding = false;
            foreach (Collider col in colliders)
            {
                if (col.CompareTag("PlacedObject")) // must have a collider + 'PlacedObject' tag
                {
                    isColliding = true;
                    break;
                }
            }

            // Check if out of bounds 
            bool isOutOfBounds =
                (placementPosition.x <= -13f || placementPosition.x >= 13f ||
                 placementPosition.z <= -13f || placementPosition.z >= 13f);

            if (isColliding || isOutOfBounds)
            {
                placementIndicator.GetComponent<Renderer>().material.color = Color.red;
                canPlace = false;
            }
            else
            {
                placementIndicator.GetComponent<Renderer>().material.color = Color.blue;
                canPlace = true;
            }
        }
        else
        {
            // If we somehow can't hit the plane, show indicator in red at its last position
            placementIndicator.SetActive(true);
            placementIndicator.GetComponent<Renderer>().material.color = Color.red;
            canPlace = false;
        }
    }

    // Called when the user releases the icon while dragging and instantiates the prefab object
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (dragIcon != null)
        {
            // Perform a raycast to see if we can place the object on the "Ground"
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (canPlace && Physics.Raycast(ray, out hit) && hit.collider.CompareTag("Ground"))
            {
                // Clamp final spawn position within the allowed range
                Vector3 clampedPosition = new Vector3(
                    Mathf.Clamp(hit.point.x, -13f, 13f),
                    0f,
                    Mathf.Clamp(hit.point.z, -13f, 13f)
                );

                // Instantiate the 3D object
                GameObject placedObject = Instantiate(objectPrefab, clampedPosition, Quaternion.identity);
                placedObject.tag = "PlacedObject";
            }

            // Clean up
            Destroy(dragIcon);
            Destroy(placementIndicator);
        }

        // Restore button opacity after drag ends
        if (buttonImage != null)
        {
            buttonImage.color = new Color(buttonImage.color.r, buttonImage.color.g, buttonImage.color.b, 1f);
        }
    }
}
