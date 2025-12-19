using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // WICHTIG: Neues Input System
using UnityEngine.UI;          // WICHTIG: Für UI Text

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI Panels (Drag & Drop)")]
    public GameObject splashPanel;
    public GameObject menuPanel;
    public GameObject gamePanel;
    public GameObject resultsPanel;

    [Header("Game UI Elements")]
    public Text scoreText;       // Oben im Spiel: "Score: 0"
    public Text livesText;       // Oben im Spiel: "Lives: 3"
    public Text finalScoreText;  // Im Game Over Screen: "Final Score: 100"

    [Header("Settings")]
    public int maxLives = 3;
    public float splashDuration = 3.0f;

    // Zustände des Spiels
    public enum GameState { Splash, Menu, Game, Results }
    public GameState CurrentState { get; private set; }

    // Interne Variablen
    private int score;
    private int currentLives;
    private float timer;

    void Awake()
    {
        // Singleton Pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Startet immer mit dem Splash Screen
        ChangeState(GameState.Splash);
    }

    void Update()
    {
        // Splash Screen Logik (Zeit oder Klick)
        if (CurrentState == GameState.Splash)
        {
            timer += Time.deltaTime;

            // Neues Input System: Maus-Klick prüfen
            bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

            if (timer >= splashDuration || mouseClicked)
            {
                ChangeState(GameState.Menu);
            }
        }
    }

    // --- State Machine ---
    public void ChangeState(GameState newState)
    {
        CurrentState = newState;

        // Alle Panels erst mal ausblenden
        if (splashPanel) splashPanel.SetActive(false);
        if (menuPanel) menuPanel.SetActive(false);
        if (gamePanel) gamePanel.SetActive(false);
        if (resultsPanel) resultsPanel.SetActive(false);

        switch (newState)
        {
            case GameState.Splash:
                if (splashPanel) splashPanel.SetActive(true);
                timer = 0f;
                break;

            case GameState.Menu:
                if (menuPanel) menuPanel.SetActive(true);

                // HIER: Musik starten, sobald das Menü erscheint!
                if (BackgroundMusicManager.Instance != null)
                {
                    BackgroundMusicManager.Instance.StartMusic();
                }
                break;

            case GameState.Game:
                if (gamePanel) gamePanel.SetActive(true);
                UpdateGameUI(); // UI sofort aktualisieren
                break;

            case GameState.Results:
                if (resultsPanel) resultsPanel.SetActive(true);
                // Finalen Score anzeigen
                if (finalScoreText) finalScoreText.text = $"Final Score: {score}";
                break;
        }
    }

    // --- Spiel Logik ---

    public void StartGame()
    {
        score = 0;
        currentLives = maxLives;

        UpdateGameUI();
        ChangeState(GameState.Game);
    }

    // Wird vom TrainController aufgerufen, wenn der Zug links verschwindet
    public void OnTrainDeparted(bool wasFull)
    {
        if (CurrentState != GameState.Game) return;

        if (wasFull)
        {
            // Belohnung
            score += 100;
            Debug.Log("Zug voll! +100 Punkte");
        }
        else
        {
            // Strafe
            currentLives--;
            Debug.Log("Zug nicht voll! Leben verloren.");

            if (currentLives <= 0)
            {
                TriggerGameOver();
            }
        }
        UpdateGameUI();
    }

    void UpdateGameUI()
    {
        if (scoreText) scoreText.text = $"Score: {score}";
        if (livesText) livesText.text = $"Lives: {currentLives}";
    }

    public void TriggerGameOver()
    {
        ChangeState(GameState.Results);
    }

    public bool IsGameRunning()
    {
        return CurrentState == GameState.Game;
    }

    // --- Button Funktionen (Im Inspector verknüpfen) ---

    public void OnStartGameButton()
    {
        StartGame();
    }

    public void OnRestartButton()
    {
        // Lädt die komplette Szene neu
        // Der MusicManager bleibt dabei erhalten (da DontDestroyOnLoad)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnQuitButton()
    {
        Application.Quit();
        Debug.Log("Spiel beendet!");
    }
}