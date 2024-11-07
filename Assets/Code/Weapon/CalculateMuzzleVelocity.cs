using Unity.Mathematics;
using UnityEngine;

public readonly struct MuzzleVelocityCalculator
{
    // Cache các giá trị tính toán trước
    private readonly float barrelArea;
    private readonly float barrelVolume;
    private readonly float pressureDifference;

    public MuzzleVelocityCalculator(float bulletDiameter, float barrelLength,
        float chamberPressure, float atmosphericPressure)
    {
        float bulletRadius = bulletDiameter * 0.5f;
        barrelArea = math.PI * bulletRadius * bulletRadius;
        barrelVolume = barrelArea * barrelLength;
        pressureDifference = chamberPressure - atmosphericPressure;
    }

    public float Calculate(float bulletMass, float propellantBurnRate,
        float efficiencyFactor, float atmosphericPressure)
    {
        float averagePressure = atmosphericPressure + (pressureDifference * 0.5f);
        float work = averagePressure * barrelVolume * efficiencyFactor;
        float kineticEnergy = work * propellantBurnRate;

        return math.sqrt(2f * kineticEnergy / bulletMass);
    }
}

public class CalculateMuzzleVelocity : MonoBehaviour
{
    [Header("Bullet Properties")]
    [SerializeField] float bulletMass = 0.01f;
    [SerializeField] float bulletDiameter = 0.00556f;
    [SerializeField] float dragCoefficient = 0.295f;

    [Header("Barrel Properties")]
    [SerializeField] float barrelLength = 0.5f;
    [SerializeField] float propellantBurnRate = 0.8f;
    [SerializeField] float chamberPressure = 30000000f;
    [SerializeField] float atmosphericPressure = 101325f;
    [SerializeField] float efficiencyFactor = 0.9f;

    // Cache các giá trị
    private MuzzleVelocityCalculator calculator;
    private float bulletArea;
    private const float AIR_DENSITY = 1.225f;

    private void Awake()
    {
        calculator = new MuzzleVelocityCalculator(
            bulletDiameter, barrelLength, chamberPressure, atmosphericPressure);
        bulletArea = math.PI * bulletDiameter * bulletDiameter * 0.25f;
    }

    public float MuzzleVelocity() => calculator.Calculate(
        bulletMass, propellantBurnRate, efficiencyFactor, atmosphericPressure);

    public void ApplyAirResistance(Rigidbody bulletRb, float deltaTime)
    {
        float bulletSpeed = bulletRb.velocity.magnitude;
        float dragForce = 0.5f * AIR_DENSITY * bulletSpeed * bulletSpeed *
            dragCoefficient * bulletArea;

        bulletRb.velocity += (dragForce / bulletMass) * deltaTime * -bulletRb.velocity.normalized;
    }
}