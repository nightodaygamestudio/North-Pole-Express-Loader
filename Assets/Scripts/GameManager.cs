using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI Panels")]
    public GameObject splashPanel;
    public GameObject menuPanel;
    public GameObject gamePanel;
    public GameObject resultsPanel;

    [Header("Game UI Elements (TMP)")]
    public TMP_Text scoreText;
    public TMP_Text highscoreText;
    public TMP_Text roundText;
    public TMP_Text livesText;
    public TMP_Text finalScoreText;  // WICHTIG für Game Over

    [Header("Settings")]
    public int maxLives = 3;
    public float splashDuration = 3.0f;

    public enum GameState { Splash, Menu, Game, Results }
    public GameState CurrentState { get; private set; }

    private int score;
    private int highscore;
    private int round;
    private int currentLives;
    private float timer;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        highscore = PlayerPrefs.GetInt("Highscore", 0);
        UpdateGameUI();
        ChangeState(GameState.Splash);
    }

    void Update()
    {
        if (CurrentState == GameState.Splash)
        {
            timer += Time.deltaTime;
            bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            if (timer >= splashDuration || mouseClicked) ChangeState(GameState.Menu);
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
                if (BackgroundMusicManager.Instance != null) BackgroundMusicManager.Instance.StartMusic();
                break;
            case GameState.Game:
                if (gamePanel) gamePanel.SetActive(true);
                UpdateGameUI();
                break;
            case GameState.Results:
                if (resultsPanel) resultsPanel.SetActive(true);
                // HIER: Den finalen Score in den Text schreiben
                if (finalScoreText) finalScoreText.text = $"Final Score: {score}";
                break;
        }
    }

    public void StartGame()
    {
        score = 0;
        currentLives = maxLives;
        round = 1;
        UpdateGameUI();
        ChangeState(GameState.Game);
    }

    // Wird vom TrainController aufgerufen
    public void OnTrainDeparted(bool wasFull)
    {
        if (CurrentState != GameState.Game) return;

        if (wasFull)
        {
            // Zug voll -> Punkte, KEIN Leben verloren
            score += 100;
            if (score > highscore)
            {
                highscore = score;
                PlayerPrefs.SetInt("Highscore", highscore);
                PlayerPrefs.Save();
            }
            Debug.Log("Zug voll! +100 Punkte");
        }
        else
        {
            // Zug nicht voll -> Leben verlieren
            currentLives--;
            Debug.Log("Zug nicht voll! Leben verloren.");

            if (currentLives <= 0)
            {
                TriggerGameOver();
                return; // Wichtig: Hier abbrechen, damit Runde nicht hochzählt
            }
        }

        // Wenn noch Leben da sind, nächste Runde
        round++;
        UpdateGameUI();
    }

    void UpdateGameUI()
    {
        if (scoreText) scoreText.text = $"Score: {score}";
        if (livesText) livesText.text = $"Lives: {currentLives}";
        if (highscoreText) highscoreText.text = $"Highscore: {highscore}";
        if (roundText) roundText.text = $"Round: {round}";
    }

    public void TriggerGameOver()
    {
        ChangeState(GameState.Results);
    }

    public bool IsGameRunning() => CurrentState == GameState.Game;

    public void OnStartGameButton() => StartGame();
    public void OnRestartButton() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    public void OnQuitButton() => Application.Quit();
}