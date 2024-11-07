using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

public class SwitchingWeapon : MonoBehaviour
{
    public enum AmmoType
    {
        Light,    // Đạn nhẹ (pistol, SMG)
        Medium,   // Đạn trung (AR)
        Heavy,    // Đạn nặng (Sniper)
        Shotgun   // Đạn shotgun
    }

    [System.Serializable]
    public class WeaponAmmo
    {
        public string weaponName;
        public AmmoType ammoType;
        public int currentAmmo;
        public int maxAmmo;

        public int AddAmmo(int ammoToAdd)
        {
            int ammoBeforePickup = currentAmmo;
            currentAmmo = Mathf.Min(currentAmmo + ammoToAdd, maxAmmo);
            return currentAmmo - ammoBeforePickup; // Số đạn thực sự được thêm vào
        }
    }

    [SerializeField] private int selectedWeaponIndex = 0;
    [SerializeField] private float scrollCooldown = 0.2f;
    [SerializeField] private WeaponAmmo[] weaponAmmos;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI ammoText;

    [Header("Color Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;
    [SerializeField] private float warningThreshold = 0.25f; // Ngưỡng cảnh báo khi người chơi gần hết đạn (%)

    private float lastScrollTime;
    private int weaponCount;
    private const string MOUSE_SCROLL_AXIS = "Mouse ScrollWheel";
    private const float SCROLL_THRESHOLD = 0f;
    private Transform[] weaponTransforms;
    private Dictionary<AmmoType, List<WeaponAmmo>> ammoTypeMap;
    private Dictionary<AmmoType, WeaponAmmo[]> _ammoCache;
    private Queue<StringBuilder> _stringBuilderPool;
    private const int POOL_SIZE = 5;

    void OnEnable()
    {
        GameEvents.OnAmmoPickup += HandleAmmoPickup;
    }

    void OnDisable()
    {
        GameEvents.OnAmmoPickup -= HandleAmmoPickup;
    }

    private void Awake()
    {
        // Cache weapon transforms
        weaponCount = transform.childCount;
        weaponTransforms = new Transform[weaponCount];
        for (int i = 0; i < weaponCount; i++)
        {
            weaponTransforms[i] = transform.GetChild(i);
        }

        // Tạo map cho ammo types
        InitializeAmmoTypeMap();
        InitializePooling();
        CacheComponents();
        ValidateInitialWeapon();
        SelectWeapon(selectedWeaponIndex);
    }

    private void Update()
    {
        if (!CanSwitchWeapon()) return;

        int newWeaponIndex = GetNewWeaponIndex();
        if (newWeaponIndex != selectedWeaponIndex)
        {
            SelectWeapon(newWeaponIndex);
        }
    }

    private bool CanSwitchWeapon() => Time.time - lastScrollTime >= scrollCooldown;

    private int GetNewWeaponIndex()
    {
        // Xử lý scroll input
        float scrollInput = Input.GetAxis(MOUSE_SCROLL_AXIS);
        if (Mathf.Abs(scrollInput) > SCROLL_THRESHOLD)
        {
            lastScrollTime = Time.time;
            return HandleScrollInput(scrollInput);
        }

        // Xử lý numeric input
        for (int i = 0; i < weaponCount; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                return i;
        }

        return selectedWeaponIndex;
    }

    private void InitializePooling()
    {
        _stringBuilderPool = new Queue<StringBuilder>(POOL_SIZE);
        for (int i = 0; i < POOL_SIZE; i++)
        {
            _stringBuilderPool.Enqueue(new StringBuilder(20)); // Predefine capacity
        }
    }

    private void CacheComponents()
    {
        // Cache weapon transforms - Chỉ thực hiện 1 lần
        weaponCount = transform.childCount;
        weaponTransforms = new Transform[weaponCount];
        for (int i = 0; i < weaponCount; i++)
        {
            weaponTransforms[i] = transform.GetChild(i);
        }

        // Tối ưu dictionary bằng cách nhóm theo AmmoType
        _ammoCache = weaponAmmos
            .GroupBy(w => w.ammoType)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    private StringBuilder GetStringBuilder()
    {
        return _stringBuilderPool.Count > 0
            ? _stringBuilderPool.Dequeue()
            : new StringBuilder(20);
    }

    private void ReleaseStringBuilder(StringBuilder sb)
    {
        if (_stringBuilderPool.Count < POOL_SIZE)
        {
            sb.Clear();
            _stringBuilderPool.Enqueue(sb);
        }
    }

    private void InitializeAmmoTypeMap()
    {
        ammoTypeMap = new Dictionary<AmmoType, List<WeaponAmmo>>();
        foreach (WeaponAmmo ammo in weaponAmmos)
        {
            if (!ammoTypeMap.ContainsKey(ammo.ammoType))
            {
                ammoTypeMap[ammo.ammoType] = new List<WeaponAmmo>();
            }
            ammoTypeMap[ammo.ammoType].Add(ammo);
        }
    }

    private int HandleScrollInput(float scrollValue)
    {
        int newIndex = selectedWeaponIndex + (scrollValue > 0 ? 1 : -1);

        // Wrap around
        if (newIndex >= weaponCount) return 0;
        if (newIndex < 0) return weaponCount - 1;

        return newIndex;
    }

    private void SelectWeapon(int index)
    {
        if (!IsValidWeaponIndex(index)) return;

        selectedWeaponIndex = index;

        for (int i = 0; i < weaponCount; i++)
        {
            weaponTransforms[i].gameObject.SetActive(i == selectedWeaponIndex);
        }

        UpdateAmmoDisplay();
    }

    private bool IsValidWeaponIndex(int index)
    {
        return index >= 0 && index < weaponCount;
    }

    public void ValidateInitialWeapon()
    {
        if (weaponCount == 0)
        {
            Debug.LogError("No weapons found!");
            return;
        }

        selectedWeaponIndex = Mathf.Clamp(selectedWeaponIndex, 0, weaponCount - 1);
    }

    private void UpdateAmmoDisplay()
    {
        if (ammoText == null || !IsValidWeaponIndex(selectedWeaponIndex)) return;

        WeaponAmmo currentWeapon = weaponAmmos[selectedWeaponIndex];
        var sb = GetStringBuilder();

        try
        {
            sb.Append(currentWeapon.currentAmmo)
              .Append('/')
              .Append(currentWeapon.maxAmmo);
            ammoText.text = sb.ToString();
        }
        finally
        {
            ReleaseStringBuilder(sb);
        }

        UpdateAmmoColor(currentWeapon);
    }

    private void UpdateAmmoColor(WeaponAmmo weapon)
    {
        if (weapon.currentAmmo == 0)
        {
            ammoText.color = criticalColor;
        }
        else if (weapon.currentAmmo <= weapon.maxAmmo * warningThreshold)
        {
            ammoText.color = warningColor;
        }
        else
        {
            ammoText.color = normalColor;
        }
    }

    public void UseAmmo(int amount = 1)
    {
        if (IsValidWeaponIndex(selectedWeaponIndex))
        {
            WeaponAmmo currentWeapon = weaponAmmos[selectedWeaponIndex];
            currentWeapon.currentAmmo = Mathf.Max(0, currentWeapon.currentAmmo - amount);
            UpdateAmmoDisplay();
        }
    }

    public bool HasAmmo()
    {
        return IsValidWeaponIndex(selectedWeaponIndex) &&
               weaponAmmos[selectedWeaponIndex].currentAmmo > 0;
    }

    void HandleAmmoPickup(AmmoType ammoType, int amount)
    {
        RestoreAmmo(ammoType, amount);
    }

    public int RestoreAmmo(AmmoType ammoType, int ammo)
    {
        if (_ammoCache.TryGetValue(ammoType, out var weapons))
        {
            int totalAmmoAdded = 0;
            foreach (var weapon in weapons)
            {
                totalAmmoAdded += weapon.AddAmmo(ammo);
            }

            if (weapons.Any(w => Array.IndexOf(weaponAmmos, w) == selectedWeaponIndex))
            {
                UpdateAmmoDisplay();
            }

            return totalAmmoAdded;
        }
        return 0;
    }
}