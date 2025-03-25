using UnityEngine;

public class CollisionToggle : MonoBehaviour
{
    private Renderer rend;
    private Collider col;

    void Start()
    {
        rend = GetComponent<Renderer>();
        col = GetComponent<Collider>();

        if (rend == null)
            Debug.LogWarning("CollisionToggle: No Renderer found on " + gameObject.name);
        if (col == null)
            Debug.LogWarning("CollisionToggle: No Collider found on " + gameObject.name);
    }

    void Update()
    {
        if (col == null)
            return;

        Vector3 halfExtents = Vector3.zero;
        Vector3 center = Vector3.zero;
        // Use collider's local size if it's a BoxCollider, otherwise use bounds.
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

        // Check for overlapping colliders.
        Collider[] colliders = Physics.OverlapBox(center, halfExtents, transform.rotation);
        bool colliding = false;
        foreach (Collider other in colliders)
        {
            if (other.gameObject != gameObject && other.CompareTag("PlacedObject"))
            {
                colliding = true;
                break;
            }
        }

        // Toggle renderer based on collision status.
        if (colliding)
        {
            if (rend.enabled)
                rend.enabled = false;
        }
        else
        {
            if (!rend.enabled)
                rend.enabled = true;
        }
    }
}
