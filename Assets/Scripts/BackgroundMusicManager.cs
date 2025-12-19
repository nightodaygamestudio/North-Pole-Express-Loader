using UnityEngine;

public class BackgroundMusicManager : MonoBehaviour
{
    public static BackgroundMusicManager Instance;
    private AudioSource audioSource;

    void Awake()
    {
        // Singleton Pattern: Es darf nur EINEN MusicManager geben
        if (Instance == null)
        {
            Instance = this;
            // WICHTIG: Dieses Objekt überlebt den Szenenwechsel!
            DontDestroyOnLoad(gameObject);

            // AudioSource holen oder hinzufügen
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Einstellungen sicherstellen
            audioSource.loop = true; // Endlosschleife
            audioSource.playOnAwake = false; // Nicht sofort starten (erst nach Splash)
        }
        else
        {
            // Wenn es schon einen gibt (z.B. nach Reload), zerstöre den neuen sofort
            Destroy(gameObject);
        }
    }

    public void StartMusic()
    {
        // Nur starten, wenn sie nicht eh schon läuft!
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    // Optional: Falls du die Musik später mal leiser machen willst
    public void SetVolume(float volume)
    {
        audioSource.volume = volume;
    }
}