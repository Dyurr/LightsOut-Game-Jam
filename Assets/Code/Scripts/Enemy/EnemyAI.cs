using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    private enum State { Roaming, Investigating, Chasing, Attacking }

    private NavMeshAgent agent;
    private Transform playerTransform;
    private State currentState = State.Roaming;
    private Coroutine mainRoutine;

    public float moveSpeed = 10f;
    public float chaseMultiplier = 1.5f;
    public Collider detectionCollider;
    public Collider killCollider;

    [Header("Vision")]
    public Transform eyePosition;
    public LayerMask obstacleLayers;
    public float eyeHeightOffsetToPlayer = 1f;

    [Header("Noise Response")]
    public float investigateWaitTime = 3f;
    public float investigateMoveSpeedMultiplier = 1.2f;

    [Header("Roaming")]
    public float roamRadius = 15f;
    public float roamMinWaitTime = 1f;
    public float roamMaxWaitTime = 3f;
    public int roamSampleAttempts = 10;

    [Header("Footsteps")]
    [SerializeField] private AudioSource footstepAudioSource;
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private float footstepInterval = 0.45f;
    [SerializeField] private float chaseFootstepInterval = 0.3f;
    [SerializeField] private float footstepVolume = 0.8f;
    [SerializeField] private float minMoveSpeedForFootsteps = 0.15f;

    private float footstepTimer;

    private Vector3 investigateTarget;
    private Animator anim;

    public bool IsRoaming => currentState == State.Roaming;
    public bool IsInvestigating => currentState == State.Investigating;
    public bool IsChasing => currentState == State.Chasing;
    public bool IsAttacking => currentState == State.Attacking;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        if (anim == null)
        {
            Debug.LogWarning($"{name}: No Animator found. Animations will not play.", this);
        }

        if (agent == null)
        {
            Debug.LogError($"{name}: EnemyAI requires a NavMeshAgent component.", this);
        }

        if (footstepAudioSource == null)
        {
            footstepAudioSource = GetComponent<AudioSource>();
        }

        if (footstepAudioSource == null)
        {
            footstepAudioSource = gameObject.AddComponent<AudioSource>();
        }

        footstepAudioSource.playOnAwake = false;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogWarning($"{name}: No GameObject tagged 'Player' was found. EnemyAI will be disabled.", this);
            enabled = false;
            return;
        }

        playerTransform = playerObj.transform;

        if (detectionCollider != null && !detectionCollider.isTrigger)
        {
            Debug.LogWarning($"{name}: detectionCollider should be set to 'Is Trigger'.", this);
        }

        if (killCollider != null && !killCollider.isTrigger)
        {
            Debug.LogWarning($"{name}: killCollider should be set to 'Is Trigger'.", this);
        }
    }

    void OnEnable()
    {
        NoiseSystem.OnNoiseMade += HandleNoise;
    }

    void OnDisable()
    {
        NoiseSystem.OnNoiseMade -= HandleNoise;
    }

    void Start()
    {
        if (agent == null) return;
        mainRoutine = StartCoroutine(StateMachine());
    }

    void Update()
    {
        if (agent == null) return;

        if (currentState == State.Chasing)
            agent.speed = moveSpeed * chaseMultiplier;
        else if (currentState == State.Investigating)
            agent.speed = moveSpeed * investigateMoveSpeedMultiplier;
        else
            agent.speed = moveSpeed;

        UpdateAnimator();
        HandleFootsteps();
    }

    private void UpdateAnimator()
    {
        if (anim == null) return;

        float normalizedSpeed = agent.velocity.magnitude / (moveSpeed * chaseMultiplier);
        anim.SetFloat("Speed", normalizedSpeed);

        anim.speed = agent.velocity.magnitude > 0.1f ? 1f : 0f;
    }

    private void HandleFootsteps()
    {
        if (footstepAudioSource == null) return;
        if (footstepClips == null || footstepClips.Length == 0) return;
        if (currentState == State.Attacking) return;

        bool isMoving = agent.velocity.magnitude > minMoveSpeedForFootsteps &&
                        agent.hasPath &&
                        !agent.pathPending;

        if (!isMoving)
        {
            footstepTimer = 0f;
            return;
        }

        float currentInterval = currentState == State.Chasing
            ? chaseFootstepInterval
            : footstepInterval;

        footstepTimer -= Time.deltaTime;

        if (footstepTimer <= 0f)
        {
            PlayRandomFootstep();
            footstepTimer = currentInterval;
        }
    }

    private void PlayRandomFootstep()
    {
        int index = Random.Range(0, footstepClips.Length);
        AudioClip clip = footstepClips[index];

        if (clip == null) return;

        footstepAudioSource.pitch = Random.Range(0.9f, 1.1f);
        footstepAudioSource.PlayOneShot(clip, footstepVolume);
    }

    private void HandleNoise(Vector3 position, float radius)
    {
        if (currentState == State.Chasing || currentState == State.Attacking) return;

        float distance = Vector3.Distance(transform.position, position);

        if (distance <= radius)
        {
            investigateTarget = position;
            currentState = State.Investigating;
        }
    }

    private IEnumerator StateMachine()
    {
        while (true)
        {
            switch (currentState)
            {
                case State.Roaming:
                    yield return RoamStep();
                    break;

                case State.Investigating:
                    yield return InvestigateStep();
                    break;

                case State.Chasing:
                    yield return ChaseStep();
                    break;

                case State.Attacking:
                    yield return null;
                    break;
            }
        }
    }

    private IEnumerator RoamStep()
    {
        Vector3? roamTarget = FindRandomRoamPoint();

        if (roamTarget == null)
        {
            yield return new WaitForSeconds(0.5f);
            yield break;
        }

        agent.SetDestination(roamTarget.Value);

        while (currentState == State.Roaming &&
               !agent.pathPending &&
               agent.remainingDistance > agent.stoppingDistance)
        {
            yield return null;
        }

        if (currentState != State.Roaming) yield break;

        float waitTime = Random.Range(roamMinWaitTime, roamMaxWaitTime);
        float timer = 0f;

        while (currentState == State.Roaming && timer < waitTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }
    }

    private Vector3? FindRandomRoamPoint()
    {
        for (int i = 0; i < roamSampleAttempts; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * roamRadius;
            Vector3 randomPoint = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, roamRadius, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();

                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    return hit.position;
                }
            }
        }

        return null;
    }

    private IEnumerator InvestigateStep()
    {
        agent.SetDestination(investigateTarget);

        while (currentState == State.Investigating &&
               Vector3.Distance(transform.position, investigateTarget) > agent.stoppingDistance)
        {
            yield return null;
        }

        float timer = 0f;

        while (currentState == State.Investigating && timer < investigateWaitTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (currentState == State.Investigating)
        {
            currentState = State.Roaming;
        }
    }

    private IEnumerator ChaseStep()
    {
        while (currentState == State.Chasing)
        {
            agent.SetDestination(playerTransform.position);
            yield return null;
        }
    }

    public void OnDetectionTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (detectionCollider != null && IsSameCollider(other, detectionCollider) == false)
        {
            // Only react to the collider dedicated to detection.
        }
    }

    public void OnDetectionTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (currentState == State.Attacking) return;

        if (detectionCollider != null)
        {
            if (HasLineOfSight(other))
            {
                currentState = State.Chasing;
            }
        }

        if (killCollider != null && killCollider.bounds.Intersects(other.bounds))
        {
            TryKill();
        }
    }

    private bool HasLineOfSight(Collider playerCollider)
    {
        Vector3 origin = eyePosition != null ? eyePosition.position : transform.position;
        Vector3 targetPoint = playerCollider.bounds.center + Vector3.up * eyeHeightOffsetToPlayer;

        Vector3 direction = targetPoint - origin;
        float distance = direction.magnitude;

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, obstacleLayers))
        {
            return false;
        }

        return true;
    }

    public void OnDetectionTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (currentState == State.Chasing)
        {
            currentState = State.Roaming;
        }
    }

    private bool IsSameCollider(Collider a, Collider b) => a == b;

    private void TryKill()
    {
        if (currentState == State.Attacking) return;

        currentState = State.Attacking;

        if (anim != null)
        {
            anim.SetTrigger("Attack");
        }

        Debug.Log("Player has been killed by the enemy!");

        StartCoroutine(ResetAfterAttack());
    }

    private IEnumerator ResetAfterAttack()
    {
        yield return new WaitForSeconds(1f);
        currentState = State.Roaming;
    }
}