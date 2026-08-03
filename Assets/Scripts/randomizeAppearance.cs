using UnityEngine;

public class randomizeAppearance : MonoBehaviour
{
    [Header("Brightness Settings")]
    [Tooltip("Minimum brightness multiplier (e.g., 0.8 for slightly darker).")]
    [SerializeField] private float minBrightness = 0.8f;
    
    [Tooltip("Maximum brightness multiplier (e.g., 1.2 for slightly brighter).")]
    [SerializeField] private float maxBrightness = 1.2f;

    void Start()
    {
        ApplyRandomBrightness();
    }

    private void ApplyRandomBrightness()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // .material creates a unique instance for this specific block 
            // so we don't accidentally alter the material asset for all blocks.
            Material mat = rend.material;

            // Generate a random brightness factor within your set range
            float randomFactor = Random.Range(minBrightness, maxBrightness);

            // Automatically check for URP (_BaseColor) or Standard pipeline (_Color)
            string colorProperty = mat.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";

            if (mat.HasProperty(colorProperty))
            {
                Color baseColor = mat.GetColor(colorProperty);

                // Multiply the RGB channels to adjust brightness while keeping your texture intact
                Color randomizedColor = new Color(
                    Mathf.Clamp01(baseColor.r * randomFactor),
                    Mathf.Clamp01(baseColor.g * randomFactor),
                    Mathf.Clamp01(baseColor.b * randomFactor),
                    baseColor.a
                );

                mat.SetColor(colorProperty, randomizedColor);
            }
        }
    }
}