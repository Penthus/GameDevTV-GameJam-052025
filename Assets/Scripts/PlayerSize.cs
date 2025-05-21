using System;
using System.Security.Cryptography;
using UnityEngine;

public class PlayerSize : MonoBehaviour
{
    public int playerName = 1;
    public int playerSize = 1;

    float scoreTimer = 0f;
    float scoreInterval = 5f;

    LeaderboardUI leaderboardUI; 
    void Start()
    {
        // Check leaderboard UI instance for sending updates when size changes
        leaderboardUI =  FindFirstObjectByType<LeaderboardUI>();
        if (leaderboardUI == null)
        {
            Debug.LogError("LeaderboardUI instance not found in the scene.");            
        }

        transform.localScale = new Vector3(playerSize, playerSize, 1f);

        // Set the AI to have a longer score interval
        if (gameObject.CompareTag("Enemy"))
        {
            scoreInterval = 10f;
        }
    }

    void Update()
    {
        scoreTimer += Time.deltaTime;

        if (scoreTimer >= scoreInterval)
        {
            scoreTimer = 0f;
            IncreaseSize();
        }
    }

    public void IncreaseSize(int food = 1)
    {
        playerSize = playerSize + food;
        transform.localScale = new Vector3(playerSize, playerSize, 1f);
        // Update the player's size in the leaderboard UI
        leaderboardUI.UpdateLeaderboard(this);
    }
}
