using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    private enum State { Patrolling, Chasing, Attacking }

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

    public bool IsPatrolling => currentState == State.Patrolling;
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

    void Start()
    {
        if (agent == null) return;
        mainRoutine = StartCoroutine(StateMachine());
    }

    void Update()
    {
        if (agent == null) return;
        agent.speed = currentState == State.Chasing ? moveSpeed * chaseMultiplier : moveSpeed;
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
        // Placeholder delay to represent an attack animation/cooldown.
        yield return new WaitForSeconds(1f);
        currentState = State.Patrolling;
    }
}