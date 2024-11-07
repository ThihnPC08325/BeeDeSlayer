using UnityEngine;

public class HealPlayer : MonoBehaviour
{
    [SerializeField] private float healAmount;
    public bool IsInUse { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameEvents.TriggerHealthPickup(healAmount);
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        if (ItemsPoolManager.Instance != null)
        {
            IsInUse = false;

            ItemsPoolManager.Instance.ReturnToPool("Heal", gameObject);
        }
    }
}