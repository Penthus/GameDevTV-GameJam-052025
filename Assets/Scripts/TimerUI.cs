using UnityEngine;
using TMPro;

public class TimerUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timerText;    
    [SerializeField] GameObject gameManager;
    float gameTime;
    float timeRemaining;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        float gameTime = FindFirstObjectByType<GameManager>().gameTime;
        timeRemaining = gameTime;
        timerText.text = $"Time Remaining: {timeRemaining:F0}";
    }

    // Update is called once per frame
    void Update()
    {
        timeRemaining -= Time.deltaTime;

        if (timeRemaining < 0)
        {
            timeRemaining = 0;
            
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("Player not found in the scene.");
                return;
            }
            PlayerSize playerSize = player.GetComponent<PlayerSize>();

            gameManager.GetComponent<GameManager>().GetPlayerStats(playerSize);
            gameManager.GetComponent<GameManager>().OnPlayerDeath(timeRemaining);

        }

        timerText.text = $"Time Remaining: {timeRemaining:F0}";
    }

    public float TimeRemaining
    {
        get { return timeRemaining; }
        private set { timeRemaining = value; }
    }

    public float GameTime
    {
        get { return gameTime; }
        private set { gameTime = value; }
    }
}
