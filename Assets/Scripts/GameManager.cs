using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // Statische Variable, die den Szenenwechsel überlebt
    private static bool restartDirectly = false;

    [Header("UI Panels")]
    public GameObject splashPanel;
    public GameObject menuPanel;
    public GameObject gamePanel;
    public GameObject resultsPanel;

    [Header("Game UI Elements (HUD)")]
    public TMP_Text roundText;         // Aktuelle Runde (oben im Spiel)
    public TMP_Text highestRoundText;  // Rekord (oben im Spiel)
    public TMP_Text livesText;

    [Header("Results UI (Game Over)")]
    // HIER GEÄNDERT: Getrennte Texte für das Ergebnis-Panel
    public TMP_Text resultsRoundText;       // Z.B. "Round: 5"
    public TMP_Text resultsHighestText;     // Z.B. "Best: 8"

    [Header("Settings")]
    public int maxLives = 3;
    public float splashDuration = 8.0f;

    public enum GameState { Splash, Menu, Game, Results }
    public GameState CurrentState { get; private set; }

    // Runden-Logik
    private int currentRound;
    private int highestRound;
    private int currentLives;
    private float timer;

    // NEU: Pause Variable
    private bool isPaused = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Laden der höchsten Runde
        highestRound = PlayerPrefs.GetInt("HighestRound", 1);
        UpdateGameUI();

        // CHECK: Kommen wir von einem "Restart"?
        if (restartDirectly)
        {
            restartDirectly = false; // Flag zurücksetzen
            StartGame(); // Sofort ins Spiel starten!
        }
        else
        {
            // Normaler Start -> Splash Screen zeigen
            ChangeState(GameState.Splash);
        }
    }

    void Update()
    {
        // 1. Splash Screen Logik
        if (CurrentState == GameState.Splash)
        {
            timer += Time.deltaTime;
            bool inputDetected = false;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) inputDetected = true;
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) inputDetected = true;
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) inputDetected = true;

            if (timer >= splashDuration || inputDetected)
            {
                SkipSplash();
            }
        }

        // 2. Pause-Funktion (P-Taste)
        if (CurrentState == GameState.Game)
        {
            if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        if (isPaused) Time.timeScale = 0f;
        else Time.timeScale = 1f;
    }

    public void SkipSplash()
    {
        if (CurrentState == GameState.Splash)
        {
            ChangeState(GameState.Menu);
        }
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;

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
                if (BackgroundMusicManager.Instance != null)
                    BackgroundMusicManager.Instance.StartMusic();

                // Im Menü schon den Rekord anzeigen
                if (highestRoundText) highestRoundText.text = $"Best Round: {highestRound}";
                break;

            case GameState.Game:
                if (gamePanel) gamePanel.SetActive(true);
                UpdateGameUI();
                break;

            case GameState.Results:
                if (resultsPanel) resultsPanel.SetActive(true);

                // HIER GEÄNDERT: Die beiden neuen Texte separat befüllen
                if (resultsRoundText) resultsRoundText.text = $"Round Reached: {currentRound}";
                if (resultsHighestText) resultsHighestText.text = $"Highest Round: {highestRound}";
                break;
        }
    }

    public void StartGame()
    {
        currentLives = maxLives;
        currentRound = 1;
        isPaused = false;
        Time.timeScale = 1f;

        // WICHTIG: Spawner komplett zurücksetzen (auch Speed)
        if (SnowmanSpawner.Instance != null) SnowmanSpawner.Instance.ResetSpawner();

        UpdateGameUI();
        ChangeState(GameState.Game);
    }

    public void OnTrainDeparted(bool wasFull)
    {
        if (CurrentState != GameState.Game) return;

        if (wasFull)
        {
            currentRound++;
            if (currentRound > highestRound)
            {
                highestRound = currentRound;
                PlayerPrefs.SetInt("HighestRound", highestRound);
                PlayerPrefs.Save();
            }

            if (SnowmanSpawner.Instance != null)
            {
                if (currentRound <= 7) SnowmanSpawner.Instance.SpawnSnowman();
                else SnowmanSpawner.Instance.IncreaseGlobalSpeed(1.0f);
            }
        }
        else
        {
            currentLives--;
            if (currentLives <= 0)
            {
                TriggerGameOver();
                return;
            }
        }
        UpdateGameUI();
    }

    void UpdateGameUI()
    {
        if (livesText) livesText.text = $"Lives: {currentLives}";
        if (roundText) roundText.text = $"Round: {currentRound}";
        if (highestRoundText) highestRoundText.text = $"Best: {highestRound}";
    }

    public void TriggerGameOver()
    {
        ChangeState(GameState.Results);
    }

    public bool IsGameRunning() => CurrentState == GameState.Game;

    public void OnStartGameButton() => StartGame();

    public void OnRestartButton()
    {
        restartDirectly = true;
        Time.timeScale = 1f; // Wichtig: Zeit wieder starten falls Pause aktiv war
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnQuitButton() => Application.Quit();
}