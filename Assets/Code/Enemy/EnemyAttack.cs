using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 2f;

    [Header("Bullet")]
    [SerializeField] float spreadIntensity;
    [SerializeField] private float bulletMass = 0.01f;
    [SerializeField] private float bulletDiameter = 0.00556f;
    [SerializeField] private Transform bulletSpawn;

    [Header("Muzzle Velocity Calculation")]
    [SerializeField] private float barrelLength = 0.5f;
    [SerializeField] private float propellantBurnRate = 0.8f;
    [SerializeField] private float chamberPressure = 30000000f;
    [SerializeField] private float atmosphericPressure = 101325f;

    [Header("Shooting Improvements")]
    [SerializeField] private float maxAccuracy = 1f;
    [SerializeField] private float minAccuracy = 0.5f;
    [SerializeField] private float aimLagTime = 0.2f;
    [SerializeField] private float predictionAccuracy = 0.8f;
    [SerializeField] private float difficultyScaling = 1f;

    private Vector3 aimTarget;
    private float currentAccuracy;
    private float lastAttackTime;
    private Transform player;
    private Vector3 lastPlayerPosition;
    private Vector3 playerVelocity;
    private readonly float velocityUpdateInterval = 0.1f;
    private float lastVelocityUpdateTime;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        StartCoroutine(AimLagCoroutine());
    }

    private void Update()
    {
        UpdatePlayerVelocity();
    }

    public void TryAttack()
    {
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            ShootAtPlayer();
            lastAttackTime = Time.time;
        }
    }

    private void UpdatePlayerVelocity()
    {
        if (Time.time - lastVelocityUpdateTime > velocityUpdateInterval)
        {
            Vector3 currentPlayerPosition = player.position;
            playerVelocity = (currentPlayerPosition - lastPlayerPosition) / velocityUpdateInterval;
            lastPlayerPosition = currentPlayerPosition;
            lastVelocityUpdateTime = Time.time;
        }
    }

    private void ShootAtPlayer()
    {
        float muzzleVelocity = CalculateMuzzleVelocity();
        float bulletTravelTime = Vector3.Distance(transform.position, player.position) / muzzleVelocity;
        Vector3 predictedPosition = PredictPlayerPosition(bulletTravelTime);

        Vector3 shootingDirection = CalculateDirectionAndSpread(predictedPosition);
        GameObject bullet = EnemyBulletPool.Instance.SpawnFromPool("EnemyBullet", bulletSpawn.position, Quaternion.LookRotation(shootingDirection));
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();

        bulletRb.velocity = shootingDirection * muzzleVelocity;
    }

    private Vector3 CalculateDirectionAndSpread(Vector3 targetPosition)
    {
        Vector3 baseDirection = (targetPosition - bulletSpawn.position).normalized;

        float u1 = 1.0f - Random.Range(0f, 1f);
        float u2 = Random.Range(0f, 1f);
        float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);

        Vector3 spreadVector = new Vector3(
            randStdNormal * spreadIntensity,
            randStdNormal * spreadIntensity,
            0
        );

        Quaternion spreadRotation = Quaternion.Euler(spreadVector);
        return (spreadRotation * baseDirection).normalized;
    }

    private IEnumerator AimLagCoroutine()
    {
        while (true)
        {
            aimTarget = CalculateAimTarget();
            yield return new WaitForSeconds(aimLagTime);
        }
    }

    private Vector3 CalculateAimTarget()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        currentAccuracy = CalculateAccuracy(distance) * difficultyScaling;

        Vector3 predictedPosition = PredictPlayerPosition(CalculateBulletTravelTime(distance));
        Vector3 targetPosition = Vector3.Lerp(player.position, predictedPosition, predictionAccuracy * currentAccuracy);

        return targetPosition + (1f - currentAccuracy) * 0.1f * distance * Random.insideUnitSphere;
    }

    private float CalculateAccuracy(float distance)
    {
        float k = 0.2f;
        float x0 = 10f;
        return maxAccuracy - (maxAccuracy - minAccuracy) / (1 + Mathf.Exp(-k * (distance - x0)));
    }

    private Vector3 PredictPlayerPosition(float bulletTravelTime)
    {
        Vector3 gravity = Physics.gravity;
        return player.position + playerVelocity * bulletTravelTime + 0.5f * gravity * bulletTravelTime * bulletTravelTime;
    }

    private float CalculateBulletTravelTime(float distance)
    {
        float muzzleVelocity = CalculateMuzzleVelocity();
        return distance / muzzleVelocity;
    }

    private float CalculateMuzzleVelocity()
    {
        float barrelArea = Mathf.PI * bulletDiameter * bulletDiameter / 4f;
        float barrelVolume = barrelArea * barrelLength;
        float averagePressure = (chamberPressure + atmosphericPressure) / 2f;
        float work = averagePressure * barrelVolume;
        float kineticEnergy = work * propellantBurnRate;
        return Mathf.Sqrt(2f * kineticEnergy / bulletMass);
    }
}