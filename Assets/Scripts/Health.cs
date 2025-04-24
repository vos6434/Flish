using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine;

public class Health : NetworkBehaviour
{

    public Text healthText;
    //public float health;
    public NetworkVariable<float> health = new NetworkVariable<float>
    (
        100f, // Default value
        NetworkVariableReadPermission.Everyone, // All clients can read
        NetworkVariableWritePermission.Server // Only the server can write
    );
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
        if (!IsServer) return; // Only the owner should update health
        if (healthText != null)
        {
            healthText.text = "Health: " + health.Value + "%";
        }
        if (health.Value > maxHealth) health.Value = maxHealth;

        if (health.Value <= 0)
        {
            //Destroy(gameObject);
            // respawn player
            
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            LogAllPlayersHealth();
        }
        

        //Debug.Log("Health: " + health);
    }

    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            health.Value = maxHealth;
        }
    }
    

        public static void LogAllPlayersHealth()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClients.Values)
            {
                var playerHealth = client.PlayerObject.GetComponent<Health>();
                if (playerHealth != null)
                {
                    Debug.Log($"Player {client.ClientId} Health: {playerHealth.health.Value}");
                }
                else
                {
                    Debug.Log($"Player {client.ClientId} has no Health component.");
                }
            }
        }
        else
        {
            Debug.Log("Only the server can log all players' health.");
        }
    }

    public void Damage(float amount)
    {
        if (IsServer)
        {
            health.Value -= amount;
            isRegenerating = false;
            RegenerationTimer = 0f; // Reset the regeneration timer when taking damage
        }
    }
    public void Heal(float amount)
    {
        if (IsServer)
        {
            if (health.Value < maxHealth) // Prevent healing if already at max health
            {
                health.Value += amount;
                isRegenerating = false;
                RegenerationTimer = 0f; // Reset the regeneration timer when healing
            }

        }
    }
    public void StartRegeneration(float delay)
    {
        if (IsServer)
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
