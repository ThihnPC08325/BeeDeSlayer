using System.Collections;
using UnityEngine;
using static EnemyManager;

public class EnemyHealth : MonoBehaviour, IPooledObject
{
    [SerializeField] private float maxHealth;
    private float currentHealth;
    [SerializeField] private ItemDropManager itemDropManager;
    [SerializeField] EnemyController enemyController;
    [SerializeField] EnemyAttack enemyAttack;
    [SerializeField] EnemyDodgeBullet enemyDodgeBullet;
    private BoxCollider BoxCollider;

    private void Awake()
    {
        BoxCollider = GetComponent<BoxCollider>();
        itemDropManager = GetComponent<ItemDropManager>();
        enemyController = GetComponent<EnemyController>();
        enemyAttack = GetComponent<EnemyAttack>();
        enemyDodgeBullet = GetComponent<EnemyDodgeBullet>();
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            BoxCollider.enabled = false;
            enemyController.enabled = false;
            enemyAttack.enabled = false;
            enemyDodgeBullet.enabled = false;
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
        enemyController.enabled = true;
        enemyAttack.enabled = true;
        enemyDodgeBullet.enabled = true;
    }
}