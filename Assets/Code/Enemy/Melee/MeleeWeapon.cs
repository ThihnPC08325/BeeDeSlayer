using UnityEngine;

public class MeleeWeapon : MonoBehaviour
{
    [SerializeField] float Damage;
    [SerializeField] float penetration;
    [SerializeField] private float damageCooldown = 1f; // Thời gian cooldown để tránh gây xác thương liên tục

    private float lastDamageTime;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && Time.time >= lastDamageTime + damageCooldown)
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            playerHealth.TakeDamage(Damage, penetration);
            lastDamageTime = Time.time;
        }
    }
}
