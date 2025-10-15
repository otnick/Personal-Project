using UnityEngine;

public class Timer : MonoBehaviour
{
    public TMPro.TMP_Text timerText;
    private float startTime;
    void Start()
    {
        startTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        float elapsed = Time.time - startTime;
        timerText.text = $"{elapsed:F2} s";
    }

    void StopTimer()
    {
        
    }

}
