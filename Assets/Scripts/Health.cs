using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine;

public class Health : NetworkBehaviour
{

    public Text healthText;
    public float health;
    public float maxHealth = 100f;
    public float healthRegeneration = 5f;
    private bool isRegenerating = false;
    private float RegenerationTimer = 0f;
    private float lastDamageTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return; // Only the owner should update health
        if (healthText != null)
        {
            healthText.text = "Health: " + health + "%";
        }
        if (health > maxHealth) health = maxHealth;

        if (health <= 0)
        {
            //Destroy(gameObject);
        }
        Debug.Log("Health: " + health);
    }
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            health = maxHealth;
        }
    }
    public void Damage(float amount)
    {
        if (IsOwner)
        {
            health -= amount;
            isRegenerating = false;
            RegenerationTimer = 0f; // Reset the regeneration timer when taking damage
        }
    }
    public void Heal(float amount)
    {
        if (IsOwner)
        {
            if (health < maxHealth) // Prevent healing if already at max health
            {
                health += amount;
                isRegenerating = false;
                RegenerationTimer = 0f; // Reset the regeneration timer when healing
            }

        }
    }
    public void StartRegeneration(float delay)
    {
        if (IsOwner)
        {
            if (isRegenerating == true && Time.time >= lastDamageTime + delay)
            {
                RegenerationTimer += Time.deltaTime;
                if (RegenerationTimer >= 1f)
                {
                    Heal(healthRegeneration * Time.deltaTime); // Heal over time
                    RegenerationTimer = 0f; // Reset the timer after each regeneration tick
                }
            }
        }
    }
}
