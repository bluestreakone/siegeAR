using UnityEngine;
using TMPro;

public class handleGameLoss : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The World Space Canvas GameObject floating above the castle.")]
    [SerializeField] private GameObject resultCanvasObject; 
    [Tooltip("The TextMeshPro component inside that canvas.")]
    [SerializeField] private TextMeshProUGUI resultText;     

    void Start()
    {
        // ensure the status text is hidden when the game begins
        if (resultCanvasObject != null)
        {
            resultCanvasObject.SetActive(false);
        }
    }

    // called when the cannonball successfully hits the castle at high speed
    public void HandleLoss()
    {
        Debug.Log("Challenge FAILED: the castle was destroyed by the cannonball.");
        
        if (resultText != null)
        {
            resultText.text = "Challenge FAILED: the wall did not protect the castle.";
            resultText.color = Color.red;
        }

        ShowResultCanvas();
    }

    // Optional: Call this if the castle survives the 10-second impact tracking window
    public void HandleWin()
    {
        Debug.Log("Challenge PASSED: the castle survived!");

        if (resultText != null)
        {
            resultText.text = "Challenge PASSED: the wall protected the castle.";
            resultText.color = Color.green;
        }

        ShowResultCanvas();
    }

    private void ShowResultCanvas()
    {
        if (resultCanvasObject != null)
        {
            resultCanvasObject.SetActive(true);
        }
    }
}