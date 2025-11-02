
using Unity.VisualScripting;
using UnityEngine;

public class ManualControl : MonoBehaviour
{
    private Rigidbody rb;
    private float horizontal;
    private float vertical;
    private bool toJump = false;
    public float checkGroundRadius = 0.2f;
    public float checkGroundDist = 0.2f;
    bool isGrounded = true;
    public bool useRandom = true;

    public LayerMask groundLayers;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Physics.gravity = new Vector3(0, -20, 0);
    }

    // Update is called once per frame
    void Update()
    {
        //only need to change these three for future ai control
        if (useRandom)
        {
            horizontal = Random.Range(-1f, 1f);
            vertical = Random.Range(-1f, 1f);
        }
        else
        {
            horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");
        toJump = Input.GetKeyDown(KeyCode.Space);
        }
        
        


        if (toJump && isGrounded)
        {

            rb.linearVelocity += new Vector3(0, 10, 0);

        }

        isGrounded = Physics.SphereCast(transform.position - Vector3.down * 0.2f, checkGroundRadius, Vector3.down, out RaycastHit hit, checkGroundDist, groundLayers);
    }
    void FixedUpdate()
    {
         
        
        Vector3 move = new Vector3(horizontal, 0, vertical).normalized;
        move *= isGrounded ? 7 : 4;
        move.y = rb.linearVelocity.y;


        rb.linearVelocity = move;
        if (horizontal != 0 || vertical != 0)
        {
            Vector3 lookDir = new Vector3(horizontal, 0, vertical).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(lookDir);
            rb.rotation = Quaternion.Slerp(rb.rotation, lookRotation, Time.fixedDeltaTime * 10);
        }else
        {
            rb.angularVelocity = Vector3.zero;
        }


        if (!isGrounded && rb.linearVelocity.magnitude <= 0.1f && (horizontal != 0 || vertical != 0))
        {
            //stuck to wall
            //rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
        
    }
}
