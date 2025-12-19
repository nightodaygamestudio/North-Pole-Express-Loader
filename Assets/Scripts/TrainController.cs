using UnityEngine;
using UnityEngine.UI;

public class TrainController : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 15f;
    public float waitTime = 10f;
    public float spawnX = 60f;
    public float stopX = 0f;
    public float despawnX = -80f;

    [Header("Visuals")]
    public GameObject emptyWagonVisual;
    public GameObject fullWagonVisual;
    public Slider timerSlider;
    public Text counterText;

    private bool isWaiting = false;
    private bool isLeaving = false;
    private float currentWaitTimer;
    private int currentGifts = 0;
    private int requiredGifts = 10;

    // Wir merken uns beim Abfahren, ob wir voll waren, für die Auswertung später
    private bool wasFullWhenDeparted = false;

    void Start()
    {
        ResetTrain(true); // true = Initialer Reset
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameRunning()) return;

        // 1. Einfahren
        if (!isWaiting && !isLeaving)
        {
            Vector3 targetPos = new Vector3(stopX, transform.position.y, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

            if (Mathf.Abs(transform.position.x - stopX) < 0.1f) StartWaiting();
        }
        // 2. Warten
        else if (isWaiting)
        {
            if (currentGifts >= requiredGifts && !fullWagonVisual.activeSelf) SetWagonVisuals(true);

            currentWaitTimer -= Time.deltaTime;
            if (timerSlider) timerSlider.value = currentWaitTimer / waitTime;

            if (currentWaitTimer <= 0 || (currentGifts >= requiredGifts && currentWaitTimer < (waitTime - 2f)))
            {
                Depart();
            }
        }
        // 3. Abfahren
        else if (isLeaving)
        {
            transform.Translate(Vector3.left * speed * Time.deltaTime);

            // Wenn der Zug ganz links angekommen ist (-80)
            if (transform.position.x < despawnX)
            {
                // HIER: Schneemann spawnen!
                if (SnowmanSpawner.Instance != null)
                {
                    SnowmanSpawner.Instance.SpawnSnowman();
                }

                // HIER: Auswertung an GameManager senden (Score oder Leben abziehen)
                // Wir machen das erst, wenn der Zug weg ist, damit es fair wirkt
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
        requiredGifts = Random.Range(3, 8);
        UpdateUI();
    }

    void Depart()
    {
        isWaiting = false;
        isLeaving = true;
        // Speichern ob wir erfolgreich waren
        wasFullWhenDeparted = (currentGifts >= requiredGifts);
    }

    void ResetTrain(bool initial)
    {
        transform.position = new Vector3(spawnX, transform.position.y, transform.position.z);
        isLeaving = false;
        isWaiting = false;
        SetWagonVisuals(false);

        // Wenn das Spiel komplett neu startet (Initial), müssen wir Schneemänner resetten
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
        if (counterText) counterText.text = $"{currentGifts} / {requiredGifts}";
    }

    void OnTriggerEnter(Collider other)
    {
        if (isWaiting && other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                int gifts = player.ClearStack();
                if (gifts > 0)
                {
                    currentGifts += gifts;
                    UpdateUI();
                    if (currentGifts >= requiredGifts) SetWagonVisuals(true);
                }
            }
        }
    }
}