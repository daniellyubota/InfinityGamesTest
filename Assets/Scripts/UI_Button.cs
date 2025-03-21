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

    // Called when the user left-clicks the button and starts dragging the UI image.
    public void OnPointerDown(PointerEventData eventData)
    {
        if (EditableObject.currentSelected != null)
            EditableObject.currentSelected.CancelEdit();

        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        // Reduce button opacity during dragging.
        if (buttonImage != null)
        {
            buttonImage.color = new Color(buttonImage.color.r, buttonImage.color.g, buttonImage.color.b, 0.3f);
        }

        // Create a draggable icon that follows the cursor.
        dragIcon = new GameObject("DragIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dragIcon.transform.SetParent(canvasTransform, false);
        dragIcon.GetComponent<RectTransform>().sizeDelta = GetComponent<RectTransform>().sizeDelta;
        Image dragImage = dragIcon.GetComponent<Image>();
        dragImage.sprite = GetComponent<Image>().sprite;
        dragImage.color = new Color(dragImage.color.r, dragImage.color.g, dragImage.color.b, 0.7f);
        dragIcon.transform.position = Input.mousePosition;

        // Create the placement indicator 
        placementIndicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
        placementIndicator.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        placementIndicator.transform.localScale = Vector3.one; // Temporary scale

        // Scale the indicator to match the prefab's footprint.
        Renderer objRenderer = objectPrefab.GetComponentInChildren<Renderer>();
        if (objRenderer != null)
        {
            placementIndicator.transform.localScale = new Vector3(
                objRenderer.bounds.size.x,
                objRenderer.bounds.size.z,
                1f
            );
        }

        // Add a BoxCollider for collision detection.
        BoxCollider indicatorCollider = placementIndicator.AddComponent<BoxCollider>();
        indicatorCollider.size = placementIndicator.transform.localScale;
        indicatorCollider.isTrigger = true;

        placementIndicator.GetComponent<Renderer>().material.color = Color.blue;
        placementIndicator.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    // Called while dragging, updates the drag icon and placement indicator.
    public void OnDrag(PointerEventData eventData)
    {
        // Update the drag icon's screen position.
        if (dragIcon != null)
        {
            dragIcon.transform.position = Input.mousePosition;
        }

        // Project the mouse position onto a plane at y = 0.
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, 0f);
        float distance;

        if (groundPlane.Raycast(ray, out distance))
        {
            Vector3 placementPosition = ray.GetPoint(distance);
            placementPosition.y = 0.1f; // Keeps indicator slightly above the ground.
            placementIndicator.transform.position = placementPosition;
            placementIndicator.SetActive(true);

            // Check for overlap with placed objects.
            Collider[] colliders = Physics.OverlapBox(
                placementIndicator.transform.position,
                placementIndicator.transform.localScale / 2,
                placementIndicator.transform.rotation
            );

            bool isColliding = false;
            foreach (Collider col in colliders)
            {
                if (col.CompareTag("PlacedObject"))
                {
                    isColliding = true;
                    break;
                }
            }

            // Check for out of bounds.
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
            placementIndicator.SetActive(true);
            placementIndicator.GetComponent<Renderer>().material.color = Color.red;
            canPlace = false;
        }
    }

    // Called when the user releases the button and places the object if the indicator is valid.
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (dragIcon != null)
        {
            if (canPlace)
            {
                // Use the placement indicator's position 
                Vector3 pos = placementIndicator.transform.position;
                Vector3 clampedPosition = new Vector3(
                    Mathf.Clamp(pos.x, -13f, 13f),
                    0f,
                    Mathf.Clamp(pos.z, -13f, 13f)
                );

                GameObject placedObject = Instantiate(objectPrefab, clampedPosition, Quaternion.identity);
                placedObject.tag = "PlacedObject";
            }

            Destroy(dragIcon);
            Destroy(placementIndicator);
        }

        if (buttonImage != null)
        {
            buttonImage.color = new Color(buttonImage.color.r, buttonImage.color.g, buttonImage.color.b, 1f);
        }
    }
}
