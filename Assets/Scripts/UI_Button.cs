using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class UI_Button : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public GameObject objectPrefab;
    private GameObject dragIcon;
    private RectTransform canvasTransform;
    private Camera mainCamera;
    private Image buttonImage;
    private GameObject placementIndicator;
    private bool canPlace = true;
    private bool didDrag = false; // Tracks whether the icon was dragged.

    void Start()
    {
        canvasTransform = GetComponentInParent<Canvas>().transform as RectTransform;
        mainCamera = Camera.main;
        buttonImage = GetComponent<Image>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (EditableObject.currentSelected != null)
            EditableObject.currentSelected.CancelEdit();
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        // Dim the button during drag.
        if (buttonImage != null)
            buttonImage.color = new Color(buttonImage.color.r, buttonImage.color.g, buttonImage.color.b, 0.3f);

        // Create a drag icon.
        dragIcon = new GameObject("DragIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dragIcon.transform.SetParent(canvasTransform, false);
        dragIcon.GetComponent<RectTransform>().sizeDelta = GetComponent<RectTransform>().sizeDelta;
        Image dragImage = dragIcon.GetComponent<Image>();
        dragImage.sprite = GetComponent<Image>().sprite;
        dragImage.color = new Color(dragImage.color.r, dragImage.color.g, dragImage.color.b, 0.7f);
        dragIcon.transform.position = Input.mousePosition;

        // Create the placement indicator.
        placementIndicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
        placementIndicator.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        placementIndicator.transform.localScale = Vector3.one;

        BoxCollider prefabCollider = objectPrefab.GetComponentInChildren<BoxCollider>();
        if (prefabCollider != null)
        {
            Vector3 localSize = prefabCollider.size;
            float scaleMultiplier = 0.4f;
            Vector3 footprint = new Vector3(localSize.x * scaleMultiplier, localSize.y * scaleMultiplier, 1f);
            placementIndicator.transform.localScale = footprint;
        }
        else
        {
            placementIndicator.transform.localScale = Vector3.one;
        }

        // Add a BoxCollider for collision detection.
        BoxCollider indicatorCollider = placementIndicator.AddComponent<BoxCollider>();
        indicatorCollider.size = placementIndicator.transform.localScale;
        indicatorCollider.isTrigger = true;

        // Assign a valid shader to the placement indicator.
        Renderer indicatorRend = placementIndicator.GetComponent<Renderer>();
        Shader unlitColor = Shader.Find("Unlit/Color");
        if (unlitColor != null)
        {
            indicatorRend.material.shader = unlitColor;
            indicatorRend.material.color = new Color(0f, 200f / 255f, 1f);
        }
        else
        {
            // Fallback: use the default sprite shader if Unlit/Color isn't found.
            Shader fallback = Shader.Find("Sprites/Default");
            if (fallback != null)
            {
                indicatorRend.material.shader = fallback;
                indicatorRend.material.color = new Color(0f, 200f / 255f, 1f);
            }
        }
        placementIndicator.layer = LayerMask.NameToLayer("Ignore Raycast");


        didDrag = false;
        canPlace = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        didDrag = true;
        if (dragIcon != null)
            dragIcon.transform.position = Input.mousePosition;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, 0f);
        float distance;
        if (groundPlane.Raycast(ray, out distance))
        {
            Vector3 placementPosition = ray.GetPoint(distance);
            placementPosition.y = 0.1f;
            placementIndicator.transform.position = placementPosition;
            placementIndicator.SetActive(true);

            Collider[] colliders = Physics.OverlapBox(
                placementIndicator.transform.position,
                placementIndicator.transform.localScale / 2,
                placementIndicator.transform.rotation);
            bool isColliding = false;
            foreach (Collider col in colliders)
            {
                if (col.CompareTag("PlacedObject"))
                {
                    isColliding = true;
                    break;
                }
            }

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
                placementIndicator.GetComponent<Renderer>().material.color = new Color(0f, 200f / 255f, 1f);
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

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        // If user never dragged, check collision via raycast.
        if (!didDrag && placementIndicator != null)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                Destroy(dragIcon);
                Destroy(placementIndicator);
                ResetButtonOpacity();
                return;
            }
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, 0f);
            float distance;
            if (groundPlane.Raycast(ray, out distance))
            {
                Vector3 placementPosition = ray.GetPoint(distance);
                placementPosition.y = 0.1f;
                placementIndicator.transform.position = placementPosition;

                Collider[] colliders = Physics.OverlapBox(
                    placementIndicator.transform.position,
                    placementIndicator.transform.localScale / 2,
                    placementIndicator.transform.rotation);
                bool isColliding = false;
                foreach (Collider col in colliders)
                {
                    if (col.CompareTag("PlacedObject"))
                    {
                        isColliding = true;
                        break;
                    }
                }
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
                    placementIndicator.GetComponent<Renderer>().material.color = new Color(0f, 200f / 255f, 1f);
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

        if (dragIcon != null)
        {
            if (canPlace && placementIndicator != null)
                StartCoroutine(AnimateAndPlace(0.15f));
            else
            {
                Destroy(dragIcon);
                if (placementIndicator != null)
                    Destroy(placementIndicator);
            }
        }

        ResetButtonOpacity();
    }

    // Coroutine that lerps the drag icon's scale to 0.2 over the given duration and then places the object.
    private IEnumerator AnimateAndPlace(float duration)
    {
        float elapsed = 0f;
        Vector3 initialScale = dragIcon.transform.localScale;
        Vector3 targetScale = Vector3.one * 0.2f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            dragIcon.transform.localScale = Vector3.Lerp(initialScale, targetScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        dragIcon.transform.localScale = targetScale;

        // Instantiate the object if placement is valid.
        if (canPlace && placementIndicator != null)
        {
            Vector3 pos = placementIndicator.transform.position;
            GameObject placedObject = Instantiate(objectPrefab, pos, Quaternion.identity);
            placedObject.tag = "PlacedObject";
        }
        Destroy(dragIcon);
        Destroy(placementIndicator);
    }

    // Resets the button image's opacity.
    private void ResetButtonOpacity()
    {
        if (buttonImage != null)
            buttonImage.color = new Color(buttonImage.color.r, buttonImage.color.g, buttonImage.color.b, 1f);
    }
}
