using UnityEngine;


public class ScaleAccordingToBlock : MonoBehaviour
{

    [Header("Ring Settings")]
    [Tooltip("How much extra space (padding) the ring should have around the block.")]
    [SerializeField] private float padding = 0.2f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //when the parent block object first starts, scale the ring size
        scaleRingSize();
    }

    private void scaleRingSize()
    {
        // validate that the ring is actually parented to something
        if (transform.parent == null)
        {
            Debug.LogWarning("Ring has no parent block to scale to.");
            return;
        }

        // get the visual renderer of the parent block & validate
        Renderer parentRenderer = transform.parent.GetComponent<Renderer>();
        if (parentRenderer == null)
        {
            Debug.LogWarning("Parent block is missing a MeshRenderer.");
            return;
        }


        // bounds.size gives the absolute world-space physical dimensions of the block
        Vector3 blockWorldSize = parentRenderer.bounds.size;

        // calculate the desired new x and y values including padding
        float targetRingSizeX = blockWorldSize.x + padding;
        float targetRingSizeY = blockWorldSize.y + padding;

        // Because the ring is a child, its local scale is multiplied by the parent's scale.
        // We divide by the parent's world scale (lossyScale) to counteract any non-uniform 
        // stretching from the parent and force the ring to remain perfectly circular.
        Vector3 parentScale = transform.parent.lossyScale;

        transform.localScale = new Vector3(
            targetRingSizeX / parentScale.x,
            targetRingSizeY / parentScale.y,
            transform.localScale.z // keep original ring height, only modifying the x and y to scale correctly
        );
    }
}
