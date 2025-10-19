using System.Collections;
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
        runnerInstance = Instantiate(runnerPrefab, runnerSpawn.transform.position, Quaternion.identity);
        chaserInstance = Instantiate(chaserPrefab, chaserSpawn.transform.position, Quaternion.identity);
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
            Destroy(chaserInstance);
        }
        StartCoroutine(FinishGame());

        
    }
    IEnumerator FinishGame()
    {
        yield return new WaitForSeconds(1f);
        Destroy(chaserInstance);
        Destroy(runnerInstance);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
