using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    private enum State { Patrolling, Investigating, Chasing, Attacking }

    private NavMeshAgent agent;
    private Transform playerTransform;
    private State currentState = State.Patrolling;
    private int currentWaypointIndex = 0;
    private Coroutine mainRoutine;

    public float moveSpeed = 10f;
    public float chaseMultiplier = 1.5f;
    public Collider detectionCollider;
    public Collider killCollider;
    public List<Transform> waypoints = new List<Transform>();

    [Header("Noise Response")]
    public float investigateWaitTime = 3f;
    public float investigateMoveSpeedMultiplier = 1.2f;

    private Vector3 investigateTarget;

    public bool IsPatrolling => currentState == State.Patrolling;
    public bool IsInvestigating => currentState == State.Investigating;
    public bool IsChasing => currentState == State.Chasing;
    public bool IsAttacking => currentState == State.Attacking;

    void Awake()
    {
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
                case State.Patrolling:
                    yield return PatrolStep();
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

    private IEnumerator PatrolStep()
    {
        if (waypoints == null || waypoints.Count == 0)
        {
            yield return null;
            yield break;
        }

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        if (targetWaypoint == null)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
            yield break;
        }

        agent.SetDestination(targetWaypoint.position);

        while (currentState == State.Patrolling &&
               Vector3.Distance(transform.position, targetWaypoint.position) > agent.stoppingDistance)
        {
            yield return null;
        }

        if (currentState == State.Patrolling)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
        }
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
            currentState = State.Patrolling;
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

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (detectionCollider != null && IsSameCollider(other, detectionCollider) == false)
        {
            // Only react to the collider dedicated to detection.
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (currentState == State.Attacking) return;

        if (detectionCollider != null && detectionCollider.bounds.Intersects(other.bounds))
        {
            currentState = State.Chasing;
        }

        if (killCollider != null && killCollider.bounds.Intersects(other.bounds))
        {
            TryKill();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (currentState == State.Chasing)
        {
            currentState = State.Patrolling;
        }
    }

    private bool IsSameCollider(Collider a, Collider b) => a == b;

    private void TryKill()
    {
        if (currentState == State.Attacking) return;

        currentState = State.Attacking;
        Debug.Log("Player has been killed by the enemy!");

        StartCoroutine(ResetAfterAttack());
    }

    private IEnumerator ResetAfterAttack()
    {
        yield return new WaitForSeconds(1f);
        currentState = State.Patrolling;
    }
}