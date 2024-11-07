using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float damage = 10f; //Xác thương gây ra
    [SerializeField] private TrailRenderer trailRenderer; //Trail renderer để vẽ đường bay cho đạn
    [SerializeField] private float maxDistance = 100f; //Khoảng cách tối đa đạn có thể bay
    public bool IsInUse { get; private set; }

    private Vector3 startPosition;

    private void Awake()
    {
        trailRenderer = GetComponent<TrailRenderer>();
    }

    private void OnEnable()
    {
        startPosition = transform.position;
    }

    private void OnDisable()
    {
        ResetBullet();
    }

    private void Update()
    {
        if (!IsInUse) return;

        // Kiểm tra khoảng cách đã bay
        float totalDistance = Vector3.Distance(startPosition, transform.position);
        if (totalDistance >= maxDistance)
        {
            ReturnToPool();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        switch (collision.gameObject.tag)
        {
            case "Enemy":
                HandleEnemyCollision(collision.gameObject);
                break;
            case "Wall":
                HandleWallCollision(collision.GetContact(0).point);
                break;
            default:
                // Xử lý các trường hợp va chạm khác nếu cần
                break;
        }

        ReturnToPool();
        ReturnImpactToPool();
    }

    private void HandleEnemyCollision(GameObject enemy)
    {
        // Kích hoạt event kẻ thù trúng đạn
        GameEvents.TriggerEnemyHit(damage, enemy);

        // Xử lý sát thương
        if (enemy.TryGetComponent(out EnemyHealth enemyHealth))
        {
            enemyHealth.TakeDamage(damage);
        }

        if (enemy.TryGetComponent(out EnemyMeleeHealth enemyMeleeHealth))
        {
            enemyMeleeHealth.TakeDamage(damage);
        }
    }

    private void HandleWallCollision(Vector3 collisionPoint)
    {
        _ = ImpactEffectPool.Instance.SpawnFromPool("ImpactSoftBody", collisionPoint, Quaternion.identity);
    }

    private void ReturnToPool()
    {
        if (BulletPool.Instance != null)
        {
            IsInUse = false;
            if (trailRenderer != null)
            {
                trailRenderer.Clear(); // Xóa trail khi đạn quay lại pool
            }
            BulletPool.Instance.ReturnToPool("Bullet", gameObject);
        }
    }

    private void ResetBullet()
    {
        IsInUse = true;
        startPosition = transform.position; // Đặt lại vị trí bắt đầu
        if (trailRenderer != null)
        {
            trailRenderer.Clear(); // Xóa trail khi reset đạn
        }
    }

    private void ReturnImpactToPool()
    {
        if (ImpactEffectPool.Instance != null)
        {
            IsInUse = false;
            ImpactEffectPool.Instance.ReturnToPool("ImpactSoftBody", gameObject);
        }
    }
}