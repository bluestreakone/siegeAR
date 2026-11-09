using UnityEngine;
using Unity.Netcode;

public class StartGameManager : MonoBehaviour
{
    [SerializeField] private GameObject gameManagerPrefab;

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += SpawnGameManager;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= SpawnGameManager;
        }
    }

    private void SpawnGameManager()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            GameObject gmInstance = Instantiate(gameManagerPrefab);
            gmInstance.GetComponent<NetworkObject>().Spawn();
        }
    }
}
