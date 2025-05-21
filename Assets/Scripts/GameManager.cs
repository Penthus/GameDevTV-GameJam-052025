using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public float gameTime = 30f; // change back to 3 minutes after testing

    public GameObject mainMenuPanel;
    public GameObject gameUIPanel;
    public GameObject gameOverPanel;
    public TMPro.TextMeshProUGUI deathOrTimedOutMessageText;
    public TMPro.TextMeshProUGUI gameOverText;
    public LeaderboardUI leaderboardUI;

    public Camera menuCamera;

    public GameObject playerPrefab;

    public int enemyCount = 5;
    public GameObject[] enemyPrefabs;

    public float minXRange = -10f;
    public float maxXRange = 10f;
    public float minYRange = -5f;
    public float maxYRange = 5f;

    public float minPlayerSpawnExclusionDistance = .5f;

    public int playerDeathSize = 1;
    public int playerDeathRank = 100;



    void Start()
    {
        ShowMainMenu();
    }

    public void StartGame()
    {
        mainMenuPanel.SetActive(false);
        gameUIPanel.SetActive(true);
        gameOverPanel.SetActive(false);

        menuCamera.gameObject.SetActive(false);

        ResetGameState(); // Reset game state before starting the game
    }

    private void ResetGameState()
    {
        List<Vector3> enemyPositions = new List<Vector3>();
        for (int i = 0; i < enemyCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, enemyPrefabs.Length);
            GameObject enemyPrefab = enemyPrefabs[randomIndex];
            Vector3 spawnPosition = new Vector3(UnityEngine.Random.Range(minXRange, maxXRange), UnityEngine.Random.Range(minYRange, maxYRange), 0f);
            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            enemyPositions.Add(spawnPosition);

            var playerSize = enemy.GetComponent<PlayerSize>();
            if (playerSize != null)
            {
                playerSize.playerName = i + 1; // Assign a unique name to each enemy
            }
            else
            {
                Debug.LogError("PlayerSize component not found on enemy prefab.");
            }
        }

        Vector3 playerSpawn = Vector3.zero;
        bool found = false;
        int maxAttempts = 100;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {

            Vector3 potentialSpawnLocation = new Vector3(
                UnityEngine.Random.Range(minXRange, maxXRange),
                UnityEngine.Random.Range(minYRange, maxYRange), 0f);

            bool tooClose = false;

            foreach (Vector3 enemyPosition in enemyPositions)
            {
                if (Vector3.Distance(playerSpawn, enemyPosition) < minPlayerSpawnExclusionDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            if (!tooClose)
            {
                playerSpawn = potentialSpawnLocation;
                found = true;
                break;
            }
            if (!found)
            {
                Debug.LogWarning("Could not find a suitable spawn location for the player after "
                    + maxAttempts + " attempts.");
                playerSpawn = Vector3.zero;
            }
        }
        Instantiate(playerPrefab, playerSpawn, Quaternion.identity);

        // Reset leaderboard UI
        leaderboardUI.BuildLeaderboard();
    }

    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        gameUIPanel.SetActive(false);
        gameOverPanel.SetActive(false);

        menuCamera.gameObject.SetActive(true);
    }

    public void OnPlayerDeath(float timeRemaining)
    {
        gameUIPanel.SetActive(false);
        gameOverPanel.SetActive(true);

        if (timeRemaining > 0)
        {
            deathOrTimedOutMessageText.text = "You have been eaten!";
        }
        else
        {
            deathOrTimedOutMessageText.text = "Time's up!";
        }
        int rank = playerDeathRank;
        int size = playerDeathSize;
        gameOverText.text = $"Game Over! You survived for {gameTime - timeRemaining:F0} seconds.\n You were ranked {rank} and had a size of {size}";

        menuCamera.gameObject.SetActive(true);
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void GetPlayerStats(PlayerSize playerSize)
    {
        float timeRemaining = FindFirstObjectByType<TimerUI>().TimeRemaining;
        float gameTime = FindFirstObjectByType<TimerUI>().GameTime;
        playerDeathSize = playerSize.playerSize;
        playerDeathRank = leaderboardUI.GetRank();
        FindFirstObjectByType<GameManager>().OnPlayerDeath(timeRemaining);
    }
}
