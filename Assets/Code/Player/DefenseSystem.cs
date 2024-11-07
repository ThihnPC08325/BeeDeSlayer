using UnityEngine;

public class DefenseSystem : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)] private float maxDefensePoint = 75f;
    [SerializeField, Range(0f, 75f)] private float diminishingReturnThreshold = 50f;
    [SerializeField, Range(0.1f, 0.5f)] private float minimumDamagePercent = 0.1f;

    // Cache các giá trị thường xuyên sử dụng
    private const float BASE_DEFENSE_VALUE = 100f;
    private const float DIMINISHING_FACTOR = 0.5f;

    // Cache các giá trị tính toán thường xuyên
    private float _cachedDefense;
    private float _cachedPenetration;
    private float _cachedDamageReduction;

    public float CalculateDamage(float damage, float defense, float penetration)
    {
        // Kiểm tra cache để tối ưu hiệu năng
        if (_cachedDefense != defense || _cachedPenetration != penetration)
        {
            UpdateDamageReduction(defense, penetration);
        }

        return CalculateFinalDamage(damage);
    }

    private void UpdateDamageReduction(float defense, float penetration)
    {
        _cachedDefense = defense;
        _cachedPenetration = penetration;

        float effectiveDefense = CalculateEffectiveDefense(defense, penetration);
        float diminishedDefense = ApplyDiminishingReturns(effectiveDefense);
        _cachedDamageReduction = BASE_DEFENSE_VALUE / (BASE_DEFENSE_VALUE + diminishedDefense);
    }

    private float CalculateEffectiveDefense(float defense, float penetration)
    {
        return Mathf.Max(0, Mathf.Min(maxDefensePoint, defense * (1 - penetration)));
    }

    private float ApplyDiminishingReturns(float defense)
    {
        if (defense <= diminishingReturnThreshold) return defense;

        float excess = defense - diminishingReturnThreshold;
        return diminishingReturnThreshold + (excess * DIMINISHING_FACTOR);
    }

    private float CalculateFinalDamage(float damage)
    {
        float reducedDamage = damage * _cachedDamageReduction;
        return Mathf.Max(reducedDamage, damage * minimumDamagePercent);
    }
}
