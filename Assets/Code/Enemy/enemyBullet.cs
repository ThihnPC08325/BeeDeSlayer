using UnityEngine;

public class enemyBullet : MonoBehaviour
{
    [SerializeField] float Damage;
    [SerializeField] float penetration;
    private Vector3 startPosition;
    [SerializeField] private float maxDistance = 50f;
    [SerializeField] private TrailRenderer trailRenderer;

    public bool IsInUse { get; private set; }

    private void OnEnable()
    {
        startPosition = transform.position;
    }

    private void OnDisable()
    {
        ResetBullet();
    }

    private void Awake()
    {
        trailRenderer = GetComponent<TrailRenderer>();
    }

    private void Update()
    {
        float totalDistance = Vector3.Distance(startPosition, transform.position);
        if (totalDistance >= maxDistance)
        {
            ReturnToPool();
        }
    }

    public void Initialize(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        ResetBullet();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Kích hoạt sự kiện OnPlayerHit
            GameEvents.TriggerPlayerHit(Damage, penetration);
            ReturnToPool();
        }
        if (other.CompareTag("Wall"))
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        if (EnemyBulletPool.Instance != null)
        {
            IsInUse = false;
            if (trailRenderer != null)
            {
                trailRenderer.Clear(); // Xóa trail khi đạn quay lại pool
            }
            EnemyBulletPool.Instance.ReturnToPool("EnemyBullet", gameObject);
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
}
