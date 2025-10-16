 using UnityEngine;
using UnityEngine.SceneManagement;
public class EndScreenController : MonoBehaviour
{
    public GameObject transitionFish;
    public GameObject fishBones;
    public TMPro.TextMeshProUGUI endText;
    public TMPro.TextMeshProUGUI instructionText;
    public AnimatorScript animatorScript;
    public AudioSource biteSound;
    private Vector3 targetPosition = new Vector3(11f, 0f, 0f);
    private Vector3 animationStartPosition = new Vector3(-1f, 0f, 0f);
    private bool transitioning = false;
    private bool hasEaten = false;
    public CanvasGroup fadeGroup;
    public float diveDuration = 0.8f;
    public float fadeDelay = 0.2f;
    public float fadeDuration = 3f;

    void Start()
    {

    }
    public void StartTransition()
    {
        transitioning = true;
        endText.gameObject.SetActive(false);
        instructionText.gameObject.SetActive(false);
    }
    void Update()
    {
        bool spaceIsPressed = Input.GetKeyDown(KeyCode.Space);
        if (spaceIsPressed)
        {
            StartTransition();
        }

        if (transitioning)
        {
            MoveTransitionFish();
        }
    }

    void MoveTransitionFish()
    {
        if (transitionFish.transform.position.x >= targetPosition.x)
        {
            transitioning = false;
            return;
        }
        else if (transitionFish.transform.position.x >= animationStartPosition.x - 1f &&
                 transitionFish.transform.position.x < animationStartPosition.x && !hasEaten)
        {
            EatFishBones();
        }

        transitionFish.transform.Translate(Vector3.right * Time.deltaTime * 3f);
    }

    void EatFishBones()

    {
        animatorScript.PlayEatAnimation();
        fishBones.SetActive(false);
        hasEaten = true;
        if (biteSound) biteSound.Play();
        StartCoroutine(FadeTransition());
    }

    System.Collections.IEnumerator FadeTransition()
    {
        float t = 0f;
        float fadeT = 0f;

        float invDive = 1f / Mathf.Max(0.0001f, diveDuration);
        float invFade = 1f / Mathf.Max(0.0001f, fadeDuration);
        float startDelayNorm = fadeDelay * invDive;

        if (fadeDelay > 0f)
            yield return new WaitForSecondsRealtime(fadeDelay);

        while (t < 1f || (fadeGroup && fadeGroup.alpha < 1f))
        {
            float dt = Time.unscaledDeltaTime;

            t += dt * invDive;

            if (fadeGroup)
            {
                if (t >= startDelayNorm)
                    fadeT += dt * invFade;

                fadeGroup.alpha = Mathf.Clamp01(fadeT);
            }

            yield return null;
        }

        if (fadeGroup) fadeGroup.alpha = 1f;

        UnityEngine.SceneManagement.SceneManager.LoadScene("Riff");
    }
}
