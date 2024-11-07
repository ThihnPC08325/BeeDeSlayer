using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float health;
    [SerializeField] private float defense;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxDefense = 30f;
    [SerializeField] private float chipSpeed = 2f;
    [SerializeField] private Image frontHealthBar;
    [SerializeField] private Image backHealthBar;
    [SerializeField] private Image DamagaOverlay;
    [SerializeField] private float duration;
    [SerializeField] private float fadeSpeed;

    private float durationTimer;
    private float lerpTimer;
    private DefenseSystem defenseSystem;
    private Transform enemy;

    private void OnEnable()
    {
        GameEvents.OnHealthPickup += RestoreHealth;
        GameEvents.OnPlayerHit += TakeDamage;
    }

    private void OnDisable()
    {
        GameEvents.OnHealthPickup -= RestoreHealth;
        GameEvents.OnPlayerHit -= TakeDamage;
    }

    private void Awake()
    {
        enemy = GameObject.FindGameObjectWithTag("Enemy").transform;
        defenseSystem = GetComponent<DefenseSystem>();
        health = maxHealth;
        defense = maxDefense;
    }

    // Start is called before the first frame update
    void Start()
    {
        DamagaOverlay.color = new Color(DamagaOverlay.color.r, DamagaOverlay.color.g, DamagaOverlay.color.b, 0);
    }

    // Update is called once per frame
    void Update()
    {
        health = Mathf.Clamp(health, 0, maxHealth);
        UpdateHealthUI();
        if (DamagaOverlay.color.a > 0)
        {
            if (health < 30)
            {
                return;
            }
            durationTimer += Time.deltaTime;
            if (durationTimer > duration)
            {
                float tempAlpha = DamagaOverlay.color.a;
                tempAlpha = Time.deltaTime * fadeSpeed;
                DamagaOverlay.color = new Color(DamagaOverlay.color.r, DamagaOverlay.color.g, DamagaOverlay.color.b, tempAlpha);
            }
        }

    }

    public void UpdateHealthUI()
    {
        float fillFrontHealth = frontHealthBar.fillAmount;
        float fillBackHealth = backHealthBar.fillAmount;
        float hFraction = health / maxHealth;
        if (fillBackHealth > hFraction)
        {
            frontHealthBar.fillAmount = hFraction;
            backHealthBar.color = Color.red;
            lerpTimer += Time.deltaTime;
            float percent = lerpTimer / chipSpeed;
            percent = percent * percent; //Cho hiệu ứng đẹp hơn
            backHealthBar.fillAmount = Mathf.Lerp(fillBackHealth, hFraction, percent);
        }

        if (fillFrontHealth < hFraction)
        {
            backHealthBar.color = Color.green;
            backHealthBar.fillAmount = hFraction;
            lerpTimer += Time.deltaTime;
            float percent = lerpTimer / chipSpeed;
            percent = percent * percent; //Cho hiệu ứng đẹp hơn
            frontHealthBar.fillAmount = Mathf.Lerp(fillFrontHealth, backHealthBar.fillAmount, percent);
        }
    }

    public void TakeDamage(float damage, float penetration)
    {
        float HealthBeforeDamage = health;
        float finalDamage = defenseSystem.CalculateDamage(damage, defense, penetration);
        health = Mathf.Max(health - finalDamage, 0);
        float healthActuallyDamage = health - HealthBeforeDamage;
        lerpTimer = 0f;
        durationTimer = 0f;
        DamagaOverlay.color = new Color(DamagaOverlay.color.r, DamagaOverlay.color.g, DamagaOverlay.color.b, 1);
    }

    public void RestoreHealth(float heal)
    {
        float HealthBeforePickup = health;
        health = Mathf.Min(health + heal, maxHealth);
        float healthActuallyHeal = health - HealthBeforePickup;
        lerpTimer = 0f;
    }
}