using System.Collections.Generic;
//using System.Numerics;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Quaternion = UnityEngine.Quaternion;


[RequireComponent(typeof(ARRaycastManager))]
public class GameCoreFloorSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject gameCorePrefab;

    private ARRaycastManager m_RaycastManager;
    private GameObject spawnedGameCore;

    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    void Awake()
    {
        m_RaycastManager = GetComponent<ARRaycastManager>();
    }

    void Update()
    {
        // only spawn the game core once per session
        if (spawnedGameCore != null) return;

        // target the exact center of the mobile screen
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        // raycast downward into the room to look for a detected horizontal plane (FLOOR))
        if (m_RaycastManager.Raycast(screenCenter, s_Hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = s_Hits[0].pose;

            // calculate direction from the spawn point to the user's camera
            Vector3 directionToPlayer = Camera.main.transform.position - hitPose.position;
            
            // flatten the direction on the Y plane so the game board stays completely level
            directionToPlayer.y = 0f;
            directionToPlayer.Normalize();

            // force a deterministic rotation so the castle/cannon always faces the player natively
            Quaternion consistentRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);

            // apply a 90 degree 3D rotation so that the scene is properly oriented
            Quaternion finalRotation = consistentRotation * Quaternion.Euler(0f, 90f, 0f);

            // instantiate the GameCore prefab cleanly onto the physical floor
            spawnedGameCore = Instantiate(gameCorePrefab, hitPose.position, finalRotation);
        }
    }
}