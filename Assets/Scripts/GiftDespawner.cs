using UnityEngine;

public class GiftDespawner : MonoBehaviour
{
    [Header("Settings")]
    public float lifetime = 10f; // Wie lange existiert es insgesamt?
    public float startBlinkingAt = 3f; // Ab wann (Restzeit) soll es blinken?

    [Header("Blink Speed")]
    public float slowBlinkInterval = 0.5f; // Langsames Blinken (Anfang)
    public float fastBlinkInterval = 0.1f; // Schnelles Flackern (Ende)

    private float age;
    private float blinkTimer;
    private Renderer[] renderers; // Alle MeshRenderer (falls das Geschenk aus mehreren Teilen besteht)
    private bool isVisible = true;

    void Start()
    {
        // Wir holen uns alle Renderer (auch von Kind-Objekten), damit das ganze Geschenk blinkt
        renderers = GetComponentsInChildren<Renderer>();
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameRunning()) return;

        age += Time.deltaTime;
        float remainingTime = lifetime - age;

        // 1. Zeit abgelaufen? -> Weg damit
        if (remainingTime <= 0)
        {
            Destroy(gameObject);
            return;
        }

        // 2. Soll es blinken? (Nur in den letzten X Sekunden)
        if (remainingTime <= startBlinkingAt)
        {
            HandleBlinking(remainingTime);
        }
    }

    void HandleBlinking(float remainingTime)
    {
        // Berechne, wie weit wir im Blink-Prozess sind (0 = Start Blinken, 1 = Tot)
        // Wir kehren den Wert um, damit 1.0 = Start (3 sek) und 0.0 = Ende (0 sek) ist, das macht Lerp einfacher
        float progress = remainingTime / startBlinkingAt;

        // Lerp: Wir berechnen das aktuelle Intervall basierend auf der Restzeit.
        // Wenn noch viel Zeit ist (progress 1), nutzen wir slowBlinkInterval.
        // Wenn wenig Zeit ist (progress 0), nutzen wir fastBlinkInterval.
        float currentInterval = Mathf.Lerp(fastBlinkInterval, slowBlinkInterval, progress);

        blinkTimer += Time.deltaTime;

        // Ist es Zeit den Status zu wechseln?
        if (blinkTimer >= currentInterval)
        {
            ToggleVisuals();
            blinkTimer = 0f;
        }
    }

    void ToggleVisuals()
    {
        isVisible = !isVisible;

        // Alle Renderer an/ausschalten
        foreach (var rend in renderers)
        {
            rend.enabled = isVisible;
        }
    }
}