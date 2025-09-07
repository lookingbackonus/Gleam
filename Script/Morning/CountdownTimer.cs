using UnityEngine;
using TMPro;

public class CountdownTimer : MonoBehaviour
{
    public TextMeshProUGUI timerText; // TextMeshPro용 텍스트 컴포넌트
    private float timeRemaining = 60f;
    private bool timerRunning = true;

    void Update()
    {
        if (timerRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                UpdateTimerDisplay();
            }
            else
            {
                timeRemaining = 0;
                timerRunning = false;
                UpdateTimerDisplay();
                OnTimerEnd();
            }
        }
    }

    void UpdateTimerDisplay()
    {
        int seconds = Mathf.CeilToInt(timeRemaining);
        timerText.text = $"Time Remaining: {seconds}s";
    }

    void OnTimerEnd()
    {
        Debug.Log("타이머 종료!");
        // 여기에 타이머 종료 시 동작 추가
    }
}
