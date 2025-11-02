using System.Collections;
using TMPro;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.SceneManagement;
public enum EndGameReason
{
    RunnerCaught,
    TimeUp
}
public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public GameObject runnerPrefab;
    public GameObject chaserPrefab;

    public GameObject runnerSpawn;
    public GameObject chaserSpawn;

    public GameObject runnerInstance = null;
    public GameObject chaserInstance = null;

    public GameObject runnerDeathEffect;
    public GameObject chaserDeathEffect;

    public float timeElapsed = 0f;
    public float timeLimit = 10f;

    public TextMeshProUGUI timeText;
    public bool gameEnded = false;
    public static int trialCount = 0;
    public TextMeshProUGUI trialText;
    void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        timeElapsed = 0f;
        Spawn();
       ResetGame();
    }

    void Spawn()
    {
        if (runnerInstance == null)
        {
            runnerInstance = Instantiate(runnerPrefab, runnerSpawn.transform.position, Quaternion.identity);
        }
        if (chaserInstance == null)
        {
            chaserInstance = Instantiate(chaserPrefab, chaserSpawn.transform.position, Quaternion.identity);
        }
        
        
        if (chaserInstance.GetComponent<ChaseAgent>() != null)
        {
            chaserInstance.GetComponent<ChaseAgent>().runnerTransform = runnerInstance.transform;
        }
    }

    void Update()
    {

        if (timeElapsed >= timeLimit)
        {
            timeElapsed = timeLimit;
            EndGame(EndGameReason.TimeUp);
        }
        else
        {
            timeElapsed += Time.deltaTime;
        }
        timeText.text = (timeLimit - timeElapsed).ToString("F2") + "s";
        trialText.text = "Trial:" + trialCount.ToString();
    }

    public void EndGame(EndGameReason reason)
    {
        if (gameEnded) return;
        gameEnded = true;
        if (reason == EndGameReason.RunnerCaught)
        {
            Instantiate(runnerDeathEffect, runnerInstance.transform.position, Quaternion.identity);
            chaserInstance.GetComponent<ChaseAgent>().OnTagSuccess();
            Debug.Log("Runner caught");
        }
        else if (reason == EndGameReason.TimeUp)
        {
            Instantiate(chaserDeathEffect, chaserInstance.transform.position, Quaternion.identity);
            chaserInstance.GetComponent<ChaseAgent>().OnTimeout();
            Debug.Log("Time up, runner wins");
        }

        ResetGame();
    }
    public float GetTimeLeft01() => Mathf.Clamp01((timeLimit - timeElapsed) / Mathf.Max(0.0001f, timeLimit));

    public float GetTimeLeft()
    {
        return timeLimit - timeElapsed;
    }
    
    public void ResetGame()
    {
        timeElapsed = 0f;
        gameEnded = false;
        trialCount += 1;
        if (runnerInstance != null)
        {
            runnerInstance.transform.position = runnerSpawn.transform.position;
        }
        if (chaserInstance != null)
        {
            chaserInstance.transform.position = chaserSpawn.transform.position;
        }
    }
}
