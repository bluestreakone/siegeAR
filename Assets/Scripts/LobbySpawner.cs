using UnityEngine;
using Unity.Netcode;

public class LobbySpawner : NetworkBehaviour
{
    [Header("PREFABS")]
    [SerializeField] private GameObject gameCorePrefab;

    private bool hasSpawned = false;

    // Called securely from the client's XR Origin via ServerRpc
    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnGameCoreServerRpc(Vector3 spawnPos, Quaternion spawnRot)
    {
        // Only the PC Server should execute this
        if (!IsServer || hasSpawned) return;
        hasSpawned = true;

        Debug.Log("Floor position received from client! Spawning GameCore on server...");

        if (gameCorePrefab != null)
        {
            // instantiate on the PC server
            GameObject instance = Instantiate(gameCorePrefab, spawnPos, spawnRot);

            // replicate and sync across the network to all connected iPhones instantly
            instance.GetComponent<NetworkObject>().Spawn(true);
        }
        else
        {
            Debug.LogError("GameCore Prefab is missing from the LobbySpawner inspector slot!");
        }
    }
}