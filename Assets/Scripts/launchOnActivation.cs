using UnityEngine;
using System.Collections;
using Debug = UnityEngine.Debug;

public class launchOnActivation : MonoBehaviour
{

    
    [Header("Launch Settings")]
    [Tooltip("The force applied to launch the cannonball out of the barrel.")]
    [SerializeField] private float launchForce = 7f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // called explicitly by ObjectManager when the timer finishes
    public void Launch()
    {
        StartCoroutine(LaunchRoutine());
    }

    IEnumerator LaunchRoutine()
    {
        // wait one physics frame for the object to register in the physics world
        yield return new WaitForFixedUpdate();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.WakeUp();

            // apply the impulse force out of the cannon's barrel
            rb.AddForce(transform.forward * launchForce, ForceMode.Impulse);
        }
        else
        {
            Debug.LogWarning("Cannonball is missing a Rigidbody component!");
        }
    }
    
}


