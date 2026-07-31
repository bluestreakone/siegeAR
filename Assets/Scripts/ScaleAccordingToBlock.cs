using UnityEngine;

public class ScaleAccordingToBlock : MonoBehaviour
{
    [Header("Ring Settings")]
    [Tooltip("How much larger the ring should be compared to the block (e.g., 1.2 = 20% bigger).")]
    [SerializeField] private float sizeMultiplier = 1.5f;

    void Start()
    {
        ScaleRingSize();
    }

    private void ScaleRingSize()
    {
        // validate that the ring is actually parented to something
        if (transform.parent == null)
        {
            Debug.LogWarning("Ring has no parent block to scale to.");
            return;
        }

        
        // apply uniform scale to the ring's transform
        transform.localScale = new Vector3(
            sizeMultiplier,
            sizeMultiplier,
            transform.localScale.z // Preserve the ring's original height/thickness
        );
    }
}