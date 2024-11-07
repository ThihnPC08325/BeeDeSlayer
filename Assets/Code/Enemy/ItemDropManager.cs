using System.Collections.Generic;
using UnityEngine;

public class ItemDropManager : MonoBehaviour
{
    [System.Serializable]
    public class DropItem
    {
        public string poolTag; // Thêm trường này
        public float dropChance;
        public int minQuantity = 1;
        public int maxQuantity = 1;
    }

    [SerializeField] private List<DropItem> possibleDrops;
    [SerializeField] private float noDropChance = 30f;
    [SerializeField] private float dropHeightOffset = 0.5f;
    [SerializeField] private float spreadRadius = 1f;
    [SerializeField] private int maxDropAttempts = 10;

    public void TryDropLoot(Vector3 dropPosition)
    {
        if (Random.value <= noDropChance / 100f)
        {
            return; // No drop
        }

        foreach (var item in possibleDrops)
        {
            if (Random.value <= item.dropChance / 100f)
            {
                SpawnItem(item, dropPosition);
            }
        }
    }

    private void SpawnItem(DropItem item, Vector3 basePosition)
    {
        int quantity = Random.Range(item.minQuantity, item.maxQuantity + 1);

        for (int i = 0; i < quantity; i++)
        {
            Vector3 dropPosition = FindValidDropPosition(basePosition);

            GameObject spawnedItem = ItemsPoolManager.Instance.SpawnFromPool(
                item.poolTag,
                dropPosition,
                Quaternion.identity
            );
        }
    }

    private Vector3 FindValidDropPosition(Vector3 basePosition)
    {
        for (int i = 0; i < maxDropAttempts; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * spreadRadius;
            Vector3 attemptPosition = basePosition + new Vector3(randomCircle.x, dropHeightOffset, randomCircle.y);

            if (!Physics.CheckSphere(attemptPosition, 0.1f))
            {
                return attemptPosition;
            }
        }

        return basePosition + Vector3.up * dropHeightOffset;
    }
}