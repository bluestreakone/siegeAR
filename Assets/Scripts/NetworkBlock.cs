using UnityEngine;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class NetworkBlock : MonoBehaviour
{
    [Header("Block Identification")]
    [SerializeField] private int blockId;

    public int BlockId => blockId;
    public bool IsLocallyHeld => isLocallyHeld;

    private bool isLocallyHeld = false;
    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;
    private Transform localRoot;

    [Header("Rest Gliding Settings")]
    private Vector3 targetRestPosition;
    private Quaternion targetRestRotation;
    private Vector3 startRestPosition;
    private Quaternion startRestRotation;
    private bool isGlidingToRest = false;
    private float glideTimer = 0f;
    private float glideDuration = 0.15f; // Duration of the smooth landing glide

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();
        localRoot = transform.root; 
    }

    private System.Collections.IEnumerator Start()
    {
        while (ObjectManager.Instance == null)
        {
            yield return null;
        }

        ObjectManager.Instance.RegisterBlock(blockId, this);

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }

        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    [Header("Settlement Tracking")]
    private bool isMoving = false;
    private float restTimer = 0f;

    private void Update()
    {
        if (isLocallyHeld && ObjectManager.Instance != null && localRoot != null)
        {
            // Actively held by local player: stream continuous updates
            Vector3 localPos = localRoot.InverseTransformPoint(transform.position);
            Quaternion localRot = Quaternion.Inverse(localRoot.rotation) * transform.rotation;
            ObjectManager.Instance.SyncBlockTransformServerRpc(blockId, localPos, localRot);
        }
        else if (!isLocallyHeld && isGlidingToRest)
        {
            // Smoothly glide into the final resting position using an ease-out curve
            glideTimer += Time.deltaTime;
            float t = Mathf.Clamp01(glideTimer / glideDuration);
            float easeT = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(startRestPosition, targetRestPosition, easeT);
            transform.rotation = Quaternion.Slerp(startRestRotation, targetRestRotation, easeT);

            if (t >= 1f)
            {
                transform.position = targetRestPosition;
                transform.rotation = targetRestRotation;
                isGlidingToRest = false;
            }
        }
    }

    private void FixedUpdate()
    {
        if (rb == null || rb.isKinematic) return;

        bool currentlyMoving = rb.linearVelocity.magnitude > 0.05f || rb.angularVelocity.magnitude > 0.05f;

        if (currentlyMoving)
        {
            isMoving = true;
            restTimer = 0f;
        }
        else if (isMoving)
        {
            restTimer += Time.fixedDeltaTime;
            if (restTimer > 0.3f)
            {
                isMoving = false;
                restTimer = 0f;

                SendRestSnapshot();
            }
        }
    }

    private void SendRestSnapshot()
    {
        if (localRoot == null || ObjectManager.Instance == null) return;

        Vector3 localPos = localRoot.InverseTransformPoint(transform.position);
        Quaternion localRot = Quaternion.Inverse(localRoot.rotation) * transform.rotation;

        if (isLocallyHeld)
        {
            ObjectManager.Instance.SyncBlockTransformServerRpc(blockId, localPos, localRot);
        }
        else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            ObjectManager.Instance.ForceSyncBlockTransform(blockId, localPos, localRot);
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (ObjectManager.Instance != null && NetworkManager.Singleton != null)
        {
            ulong clientId = NetworkManager.Singleton.LocalClientId;
            ObjectManager.Instance.RequestBlockOwnershipServerRpc(blockId, clientId);
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isLocallyHeld = false;
        isMoving = true;
        restTimer = 0f;

        Vector3 throwVelocity = rb != null ? rb.linearVelocity : Vector3.zero;
        Vector3 throwAngularVelocity = rb != null ? rb.angularVelocity : Vector3.zero;

        if (ObjectManager.Instance != null && NetworkManager.Singleton != null)
        {
            ulong clientId = NetworkManager.Singleton.LocalClientId;
            ObjectManager.Instance.ReleaseBlockOwnershipServerRpc(blockId, clientId, throwVelocity, throwAngularVelocity);
        }
    }

    public void HandleRemoteRelease(Vector3 velocity, Vector3 angularVelocity)
    {
        isLocallyHeld = false;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = velocity;
            rb.angularVelocity = angularVelocity;
        }

        isMoving = true;
        restTimer = 0f;
    }

    public void SetOwnershipState(bool held, bool heldByAnyone)
    {
        isLocallyHeld = held;

        if (rb != null)
        {
            if (held)
            {
                rb.useGravity = false;
            }
            else if (heldByAnyone)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            else
            {
                bool isServer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
                rb.isKinematic = !isServer; 
                rb.useGravity = isServer;
            }
        }
    }

    public void UpdateRemoteTransform(Vector3 localPos, Quaternion localRot, bool isRestSnapshot = false)
    {
        if (isLocallyHeld) return;

        if (localRoot != null)
        {
            Vector3 worldPos = localRoot.TransformPoint(localPos);
            Quaternion worldRot = localRoot.rotation * localRot;

            if (isRestSnapshot)
            {
                startRestPosition = transform.position;
                startRestRotation = transform.rotation;
                targetRestPosition = worldPos;
                targetRestRotation = worldRot;
                glideTimer = 0f;
                isGlidingToRest = true;
            }
            else
            {
                transform.position = worldPos;
                transform.rotation = worldRot;
                isGlidingToRest = false;
            }
        }
    }

    public void DropToGround()
    {
        foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
        {
            renderer.enabled = true;
        }

        foreach (var col in GetComponentsInChildren<Collider>())
        {
            col.enabled = true;
        }

        // Enable physics and gravity on BOTH host and client simultaneously 
        // so blocks drop immediately together with zero floating or delay.
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        if (grabInteractable != null)
        {
            grabInteractable.enabled = true;
        }
    }

    public void DisableBuilding()
    {
        if (grabInteractable != null)
        {
            grabInteractable.enabled = false;
        }
    }
}