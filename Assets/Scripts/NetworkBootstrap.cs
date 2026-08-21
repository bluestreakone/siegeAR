using Unity.Netcode;
using UnityEngine;

public class NetworkBootstrap : MonoBehaviour
{
    private void Start()
    {
        // Automatically start as a host (both server and client) for local testing
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.StartHost();
        }
    }
}