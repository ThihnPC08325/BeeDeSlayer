using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static SwitchingWeapon;

[System.Serializable]
public class AmmoDamageConfig
{
    public AmmoType type;
    public float baseDamage;
    public float armorPenetration;
}

public class DamageSystem : MonoBehaviour
{
    [SerializeField] private List<AmmoDamageConfig> ammoConfigs;
    private Dictionary<AmmoType, AmmoDamageConfig> damageConfigDict;

    private void Awake()
    {
        // Khởi tạo dictionary để tra cứu nhanh
        damageConfigDict = ammoConfigs.ToDictionary(config => config.type);
    }

    public float CalculateDamage(AmmoType ammoType, float distance, bool isCritical, float targetArmor)
    {
        if (!damageConfigDict.TryGetValue(ammoType, out var config))
            return 0f;

        float damage = config.baseDamage;

        // Xử lý xuyên giáp
        float effectiveArmor = Mathf.Max(0, targetArmor - config.armorPenetration);
        damage *= (100f / (100f + effectiveArmor));

        // Xử lý đặc biệt cho đạn shotgun
        if (ammoType == AmmoType.Shotgun)
        {
            damage = CalculateShotgunDamage(damage, distance);
        }

        return damage;
    }

    private float CalculateShotgunDamage(float baseDamage, float distance)
    {
        // Số viên đạn trong một phát bắn
        int bulletCount = 8;
        // Tính toán số viên đạn trúng dựa vào khoảng cách
        int hitPellets = Mathf.CeilToInt(bulletCount * Mathf.InverseLerp(10f, 2f, distance));
        return (baseDamage / bulletCount) * hitPellets;
    }
}
