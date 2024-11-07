using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Pool;
using UnityUtils;

public class EnemyBulletHellManager : Singleton<EnemyBulletHellManager>
{
    #region Settings and Refs
    [SerializeField] int bulletCount = 100; // Number of bullets to spawn
    [SerializeField] float bulletSpeed = 10f; // Speed of each bullet
    [SerializeField] float bulletMaxDistance = 30f; // Maximum distance each bullet can travel
    [SerializeField] LayerMask collisionMask; // Layer mask for raycast collision detection
    [SerializeField] Transform bulletOrigin; // Origin point for bullet spawning

    [SerializeField] GameObject bulletPrefab; // Bullet prefab to instantiate
    [SerializeField] GameObject impactEffectPrefab; // Effect to spawn on bullet impact
    #endregion

    ObjectPool<EBullet> bulletPool;
    EnemyBulletGenerator patternGenerator;

    readonly List<EBullet> activeProjectiles = new List<EBullet>();
    readonly List<EBullet> bulletsToReturn = new List<EBullet>();

    TransformAccessArray bulletTransforms;

    // Start is called before the first frame update
    void Start()
    {
        patternGenerator = new EnemyBulletGenerator(new RadialPattern());

        bulletPool = new ObjectPool<EBullet>(
            createFunc: () =>
            {
                GameObject bulletObj = Instantiate(bulletPrefab);
                bulletObj.SetActive(false);
                return bulletObj.GetOrAdd<EBullet>();
            },
            actionOnGet: bullet => bullet.gameObject.SetActive(true),
            actionOnRelease: bullet => bullet.gameObject.SetActive(false),
            actionOnDestroy: bullet => DestroyBullet(bullet),
            collectionCheck: false,
            defaultCapacity: bulletCount,
            maxSize: bulletCount * 10
        );
    }

    void DestroyBullet(EBullet bullet)
    {
        if (bullet)
        {
            Destroy(bullet.gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        int subSteps = 5;
        float subStepTime = Time.deltaTime / subSteps;

        // Consider caching the TransformAccessArray if possible
        using (bulletTransforms = new TransformAccessArray(activeProjectiles.Count))
        {
            for (int i = activeProjectiles.Count; i-- > 0;)
            {
                EBullet bullet = activeProjectiles[i];
                if (bullet.HasTraveledMaxDistance())
                {
                    ReturnBullet(bullet);
                    continue;
                }
                bulletTransforms.Add(bullet.transform);
            }

            for (int step = 0; step < subSteps; step++)
            {
                var job = new BulletMoveJob
                {
                    deltaTime = subStepTime,
                    speed = bulletSpeed
                };

                JobHandle jobHandle = job.Schedule(bulletTransforms);
                jobHandle.Complete();

                HandleCollisions();
            }
        }
    }

    void HandleCollisions()
    {
        Vector3[] origins = new Vector3[activeProjectiles.Count];
        Vector3[] directions = new Vector3[activeProjectiles.Count];

        for (int i = 0; i < activeProjectiles.Count; i++)
        {
            EBullet bullet = activeProjectiles[i];
            origins[i] = bullet.transform.position;
            directions[i] = bullet.direction;
        }

        RaycastBatchProcessor.Instance.PerformRaycasts(origins, directions, collisionMask.value, false, false, false, OnRaycastResults);
    }

    void OnRaycastResults(RaycastHit[] hits)
    {
        for (int i = hits.Length; i-- > 0;)
        {
            if (hits[i].collider != null)
            {
                ReturnBullet(activeProjectiles[i]);

                // TODO Pool the impact effects
                GameObject impactEffect = Instantiate(impactEffectPrefab, hits[i].point, Quaternion.identity);
                impactEffect.transform.SetParent(hits[i].collider.transform);
                impactEffect.transform.up = hits[i].normal;
                Destroy(impactEffect, 2f);
            }
        }
    }

    void ReturnBullet(EBullet bullet)
    {
        bulletsToReturn.Add(bullet);
        activeProjectiles.Remove(bullet);
    }

    [BurstCompile]
    struct BulletMoveJob : IJobParallelForTransform
    {
        public float deltaTime;
        public float speed;

        public void Execute(int index, TransformAccess transform)
        {
            Vector3 forward = transform.rotation * Vector3.forward;
            transform.position += forward * speed * deltaTime;
        }
    }

    public void SpawnBulletPattern()
    {
        EnemyBulletProjectile[] newBullets = patternGenerator.GeneratePattern(bulletOrigin.position, bulletCount, bulletSpeed);

        foreach (EnemyBulletProjectile projectile in newBullets)
        {
            EBullet bullet = bulletPool.Get();
            bullet.Initialize(projectile.Position, projectile.Direction, bulletMaxDistance);
            activeProjectiles.Add(bullet);
        }
    }

    public void SetPattern(IBulletPattern pattern) => patternGenerator.SetPattern(pattern);
    void LateUpdate()
    {
        foreach (var bullet in bulletsToReturn)
        {
            bulletPool.Release(bullet);
        }
        bulletsToReturn.Clear();
    }

    void OnDestroy()
    {
        bulletPool.Dispose();
    }
}
