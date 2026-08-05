using UnityEngine;

public class BlockValidator : MonoBehaviour
{
    private bool isInsideZone = false;

    
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("buildingZone"))
        {
            isInsideZone = true;
        }
    }

    // called when the block leaves the trigger zone
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("buildingZone"))
        {
            isInsideZone = false;
        }
    }

    // called by ObjectManager when the timer finishes
    public void DestructIfInvalid()
    {
        if (!isInsideZone)
        {
            Destroy(gameObject);
        }
    }
}