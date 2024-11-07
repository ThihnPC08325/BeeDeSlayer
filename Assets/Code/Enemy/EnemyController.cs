using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Chasing,
        CircleAndShoot,
        Sprinting,
    }

    [Header("Enemy Settings")]
    [SerializeField] float randomness = 0.5f;
    [SerializeField] private float normalSpeed = 12f;
    [SerializeField] private float sprintSpeed = 18f;
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private float detectionRange = 20f;
    [SerializeField] private float sprintThreshold = 30f;

    [SerializeField] private float rotationSpeed = 720f; // Tốc độ xoay tính bằng độ/giây
    [SerializeField] private float snapThreshold = 5f; // Ngưỡng góc để snap đến hướng mục tiêu
    [SerializeField] private float stateUpdateInterval = 0.2f;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Prediction")]
    [SerializeField] float predictionTime = 1f;

    [Header("Circle Shoot Settings")]
    [SerializeField] private float circleRadius = 8f;
    [SerializeField] float circleSpeed = 1f;
    [SerializeField] float verticalAmplitude = 1f;
    [SerializeField] float verticalFrequency = 1f;

    private readonly Queue<Vector3> playerPositions = new Queue<Vector3>();
    private NavMeshAgent agent;
    private Transform player;
    private EnemyState currentState = EnemyState.Idle;
    private EnemyAttack enemyAttack;
    private float currentAngle = 0f;
    private Vector3 currentTargetPosition;
    private readonly float smoothTime = 0.5f;
    private Vector3 velocity = Vector3.zero;

    private float sqrAttackRange;
    private float sqrDetectionRange;
    private float sqrSprintThreshold;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        enemyAttack = GetComponent<EnemyAttack>();

        // Tính trước bình phương khoảng cách để tối ưu hiệu suất
        sqrAttackRange = attackRange * attackRange;
        sqrDetectionRange = detectionRange * detectionRange;
        sqrSprintThreshold = sprintThreshold * sprintThreshold;
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

            float sqrDistance = (transform.position - player.position).sqrMagnitude;

            if (sqrDistance <= sqrAttackRange)
            {
                currentState = EnemyState.CircleAndShoot;
            }
            else if (sqrDistance <= sqrDetectionRange)
            {
                currentState = EnemyState.Chasing;
            }
            else if (sqrDistance > sqrSprintThreshold)
            {
                currentState = EnemyState.Sprinting;
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
                ChasePlayer(normalSpeed);
                break;
            case EnemyState.Sprinting:
                ChasePlayer(sprintSpeed);
                break;
            case EnemyState.CircleAndShoot:
                CircleAndShoot();
                break;
        }
    }

    private void Idle()
    {
        agent.SetDestination(transform.position);
    }

    private void ChasePlayer(float speed)
    {
        agent.SetDestination(player.position);
        agent.speed = speed;
        RotateTowardsTarget();
    }

    private void CircleAndShoot()
    {
        // Tính toán địa điểm tiếp theo trong vòng tròn
        currentAngle += circleSpeed * Time.fixedDeltaTime;
        Vector3 horizontalOffset = new Vector3(Mathf.Sin(currentAngle), 0, Mathf.Cos(currentAngle)) * circleRadius;
        Vector3 verticalOffset = Vector3.up * Mathf.Sin(currentAngle * verticalFrequency) * verticalAmplitude;

        // Dự đoán vị trí của người chơi
        Vector3 predictedPlayerPos = PredictPlayerPosition();
        Vector3 targetPosition = predictedPlayerPos + horizontalOffset + verticalOffset;

        // Thêm yếu tố ngẫu nhiên
        targetPosition += Random.insideUnitSphere * randomness;

        // Sử dụng SmoothDamp để di chuyển có phần mượt hơn
        currentTargetPosition = Vector3.SmoothDamp(currentTargetPosition, targetPosition, ref velocity, smoothTime);

        // Kiểm tra vật cản
        if (IsPathClear(currentTargetPosition))
        {
            agent.SetDestination(currentTargetPosition);
        }
        else
        {
            // Tìm đường khác
            Vector3 alternativePath = FindAlternativePath(currentTargetPosition);
            agent.SetDestination(alternativePath);
        }

        // Xoay về phía người chơi
        RotateTowardsTarget();

        // Bắn
        enemyAttack.TryAttack();
    }

    private Vector3 PredictPlayerPosition()
    {
        if (playerPositions.Count < 2)
            return player.position;

        List<Vector3> positions = new List<Vector3>(playerPositions);
        Vector3 averageVelocity = Vector3.zero;

        for (int i = 1; i < positions.Count; i++)
        {
            averageVelocity += (positions[i] - positions[i - 1]) / Time.fixedDeltaTime;
        }
        averageVelocity /= (positions.Count - 1);

        // Sử dụng thêm gia tốc để dự đoán người chơi tốt hơn
        Vector3 acceleration = (positions[positions.Count - 1] - positions[positions.Count - 2]) / Time.fixedDeltaTime - averageVelocity;

        return player.position + averageVelocity * predictionTime + 0.5f * acceleration * predictionTime * predictionTime;
    }

    private bool IsPathClear(Vector3 targetPosition)
    {
        RaycastHit hit;
        Vector3 direction = (targetPosition - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPosition);

        if (Physics.Raycast(transform.position, direction, out hit, distance, obstacleLayer))
        {
            return false;
        }
        return true;
    }

    private Vector3 FindAlternativePath(Vector3 originalTarget)
    {
        Vector3 directionToOriginalTarget = (originalTarget - transform.position).normalized;
        Vector3 directionToPlayer = (player.position - transform.position).normalized;

        // Tạo một mảng các hướng có thể
        Vector3[] possibleDirections = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f; // 8 hướng, mỗi hướng cách nhau 45 độ
            possibleDirections[i] = Quaternion.Euler(0, angle, 0) * directionToOriginalTarget;
        }

        // Đánh giá và chọn hướng tốt nhất
        float bestScore = float.MinValue;
        Vector3 bestDirection = directionToOriginalTarget;

        foreach (Vector3 direction in possibleDirections)
        {
            Vector3 potentialTarget = transform.position + direction * circleRadius;

            if (IsPathClear(potentialTarget))
            {
                float score = EvaluateDirection(direction, directionToPlayer);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDirection = direction;
                }
            }
        }

        return transform.position + bestDirection * circleRadius;
    }

    private float EvaluateDirection(Vector3 direction, Vector3 directionToPlayer)
    {
        float score = 0f;

        // Ưu tiên hướng không quá gần hoặc quá xa người chơi
        float dotProduct = Vector3.Dot(direction, directionToPlayer);
        score += 1f - Mathf.Abs(dotProduct); // Cao nhất khi vuông góc với hướng đến người chơi

        // Ưu tiên hướng gần với hướng người chơi
        float angleToPlayer = Vector3.Angle(direction, player.position);
        score += 1f - (angleToPlayer / 180f); // Cao nhất khi gần với hướng người chơi

        // Thêm một chút ngẫu nhiên để tránh hành vi quá dự đoán
        score += Random.Range(0f, 0.2f);

        return score;
    }

    private void RotateTowardsTarget()
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

        float angle = Quaternion.Angle(transform.rotation, targetRotation);

        if (angle < snapThreshold)
        {
            // Nếu góc nhỏ, snap trực tiếp đến hướng mục tiêu
            transform.rotation = targetRotation;
        }
        else
        {
            // Sử dụng RotateTowards để có sự xoay mượt mà và nhanh hơn
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
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, sprintThreshold);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, circleRadius);
    }
}