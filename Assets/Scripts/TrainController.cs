using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class TrainController : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 15f;
    public float waitTime = 10f;
    public float spawnX = 60f;
    public float stopX = 0f;
    public float despawnX = -80f;
    public int giftsNeededPerRound = 10;

    [Header("Start Delay")]
    // HIER GEÄNDERT: Von 5.0f auf 7.0f erhöht
    public float initialStartDelay = 7.0f; // Nur in Runde 1

    [Header("Collision")]
    public Collider deliveryZoneCollider;

    [Header("Audio")]
    public AudioClip arrivalSound;

    [Header("Visuals")]
    public GameObject emptyWagonVisual;
    public GameObject fullWagonVisual;

    [Header("UI (World Space)")]
    public Slider timerSlider;
    public Slider cargoSlider;
    public TMP_Text cargoText;

    // Zustände
    private bool isWaiting = false;
    private bool isLeaving = false;
    private bool isInStartDelay = false;

    private float currentWaitTimer;
    private float startDelayTimer;

    private int currentGifts = 0;
    private int requiredGifts;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        if (deliveryZoneCollider != null) deliveryZoneCollider.enabled = false;

        // Reset mit "true" für erste Runde (Delay)
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

        // Start Verzögerung (nur Runde 1)
        if (isInStartDelay)
        {
            startDelayTimer -= Time.deltaTime;
            if (startDelayTimer <= 0)
            {
                isInStartDelay = false;
                PlayArrivalSound();
            }
            return;
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
            currentWaitTimer -= Time.deltaTime;
            if (timerSlider) timerSlider.value = currentWaitTimer / waitTime;

            if (currentWaitTimer <= 0)
            {
                Depart();
            }
        }
        // 3. Abfahren
        else if (isLeaving)
        {
            transform.Translate(Vector3.left * speed * Time.deltaTime);

            // Wenn Zug weg ist -> Reset
            if (transform.position.x < despawnX)
            {
                // GameManager kümmert sich um Schneemänner und Runden, wir resetten nur den Zug
                ResetTrain(false);
            }
        }
    }

    void StartWaiting()
    {
        isWaiting = true;
        currentWaitTimer = waitTime;

        currentGifts = 0;
        requiredGifts = giftsNeededPerRound;

        if (deliveryZoneCollider != null) deliveryZoneCollider.enabled = true;
        UpdateUI();
    }

    void Depart()
    {
        isWaiting = false;
        isLeaving = true;

        if (deliveryZoneCollider != null) deliveryZoneCollider.enabled = false;

        // Abrechnung
        bool isFull = (currentGifts >= requiredGifts);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTrainDeparted(isFull);
        }
    }

    void ResetTrain(bool initial)
    {
        transform.position = new Vector3(spawnX, transform.position.y, transform.position.z);
        isLeaving = false;
        isWaiting = false;

        if (deliveryZoneCollider != null) deliveryZoneCollider.enabled = false;
        SetWagonVisuals(false);

        if (initial)
        {
            // Runde 1: Delay
            isInStartDelay = true;
            startDelayTimer = initialStartDelay;

            currentGifts = 0;
            requiredGifts = giftsNeededPerRound;
            UpdateUI();

            // Spawner Reset
            if (SnowmanSpawner.Instance != null) SnowmanSpawner.Instance.ResetSpawner();
        }
        else
        {
            // Folgerunden: Sofort Sound
            PlayArrivalSound();
        }
    }

    void PlayArrivalSound()
    {
        if (arrivalSound != null)
        {
            audioSource.clip = arrivalSound;
            audioSource.Play();
        }
    }

    void SetWagonVisuals(bool isFull)
    {
        if (emptyWagonVisual) emptyWagonVisual.SetActive(!isFull);
        if (fullWagonVisual) fullWagonVisual.SetActive(isFull);
    }

    void UpdateUI()
    {
        if (cargoText != null)
            cargoText.text = $"{currentGifts} / {requiredGifts}";

        if (cargoSlider != null)
        {
            if (requiredGifts > 0)
                cargoSlider.value = (float)currentGifts / (float)requiredGifts;
            else
                cargoSlider.value = 1f;
        }
    }

    public void AttemptDelivery(PlayerController player)
    {
        if (isWaiting && player != null)
        {
            int missing = requiredGifts - currentGifts;
            if (missing > 0)
            {
                int taken = player.GiveGifts(missing);
                if (taken > 0)
                {
                    currentGifts += taken;
                    UpdateUI();
                    if (currentGifts >= requiredGifts)
                    {
                        SetWagonVisuals(true);
                    }
                }
            }
        }
    }
}