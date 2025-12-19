using UnityEngine;
using TMPro;

/// <summary>
/// Creates a gentle pulsing effect by modifying the font size of a TextMeshPro object over time.
/// Used to draw attention to UI elements.
/// </summary>
public class PulsingText : MonoBehaviour
{
    [Header("Settings")]
    public float minSize = 80f;     // Minimum font size
    public float maxSize = 110f;    // Maximum font size
    public float speed = 5f;        // Speed of the pulse wave

    private TMP_Text textMesh;

    private void Start()
    {
        // Automatically grab the TMP component from this GameObject
        textMesh = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (textMesh == null) return;

        // Generate a value that oscillates smoothly between 0 and 1 using Sine
        // Mathf.Sin returns -1 to 1; we shift and scale it to 0 to 1
        float wave = (Mathf.Sin(Time.time * speed) + 1f) / 2f;

        // Interpolate between min and max size based on the wave value
        textMesh.fontSize = Mathf.Lerp(minSize, maxSize, wave);
    }
}