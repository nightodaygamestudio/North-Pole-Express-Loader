using UnityEngine;
using UnityEngine.UI; // Für Slider
using TMPro;          // WICHTIG: Für TextMeshPro

[RequireComponent(typeof(AudioSource))]
public class TrainController : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 15f;
    public float waitTime = 10f;
    public float spawnX = 60f;
    public float stopX = 0f;
    public float despawnX = -80f;

    // NEU: Festgelegte Anzahl (Standard 10)
    public int giftsNeededPerRound = 10;

    [Header("Audio")]
    public AudioClip arrivalSound;

    [Header("Visuals")]
    public GameObject emptyWagonVisual;
    public GameObject fullWagonVisual;

    [Header("UI (World Space)")]
    public Slider timerSlider;   // Der existierende Timer-Slider (wenn du ihn behalten willst)

    // NEU: Slider und Text für die Pakete
    public Slider cargoSlider;
    public TMP_Text cargoText;       // "0 / 10"

    private bool isWaiting = false;
    private bool isLeaving = false;
    private float currentWaitTimer;
    private int currentGifts = 0;
    private int requiredGifts;
    private bool wasFullWhenDeparted = false;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        ResetTrain(true);
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameRunning())
        {
            if (audioSource.isPlaying) audioSource.Pause();
            return;
        }
        else
        {
            audioSource.UnPause();
        }

        // 1. Einfahren
        if (!isWaiting && !isLeaving)
        {
            Vector3 targetPos = new Vector3(stopX, transform.position.y, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

            if (Mathf.Abs(transform.position.x - stopX) < 0.1f)
            {
                StartWaiting();
            }
        }
        // 2. Warten
        else if (isWaiting)
        {
            // Visual Update wenn voll
            if (currentGifts >= requiredGifts && !fullWagonVisual.activeSelf) SetWagonVisuals(true);

            currentWaitTimer -= Time.deltaTime;

            // Timer Slider Logik (optional, falls du den noch nutzt)
            if (timerSlider) timerSlider.value = currentWaitTimer / waitTime;

            // Abfahrtbedingungen: Zeit um ODER (Voll UND kurze Wartezeit vorbei)
            // Wir lassen ihn sofort abfahren, wenn er voll ist? Oder soll er warten? 
            // Dein Code war: Er fährt ab, wenn Zeit um.
            if (currentWaitTimer <= 0)
            {
                Depart();
            }
        }
        // 3. Abfahren
        else if (isLeaving)
        {
            transform.Translate(Vector3.left * speed * Time.deltaTime);

            if (transform.position.x < despawnX)
            {
                if (SnowmanSpawner.Instance != null) SnowmanSpawner.Instance.SpawnSnowman();

                // Hier checken wir, ob er voll war für Punkte/Leben
                GameManager.Instance.OnTrainDeparted(wasFullWhenDeparted);
                ResetTrain(false);
            }
        }
    }

    void StartWaiting()
    {
        isWaiting = true;
        currentWaitTimer = waitTime;

        currentGifts = 0;
        // HIER: Feste Anzahl nehmen statt Random
        requiredGifts = giftsNeededPerRound;

        UpdateUI();
    }

    void Depart()
    {
        isWaiting = false;
        isLeaving = true;
        wasFullWhenDeparted = (currentGifts >= requiredGifts);
    }

    void ResetTrain(bool initial)
    {
        transform.position = new Vector3(spawnX, transform.position.y, transform.position.z);
        isLeaving = false;
        isWaiting = false;
        SetWagonVisuals(false);

        if (arrivalSound != null)
        {
            audioSource.clip = arrivalSound;
            audioSource.Play();
        }

        if (initial && SnowmanSpawner.Instance != null)
        {
            SnowmanSpawner.Instance.ResetSnowmen();
        }
    }

    void SetWagonVisuals(bool isFull)
    {
        if (emptyWagonVisual) emptyWagonVisual.SetActive(!isFull);
        if (fullWagonVisual) fullWagonVisual.SetActive(isFull);
    }

    void UpdateUI()
    {
        // 1. Text aktualisieren
        // Zeigt z.B. "0 / 10" oder "10 / 10" an
        if (cargoText != null)
        {
            cargoText.text = $"{currentGifts} / {requiredGifts}";
        }

        // 2. Slider aktualisieren (Berechnung für 0 bis 1 Range)
        if (cargoSlider != null)
        {
            // Sicherstellen, dass wir nicht durch 0 teilen
            if (requiredGifts > 0)
            {
                // WICHTIG: (float) benutzen, sonst kommt bei Ganzzahlen (5/10) immer 0 raus!
                float percentage = (float)currentGifts / (float)requiredGifts;
                cargoSlider.value = percentage;
            }
            else
            {
                cargoSlider.value = 1f; // Falls 0 benötigt werden, ist er voll
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Nur wenn wir warten und der Spieler kommt
        if (isWaiting && other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // WICHTIG: Berechnen, wie viele noch fehlen
                int missing = requiredGifts - currentGifts;

                if (missing > 0)
                {
                    // Wir nehmen nur so viele vom Spieler, wie fehlen!
                    int taken = player.GiveGifts(missing);

                    currentGifts += taken;
                    UpdateUI();

                    if (currentGifts >= requiredGifts)
                    {
                        SetWagonVisuals(true);
                        // Optional: Wenn voll, sofort Abfahrt-Timer verkürzen?
                        // currentWaitTimer = 1.0f; // z.B. nur noch 1 Sekunde warten
                    }
                }
            }
        }
    }
}