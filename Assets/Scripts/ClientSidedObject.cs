using UnityEngine;

public class ClientSidedObject : MonoBehaviour
{
    private void Awake()
    {
        if(!Application.isMobilePlatform)
        {
            gameObject.SetActive(false);
        }
    }
}
