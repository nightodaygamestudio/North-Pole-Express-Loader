using UnityEngine;
using System.Collections.Generic;

public class SnowmanSpawner : MonoBehaviour
{
    public static SnowmanSpawner Instance; // Singleton für einfachen Zugriff

    public GameObject snowmanPrefab;
    public int maxSnowmen = 7;

    [Header("Spawn Area")]
    public float minX = -30f;
    public float maxX = 30f;
    public float minZ = -13f;
    public float maxZ = 9f;

    private List<GameObject> activeSnowmen = new List<GameObject>();

    void Awake()
    {
        Instance = this;
    }

    // Wird vom GameManager beim Start aufgerufen (manuell hinzufügen im GameManager StartGame wäre sauberer, aber so geht's automatisch)
    void Update()
    {
        // Kleiner Hack: Wenn das Spiel startet und wir haben 0 Schneemänner, spawn den ersten.
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

        // Startposition unter der Erde (Y = -2)
        Vector3 spawnPos = new Vector3(randomX, -2f, randomZ);

        GameObject newSnowman = Instantiate(snowmanPrefab, spawnPos, Quaternion.identity);
        activeSnowmen.Add(newSnowman);
    }

    public void ResetSnowmen()
    {
        foreach (var snowman in activeSnowmen)
        {
            Destroy(snowman);
        }
        activeSnowmen.Clear();
        // Der erste spawnt dann wieder automatisch im Update
    }
}