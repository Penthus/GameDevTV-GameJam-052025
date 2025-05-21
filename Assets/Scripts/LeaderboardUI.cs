using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class LeaderboardUI : MonoBehaviour
{

    public TMP_Text entryPrefab; // Prefab for the leaderboard entry
    public Transform entryContainer; // Container for the leaderboard entries

    private List<PlayerEntry> leaderboardEntries = new List<PlayerEntry>();

    public class PlayerEntry
    {
        public int rank;
        public string prefix; // "Player" or "Enemy"
        public string name;
        public int size;
    }

    void Start()
    {
        leaderboardEntries.Clear();
        int playerCount = 1;
        int enemyCount = 1; 

        foreach (var go in GameObject.FindGameObjectsWithTag("Player"))
        {
            var sizeComponent = go.GetComponent<PlayerSize>();
            if (sizeComponent != null)
            {
                leaderboardEntries.Add(new PlayerEntry
                {
                    prefix = "Player",
                    name = sizeComponent.playerName.ToString(),
                    size = sizeComponent.playerSize
                });
                playerCount++;
            }
        }

        foreach (var go in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            var sizeComponent = go.GetComponent<PlayerSize>();
            if (sizeComponent != null)
            {
                leaderboardEntries.Add(new PlayerEntry
                {
                    prefix = "Enemy",
                    name = sizeComponent.playerName.ToString(),
                    size = sizeComponent.playerSize
                });
                enemyCount++;
            }
        }

        AssignRanks(leaderboardEntries);
        PopulateLeaderboard(leaderboardEntries);
    }

    public void PopulateLeaderboard(List<PlayerEntry> players)
    {
        // Clear old entries
        foreach (Transform child in entryContainer)
        {
            if (child != entryPrefab.transform) // Keep the header
                Destroy(child.gameObject);
        }

        // Populate entries
        foreach (var player in players)
        {
            var entry = Instantiate(entryPrefab, entryContainer);
            entry.gameObject.SetActive(true);
            entry.text = $"{player.rank} - {player.size} - {player.prefix} {player.name}";
        }
    }

    public void UpdateLeaderboard(PlayerSize playerSize)
    {
        // Find the entry by name and prefix
        foreach (var entry in leaderboardEntries)
        {
            if (entry.name == playerSize.playerName.ToString())
            {
                entry.size = playerSize.playerSize;
                break;
            }
        }

        AssignRanks(leaderboardEntries);
        PopulateLeaderboard(leaderboardEntries);
    }
    private void AssignRanks(List<PlayerEntry> entries)
    {
        entries.Sort((x, y) => y.size.CompareTo(x.size));
        for (int i = 0; i < entries.Count; i++)
        {
            entries[i].rank = i + 1;
        }
    }
}
