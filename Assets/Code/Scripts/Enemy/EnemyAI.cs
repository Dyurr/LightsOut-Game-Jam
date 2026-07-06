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
    public Transform eyePosition; // assign to the vision cone's origin, or enemy head
    public LayerMask obstacleLayers; // set this to whatever layers count as "blocks sight" (walls, doors, etc.)
    public float eyeHeightOffsetToPlayer = 1f; // aim roughly at player's chest/head, not their feet

    [Header("Noise Response")]
    public float investigateWaitTime = 3f;
    public float investigateMoveSpeedMultiplier = 1.2f;

    [Header("Roaming")]
    public float roamRadius = 15f;       // how far from current position to search for a new roam point
    public float roamMinWaitTime = 1f;   // pause after arriving before picking a new point
    public float roamMaxWaitTime = 3f;
    public int roamSampleAttempts = 10;  // how many tries to find a valid point before giving up this cycle

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

        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError($"{name}: EnemyAI requires a NavMeshAgent component.", this);
        }

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
    }

    private void UpdateAnimator()
    {
        if (anim == null) return;

        float normalizedSpeed = agent.velocity.magnitude / (moveSpeed * chaseMultiplier);
        anim.SetFloat("Speed", normalizedSpeed);
        anim.speed = agent.velocity.magnitude > 0.1f ? 1f : 0f;
    }

    private void HandleNoise(Vector3 position, float radius)
    {
        // Don't get distracted by noise if already actively chasing or attacking
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
        // Couldn't find a valid point this cycle — wait a moment and try again next loop.
            yield return new WaitForSeconds(0.5f);
            yield break;
        }

        agent.SetDestination(roamTarget.Value);

        // Travel toward the roam point, bail out immediately if state changes (noise, spotted, etc.)
        while (currentState == State.Roaming &&
               !agent.pathPending &&
               agent.remainingDistance > agent.stoppingDistance)
        {
            yield return null;
        }

        if (currentState != State.Roaming) yield break;

        // Arrived — pause briefly like it's looking around, then loop back for a new point
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
            // Random direction and distance within roamRadius, relative to current position
            Vector2 randomCircle = Random.insideUnitCircle * roamRadius;
            Vector3 randomPoint = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, roamRadius, NavMesh.AllAreas))
            {
                // Optional: confirm there's an actual walkable path there, not just a nearby navmesh point
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    return hit.position;
                }
            }
        }

        return null; // no valid point found this cycle
    }

    private IEnumerator InvestigateStep()
    {
        agent.SetDestination(investigateTarget);

        // travel to the noise location
        while (currentState == State.Investigating &&
               Vector3.Distance(transform.position, investigateTarget) > agent.stoppingDistance)
        {
            yield return null;
        }

        // arrived, look around for a bit
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

        // If something on the obstacle layers is hit before reaching the player, sight is blocked.
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
        if (anim != null) anim.SetTrigger("Attack");
            Debug.Log("Player has been killed by the enemy!");

        StartCoroutine(ResetAfterAttack());
    }

    private IEnumerator ResetAfterAttack()
    {
        yield return new WaitForSeconds(1f);
        currentState = State.Roaming;
    }
}