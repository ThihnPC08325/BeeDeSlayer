using UnityEngine;

public class SMGGuns : MonoBehaviour
{
    [System.Serializable]
    public class GunStats
    {
        public float damage = 10f;
        public float range = 100f;
        public float fireRate = 15f;
        public Vector2 recoilRange = new Vector2(0.1f, 0.2f);
        public float verticalRecoil = 1f;
        public float horizontalRecoil = 0.2f;
        public float recoilSpeed = 10f;
        public float returnSpeed = 2f;
    }

    [Header("Gun Configuration")]
    [SerializeField] private GunStats stats;
    [SerializeField] private AnimationCurve recoilCurve;

    [Header("References")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private Camera playerCamera;

    private Vector3 currentRotation;
    private Vector3 targetRotation;
    private float recoilTimer;
    private float nextTimeToFire = 0f;

    private void Update()
    {
        if (Input.GetButton("Fire1"))
        {
            TryShoot();
        }
        HandleRecoil();
    }

    private void TryShoot()
    {
        if (Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + 1f / stats.fireRate;
            Shoot();
        }
    }

    private void Shoot()
    {
        muzzleFlash.Play();
        ApplyRecoil();

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, stats.range))
        {
            ProcessHit(hit);
        }
    }

    private void ProcessHit(RaycastHit hit)
    {
        if (hit.transform.TryGetComponent(out EnemyHealth enemy))
        {
            enemy.TakeDamage(stats.damage);
        }
        SpawnImpactEffect(hit);
    }

    private void SpawnImpactEffect(RaycastHit hit)
    {
        Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
    }

    private void ApplyRecoil()
    {
        targetRotation += new Vector3(
            -stats.verticalRecoil,
            Random.Range(-stats.horizontalRecoil, stats.horizontalRecoil),
            0f
        );
        recoilTimer = 0f;
    }

    private void HandleRecoil()
    {
        recoilTimer += Time.deltaTime;
        float recoilFraction = recoilCurve.Evaluate(recoilTimer);

        currentRotation = Vector3.Slerp(currentRotation, targetRotation, stats.recoilSpeed * Time.deltaTime);
        targetRotation = Vector3.Slerp(targetRotation, Vector3.zero, stats.returnSpeed * Time.deltaTime);

        transform.localRotation = Quaternion.Euler(currentRotation * recoilFraction);
    }
}