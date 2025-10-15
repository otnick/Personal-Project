using UnityEngine;

public class KillCounter : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI killCountText;
    private int killCount = 0;

    private void Start()
    {
        UpdateKillCountText();
    }

    public void AddKill(int amount = 1)
    {
        killCount += amount;
        UpdateKillCountText();
    }

    public void ResetKillCount()
    {
        killCount = 0;
        UpdateKillCountText();
    }

    private void UpdateKillCountText()
    {
        if (killCountText != null)
        {
            killCountText.text = $"Kills: {killCount}";
        }
    }

    public int GetKillCount()
    {
        return killCount;
    }
}