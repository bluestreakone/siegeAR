using Unity.Netcode;
using UnityEngine;

public class ServerBootstrap : MonoBehaviour
{
    private void Start()
    {
        // if the device is a mobile platform, return & avoid server setup steps
        if (Application.isMobilePlatform) return;

        // otherwise, this is running on the laptop (designated as server)
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.StartServer();
            Debug.Log("Dedicated server started on the laptop.");
        }
    }
}
