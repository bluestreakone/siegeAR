using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem; // Added New Input System namespace
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

using Debug = UnityEngine.Debug;

[RequireComponent(typeof(XRGrabInteractable))]
public class PlaneClipPrevention : MonoBehaviour
{
    [Header("AR Plane Settings")]
    [Tooltip("Assign the Layer used by your AR Plane Manager / Colliders.")]
    [SerializeField] private LayerMask arPlaneLayer = ~0;

    [Tooltip("Small buffer (in meters) so resting flush against a surface doesn't trigger a false drop.")]
    [SerializeField] private float surfaceBuffer = 0.02f;

    private XRGrabInteractable grabInteractable;
    private Camera mainCamera;
    private float grabDepth;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
            grabInteractable.selectEntered.AddListener(OnGrabbed);
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (mainCamera == null) mainCamera = Camera.main;

        if (mainCamera != null)
        {
            // record distance from camera to the block at grab start (the length of the interacting "arm")
            grabDepth = Vector3.Distance(mainCamera.transform.position, transform.position);
        }
    }

    private void Update()
    {
        if (grabInteractable == null || !grabInteractable.isSelected) return;
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector2 screenPoint = GetCurrentPointerPosition();
        if (screenPoint == Vector2.zero) return;

        Ray screenRay = mainCamera.ScreenPointToRay(screenPoint);

        // raycast at a length of grabDepth
        if (Physics.Raycast(screenRay, out RaycastHit hit, grabDepth, arPlaneLayer))
        {
            // ignore hitting the block's own collider to avoid false positives
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) return;

            // if ray hits a plane at a distance SHORTER than grabDepth,
            // the cursor is pointing through a solid surface and the ray is intersecting a plane (Bad grab)
            if (hit.distance < (grabDepth - surfaceBuffer))
            {
                var interactor = grabInteractable.interactorsSelecting[0];
                if (grabInteractable.interactionManager != null && interactor != null)
                {
                    //disconnect from the object
                    grabInteractable.interactionManager.SelectExit(interactor, grabInteractable);
                    Debug.Log("object grab disconnected to avoid clipping at " + hit.transform);
                }
            }
        }
    }

    private Vector2 GetCurrentPointerPosition()
    {
        if (Pointer.current != null)
        {
            return Pointer.current.position.ReadValue();
        }
        return Vector2.zero;
    }
}