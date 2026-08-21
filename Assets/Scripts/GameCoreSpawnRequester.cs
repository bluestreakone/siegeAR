using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Netcode;

[RequireComponent(typeof(ARRaycastManager))]
public class GameCoreSpawnRequester : MonoBehaviour
{

    private ARRaycastManager m_RaycastManager;
    private static bool hasSpawnBeenRequested = false;

    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    private LobbySpawner m_LobbySpawner;

    void Awake()
    {
        m_RaycastManager = GetComponent<ARRaycastManager>();

        // find lobbyspawner once when the object loads
        m_LobbySpawner = FindAnyObjectByType<LobbySpawner>();
        
        if (m_LobbySpawner == null)
        {
            Debug.LogError("No LobbySpawner found in the scene!");
        }
    }

    void Update()
    {
        if (hasSpawnBeenRequested) return;
        
        //verify that there is a valid networkmanager
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) return;

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        if (m_RaycastManager.Raycast(screenCenter, s_Hits, TrackableType.PlaneWithinPolygon))
        {
            hasSpawnBeenRequested = true;

            Pose hitPose = s_Hits[0].pose;

            Vector3 directionToPlayer = Camera.main.transform.position - hitPose.position;
            directionToPlayer.y = 0f;
            directionToPlayer.Normalize();

            Quaternion baseRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
            Quaternion finalRotation = baseRotation * Quaternion.Euler(0f, 90f, 0f);

            // Send the position to the PC server's LobbySpawner
            
            if (m_LobbySpawner != null)
            {
                m_LobbySpawner.RequestSpawnGameCoreServerRpc(hitPose.position, finalRotation);
            }
        }
    }
}