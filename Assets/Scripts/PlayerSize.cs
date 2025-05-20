using System;
using System.Security.Cryptography;
using UnityEngine;

public class PlayerSize : MonoBehaviour
{
    public int playerNumber = 1;
    public int playerSize = 1;

    float scoreTimer = 0f;
    float scoreInterval = 5f;

    void Start()
    {
        transform.localScale = new Vector3(playerSize, playerSize, 1f);
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
    }
}
