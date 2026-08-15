using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARImageContentManager : MonoBehaviour
{
    [SerializeField]
    private ARTrackedImageManager m_TrackedImageManager;

    [SerializeField]
    private GameObject gameCorePrefab; // assign GameCore prefab here

    private GameObject spawnedContent;

    void OnEnable() => m_TrackedImageManager.trackedImagesChanged += OnChanged;
    void OnDisable() => m_TrackedImageManager.trackedImagesChanged -= OnChanged;

    void OnChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // handle when a new image is detected for the first time
        foreach (var newImage in eventArgs.added)
        {
            if (spawnedContent == null)
            {
                // instantiate the game right at the detected image
                spawnedContent = Instantiate(gameCorePrefab, newImage.transform);
            }
        }
    }
}
