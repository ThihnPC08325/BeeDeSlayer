using UnityEngine;

public class BulletDirectionCalculator : MonoBehaviour
{
    private const float TWO_PI = 2f * Mathf.PI;
    private static readonly System.Random random = new();

    // Cache các giá trị sin/cos thường dùng
    private static readonly float[] precomputedSin;
    private static readonly float[] precomputedCos;
    private const int PRECOMPUTE_STEPS = 360;

    static BulletDirectionCalculator()
    {
        precomputedSin = new float[PRECOMPUTE_STEPS];
        precomputedCos = new float[PRECOMPUTE_STEPS];

        for (int i = 0; i < PRECOMPUTE_STEPS; i++)
        {
            float angle = (TWO_PI * i) / PRECOMPUTE_STEPS;
            precomputedSin[i] = Mathf.Sin(angle);
            precomputedCos[i] = Mathf.Cos(angle);
        }
    }

    public static Vector3 CalculateDirection(Transform bulletSpawn, float spreadIntensity)
    {
        if (bulletSpawn == null)
        {
            Debug.LogError("🔴 BulletSpawn transform is null!");
            return Vector3.forward;
        }

        Vector3 baseDirection = bulletSpawn.forward;

        // Sử dụng lookup table
        int angleIndex = random.Next(0, PRECOMPUTE_STEPS);
        float u1 = 1f - (float)random.NextDouble();

        float sqrtLog = Mathf.Sqrt(-2f * Mathf.Log(u1));
        Vector2 randStdNormal = new Vector2(
            sqrtLog * precomputedCos[angleIndex],
            sqrtLog * precomputedSin[angleIndex]
        ) * spreadIntensity;

        return Quaternion.Euler(randStdNormal.x, randStdNormal.y, 0f) * baseDirection;
    }
}