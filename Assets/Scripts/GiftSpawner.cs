using UnityEngine;

public class GiftSpawner : MonoBehaviour
{
    [Header("Geschenke Varianten")]
    public GameObject giftPrefabA; // Erstes Design (z.B. Rot)
    public GameObject giftPrefabB; // Zweites Design (z.B. Blau)

    [Header("Settings")]
    public float spawnInterval = 2.0f;
    public float spawnY = 0.5f;

    [Header("Spawn Area")]
    public float minX = -30f;
    public float maxX = 30f;
    public float minZ = -13f;
    public float maxZ = 9f;

    private float timer;
    private bool spawnNextTypeA = true; // Schalter für Abwechslung

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameRunning()) return;

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnAlternatingGift();
            timer = 0f;
        }
    }

    void SpawnAlternatingGift()
    {
        // Zufallsposition
        float randomX = Random.Range(minX, maxX);
        float randomZ = Random.Range(minZ, maxZ);
        Vector3 spawnPos = new Vector3(randomX, spawnY, randomZ);

        // Welches Prefab?
        GameObject prefabToSpawn = spawnNextTypeA ? giftPrefabA : giftPrefabB;

        if (prefabToSpawn != null)
        {
            Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        }

        // Schalter umlegen für das nächste Mal
        spawnNextTypeA = !spawnNextTypeA;
    }
}