using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    public KillCounter killCounter;
    public Timer gameTimer;
    public PlayerController playerController;

    [Header("Scene Names")]
    public string sceneTitle = "TitleScreen";
    public string sceneGame  = "Riff";
    public string sceneEnd = "EndScreen";
    
    [Header("Fader")]
    public ScreenFader fader;
    public float fadeOutDur = 0.8f;
    public float fadeInDur = 0.4f;
    private bool gameEnded = false;
    public int lastRunKills;
    public float lastRunTime;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // reconect references
        if (!playerController) playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (!killCounter) killCounter = FindFirstObjectByType<KillCounter>(FindObjectsInactive.Include);
        if (!gameTimer) gameTimer = FindFirstObjectByType<Timer>(FindObjectsInactive.Include);

        // scene setup logic
        if (scene.name == sceneGame)
        {
            ResetGame();
            StartGame(); 
        }
        else if (scene.name == sceneTitle)
        {
            ResetGame();
        }
        else if (scene.name == sceneEnd)
        {
            EndGame();
        }
    }

    void Update()
    {
        if (playerController != null && playerController.damageable.isDead && !gameEnded)
        {
            GameOver();
        }
    }
    public void StartGame()
    {
        gameTimer?.StartTimer();
    }

    public void EndGame()
    {
        gameEnded = true;
        gameTimer?.StopTimer();
    }

    public void ResetGame()
    {
        killCounter?.ResetKillCount();
        gameTimer?.ResetTimer();
        gameEnded = false;
    }

    public void GameOver()
    {
        if (gameEnded) return;
        gameEnded = true;
        lastRunKills = killCounter ? killCounter.GetKillCount() : 0;
        lastRunTime  = gameTimer   ? gameTimer.elapsed : 0f;
        StartCoroutine(GameOverDelay());
        return;

        System.Collections.IEnumerator GameOverDelay()
        {
            yield return new WaitForSeconds(3f);
            EndGame();
            TransitionTo(sceneEnd);
        }
    }

    public void ReturnToTitleScreen()
    {
        TransitionTo(sceneTitle);
    }

    public void RestartGame()
    {
        TransitionTo(sceneGame);
    }
    public void TransitionTo(string sceneName)
    {
        StartCoroutine(DoTransition(sceneName));
    }

    System.Collections.IEnumerator DoTransition(string sceneName)
    {
        if (fader) yield return StartCoroutine(fader.FadeOut(fadeOutDur));
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        if (fader) yield return StartCoroutine(fader.FadeIn(fadeInDur));
    }

    // convenience methods
    public void ToGame() => TransitionTo(sceneGame);
    public void ToTitle() => TransitionTo(sceneTitle);
    public void ToEnd() => TransitionTo(sceneEnd);
}
