using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    
    [Header("Movement")]
    private float moveSpeed;
    public float sprintSpeed;
    public float walkSpeed;

    public float groundDrag;


    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    [Header("Keybinds")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    public Transform orientation;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;

    [Header("Functional Options")]
    [SerializeField] private bool useFootsteps = true;

    [Header("Footstep Parameters")]
    [SerializeField] private float baseStepSpeed = 0.5f;
    [SerializeField] private float crouchStepMultiplier = 1.5f;
    [SerializeField] private float sprintStepMultiplier = 0.6f;
    [SerializeField] private AudioSource footstepAudioSource = default;
    [SerializeField] private AudioClip[] walkCrouchClips = default;
    [SerializeField] private AudioClip[] sprintClips = default;
    // private float footstepTimer = 0;
    private float GetCurrentOffset => state == MovementState.crouching ? baseStepSpeed * crouchStepMultiplier : state == MovementState.sprinting ? baseStepSpeed * sprintStepMultiplier : baseStepSpeed;

    [Header("Noise Settings")]
    [SerializeField] private float walkNoiseRadius = 5f;
    [SerializeField] private float sprintNoiseRadius = 10f;
    [SerializeField] private float crouchNoiseRadius = 2f;
    [SerializeField] private float noiseCooldown = 0.5f;


    [Header("Collision Noise")]
    [SerializeField] private float collisionNoiseRadius = 10f;
    [SerializeField] private float collisionNoiseCooldown = 1f;
    [SerializeField] private float minCollisionForce = 2f;
    [SerializeField] private LayerMask whatIsFloor; // Layers that can trigger collision noise

    private float noiseTimer = 0f;
    private float collisionNoiseTimer = 0f;

    public MovementState state;
    public enum MovementState
    {
        walking,
        sprinting,
        air,
        crouching
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        startYScale = transform.localScale.y;
    }

    private void Update()
    {

        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, whatIsGround);

        MyInput();
        SpeedControl();
        StateHandler();
        

        if (collisionNoiseTimer > 0f)
        {
            collisionNoiseTimer -= Time.deltaTime;
        }

        if(grounded)
            rb.linearDamping = groundDrag;
        else
            rb.linearDamping = 0;

        if (useFootsteps)
        {
            HandleFootstepNoise();
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");


        if(Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }


        if(Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }
    }


    private void StateHandler()
    {
        if (Input.GetKey(crouchKey))
        {
            state = MovementState.crouching;
            moveSpeed = crouchSpeed;
        }

        // Mode - Sprinting
        if(grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        }
        // Mode - Walking
        else if(grounded)
        {
            state = MovementState.walking;
            moveSpeed = walkSpeed;
        }
        // Mode - Air
        else
        {
            state = MovementState.air;
        }
    }

    private void MovePlayer()
    {
        // calc direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
    }


    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if(flatVel.magnitude > moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }


    private void HandleFootstepNoise()
    {
        bool isMoving = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude > 0.1f;

        if (!grounded || !isMoving)
        {
            noiseTimer = 0f;
            return;
        }

        noiseTimer -= Time.deltaTime;

        if (noiseTimer <= 0f)
        {
            // Tom's code for noise detection
           float radius = state switch
            {
                MovementState.walking => walkNoiseRadius,
                MovementState.sprinting => sprintNoiseRadius,
                MovementState.crouching => crouchNoiseRadius,
                _ => 0f
            };

            NoiseSystem.MakeNoise(transform.position, radius);
            noiseTimer = GetCurrentOffset;

            // Sam's code for playing noises
            if (state == MovementState.sprinting){
                footstepAudioSource.PlayOneShot(sprintClips[Random.Range(0, sprintClips.Length - 1)]);
            }
            else
            {
                footstepAudioSource.PlayOneShot(walkCrouchClips[Random.Range(0, walkCrouchClips.Length - 1)]);
            }
        }



    }

    private void OnCollisionEnter(Collision collision)
    {
        if ((whatIsFloor.value & (1 << collision.gameObject.layer)) != 0) return;
        if (collision.relativeVelocity.magnitude < minCollisionForce) return;
        if (collisionNoiseTimer > 0f) return;
        if (state == MovementState.crouching) return; // no sound if crouch

        float forceMultiplier = Mathf.Clamp(collision.relativeVelocity.magnitude / 10f, 0.8f, 2f); 
        float finalRadius = collisionNoiseRadius * forceMultiplier;

        NoiseSystem.MakeNoise(transform.position, finalRadius);
        collisionNoiseTimer = collisionNoiseCooldown;
    }

}
