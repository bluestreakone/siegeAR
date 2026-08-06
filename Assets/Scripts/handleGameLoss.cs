using UnityEngine;
using Debug = UnityEngine.Debug;

public class handleGameLoss : MonoBehaviour
{
    public void handleLoss()
    {
        Debug.Log("Challenge FAILED: the castle was destroyed by the cannonball.");
    }
}
