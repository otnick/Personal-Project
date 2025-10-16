using UnityEngine;

public class EndScreenController : MonoBehaviour
{
    public GameObject transitionFish;
    public GameObject fishBones;
    public TMPro.TextMeshProUGUI endText;
    public TMPro.TextMeshProUGUI instructionText;
    public TMPro.TextMeshProUGUI killCountText;
    public TMPro.TextMeshProUGUI timeText;
    public AnimatorScript animatorScript;
    public AudioSource biteSound;
    public GameController gc;

    Vector3 targetPos = new Vector3(11f, 0f, 0f);
    Vector3 bitePos   = new Vector3(-1f, 0f, 0f);

    bool transitioning;
    bool hasEaten;

    void Start()
    {
        gc = FindFirstObjectByType<GameController>();
        if (gc)
        {
            killCountText.text = $"SCORE: {gc.lastRunKills}";
            timeText.text      = $"{gc.lastRunTime:F1}s";
        }
    }

    void Update()
    {
        if (!transitioning && Input.GetKeyDown(KeyCode.Space))
            StartTransition();

        if (transitioning)
            MoveFish();
    }

    void StartTransition()
    {
        transitioning = true;
        endText.gameObject.SetActive(false);
        instructionText.gameObject.SetActive(false);
        timeText.gameObject.SetActive(false);
        killCountText.gameObject.SetActive(false);
    }

    void MoveFish()
    {
        if (!transitionFish) return;

        transitionFish.transform.Translate(Vector3.right * Time.deltaTime * 3f);

        if (!hasEaten &&
            transitionFish.transform.position.x >= bitePos.x - 0.5f &&
            transitionFish.transform.position.x <= bitePos.x + 0.5f)
        {
            EatBones();
        }

        if (transitionFish.transform.position.x >= targetPos.x)
        {
            transitioning = false;
            // hier zentralen Fade benutzen
            GameController.Instance?.ToGame();   // oder ToTitle(), je nach Ziel
        }
    }

    void EatBones()
    {
        hasEaten = true;
        if (animatorScript) animatorScript.PlayEatAnimation();
        if (biteSound) biteSound.Play();
        if (fishBones) fishBones.SetActive(false);
    }
}
