using Unity.Netcode.Components;
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

    public Transform spawnPoint; // Reference to the spawn point

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
            //Debug.Log(gameObject.name + " is dead!");

            if (gameObject.CompareTag("Enemy"))
            {
                GetComponent<NetworkObject>().Despawn(); // Despawn the enemy object
            }
            else if (NetworkObject.CompareTag("Player"))
            {
                Debug.Log(gameObject.name + " is dead!");
                if (spawnPoint != null)
                {
                    //NetworkObject.transform.position = spawnPoint.position; // Move player to spawn point
                    health.Value = maxHealth; // Reset health to maxHealth
                    Debug.Log("Sending Teleport RPC to client.");
                    TeleportClientRpc(spawnPoint.position);
                }
                else
                {
                    Debug.LogError("Spawn point not assigned for player respawn.");
                }
            }
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
            if (spawnPoint == null)
            {
                GameObject spawnPointObject = GameObject.FindGameObjectWithTag("SpawnPoint");
                if (spawnPointObject != null)
                {
                    spawnPoint = spawnPointObject.transform;
                }
                else
                {
                    Debug.LogError("Spawn point not found. Please assign a spawn point in the inspector.");
                }
            }
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

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void TeleportClientRpc(Vector3 newPosition)
    {
        Debug.Log($"Server teleporting {gameObject.name} to position");
        var networkTransform = GetComponent<NetworkTransform>();
        if (networkTransform != null)
        {
            networkTransform.SetState(newPosition, Quaternion.identity, transform.localScale, true);
        }
        else
        {
            // Fallback if NetworkTransform is not present
            transform.position = newPosition;
        }
    }
}
