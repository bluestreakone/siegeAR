using UnityEngine;
using System.Collections;

public class ObjectManager : MonoBehaviour
{
    [Header("OBJECT CONTROLS")]
    [Tooltip("Check this box to enable all blocks.")]
    [SerializeField] private bool activateBlocks = false;
    [SerializeField] private bool activateCannonball = false;

    [Header("CANNONBALL")]
    [Tooltip("Assign cannonball GameObject or prefab here.")]
    [SerializeField] private GameObject cannonball;



    public int buildingTimeLimit = 90;

    private bool previousBlockActivationState = false;
    private bool previousCannonballActivationState = false;
    Rigidbody[] allBlocks; //list of blocks in scene
    

    void Start()
    {
        // Begin the 10-second auto-init countdown as soon as the scene loads
        StartCoroutine(BlockTimerRoutine());
    }

    // Update is called once per frame
    void Update()
    {
        //if the checkbox for activating blocks is NEWLY pressed
        if(activateBlocks && !previousBlockActivationState)
        {
            enableAllBlocks();
            previousBlockActivationState = true;
            StartCoroutine(BuildingTimerRoutine());
        }

        // if the cannonball is newly activated, activate the cannonball and delete any blocks outside of build area
        if(activateCannonball && !previousCannonballActivationState)
        {
            enableCannonball();
            destroyInvalidBlocks();
            previousCannonballActivationState = true;
        }
        
    }
    
    IEnumerator BlockTimerRoutine()
    {
        // wait for 10 seconds (works great on mobile without needing the Inspector)
        yield return new WaitForSeconds(10f);

        // activate the blocks and signal build timer to start
        activateBlocks = true;
    }

    IEnumerator BuildingTimerRoutine()
    {
        // wait for the designated building time
        yield return new WaitForSeconds(buildingTimeLimit);

        // activate the cannonball
        activateCannonball = true;
    }

    private void enableAllBlocks()
    {
        // finds all rigidbodies in the scene (or can filter by tag/component)
        allBlocks = FindObjectsOfType<Rigidbody>();

        // for every block, enable all core features
        foreach(Rigidbody rb in allBlocks)
        {
            // safety check: skip the cannonball so it doesn't activate early
            if (cannonball != null && rb.gameObject == cannonball) continue;

            rb.useGravity = true;
            rb.GetComponent<MeshRenderer>().enabled = true;
            rb.GetComponent<Collider>().enabled = true;
            rb.WakeUp();
        }
    }

    private void enableCannonball()
    {
        if (cannonball != null)
        {
            // activate the game object
            cannonball.SetActive(true);

            // enable physics components if attached
            Rigidbody rb = cannonball.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = true;
                rb.WakeUp();
            }

            MeshRenderer mr = cannonball.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = true;

            Collider col = cannonball.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            // explicitly tell the cannonball script to fire
            launchAndImpactHandler cbScript = cannonball.GetComponent<launchAndImpactHandler>();
            if (cbScript != null)
            {
                cbScript.Launch();
            }
            else
            {
                Debug.LogWarning("Cannonball GameObject is missing the Cannonball script component.");
            }
        }
        else
        {
            Debug.LogWarning("Cannonball object reference is missing in the ObjectManager Inspector.");
        }
    }

    private void destroyInvalidBlocks()
    {
        // for every block, call deletion function if not touching the building area
        foreach(Rigidbody rb in allBlocks)
        {
            if (rb == null) continue;

            // skip the cannonball
            if (cannonball != null && rb.gameObject == cannonball) continue;

            // call the public method on the block's own script
            BlockValidator validator = rb.GetComponent<BlockValidator>();
            if (validator != null)
            {
                validator.DestructIfInvalid();
            }
        }
    }
}
