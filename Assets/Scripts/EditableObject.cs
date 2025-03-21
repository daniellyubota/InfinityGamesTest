using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EditableObject : MonoBehaviour
{
    public static EditableObject currentSelected;

    // Panel with Confirm/Cancel buttons and a rotation slider.
    public GameObject editUIPanel;
    public Button confirmButton;
    public Button cancelButton;
    public Slider rotationSlider;

    // Materials for valid and invalid states.
    public Material validMaterial;
    public Material invalidMaterial;
    private Material originalMaterial;

    // Variables for dragging the object.
    private bool isDragging = false;
    private Vector3 dragOffset;
    private Camera mainCamera;

    // Store the position and rotation when the object is first selected.
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    // Boundaries for valid placement.
    public float minX = -13f;
    public float maxX = 13f;
    public float minZ = -13f;
    public float maxZ = 13f;

    void Start()
    {
        // Get the main camera and save the original material.
        mainCamera = Camera.main;
        if (GetComponent<Renderer>() != null)
            originalMaterial = GetComponent<Renderer>().material;

        // Hide the edit UI panel at startup.
        if (editUIPanel != null)
            editUIPanel.SetActive(false);
    }

    // When the object is clicked and released.
    void OnMouseUpAsButton()
    {
        if (Input.GetMouseButtonUp(0))
        {
            Select();
        }
    }

    // When the mouse is pressed on the object.
    void OnMouseDown()
    {
        // Start drag only if this object is already selected.
        if (currentSelected == this && Input.GetMouseButtonDown(0))
        {
            // Do not start drag if clicking on a UI element.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            isDragging = true;
            Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position);
            Vector3 mousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPos.z);
            dragOffset = transform.position - mainCamera.ScreenToWorldPoint(mousePos);
        }
    }

    // Update the object's position while dragging.
    void OnMouseDrag()
    {
        if (isDragging && currentSelected == this)
        {
            Vector3 screenPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y,
                                            mainCamera.WorldToScreenPoint(transform.position).z);
            Vector3 newPos = mainCamera.ScreenToWorldPoint(screenPos) + dragOffset;
            // Keep the Y position unchanged.
            newPos.y = transform.position.y;
            transform.position = newPos;

            // Check if the new position is valid.
            CheckValidity();
        }
    }

    // Stop dragging when the mouse is released.
    void OnMouseUp()
    {
        isDragging = false;
    }

    // Select this object for editing.
    public void Select()
    {
        // If already selected, do nothing.
        if (currentSelected == this)
            return;

        // Deselect any other object that is currently selected.
        if (currentSelected != null && currentSelected != this)
        {
            currentSelected.Deselect();
        }
        currentSelected = this;

        // Save the object's current position and rotation.
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        // Check if the object is in a valid position and update the material.
        CheckValidity();

        // Show the edit UI panel and reset the rotation slider to its neutral value (0.5).
        if (editUIPanel != null)
        {
            editUIPanel.SetActive(true);
            if (rotationSlider != null)
                rotationSlider.value = 0.5f;
        }
    }

    // Deselect the object and hide the edit UI panel.
    public void Deselect()
    {
        currentSelected = null;
        if (editUIPanel != null)
            editUIPanel.SetActive(false);

        // Restore the original material.
        if (GetComponent<Renderer>() != null && originalMaterial != null)
            GetComponent<Renderer>().material = originalMaterial;
    }

    // Called when the Confirm button is pressed.
    public void ConfirmEdit()
    {
        // Accept the changes and exit edit mode.
        Deselect();
    }

    // Called when the Cancel button is pressed.
    public void CancelEdit()
    {
        // Revert the object to its original position and rotation.
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        Deselect();
    }

    // Called when clicking the UI background to cancel editing.
    public void DeselectViaUI()
    {
        CancelEdit();
    }

    // Called when the rotation slider value changes.
    // The slider should range from 0 to 1 with 0.5 as the neutral value.
    public void OnRotationSliderChanged(float value)
    {
        Debug.Log("Slider value: " + value);
        float rotationRange = 180f; // Total rotation range is 180° (±90° from neutral).
        float delta = value - 0.5f;
        // Multiply delta by rotation range. A negative delta rotates in the opposite direction.
        float rotationOffset = -delta * rotationRange;
        // Set the rotation relative to the initial rotation.
        transform.rotation = Quaternion.Euler(initialRotation.eulerAngles.x,
                                              initialRotation.eulerAngles.y + rotationOffset,
                                              initialRotation.eulerAngles.z);
        // Update validity after rotation.
        CheckValidity();
    }

    // Check if the object's position is valid (not colliding or out of bounds).
    public void CheckValidity()
    {
        bool isOutOfBounds = transform.position.x < minX || transform.position.x > maxX ||
                             transform.position.z < minZ || transform.position.z > maxZ;
        bool isColliding = false;

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Vector3 halfExtents = Vector3.zero;
            Vector3 center = transform.position; // Default center

            // If the collider is a BoxCollider, calculate proper half extents using its size and center.
            if (col is BoxCollider box)
            {
                halfExtents = Vector3.Scale(box.size, transform.lossyScale) * 0.5f;
                center = transform.position + box.center;
            }
            else
            {
                // Use the collider's bounds if it's not a BoxCollider.
                halfExtents = col.bounds.extents;
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

        // If out of bounds or colliding, set the invalid material and disable the confirm button.
        if (isOutOfBounds || isColliding)
        {
            if (invalidMaterial != null && GetComponent<Renderer>() != null)
                GetComponent<Renderer>().material = invalidMaterial;
            if (confirmButton != null)
                confirmButton.interactable = false;
        }
        else
        {
            // Otherwise, use the valid material and enable the confirm button.
            if (validMaterial != null && GetComponent<Renderer>() != null)
                GetComponent<Renderer>().material = validMaterial;
            if (confirmButton != null)
                confirmButton.interactable = true;
        }
    }

    // Update the UI panel's screen position to follow the object.
    void LateUpdate()
    {
        if (currentSelected == this && editUIPanel != null)
        {
            Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position);
            // Adjust the panel position if needed.
            screenPos.y += 0;
            editUIPanel.GetComponent<RectTransform>().position = screenPos;
        }
    }
}
