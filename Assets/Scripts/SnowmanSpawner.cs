using UnityEngine;
using System.Collections.Generic;

public class SnowmanSpawner : MonoBehaviour
{
    public static SnowmanSpawner Instance;

    public GameObject snowmanPrefab;
    public int maxSnowmen = 7;

    [Header("Spawn Area")]
    public float minX = -30f;
    public float maxX = 30f;
    public float minZ = -13f;
    public float maxZ = 9f;

    [Header("Difficulty")]
    public float initialSpeed = 3f; // Startgeschwindigkeit
    private float currentGlobalSpeed;

    private List<GameObject> activeSnowmen = new List<GameObject>();

    void Awake()
    {
        Instance = this;
        currentGlobalSpeed = initialSpeed;
    }

    void Update()
    {
        // Erster Schneemann spawnt automatisch beim Start, wenn Liste leer
        if (GameManager.Instance.IsGameRunning() && activeSnowmen.Count == 0)
        {
            SpawnSnowman();
        }
    }

    public void SpawnSnowman()
    {
        if (activeSnowmen.Count >= maxSnowmen) return;

        float randomX = Random.Range(minX, maxX);
        float randomZ = Random.Range(minZ, maxZ);
        Vector3 spawnPos = new Vector3(randomX, -2f, randomZ);

        GameObject newSnowman = Instantiate(snowmanPrefab, spawnPos, Quaternion.identity);

        // Speed sofort setzen
        SnowmanController sc = newSnowman.GetComponent<SnowmanController>();
        if (sc != null) sc.SetSpeed(currentGlobalSpeed);

        activeSnowmen.Add(newSnowman);
    }

    // Neue Funktion: Alle schneller machen
    public void IncreaseGlobalSpeed(float amount)
    {
        currentGlobalSpeed += amount;

        // Alle aktiven updaten
        foreach (var snowman in activeSnowmen)
        {
            if (snowman != null)
            {
                SnowmanController sc = snowman.GetComponent<SnowmanController>();
                if (sc != null) sc.SetSpeed(currentGlobalSpeed);
            }
        }
    }

    // Reset Funktion für neues Spiel
    public void ResetSpawner()
    {
        foreach (var snowman in activeSnowmen)
        {
            if (snowman != null) Destroy(snowman);
        }
        activeSnowmen.Clear();
        currentGlobalSpeed = initialSpeed; // Speed zurücksetzen!
    }
}