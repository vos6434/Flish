using Unity.Netcode;
using UnityEngine;

public class ShootPlayer : NetworkBehaviour
{
    [SerializeField] private GameObject bulletPrefab; // The bullet prefab to be shot at the player
    [SerializeField] private Transform bulletSpawnPoint; // The location where the bullet spawns
    [SerializeField] private float damageRate = 1f; // Bullets per second

    private Transform playerTransform; // The player location
    private float damageCooldown = 0f; // The damage cooldown set to 0

    // Update is called once per frame
    void Update()
    {
         FieldOfView fieldOfView = GetComponent<FieldOfView>(); // Get the field of view component
        if (fieldOfView != null)
        {
            if (fieldOfView.playerRef != null)
                playerTransform = fieldOfView.playerRef.transform; // Set the player location to the field of view player location

            else
                playerTransform = null; // Reset playerTransform if playerRef is null
        }

        if (fieldOfView.canSeePlayer) // If the player is seen
        {
            playerTransform = fieldOfView.playerRef.transform; // Set the player location to the field of view player location
            ShootServerRpc();
        }
        damageCooldown -= Time.deltaTime; // Decrease cooldown timer
    }

    [ServerRpc(RequireOwnership = false)] // ServerRpc to handle shooting on the server
    private void ShootServerRpc()
    {
        if (!IsServer) return; // Only execute on the server

        if (playerTransform == null) return; // Return if playerTransform is null

        if (damageCooldown <= 0f) // Check if the cooldown has expired
        {
            GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation); // Instantiate the bullet prefab
            bullet.GetComponent<bullet>().Owner = gameObject; // Set the owner of the bullet
            bullet.GetComponent<NetworkObject>().Spawn(); // Spawn the bullet on the server

            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>(); // Get the rigidbody of the bullet
            if (bulletRb != null)
            {
                Vector3 direction = (playerTransform.position - bulletSpawnPoint.position).normalized; // Calculate the direction to the player
                bulletRb.AddForce(direction * 10f, ForceMode.Impulse); // Add force to the bullet in the direction of the player
            }
            
            damageCooldown = 1f / damageRate; // Reset cooldown timer
        }
    }
}
