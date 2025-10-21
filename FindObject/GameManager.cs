using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private int score = 0;
    public int totalObjects = 10; 

    private void Awake()
    {
        
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject); 
    }

    public void AddScore()
    {
        score++;
        Debug.Log($"your score: {score}/{totalObjects}");

        if (score >= totalObjects)
        {
            MissionComplete();
        }
    }

    private void MissionComplete()
    {
        Debug.Log("you good");
        
    }

    public int GetScore() => score;
}
