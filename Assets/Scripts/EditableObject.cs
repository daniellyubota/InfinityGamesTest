using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class EditableObject : MonoBehaviour
{
    // Static references to track the current selected object and UI input.
    public static EditableObject currentSelected;
    public static bool IsPressingUI = false;
    public static bool IsDraggingObject = false;

    // UI elements for object editing.
    public GameObject editUIPanel;
    public Button editButton;
    public Button deleteButton;
    public Button confirmButton;
    public Button cancelButton;
    public Slider rotationSlider;
    public float spawnAnimDuration;

    // Materials used for editing state.
    public Material validMaterial;
    public Material invalidMaterial;
    // Array to choose a random material from.
    public Material[] randomMaterials;
    // Stores the original material array assigned on spawn.
    private Material[] originalMaterials;
    // Saved material instance for later restoration.
    private Material[] originalMatInstance = null;

    private bool isDragging = false;
    private Vector3 dragOffset;
    private Camera mainCamera;

    // Saved transform values for resetting position/rotation.
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    // Boundaries for placement.
    public float minX = -13f;
    public float maxX = 13f;
    public float minZ = -13f;
    public float maxZ = 13f;

    private bool isEditing = false; // True when object is in edit mode.
    private Transform rootTransform; // Reference to the parent transform used for movement.

    void Start()
    {
        mainCamera = Camera.main;
        rootTransform = (transform.parent != null) ? transform.parent : transform;

        // Cache and set up the original materials.
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            originalMaterials = rend.materials; // Assumes two materials assigned.
            if (randomMaterials != null && randomMaterials.Length > 0)
            {
                int randIndex = Random.Range(0, randomMaterials.Length);
                Material[] newMats = rend.materials;
                newMats[0] = randomMaterials[randIndex];
                rend.materials = newMats;
                originalMaterials = (Material[])newMats.Clone();
            }
            // Randomize the _NoiseSize property on each material.
            for (int i = 0; i < originalMaterials.Length; i++)
            {
                if (originalMaterials[i].HasProperty("_NoiseSize"))
                {
                    float randomNoiseSize = Random.Range(10f, 60f);
                    originalMaterials[i].SetFloat("_NoiseSize", randomNoiseSize);
                }
            }
        }

        if (editUIPanel != null)
            editUIPanel.SetActive(false);

        // Add UI blocking to buttons.
        AddUIBlocker(editButton);
        AddUIBlocker(deleteButton);
        AddUIBlocker(confirmButton);
        AddUIBlocker(cancelButton);

        PlayPlacementAnimation(spawnAnimDuration);
    }

    // Adds pointer events to a button to set/reset the UI flag.
    private void AddUIBlocker(Button btn)
    {
        if (btn == null)
            return;

        EventTrigger trigger = btn.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry pointerDown = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerDown
        };
        pointerDown.callback.AddListener((BaseEventData data) => { IsPressingUI = true; });
        trigger.triggers.Add(pointerDown);

        EventTrigger.Entry pointerUp = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerUp
        };
        pointerUp.callback.AddListener((BaseEventData data) => { IsPressingUI = false; });
        trigger.triggers.Add(pointerUp);
    }

    void Update()
    {
        // Deselect if click occurs outside this object while not editing.
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

        // Only allow dragging if in edit mode.
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

    // Selects this object and saves its material instance for later restoration.
    public void Select()
    {
        // Avoid selecting if another object is in edit mode.
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
            if (editButton != null) editButton.gameObject.SetActive(true);
            if (deleteButton != null) deleteButton.gameObject.SetActive(true);
            if (confirmButton != null) confirmButton.gameObject.SetActive(false);
            if (cancelButton != null) cancelButton.gameObject.SetActive(false);
            if (rotationSlider != null) rotationSlider.gameObject.SetActive(false);
        }
        // Save the current material instance only once.
        Renderer rend = GetComponent<Renderer>();
        if (rend != null && originalMatInstance == null)
        {
            originalMatInstance = rend.materials;
        }
        // Set the object's layer to "Outlined" for the outline effect.
        gameObject.layer = LayerMask.NameToLayer("Outlined");
        isEditing = false;
    }

    // Deselects this object, restores the saved material instance and resets the layer.
    public void Deselect()
    {
        currentSelected = null;
        if (editUIPanel != null)
            editUIPanel.SetActive(false);
        Renderer rend = GetComponent<Renderer>();
        if (rend != null && originalMatInstance != null)
        {
            rend.materials = originalMatInstance;
        }
        gameObject.layer = LayerMask.NameToLayer("Default");
    }

    // Enters edit mode, changing the layer back and applying valid/invalid material logic.
    public void EnterEditMode()
    {
        isEditing = true;
        gameObject.layer = LayerMask.NameToLayer("Default");
        if (rotationSlider != null)
        {
            rotationSlider.value = 6; // Neutral slider value.
            rotationSlider.gameObject.SetActive(true);
        }
        if (editButton != null) editButton.gameObject.SetActive(false);
        if (deleteButton != null) deleteButton.gameObject.SetActive(false);
        if (confirmButton != null) confirmButton.gameObject.SetActive(true);
        if (cancelButton != null) cancelButton.gameObject.SetActive(true);
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

    // Rotates the object based on slider input. Each unit equals 30° rotation.
    public void OnRotationSliderChanged(float value)
    {
        float rotationOffset = -(value - 6) * 30f;
        transform.rotation = Quaternion.Euler(initialRotation.eulerAngles.x,
                                              initialRotation.eulerAngles.y + rotationOffset,
                                              initialRotation.eulerAngles.z);
        CheckValidity();
    }

    // Checks if the object is out of bounds or colliding with other placed objects.
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
        if (isOutOfBounds || isColliding)
        {
            if (invalidMaterial != null && rend != null)
            {
                Material mat1 = Instantiate(invalidMaterial);
                Material mat2 = Instantiate(invalidMaterial);
                rend.materials = new Material[] { mat1, mat2 };
            }
            if (confirmButton != null)
                confirmButton.interactable = false;
        }
        else
        {
            if (isEditing)
            {
                if (validMaterial != null && rend != null)
                {
                    Material mat1 = Instantiate(validMaterial);
                    Material mat2 = Instantiate(validMaterial);
                    rend.materials = new Material[] { mat1, mat2 };
                }
                if (confirmButton != null)
                    confirmButton.interactable = true;
            }
            else
            {
                if (confirmButton != null)
                    confirmButton.interactable = true;
            }
        }
    }

    public void PlayPlacementAnimation(float duration)
    {
        StartCoroutine(AnimatePlacement(duration));
    }

    // Plays an animation that scales the object up from zero.
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
            screenPos.y += 50; // Offset to position the UI above the object.
            editUIPanel.GetComponent<RectTransform>().position = screenPos;
        }
    }
}
