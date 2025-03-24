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
    // Array to pick the random material from.
    public Material[] randomMaterials;
    // Store the original materials (first will be random, second remains white)
    private Material[] originalMaterials;

    private bool isDragging = false;
    private Vector3 dragOffset;
    private Camera mainCamera;

    // Saved parent's position on selection
    private Vector3 initialPosition;
    // Saved child's rotation on selection
    private Quaternion initialRotation;

    public float minX = -13f;
    public float maxX = 13f;
    public float minZ = -13f;
    public float maxZ = 13f;

    private bool isEditing = false; // True when in active edit mode
    private Transform rootTransform; // Parent that moves during drag

    void Start()
    {
        mainCamera = Camera.main;
        rootTransform = (transform.parent != null) ? transform.parent : transform;

        // Get the current materials from the renderer.
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            originalMaterials = rend.materials; // assumes two materials already assigned
            // Pick a random material for the first slot if available.
            if (randomMaterials != null && randomMaterials.Length > 0)
            {
                int randIndex = Random.Range(0, randomMaterials.Length);
                Material[] newMats = rend.materials;
                newMats[0] = randomMaterials[randIndex];
                rend.materials = newMats;
                originalMaterials = (Material[])newMats.Clone();
            }
        }

        if (editUIPanel != null)
            editUIPanel.SetActive(false);

        // Hook pointer events on each button to set the IsPressingUI flag.
        AddUIBlocker(editButton);
        AddUIBlocker(deleteButton);
        AddUIBlocker(confirmButton);
        AddUIBlocker(cancelButton);

        PlayPlacementAnimation(spawnAnimDuration);
    }

    // Adds EventTrigger entries to a button to update the UI press flag.
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
        // Only drag if in edit mode.
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
        // Prevent selecting a new object if another is in active edit mode.
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
        // Force the object to switch to the "Outlined" layer.
        gameObject.layer = LayerMask.NameToLayer("Outlined");
        // Restore the original materials so that no valid material is forced.
        Renderer rend = GetComponent<Renderer>();
        if (rend != null && originalMaterials != null)
        {
            rend.materials = originalMaterials;
        }
        isEditing = false;
    }

    public void Deselect()
    {
        currentSelected = null;
        if (editUIPanel != null)
            editUIPanel.SetActive(false);
        // Restore original materials.
        if (GetComponent<Renderer>() != null && originalMaterials != null)
            GetComponent<Renderer>().materials = originalMaterials;
        // Ensure layer is set back to Default (0).
        gameObject.layer = LayerMask.NameToLayer("Default");
    }

    public void EnterEditMode()
    {
        isEditing = true;
        // Change layer back to Default so that valid/invalid material is applied.
        gameObject.layer = LayerMask.NameToLayer("Default");
        if (rotationSlider != null)
        {
            rotationSlider.value = 6; // Set neutral value to 6.
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
        // Now that we're in edit mode, CheckValidity() will assign valid/invalid materials.
        CheckValidity();
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
            Vector3 center = Vector3.zero;
            if (col is BoxCollider box)
            {
                halfExtents = Vector3.Scale(box.size, transform.lossyScale) * 0.5f;
                center = transform.TransformPoint(box.center);
            }
            else
            {
                halfExtents = col.bounds.extents;
                center = col.bounds.center;
            }
            Collider[] colliders = Physics.OverlapBox(center, halfExtents, transform.rotation);
            foreach (Collider other in colliders)
            {
                if (other.gameObject != gameObject && other.CompareTag("PlacedObject"))
                {
                    isColliding = true;
                    break;
                }
            }
        }
        Renderer rend = GetComponent<Renderer>();
        // Only update materials if in edit mode.
        if (isEditing)
        {
            if (isOutOfBounds || isColliding)
            {
                if (invalidMaterial != null && rend != null)
                    rend.materials = new Material[] { Instantiate(invalidMaterial), Instantiate(invalidMaterial) };
                if (confirmButton != null)
                    confirmButton.interactable = false;
            }
            else
            {
                if (validMaterial != null && rend != null)
                    rend.materials = new Material[] { Instantiate(validMaterial), Instantiate(validMaterial) };
                if (confirmButton != null)
                    confirmButton.interactable = true;
            }
        }
        else
        {
            // When not editing, do not change materials (outline will be applied via the Outlined layer).
            if (confirmButton != null)
                confirmButton.interactable = !(isOutOfBounds || isColliding);
        }
    }

    public void PlayPlacementAnimation(float duration)
    {
        StartCoroutine(AnimatePlacement(duration));
    }

    private System.Collections.IEnumerator AnimatePlacement(float duration)
    {
        float targetY = transform.position.y;
        Vector3 targetScale = transform.localScale;

        Vector3 startPos = new Vector3(transform.position.x, 0f, transform.position.z);
        transform.position = startPos;
        transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0.7f, 1f, elapsed / duration);
            float newY = Mathf.Lerp(0f, targetY, t);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
        transform.localScale = targetScale;
    }

    void LateUpdate()
    {
        if (currentSelected == this && editUIPanel != null)
        {
            Vector3 screenPos = mainCamera.WorldToScreenPoint(rootTransform.position);
            screenPos.y += 50; // This offset makes the UI appear above the object.
            editUIPanel.GetComponent<RectTransform>().position = screenPos;
        }
    }
}
