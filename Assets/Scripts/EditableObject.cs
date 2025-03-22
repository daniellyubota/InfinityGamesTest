using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class EditableObject : MonoBehaviour
{
    public static EditableObject currentSelected;
    public static bool IsPressingUI = false;
    public static bool IsDraggingObject = false;

    public GameObject editUIPanel;
    public Button editButton;
    public Button deleteButton;
    public Button confirmButton;
    public Button cancelButton;
    public Slider rotationSlider;
    public float spawnAnimDuration;

    public Material validMaterial;
    public Material invalidMaterial;
    private Material originalMaterial;

    private bool isDragging = false;
    private Vector3 dragOffset;
    private Camera mainCamera;

    private Vector3 initialPosition;    // Saved parent's position on selection
    private Quaternion initialRotation; // Saved child's rotation on selection

    public float minX = -13f;
    public float maxX = 13f;
    public float minZ = -13f;
    public float maxZ = 13f;

    private bool isEditing = false;     // True when in active edit mode
    private Transform rootTransform;    // Parent that moves during drag

    void Start()
    {
        mainCamera = Camera.main;
        if (GetComponent<Renderer>() != null)
            originalMaterial = GetComponent<Renderer>().material;
        rootTransform = (transform.parent != null) ? transform.parent : transform;
        if (editUIPanel != null)
            editUIPanel.SetActive(false);

        // Add UI blocking events to each button to set the IsPressingUI flag.
        AddUIBlocker(editButton);
        AddUIBlocker(deleteButton);
        AddUIBlocker(confirmButton);
        AddUIBlocker(cancelButton);
        PlayPlacementAnimation(spawnAnimDuration);
    }

    // Add an EventTrigger to a button to update the UI-press flag.
    private void AddUIBlocker(Button btn)
    {
        if (btn == null)
            return;
        EventTrigger trigger = btn.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry pointerDown = new EventTrigger.Entry();
        pointerDown.eventID = EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((BaseEventData data) => { IsPressingUI = true; });
        trigger.triggers.Add(pointerDown);

        EventTrigger.Entry pointerUp = new EventTrigger.Entry();
        pointerUp.eventID = EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((BaseEventData data) => { IsPressingUI = false; });
        trigger.triggers.Add(pointerUp);
    }

    void Update()
    {
        // In selection mode (not editing), if a click occurs away from this object, deselect it.
        if (currentSelected == this && !isEditing && Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (!hit.collider.transform.IsChildOf(transform))
                    Deselect();
            }
            else
            {
                Deselect();
            }
        }
    }

    void OnMouseUpAsButton()
    {
        if (IsPressingUI)
            return;
        if (Input.GetMouseButtonUp(0))
            Select();
    }

    void OnMouseDown()
    {
        if (IsPressingUI)
            return;
        // Start dragging only if in edit mode.
        if (currentSelected == this && isEditing && Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
            isDragging = true;
            IsDraggingObject = true;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(rootTransform.position);
            Vector3 mousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPos.z);
            dragOffset = rootTransform.position - mainCamera.ScreenToWorldPoint(mousePos);
        }
    }

    void OnMouseDrag()
    {
        if (isDragging && currentSelected == this && isEditing)
        {
            Vector3 screenPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y,
                mainCamera.WorldToScreenPoint(rootTransform.position).z);
            Vector3 newPos = mainCamera.ScreenToWorldPoint(screenPos) + dragOffset;
            newPos.y = rootTransform.position.y;
            rootTransform.position = newPos;
            CheckValidity();
        }
    }

    void OnMouseUp()
    {
        isDragging = false;
        IsDraggingObject = false;
    }

    public void Select()
    {
        // Prevent selecting a new object if another is already being edited.
        if (currentSelected != null && currentSelected.isEditing && currentSelected != this)
            return;
        if (currentSelected == this)
            return;
        if (currentSelected != null && currentSelected != this)
            currentSelected.Deselect();

        currentSelected = this;
        initialPosition = rootTransform.position;
        initialRotation = transform.rotation;
        CheckValidity();

        if (editUIPanel != null)
        {
            editUIPanel.SetActive(true);
            if (editButton != null)
                editButton.gameObject.SetActive(true);
            if (deleteButton != null)
                deleteButton.gameObject.SetActive(true);
            if (confirmButton != null)
                confirmButton.gameObject.SetActive(false);
            if (cancelButton != null)
                cancelButton.gameObject.SetActive(false);
            if (rotationSlider != null)
                rotationSlider.gameObject.SetActive(false);
        }
        isEditing = false;
    }

    public void Deselect()
    {
        currentSelected = null;
        if (editUIPanel != null)
            editUIPanel.SetActive(false);
        if (GetComponent<Renderer>() != null && originalMaterial != null)
            GetComponent<Renderer>().material = originalMaterial;
    }

    public void EnterEditMode()
    {
        isEditing = true;
        if (rotationSlider != null)
        {
            rotationSlider.value = 6;
            rotationSlider.gameObject.SetActive(true);
        }
        if (editButton != null)
            editButton.gameObject.SetActive(false);
        if (deleteButton != null)
            deleteButton.gameObject.SetActive(false);
        if (confirmButton != null)
            confirmButton.gameObject.SetActive(true);
        if (cancelButton != null)
            cancelButton.gameObject.SetActive(true);
    }

    public void DeleteSelf()
    {
        if (rootTransform != null)
            Destroy(rootTransform.gameObject);
    }

    public void ConfirmEdit()
    {
        Deselect();
    }

    public void CancelEdit()
    {
        rootTransform.position = initialPosition;
        transform.rotation = initialRotation;
        Deselect();
    }

    public void DeselectViaUI()
    {
        CancelEdit();
    }

    // When the slider changes, rotate the child (this object) by fixed steps.
    // Slider range: 0 to 12 with 6 as neutral; each unit equals 30°.
    public void OnRotationSliderChanged(float value)
    {
        Debug.Log("Slider value: " + value);
        float rotationOffset = -(value - 6) * 30f;
        transform.rotation = Quaternion.Euler(initialRotation.eulerAngles.x,
                                              initialRotation.eulerAngles.y + rotationOffset,
                                              initialRotation.eulerAngles.z);
        CheckValidity();
    }

    public void CheckValidity()
    {
        bool isOutOfBounds = rootTransform.position.x < minX || rootTransform.position.x > maxX ||
                             rootTransform.position.z < minZ || rootTransform.position.z > maxZ;
        bool isColliding = false;
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Vector3 halfExtents = Vector3.zero;
            Vector3 center = rootTransform.position;
            if (col is BoxCollider box)
            {
                halfExtents = Vector3.Scale(box.size, transform.lossyScale) * 0.5f;
                center = rootTransform.position + box.center;
            }
            else
            {
                halfExtents = col.bounds.extents;
            }
            Collider[] colliders = Physics.OverlapBox(center, halfExtents, rootTransform.rotation);
            foreach (Collider other in colliders)
            {
                if (other.gameObject != gameObject && other.CompareTag("PlacedObject"))
                {
                    isColliding = true;
                    break;
                }
            }
        }
        if (isOutOfBounds || isColliding)
        {
            if (invalidMaterial != null && GetComponent<Renderer>() != null)
                GetComponent<Renderer>().material = invalidMaterial;
            if (confirmButton != null)
                confirmButton.interactable = false;
        }
        else
        {
            if (validMaterial != null && GetComponent<Renderer>() != null)
                GetComponent<Renderer>().material = validMaterial;
            if (confirmButton != null)
                confirmButton.interactable = true;
        }
    }

    public void PlayPlacementAnimation(float duration)
    {
        StartCoroutine(AnimatePlacement(duration));
    }

    private System.Collections.IEnumerator AnimatePlacement(float duration)
    {
        // Store the current Y position and local scale as the target values.
        float targetY = transform.position.y;
        Vector3 targetScale = transform.localScale;

        // Set starting values: Y = 0 and scale = 0.
        Vector3 startPos = new Vector3(transform.position.x, 0f, transform.position.z);
        transform.position = startPos;
        transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // t goes from 0 to 1 over the duration (using SmoothStep for a smooth curve).
            float t = Mathf.SmoothStep(0.5f, 1f, elapsed / duration);


            // Interpolate Y position and scale.
            float newY = Mathf.Lerp(0f, targetY, t);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure final values are set.
        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
        transform.localScale = targetScale;
    }
    void LateUpdate()
    {
        if (currentSelected == this && editUIPanel != null)
        {
            Vector3 screenPos = mainCamera.WorldToScreenPoint(rootTransform.position);
            editUIPanel.GetComponent<RectTransform>().position = screenPos;
        }
    }
}
