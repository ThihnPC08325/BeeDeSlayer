using UnityEngine;
using static SwitchingWeapon;

public class GameEvents : MonoBehaviour
{
    //Event nhặt đạn
    public delegate void AmmoPickupAction(AmmoType ammoType, int amount);
    public static event AmmoPickupAction OnAmmoPickup;

    //Event nhặt máu
    public delegate void HealthPickupAction(float amount);
    public static event HealthPickupAction OnHealthPickup;

    //Event người chơi trúng đạn
    public delegate void PlayerHitAction(float damage, float penetration);
    public static event PlayerHitAction OnPlayerHit;

    //Event kẻ thù trúng đạn
    public delegate void EnemyHitAction(float damage, GameObject enemy);
    public static event EnemyHitAction OnEnemyHit;

    public static void TriggerAmmoPickup(AmmoType ammoType, int amount)
    {
        OnAmmoPickup?.Invoke(ammoType, amount);
    }

    public static void TriggerHealthPickup(float amount)
    {
        OnHealthPickup?.Invoke(amount);
    }

    public static void TriggerPlayerHit(float damage, float penetration)
    {
        OnPlayerHit?.Invoke(damage, penetration);
    }

    public static void TriggerEnemyHit(float damage, GameObject enemy)
    {
        OnEnemyHit?.Invoke(damage, enemy);
    }
}