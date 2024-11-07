using UnityEngine;
using static SwitchingWeapon;

public class AmmoUp : MonoBehaviour
{
    [SerializeField] private AmmoType ammoType;
    [SerializeField] private int restoreAmount;
    public bool IsInUse { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameEvents.TriggerAmmoPickup(ammoType, restoreAmount);
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        if (ItemsPoolManager.Instance != null)
        {
            IsInUse = false;
            
            ItemsPoolManager.Instance.ReturnToPool("Ammo", gameObject);
        }
    }
}