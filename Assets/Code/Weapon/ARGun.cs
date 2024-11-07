using System.Collections;
using UnityEngine;
using System;

public class ARGun : MonoBehaviour
{
    [Header("System")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform bulletSpawn;
    [SerializeField] private SwitchingWeapon weaponSwitcher;

    [Header("Shooting")]
    [SerializeField] private float shootingDelay;
    [SerializeField] private ShootMode currentShootMode;

    [Header("Spread")]
    [SerializeField] float BulletSpreadIntensity;

    private Vector3 gravity = Physics.gravity; // Sử dụng trọng lực mặc định của Unity
    private void UpdateBulletTrajectory(Rigidbody bulletRb, float deltaTime)
    {
        // Áp dụng trọng lực
        bulletRb.velocity += gravity * deltaTime;
    }

    private WeaponRecoilSystem recoilSystem;
    private CalculateMuzzleVelocity calculateMuzzleVelocity;
    private bool isShooting, readyToShoot;
    bool allowReset = true;
    private readonly int bulletPerBurst = 3;
    private int currentBurst;
    private readonly float lastShotTime;

    private enum ShootMode
    {
        Single,
        Burst,
        Auto
    }

    private void UpdateShootingParameters()
    {
        switch (currentShootMode)
        {
            case ShootMode.Single:
                shootingDelay = 0.2f;
                BulletSpreadIntensity = 0.02f;
                break;
            case ShootMode.Burst:
                shootingDelay = 0.1f;
                BulletSpreadIntensity = 0.03f;
                break;
            case ShootMode.Auto:
                shootingDelay = 0.2f;
                BulletSpreadIntensity = 0.5f;
                break;
        }
    }

    private void Awake()
    {
        readyToShoot = true;
        currentBurst = bulletPerBurst;
        recoilSystem = GetComponent<WeaponRecoilSystem>();
        calculateMuzzleVelocity = GetComponent<CalculateMuzzleVelocity>();
    }

    void Update()
    {
        // Kiểm tra đầu vào để bắn
        if (currentShootMode == ShootMode.Single || currentShootMode == ShootMode.Burst)
        {
            isShooting = Input.GetButtonDown("Fire1");
        }
        else if (currentShootMode == ShootMode.Auto)
        {
            isShooting = Input.GetButton("Fire1");
        }

        // Kiểm tra để đổi chế độ bắn
        if (Input.GetKeyDown(KeyCode.B))
        {
            SwitchShootMode();
        }

        if (readyToShoot && isShooting && weaponSwitcher.HasAmmo())
        {
            currentBurst = bulletPerBurst;
            FireWeapon();
            recoilSystem.ApplyRecoil();
            weaponSwitcher.UseAmmo();
        }

        recoilSystem.HandleRecoil();
        recoilSystem.HandleSpread();
    }

    private void SwitchShootMode()
    {
        currentShootMode = (ShootMode)(((int)currentShootMode + 1) % Enum.GetValues(typeof(ShootMode)).Length);

        UpdateShootingParameters();

        // Hiển thị thông báo chế độ bắn mới
        StartCoroutine(ShowShootModeMessage());
    }

    private IEnumerator ShowShootModeMessage()
    {
        string modeMessage = $"Chế độ bắn: {currentShootMode}";
        // Hiển thị modeMessage trên UI
        Debug.Log(modeMessage);

        yield return new WaitForSeconds(2f); // Hiển thị trong 2 giây
    }

    private void FireWeapon()
    {
        if (!readyToShoot) return;

        readyToShoot = false;
        PlayMuzzleEffect();
        FireBullet();
        HandleShootingModes();
    }

    private void PlayMuzzleEffect()
    {
        if (muzzleFlash != null) muzzleFlash.Play();
    }

    private void FireBullet()
    {
        Vector3 shootingDirection = BulletDirectionCalculator.CalculateDirection(bulletSpawn, BulletSpreadIntensity);
        float muzzleVelocity = calculateMuzzleVelocity.MuzzleVelocity();

        GameObject bullet = BulletPool.Instance.SpawnFromPool("Bullet", bulletSpawn.position, bulletSpawn.rotation);

        var bulletRb = bullet.GetComponent<Rigidbody>();

        ApplyBulletPhysics(bulletRb, shootingDirection, muzzleVelocity);
    }

    private void ApplyBulletPhysics(Rigidbody bulletRb, Vector3 direction, float velocity)
    {
        bulletRb.velocity = direction * velocity;

        StartCoroutine(HandleBulletPhysics(bulletRb));
    }

    private IEnumerator HandleBulletPhysics(Rigidbody bulletRb)
    {
        while (bulletRb != null && bulletRb.gameObject.activeInHierarchy)
        {
            UpdateBulletTrajectory(bulletRb, Time.fixedDeltaTime);
            calculateMuzzleVelocity.ApplyAirResistance(bulletRb, Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }
    }

    private void HandleShootingModes()
    {
        if (allowReset)
        {
            Invoke(nameof(ResetShoot), shootingDelay);
            allowReset = false;
        }

        if (currentShootMode == ShootMode.Burst && currentBurst > 1)
        {
            currentBurst--;
            Invoke(nameof(FireWeapon), shootingDelay);
        }
    }

    private void ResetShoot()
    {
        readyToShoot = true;
        allowReset = true;
    }

}