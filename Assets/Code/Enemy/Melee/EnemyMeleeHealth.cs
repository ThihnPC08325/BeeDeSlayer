using System.Collections;
using UnityEngine;
using static EnemyManager;

public class EnemyMeleeHealth : MonoBehaviour, IPooledObject
{
    [SerializeField] private float maxHealth;
    private float currentHealth;
    [SerializeField] EnemyMeleeController controller;
    [SerializeField] MeleeAttack attack;
    [SerializeField] EnemyDodgeBullet dodgeBullet;
    [SerializeField] private ItemDropManager itemDropManager;
    private BoxCollider BoxCollider;

    private void Awake()
    {
        BoxCollider = GetComponent<BoxCollider>();
        itemDropManager = GetComponent<ItemDropManager>();
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            BoxCollider.enabled = false;
            controller.enabled = false;
            attack.enabled = false;
            dodgeBullet.enabled = false;
            Die();
        }
    }

    private void Die()
    {
        if (itemDropManager != null)
        {
            itemDropManager.TryDropLoot(transform.position);
        }
        StartCoroutine(TimeToDie(5f));
        gameObject.SetActive(false);
    }

    private IEnumerator TimeToDie(float duration)
    {
        yield return new WaitForSeconds(duration);
    }

    public virtual void OnObjectSpawn()
    {
        ResetEnemy();
    }

    protected virtual void ResetEnemy()
    {
        currentHealth = maxHealth;
        BoxCollider.enabled = true;
        controller.enabled = true;
        attack.enabled = true;
        dodgeBullet.enabled = true;
    }
}
