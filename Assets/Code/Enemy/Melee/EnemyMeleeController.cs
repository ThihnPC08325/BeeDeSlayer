using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMeleeController : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Chasing,
        Circling,
        Charging,
        Cooldown
    }

    [Header("Movement Settings")]
    [SerializeField] private float normalSpeed = 12f;
    [SerializeField] private float chargeSpeed = 22f;
    [SerializeField] private float detectionRange = 20f;
    [SerializeField] private float attackRange = 8f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float snapThreshold = 5f;
    [SerializeField] private float stateUpdateInterval = 0.2f;
    [SerializeField] private float circleSpeed = 5f;
    [SerializeField] private float circleRadius = 8f;
    [SerializeField] private float chargeProbability = 0.1f;
    [SerializeField] private float chargeDuration = 1f;
    [SerializeField] private float cooldownDuration = 1.5f;

    private NavMeshAgent agent;
    private Transform target;
    private EnemyState currentState = EnemyState.Idle;
    private float currentAngle = 0f;
    private float stateTimer;
    private Vector3 currentTargetPosition;
    private readonly float smoothTime = 0.1f;
    private Vector3 velocity = Vector3.zero;
    private float sqrAttackRange;
    private float sqrDetectionRange;

    public EnemyState CurrentState => currentState;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        target = GameObject.FindGameObjectWithTag("Player").transform;

        // Tính trước bình phương khoảng cách để tối ưu hiệu suất
        sqrAttackRange = attackRange * attackRange;
        sqrDetectionRange = detectionRange * detectionRange;
    }

    private void Start()
    {
        StartCoroutine(UpdateEnemyState());
    }

    private IEnumerator UpdateEnemyState()
    {
        while (true)
        {
            yield return new WaitForSeconds(stateUpdateInterval);

            float sqrDistance = (transform.position - target.position).sqrMagnitude;

            if (currentState == EnemyState.Cooldown)
            {
                // Stay in cooldown state until timer expires
                if (stateTimer <= 0)
                {
                    currentState = EnemyState.Circling;
                }
            }
            else if (sqrDistance <= sqrAttackRange)
            {
                if (currentState != EnemyState.Charging && Random.value < chargeProbability)
                {
                    currentState = EnemyState.Charging;
                    stateTimer = chargeDuration;
                }
                else if (currentState != EnemyState.Circling && currentState != EnemyState.Charging)
                {
                    currentState = EnemyState.Circling;
                }
            }
            else if (sqrDistance <= sqrDetectionRange)
            {
                currentState = EnemyState.Chasing;
            }
            else
            {
                currentState = EnemyState.Idle;
            }
        }
    }

    private void FixedUpdate()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                Idle();
                break;
            case EnemyState.Chasing:
                ChaseTarget(normalSpeed);
                break;
            case EnemyState.Circling:
                CircleTarget();
                break;
            case EnemyState.Charging:
                ChargeAtTarget();
                break;
            case EnemyState.Cooldown:
                Cooldown();
                break;
        }

        RotateTowardsTarget();
        stateTimer -= Time.fixedDeltaTime;
    }

    private void Idle()
    {
        agent.SetDestination(transform.position);
    }

    private void ChaseTarget(float speed)
    {
        agent.speed = speed;
        agent.SetDestination(target.position);
    }

    private void CircleTarget()
    {
        // Tối ưu hóa tính toán góc
        currentAngle = Mathf.Repeat(currentAngle + circleSpeed * Time.fixedDeltaTime, 2 * Mathf.PI);

        Vector3 offset = new Vector3(Mathf.Sin(currentAngle), 0, Mathf.Cos(currentAngle)) * circleRadius;
        Vector3 targetPosition = target.position + offset;

        // Sử dụng SmoothDamp để làm mượt chuyển động
        currentTargetPosition = Vector3.SmoothDamp(currentTargetPosition, targetPosition, ref velocity, smoothTime);

        // Điều chỉnh tốc độ dựa trên khoảng cách
        float distanceToTarget = Vector3.Distance(transform.position, currentTargetPosition);
        agent.speed = Mathf.Lerp(normalSpeed * 0.5f, normalSpeed, distanceToTarget / circleRadius);

        agent.SetDestination(currentTargetPosition);
    }

    private void ChargeAtTarget()
    {
        agent.speed = chargeSpeed;
        agent.SetDestination(target.position);

        if (stateTimer <= 0)
        {
            currentState = EnemyState.Cooldown;
            stateTimer = cooldownDuration;
        }
    }

    private void Cooldown()
    {
        agent.speed = 0;
        agent.SetDestination(transform.position);
    }

    private void RotateTowardsTarget()
    {
        if (target == null) return;

        Vector3 direction = (target.position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

        float angle = Quaternion.Angle(transform.rotation, targetRotation);

        if (angle < snapThreshold)
        {
            transform.rotation = targetRotation;
        }
        else
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, circleRadius);
    }
}