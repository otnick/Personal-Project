using Unity.VisualScripting;
using UnityEngine;

public class Timer : MonoBehaviour
{
    public TMPro.TMP_Text timerText;
    private float startTime;
    private bool running = false;
    public float elapsed = 0f;
    void Start()
    {
        startTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if (running)
        {
            UpdateTimer();
            timerText.text = $"{elapsed:F2} s";
        }   
    }

    public void StopTimer()
    {
        running = false;
    }

    public void StartTimer()
    {
        startTime = Time.time;
        running = true;
    }

    public void ResetTimer()
    {
        startTime = Time.time;
    }

    public void PauseTimer()
    {
        running = false;
    }

    public void ResumeTimer()
    {
        startTime = Time.time - elapsed;
        running = true;
    }

    public void UpdateTimer()
    {
        elapsed = Time.time - startTime;
    }
}
