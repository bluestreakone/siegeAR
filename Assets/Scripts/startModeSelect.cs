using UnityEngine;

public class startModeSelect : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        // Ensure the duration selection panel pops up when GameCore spawns
        ModeSelection.ShowPanel();
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
