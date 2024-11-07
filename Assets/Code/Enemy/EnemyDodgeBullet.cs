using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDodgeBullet : MonoBehaviour
{
    [Header("Dodge Settings")]
    [SerializeField] private float dodgeDistance = 3f;
    [SerializeField] private float quickDodgeDistance = 5f;
    [SerializeField] private float dodgeTime = 0.5f;
    [SerializeField] private float quickDodgeTime = 0.3f;
    [SerializeField] private float dodgeCooldown = 2f;
    [SerializeField] private LayerMask bulletLayer;
    [SerializeField] private float bulletDetectionRadius = 5f;
    [SerializeField] private float maxDetectionDistance = 10f;
    [SerializeField] private float maxTimeToReact = 0.5f;

    [Header("LOD Settings")]
    [SerializeField] private float highLODDistance = 10f;
    [SerializeField] private float mediumLODDistance = 20f;
    [SerializeField] private float lowLODDistance = 30f;

    [Header("Learning Settings")]
    [SerializeField] private int maxMemorySize = 10;
    [SerializeField] private float learningRate = 0.1f;

    private bool canDodge = true;
    private Transform playerTransform;
    private LODLevel currentLODLevel = LODLevel.High;
    private readonly List<Vector3> successfulDodges = new();

    private enum LODLevel
    {
        High,
        Medium,
        Low,
        VeryLow
    }

    private void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        StartCoroutine(UpdateLODLevel());
    }

    private void Update()
    {
        if (ShouldProcessAI())
        {
            List<BulletThreat> threats = DetectIncomingBullets();
            if (threats.Count > 0)
            {
                StartCoroutine(DodgeBullets(threats));
            }
        }
    }

    private IEnumerator UpdateLODLevel()
    {
        WaitForSeconds wait = new WaitForSeconds(0.5f); // Cache WaitForSeconds
        Vector3 playerPos, myPos;
        float sqrHighDist = highLODDistance * highLODDistance;
        float sqrMediumDist = mediumLODDistance * mediumLODDistance;
        float sqrLowDist = lowLODDistance * lowLODDistance;

        while (true)
        {
            playerPos = playerTransform.position;
            myPos = transform.position;
            float sqrDistance = (playerPos - myPos).sqrMagnitude; // Dùng sqrMagnitude thay vì Distance

            currentLODLevel = sqrDistance <= sqrHighDist ? LODLevel.High :
                             sqrDistance <= sqrMediumDist ? LODLevel.Medium :
                             sqrDistance <= sqrLowDist ? LODLevel.Low :
                             LODLevel.VeryLow;

            yield return wait;
        }
    }

    private static readonly Dictionary<LODLevel, float> LODMultipliers = new()
{
    { LODLevel.High, 1f },
    { LODLevel.Medium, 0.75f },
    { LODLevel.Low, 0.5f },
    { LODLevel.VeryLow, 0.25f }
};

    private static readonly Dictionary<LODLevel, int> ProcessFrameIntervals = new()
{
    { LODLevel.High, 1 },
    { LODLevel.Medium, 2 },
    { LODLevel.Low, 4 },
    { LODLevel.VeryLow, 0 }
};

    private bool ShouldProcessAI()
    {
        int interval = ProcessFrameIntervals[currentLODLevel];
        return interval > 0 && Time.frameCount % interval == 0;
    }

    private float GetLODMultiplier() => LODMultipliers[currentLODLevel];

    private List<BulletThreat> DetectIncomingBullets()
    {
        List<BulletThreat> threats = new();

        if (currentLODLevel == LODLevel.VeryLow)
            return threats;

        float currentDetectionRadius = bulletDetectionRadius * GetLODMultiplier();
        float currentMaxDetectionDistance = maxDetectionDistance * GetLODMultiplier();

        RaycastHit[] hits = Physics.SphereCastAll(transform.position, currentDetectionRadius, transform.forward, currentMaxDetectionDistance, bulletLayer);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.TryGetComponent<Rigidbody>(out var bulletRb))
            {
                float distanceToBullet = Vector3.Distance(transform.position, hit.point);
                float timeToImpact = distanceToBullet / bulletRb.velocity.magnitude;

                if (timeToImpact <= maxTimeToReact * GetLODMultiplier())
                {
                    Vector3 predictedImpactPoint = hit.point + bulletRb.velocity * timeToImpact;
                    threats.Add(new BulletThreat(bulletRb.velocity.normalized, predictedImpactPoint, timeToImpact));
                }
            }
        }

        threats.Sort((a, b) => a.TimeToImpact.CompareTo(b.TimeToImpact));
        return threats;
    }

    private IEnumerator DodgeBullets(List<BulletThreat> threats)
    {
        if (!canDodge) yield break;

        canDodge = false;

        Vector3 dodgeDirection = CalculateBestDodgeDirection(threats);
        bool isQuickDodge = threats[0].TimeToImpact < quickDodgeTime;

        Vector3 startPos = transform.position;
        Vector3 dodgeTarget = startPos + dodgeDirection * (isQuickDodge ? quickDodgeDistance : dodgeDistance);

        float dodgeDuration = isQuickDodge ? quickDodgeTime : dodgeTime;
        float elapsedTime = 0f;

        while (elapsedTime < dodgeDuration)
        {
            transform.position = Vector3.Lerp(startPos, dodgeTarget, elapsedTime / dodgeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = dodgeTarget;

        // Học hỏi từ né thành công
        LearnFromSuccessfulDodge(dodgeDirection);

        yield return new WaitForSeconds(dodgeCooldown);
        canDodge = true;
    }

    private Vector3 CalculateBestDodgeDirection(List<BulletThreat> threats)
    {
        Vector3 averageThreatDirection = Vector3.zero;
        foreach (var threat in threats)
        {
            averageThreatDirection += threat.Direction;
        }
        averageThreatDirection /= threats.Count;

        Vector3 dodgeDirection = Vector3.Cross(averageThreatDirection, Vector3.up).normalized;

        // Kiểm tra môi trường xung quanh để chọn hướng né tốt nhất
        RaycastHit hit;
        if (Physics.Raycast(transform.position, dodgeDirection, out hit, dodgeDistance))
        {
            dodgeDirection = -dodgeDirection;
        }

        // Áp dụng kinh nghiệm từ các lần né trước
        if (successfulDodges.Count > 0)
        {
            Vector3 learnedDirection = Vector3.zero;
            foreach (var successfulDodge in successfulDodges)
            {
                learnedDirection += successfulDodge;
            }
            learnedDirection /= successfulDodges.Count;

            dodgeDirection = Vector3.Lerp(dodgeDirection, learnedDirection, learningRate);
        }

        return dodgeDirection.normalized;
    }

    private void LearnFromSuccessfulDodge(Vector3 dodgeDirection)
    {
        successfulDodges.Add(dodgeDirection);
        if (successfulDodges.Count > maxMemorySize)
        {
            successfulDodges.RemoveAt(0);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, bulletDetectionRadius);
    }

    private class BulletThreat
    {
        public Vector3 Direction { get; }
        public Vector3 PredictedImpactPoint { get; }
        public float TimeToImpact { get; }

        public BulletThreat(Vector3 direction, Vector3 predictedImpactPoint, float timeToImpact)
        {
            Direction = direction;
            PredictedImpactPoint = predictedImpactPoint;
            TimeToImpact = timeToImpact;
        }
    }
}