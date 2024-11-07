using UnityEngine;

public class WeaponRecoilSystem : MonoBehaviour
{
    [System.Serializable]
    public struct RecoilPattern
    {
        public Vector2[] pattern;
        public float[] zPattern;
    }

    #region Serialized Fields
    [SerializeField] private RecoilPattern recoilPattern;

    [Header("Recoil Settings")]
    [SerializeField, Range(0f, 20f)] float recoilSpeed = 10f;
    [SerializeField, Range(0f, 10f)] float returnSpeed = 5f;
    [SerializeField, Range(0f, 1f)] float recoilDecayRate = 0.9f;
    [SerializeField, Range(0f, 1f)] float randomness = 0.1f;
    [SerializeField, Range(0f, 5f)] float maxRecoilZ = 0.5f;

    [Header("Spread Settings")]
    [SerializeField, Range(0f, 10f)] float maxSpread = 5f;
    [SerializeField, Range(0f, 1f)] float spreadIncreaseRate = 0.1f;
    [SerializeField, Range(0f, 5f)] float spreadDecreaseRate = 1f;
    #endregion

    #region Private Fields
    private Vector3 currentRotation;
    private Vector3 targetRotation;
    private Vector3 initialPosition;
    private float currentRecoilZ;
    private float targetRecoilZ;
    private float currentSpread;
    private float lastShotTime;
    private int currentPatternIndex;

    // Cached components
    private Transform cachedTransform;
    private readonly Vector3 forwardVector = Vector3.forward;
    private Vector2 cachedPerlinOffset;
    private float lastPerlinUpdateTime;
    private const float PERLIN_UPDATE_INTERVAL = 0.05f; // Update Perlin noise mỗi 50ms
    #endregion

    private void Awake()
    {
        cachedTransform = transform;
        initialPosition = cachedTransform.localPosition;
        UpdatePerlinOffset();
    }

    public void ApplyRecoil()
    {
        if (currentPatternIndex >= recoilPattern.pattern.Length)
            currentPatternIndex = 0;

        Vector2 recoil = recoilPattern.pattern[currentPatternIndex];
        recoil += GetPerlinNoiseOffset() * randomness;

        float recoilZ = Mathf.Min(recoilPattern.zPattern[currentPatternIndex], maxRecoilZ);

        ApplyRecoilForces(recoil, recoilZ);
        UpdateSpread();

        currentPatternIndex++;
        lastShotTime = Time.time;
    }

    private void UpdatePerlinOffset()
    {
        float time = Time.time;
        cachedPerlinOffset.x = Mathf.PerlinNoise(time, 0) * 2 - 1;
        cachedPerlinOffset.y = Mathf.PerlinNoise(0, time) * 2 - 1;
        lastPerlinUpdateTime = time;
    }

    private Vector2 GetPerlinNoiseOffset()
    {
        if (Time.time - lastPerlinUpdateTime >= PERLIN_UPDATE_INTERVAL)
        {
            UpdatePerlinOffset();
        }
        return cachedPerlinOffset;
    }

    private void ApplyRecoilForces(Vector2 recoil, float recoilZ)
    {
        targetRotation *= recoilDecayRate;
        targetRotation += new Vector3(-recoil.y, recoil.x, 0f);

        targetRecoilZ = -recoilZ;
        cachedTransform.localPosition -= forwardVector * recoilZ;
    }

    private void UpdateSpread()
    {
        currentSpread = Mathf.Min(currentSpread + spreadIncreaseRate, maxSpread);
    }

    public void HandleRecoil()
    {
        float deltaTime = Time.deltaTime;
        float lerpFactor = recoilSpeed * deltaTime;
        float returnFactor = returnSpeed * deltaTime;

        currentRotation = Vector3.Slerp(currentRotation, targetRotation, lerpFactor);
        targetRotation = Vector3.Slerp(targetRotation, Vector3.zero, returnFactor);

        currentRecoilZ = Mathf.Lerp(currentRecoilZ, 0f, returnFactor);

        cachedTransform.SetLocalPositionAndRotation(Vector3.Lerp(cachedTransform.localPosition, initialPosition, returnFactor), Quaternion.Euler(currentRotation.x, currentRotation.y, currentRecoilZ));
    }

    public void HandleSpread()
    {
        if (Time.time - lastShotTime > 0.1f)
            currentSpread = Mathf.Max(0, currentSpread - spreadDecreaseRate * Time.deltaTime);
    }
}