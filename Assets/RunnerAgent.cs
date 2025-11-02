using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class RunnerAgent : Agent
{
    public Transform chaserTransform;
    private Rigidbody chaserRb;
    private Rigidbody runnerRb;
    void Start()
    {
        chaserRb = chaserTransform.GetComponent<Rigidbody>();
        runnerRb = GetComponent<Rigidbody>();
    }
    

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        GameManager.instance.ResetGame();
    }

    public override void CollectObservations(VectorSensor sensor) //give ai observations
    {
        base.CollectObservations(sensor);

        //Add myself
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(runnerRb.linearVelocity);

        //State chaser
        sensor.AddObservation(chaserTransform.position);
        sensor.AddObservation(chaserRb.linearVelocity);

        //Relative pos
        sensor.AddObservation(transform.localPosition - chaserTransform.localPosition);

        //Time left
        sensor.AddObservation(GameManager.instance.GetTimeLeft());
    }

    public override void OnActionReceived(ActionBuffers actions) //what the ai wants to do
    {
        base.OnActionReceived(actions);
    }
}
