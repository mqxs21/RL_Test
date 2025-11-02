using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class ChaseAgent : Agent
{
    [Header("Refs")]
    public Transform runnerTransform;
    private Rigidbody chaserRb;
    private Rigidbody runnerRb;

    [Header("Movement")]
    public float speedGround = 7f;
    public float speedAir = 4f;
    public float jumpVel = 10f;

    [Header("Ground Check")]
    public LayerMask groundLayers;
    public float checkGroundRadius = 0.2f;
    public float checkGroundDist = 0.2f;
    private bool isGrounded = true;

    [Header("Obs Norm")]
    public float arenaHalfSize = 7f; 

    // cached action intents
    private int ax, az;    // -1,0,+1 after mapping
    private bool toJump;

    private float lastDist;
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Runner"))
        {
            GameManager.instance.EndGame(EndGameReason.RunnerCaught);
        }
    }
    void Start()
    {
        chaserRb = GetComponent<Rigidbody>();
        chaserRb.interpolation = RigidbodyInterpolation.Interpolate;
        chaserRb.freezeRotation = true;
        Physics.gravity = new Vector3(0, -20, 0);

        AcquireRunnerRefs(); // find at startup
    }

    void AcquireRunnerRefs()
    {
        var go = GameObject.FindGameObjectWithTag("Runner");
        if (go != null)
        {
            runnerTransform = go.transform;
            runnerRb = go.GetComponent<Rigidbody>();
        }
    }

    public override void Initialize()
{
    // Ensure a DecisionRequester exists and is configured
    var dr = GetComponent<DecisionRequester>();
    if (dr == null) dr = gameObject.AddComponent<DecisionRequester>();
    dr.DecisionPeriod = 1;
    dr.TakeActionsBetweenDecisions = true;
}

    public override void OnEpisodeBegin()
    {
        AcquireRunnerRefs();
        //GameManager.instance.ResetGame();   

        // Clear own physics
        chaserRb.linearVelocity = Vector3.zero;
        chaserRb.angularVelocity = Vector3.zero;
        ax = az = 0; toJump = false;
        lastDist = Vector3.Distance(transform.position, runnerTransform.position);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Defensive null checks (during respawn frames)
        if (runnerTransform == null || runnerRb == null)
        {
            sensor.AddObservation(Vector3.zero); // self pos (x,z)
            sensor.AddObservation(Vector2.zero); // self vel (x,z)
            sensor.AddObservation(Vector3.zero); // runner pos (x,z)
            sensor.AddObservation(Vector2.zero); // runner vel (x,z)
            sensor.AddObservation(Vector2.zero); // relative (x,z)
            sensor.AddObservation(0f);           // time left (0..1)
            return;
        }

        // Self
        Vector3 selfP = transform.position;
        Vector3 selfV = chaserRb.linearVelocity; selfV.y = 0f;

        // Runner
        Vector3 runP = runnerTransform.position;
        Vector3 runV = runnerRb.linearVelocity; runV.y = 0f;

        // Relative (runner - chaser)
        Vector3 rel = runP - selfP; rel.y = 0f;

        float maxSpd = Mathf.Max(speedGround, speedAir, 0.001f);

        // Normalize & add
        sensor.AddObservation(new Vector2(selfP.x / arenaHalfSize, selfP.z / arenaHalfSize));
        sensor.AddObservation(new Vector2(selfV.x / maxSpd,       selfV.z / maxSpd));

        sensor.AddObservation(new Vector2(runP.x / arenaHalfSize, runP.z / arenaHalfSize));
        sensor.AddObservation(new Vector2(runV.x / maxSpd,        runV.z / maxSpd));

        sensor.AddObservation(new Vector2(rel.x / (2f*arenaHalfSize), rel.z / (2f*arenaHalfSize)));

        // Time left 0..1 (implement GetTimeLeft01 accordingly)
        sensor.AddObservation(GameManager.instance.GetTimeLeft01());

       // Debug.Log("collecting obs");
    }

    // Add near top
[SerializeField] private float distScale = 1f;   // tune 0.05–0.2
[SerializeField] private float distEpsilon = 0.01f; // ignore tiny noise

public override void OnActionReceived(ActionBuffers actions)
{
    // your action mapping...
    int bx = actions.DiscreteActions[0];
    int bz = actions.DiscreteActions[1];
    int bj = actions.DiscreteActions[2];
    ax = bx - 1;
    az = bz - 1;
    toJump = (bj == 1);

    if (runnerTransform != null)
    {
        float currDist = Vector3.Distance(transform.position, runnerTransform.position);

        // Distance delta (positive if got closer)
        float dDelta = lastDist - currDist;

            // Normalize by arena size, clamp small jitter
            float normDelta = dDelta;
            // Reward getting closer, penalize getting farther (small weight)
            AddReward(normDelta * distScale);
        

        // Small time pressure (keep tiny if you use distance shaping)
        AddReward(-0.0005f);

        lastDist = currDist;
    }
}


    void Update()
    {
        // Ground check
        isGrounded = Physics.SphereCast(
            transform.position - Vector3.down * 0.2f,
            checkGroundRadius,
            Vector3.down,
            out _,
            checkGroundDist,
            groundLayers);

        // Jump (single-frame intent)
        if (toJump && isGrounded)
        {
            Vector3 v = chaserRb.linearVelocity;
            v.y = jumpVel;
            chaserRb.linearVelocity = v;
        }
        toJump = false; // consume jump intent
    }

    void FixedUpdate()
    {
        // Apply horizontal movement
        Vector3 input = new Vector3(ax, 0f, az).normalized;
        float spd = isGrounded ? speedGround : speedAir;
        Vector3 vel = input * spd;
        vel.y = chaserRb.linearVelocity.y;
        chaserRb.linearVelocity = vel;

        // Face move direction
        if (input.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(input);
            chaserRb.rotation = Quaternion.Slerp(chaserRb.rotation, look, Time.fixedDeltaTime * 10f);
        }
        else
        {
            chaserRb.angularVelocity = Vector3.zero;
        }
    }


    public void OnTagSuccess()
    {
        AddReward(+1f);
        EndEpisode();
    }

    public void OnTimeout()
    {
        AddReward(-1f);
        EndEpisode();
    }
}
