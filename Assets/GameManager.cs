using System.Collections;
using TMPro;
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

    private GameObject runnerInstance;
    private GameObject chaserInstance;

    public GameObject runnerDeathEffect;
    public GameObject chaserDeathEffect;

    private float timeElapsed = 0f;
    private float timeLimit = 10f;

    public TextMeshProUGUI timeText;
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
        Spawn();
    }

    void Spawn()
    {
        if (runnerInstance != null)
        {
            Destroy(runnerInstance);
        }
        if (chaserInstance != null)
        {
            Destroy(chaserInstance);
        }
        runnerInstance = Instantiate(runnerPrefab, runnerSpawn.transform.position, Quaternion.identity);
        chaserInstance = Instantiate(chaserPrefab, chaserSpawn.transform.position, Quaternion.identity);
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
    }

    public void EndGame(EndGameReason reason)
    {
        if (reason == EndGameReason.RunnerCaught)
        {
            Instantiate(runnerDeathEffect, runnerInstance.transform.position, Quaternion.identity);
            Destroy(runnerInstance);
        }
        else if (reason == EndGameReason.TimeUp)
        {
            Instantiate(chaserDeathEffect, chaserInstance.transform.position, Quaternion.identity);
            Destroy(chaserInstance);
        }
        StartCoroutine(FinishGame());

        
    }
    IEnumerator FinishGame()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
