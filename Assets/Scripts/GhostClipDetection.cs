using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class GhostClipDetection : MonoBehaviour
{
    [Header("Ghost Reference")]
    [SerializeField] private Transform ghostTransform;

    [Header("Detach Threshold")]
    [SerializeField] private float maxOffset = 0.15f;

    [Header("Throwing Settings")]
    [Tooltip("Multiplier for how strong the throw impulse is applied to the parent block.")]
    [SerializeField] private float throwVelocityScale = 1.5f;

    [Header("Debug Info")]
    public float currentOffset;
    public bool wasForceDropped = false;

    private XRGrabInteractable ghostInteractable;
    private Rigidbody parentRigidbody;
    private Transform originalParent;

    // --- Velocity Tracking ---
    private Vector3 lastGhostPosition;
    private Vector3 calculatedGhostVelocity;

    private void Awake()
    {
        parentRigidbody = GetComponent<Rigidbody>();

        if (ghostTransform != null)
        {
            ghostInteractable = ghostTransform.GetComponent<XRGrabInteractable>();
            originalParent = ghostTransform.parent;
        }
    }

    private void OnEnable()
    {
        if (ghostInteractable != null)
        {
            ghostInteractable.selectEntered.AddListener(OnGhostGrabbed);
            ghostInteractable.selectExited.AddListener(OnGhostReleased);
        }
    }

    private void OnDisable()
    {
        if (ghostInteractable != null)
        {
            ghostInteractable.selectEntered.RemoveListener(OnGhostGrabbed);
            ghostInteractable.selectExited.RemoveListener(OnGhostReleased);
        }
    }

    private void OnGhostGrabbed(SelectEnterEventArgs args)
    {
        if (wasForceDropped)
        {
            IXRSelectInteractor interactor = args.interactorObject;
            if (ghostInteractable.interactionManager != null && interactor != null)
            {
                ghostInteractable.interactionManager.SelectExit(interactor, ghostInteractable);
            }
            return;
        }

        if (ghostTransform != null)
        {
            // Center ghost on testBlock before unparenting
            ghostTransform.SetParent(null);
            ghostTransform.position = transform.position;
            ghostTransform.rotation = transform.rotation;
            
            // Initialize velocity tracking position
            lastGhostPosition = ghostTransform.position;
            calculatedGhostVelocity = Vector3.zero;
        }
    }

    private void OnGhostReleased(SelectExitEventArgs args)
    {
        if (!wasForceDropped && parentRigidbody != null)
        {
            // Apply calculated drag velocity scaled by throw multiplier
            parentRigidbody.linearVelocity = calculatedGhostVelocity * throwVelocityScale;
        }

        // Delay re-parenting to end of frame to avoid XR toolkit timing offsets
        StartCoroutine(ResetGhostNextFrame());
    }

    private void Update()
    {
        if (ghostTransform == null || ghostInteractable == null) return;

        if (wasForceDropped && !ghostInteractable.isSelected && !ghostInteractable.isHovered)
        {
            wasForceDropped = false;
        }

        if (ghostInteractable.isSelected && !wasForceDropped)
        {
            // Track drag velocity across frames
            if (Time.deltaTime > 0f)
            {
                calculatedGhostVelocity = (ghostTransform.position - lastGhostPosition) / Time.deltaTime;
                lastGhostPosition = ghostTransform.position;
            }

            currentOffset = Vector3.Distance(transform.position, ghostTransform.position);

            if (currentOffset > maxOffset)
            {
                ForceDrop();
            }
        }
        else
        {
            currentOffset = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (ghostTransform == null || ghostInteractable == null) return;

        if (ghostInteractable.isSelected && parentRigidbody != null && !wasForceDropped)
        {
            // Smoothly move physics body directly to ghost
            parentRigidbody.MovePosition(ghostTransform.position);

            // Match ghost rotation
            Quaternion targetRotation = ghostTransform.rotation;
            Quaternion nextRotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 20f);
            parentRigidbody.MoveRotation(nextRotation);
        }
    }

    private void ForceDrop()
    {
        wasForceDropped = true;

        if (ghostInteractable != null && ghostInteractable.isSelected)
        {
            if (ghostInteractable.interactorsSelecting.Count > 0)
            {
                IXRSelectInteractor interactor = ghostInteractable.interactorsSelecting[0];

                if (ghostInteractable.interactionManager != null && interactor != null)
                {
                    ghostInteractable.interactionManager.SelectExit(interactor, ghostInteractable);
                }
            }
        }

        // Wait until end of frame to reset transforms
        StartCoroutine(ResetGhostNextFrame());
    }

    private IEnumerator ResetGhostNextFrame()
    {
        // Wait for Unity to finish all XR / Physics updates for this frame
        yield return new WaitForEndOfFrame();

        if (ghostTransform != null && originalParent != null)
        {
            ghostTransform.SetParent(originalParent);
            ghostTransform.localPosition = Vector3.zero;
            ghostTransform.localRotation = Quaternion.identity;
        }

        currentOffset = 0f;
        calculatedGhostVelocity = Vector3.zero;
    }
}