using UnityEngine;

public class Runner : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Chaser"))
        {
            GameManager.instance.EndGame(EndGameReason.RunnerCaught);
        }
    }
}
