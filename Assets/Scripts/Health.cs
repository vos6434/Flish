using Unity.Netcode.Components;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine;

public class Health : NetworkBehaviour
{

    public Text healthText; // UI Text component for health
    public NetworkVariable<float> health = new NetworkVariable<float>
    (
        100f, // Default value of health
        NetworkVariableReadPermission.Everyone, // All clients can read
        NetworkVariableWritePermission.Server // Only the server can write
    );
    public float maxHealth = 100f; // The max health

    public Transform spawnPoint; // The spawnpoint location

    // Update is called once per frame
    void Update()
    {
        if (!IsServer) return; // Only the server should update health
        if (healthText != null) // Check if healthText is assigned
        {
            healthText.text = "Health: " + health.Value + "%"; // Update the UI text with current health
        }
        if (health.Value > maxHealth) health.Value = maxHealth; // make sure health does not go above max health

        if (health.Value <= 0) // if health is less than 0
        {

            if (gameObject.CompareTag("Enemy")) // if the game object is an enemy
            {
                GetComponent<NetworkObject>().Despawn(); // Despawn the enemy object
            }
            else if (NetworkObject.CompareTag("Player")) // if the game object is a player
            {
                Debug.Log(gameObject.name + " is dead!");
                if (spawnPoint != null) // if spawn point exists
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
    }

    
    public override void OnNetworkSpawn()
    {
        if (IsServer) // Only server executes
        {
            health.Value = maxHealth; // Set health to max health
            if (spawnPoint == null)
            {
                GameObject spawnPointObject = GameObject.FindGameObjectWithTag("SpawnPoint"); // Find the spawn point game object
                if (spawnPointObject != null)
                {
                    spawnPoint = spawnPointObject.transform; // Set spawnpoint to the game object transform
                }
                else
                {
                    Debug.Log("Spawn point not found.");
                }
            }
        }
    }
    

        public static void LogAllPlayersHealth() // List all players health in console
    {
        if (NetworkManager.Singleton.IsServer) // Check if server
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClients.Values) // Loop all clients
            {
                var playerHealth = client.PlayerObject.GetComponent<Health>(); // Get heath of all clients
                if (playerHealth != null)
                {
                    Debug.Log($"Player {client.ClientId} Health: {playerHealth.health.Value}"); // Print client health
                }
                else
                {
                    Debug.Log($"Player {client.ClientId} has no Health component.");
                }
            }
        }
    }

    public void Damage(float amount)
    {
        if (IsServer)
        {
            health.Value -= amount; // Decrease health
        }
    }
    public void Heal(float amount)
    {
        if (IsServer)
        {
            if (health.Value < maxHealth) // Prevent healing if already at max health
            {
                health.Value += amount; // Increase health
            }

        }
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)] // Call this on all clients
    private void TeleportClientRpc(Vector3 newPosition) // Teleport client to spawn point
    {
        Debug.Log($"Server teleporting {gameObject.name} to position");
        var networkTransform = GetComponent<NetworkTransform>(); // Get client network transform component
        if (networkTransform != null)
        {
            networkTransform.SetState(newPosition, Quaternion.identity, transform.localScale, true); // Set position of the network transform to the new position
        }
        else
        {
            transform.position = newPosition; // Transform position if network transform did not work
        }
    }
}
