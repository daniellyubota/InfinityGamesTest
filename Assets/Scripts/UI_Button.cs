using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_Button : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public GameObject objectPrefab; // Prefab to spawn when dropping the object
    private GameObject dragIcon; // UI icon that follows the cursor
    private RectTransform canvasTransform;
    private Camera mainCamera;
    private Image buttonImage; // Reference to button image to modify opacity

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
    }

    // Updates the icon position while dragging
    public void OnDrag(PointerEventData eventData)
    {
        if (dragIcon != null)
        {
            dragIcon.transform.position = Input.mousePosition;
        }
    }

    // Called when the user releases the icon while dragging and instantiates the prefab object
    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (dragIcon != null)
        {
            // Perform a raycast to determine where to place the object
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit) && hit.collider.CompareTag("Ground"))
            {
                // Clamp the object position within the allowed range (-13 to 13) and set Y to 0
                Vector3 clampedPosition = new Vector3(
                    Mathf.Clamp(hit.point.x, -13f, 13f),
                    0f,
                    Mathf.Clamp(hit.point.z, -13f, 13f)
                );

                // Instantiate the 3D object at the determined position
                Instantiate(objectPrefab, clampedPosition, Quaternion.identity);
            }

            Destroy(dragIcon);
        }

        // Restore button opacity after drag ends
        if (buttonImage != null)
        {
            buttonImage.color = new Color(buttonImage.color.r, buttonImage.color.g, buttonImage.color.b, 1f);
        }
    }
}