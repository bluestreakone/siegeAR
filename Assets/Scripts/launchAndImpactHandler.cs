using UnityEngine;
using System.Collections;
using Debug = UnityEngine.Debug;

public class launchAndImpactHandler : MonoBehaviour
{

    
    [Header("Launch Settings")]
    [Tooltip("The force applied to launch the cannonball out of the barrel.")]
    [SerializeField] private float launchForce = 7f;

    [Header("Loss Condition Settings")]
    [Tooltip("How many seconds after launch to track impacts.")]
    [SerializeField] private float trackingDuration = 10f;
    [Tooltip("Minimum speed required upon impact to trigger a loss (prevents rolling/bumping false alarms).")]
    [SerializeField] private float minImpactVelocity = 4.0f;

    [SerializeField] private GameObject manager;

    private Rigidbody rb;

    private bool isTrackingImpacts = false;

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

            // begin 10-second tracking window for checking collision with castle's boxcollider
            StartCoroutine(ImpactWindowRoutine());
        }
        else
        {
            Debug.LogWarning("Cannonball is missing a Rigidbody component!");
        }
    }

    IEnumerator ImpactWindowRoutine()
    {
        isTrackingImpacts = true;
        yield return new WaitForSeconds(trackingDuration);
        isTrackingImpacts = false;
        
        Debug.Log("Cannonball impact tracking window has closed.");
    }

    // triggered when the cannonball physically hits another collider
    void OnCollisionEnter(Collision collision)
    {
        // ignore non-castle colliders
        if (!isTrackingImpacts) return;

        // check if collision was with castle
        if (collision.gameObject.name.Contains("castle"))
        {
            // get the ball's current velocity magnitude
            float impactSpeed = rb.linearVelocity.magnitude;

            if (impactSpeed >= minImpactVelocity)
            {
                //Debug.Log($"[DEFEAT] Cannonball struck castle at high speed: {impactSpeed:F2} m/s");
                TriggerGameLoss();
            }
            else
            {
                Debug.Log($"[Ignored] Cannonball hit castle, but speed was too low ({impactSpeed:F2} m/s).");
            }
        }
    }

    private void TriggerGameLoss()
    {
        // stop tracking so it doesn't trigger multiple times
        isTrackingImpacts = false;

        if (manager != null)
        {
            handleGameLoss lossScript = manager.GetComponent<handleGameLoss>();
            lossScript.handleLoss();
        }
    }
    
}


