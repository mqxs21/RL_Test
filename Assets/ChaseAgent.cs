using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System;

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
    if (collision.gameObject.CompareTag("Wall"))
    {
        AddReward(-0.02f); // slightly stronger vs -0.01 if you need stronger discouragement
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

    // Clear own physics
    chaserRb.linearVelocity = Vector3.zero;
    chaserRb.angularVelocity = Vector3.zero;
    ax = az = 0; toJump = false;

    if (runnerTransform != null)
    {
        lastRel = runnerTransform.position - transform.position;
        lastRel.y = 0f;
        lastDist = lastRel.magnitude;
    }
    else
    {
        lastRel = Vector3.zero;
        lastDist = 0f;
    }
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

       Debug.Log("collecting obs");
    }

    // Add near top
[SerializeField] private float distScale = 0.08f;   // tune: 0.03..0.2
[SerializeField] private float distEpsilon = 0.01f; // ignore tiny noise
[SerializeField] private float maxStepReward = 0.2f; // clamp per-step reward
[SerializeField] private float timePenalty = -0.0005f;
private Vector3 lastRel;   // last relative vector runner - chaser (x,z)


public override void OnActionReceived(ActionBuffers actions)
{
    // decode actions
    int bx = actions.DiscreteActions[0];
    int bz = actions.DiscreteActions[1];
    int bj = actions.DiscreteActions[2];
    ax = bx - 1;
    az = bz - 1;
    toJump = (bj == 1);

    if (runnerTransform == null) return;

    // compute current relative vector (runner - chaser) in XZ plane
    Vector3 rel = runnerTransform.position - transform.position;
    rel.y = 0f;
    float currDist = rel.magnitude;

    // if either distance is tiny / identical -> skip
    if (currDist < 1e-6f || lastDist < 1e-6f)
    {
        lastRel = rel;
        lastDist = currDist;
        return;
    }

    // We want the *component* of movement towards the runner:
    // Compute how agent's relative vector changed along the previous direction.
    Vector3 prevDir = lastRel.normalized; // direction from chaser -> runner at last step
    // change in distance along that direction:
    // positive dproj means we got *closer* along the ray
    float prevProj = Vector3.Dot(lastRel, prevDir); // == lastDist
    float currProj = Vector3.Dot(rel, prevDir);     // projection of current rel onto previous direction
    float dproj = prevProj - currProj; // positive means moved toward runner along original direction

    // fallback: if numeric weirdness occurs, use scalar delta
    if (float.IsNaN(dproj))
    {
        dproj = lastDist - currDist;
    }

    // ignore tiny noise
    if (Mathf.Abs(dproj) > distEpsilon)
    {
        float shaped = dproj * distScale;
        // clamp step reward so it doesn't explode from single steps
        shaped = Mathf.Clamp(shaped, -maxStepReward, maxStepReward);
        AddReward(shaped);
    }

    // tiny time penalty to encourage faster solves
    AddReward(timePenalty);
    
    // update memory
    lastRel = rel;
    lastDist = currDist;
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
