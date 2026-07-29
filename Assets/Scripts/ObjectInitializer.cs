using UnityEngine;
using System.Collections;

public class ObjectInitializer : MonoBehaviour
{
    [Header("BLOCK CONTROLS")]
    [Tooltip("Check this box to enable all blocks.")]
    [SerializeField] private bool activateBlocks = false;

    private bool previousActivationState = false;


    void Start()
    {
        // Begin the 10-second auto-init countdown as soon as the scene loads
        StartCoroutine(TimerRoutine());
    }

    // Update is called once per frame
    void Update()
    {
        //if the checkbox for activating blocks is NEWLY pressed
        if(activateBlocks && !previousActivationState)
        {
            enableAllBlocks();
            previousActivationState = true;
        }  

    }
    
    IEnumerator TimerRoutine()
    {
        // wait for 10 seconds (works great on mobile without needing the Inspector)
        yield return new WaitForSeconds(10f);

        // activate the blocks
        activateBlocks = true;
    }
    private void enableAllBlocks()
    {
        // finds all rigidbodies in the scene (or can filter by tag/component)
        Rigidbody[] allBlocks = FindObjectsOfType<Rigidbody>();

        // for every block, enable all core features
        foreach(Rigidbody rb in allBlocks)
        {
            rb.useGravity = true;
            rb.GetComponent<MeshRenderer>().enabled = true;
            rb.GetComponent<Collider>().enabled = true;
            rb.WakeUp();
        }
    }
}
