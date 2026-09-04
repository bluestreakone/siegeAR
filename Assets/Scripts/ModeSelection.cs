using UnityEngine;

public class ModeSelection : MonoBehaviour
{
    // static property keeps the value accessible anywhere in memory
    public static int SelectedDurationSeconds { get; private set; } = 60;
    public static ModeSelection Instance { get; private set; }


    private void Awake()
    {
        Instance = this;
        // Ensures the UI starts hidden when the scene boots
        gameObject.SetActive(false); 
    }

    public static void ShowPanel()
    {
        if (Instance != null)
        {
            Instance.gameObject.SetActive(true);
        }
    }

    public void SelectDuration(int seconds)
    {
        SelectedDurationSeconds = seconds;
        Debug.Log($"Test duration selected: {seconds} seconds.");

        gameObject.SetActive(false);

        // Notify GameCore to start the countdown timer now that time is set
        if (ObjectManager.Instance != null)
        {
            ObjectManager.Instance.OnDurationSelected(seconds);
        }
    }
}