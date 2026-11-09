using UnityEngine;
using TMPro;

public class TimerTextBridge : MonoBehaviour
{
    private void Start()
    {
        TMP_Text txt = GetComponent<TMP_Text>();
        if (txt != null && ObjectManager.Instance != null)
        {
            ObjectManager.Instance.RegisterTimer(txt);
        }
    }
}