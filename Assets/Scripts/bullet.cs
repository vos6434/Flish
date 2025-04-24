using Unity.Netcode;
using UnityEngine;

public class bullet : NetworkBehaviour
{

    public float lifeTime = 5f; // Time in seconds before the bullet despawns
    public float damage = 10f; // Damage dealt by the bullet
    public GameObject Owner {get; set;} // The gameobject who shot the bullet

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (IsServer)
        {
            Invoke(nameof(DespawnBullet), lifeTime); // Set bullet despawn after time
        }
    }

    void OnTriggerEnter(Collider collision)
    {
        if (!IsServer) return; // Only the server should handle collisions

        if (collision.gameObject.CompareTag("Player") && collision.gameObject != Owner) // Check if the collided object is a player and not the owner of the bullet
        {
            Health playerHealth = collision.gameObject.GetComponentInParent<Health>(); // Get the Health component of the player
            if (playerHealth != null) // Check if the player has a healt component
            {
                playerHealth.Damage(damage); // Damage the player
                //Debug.Log("Target health: " + playerHealth.health.Value);
            }
            DespawnBullet();
        }

        else if (collision.gameObject.CompareTag("Enemy") && collision.gameObject != Owner) // Check if the collided object is an enemy and not the owner of the bullet
        {
            Health enemyHealth = collision.gameObject.GetComponentInParent<Health>(); // Get the Health component of the enemy
            if (enemyHealth != null) // Check if the enemy has a health component
            {
                enemyHealth.Damage(damage); // Damage the enemy
                //Debug.Log("Target health: " + enemyHealth.health.Value);
            }
            DespawnBullet();
        }

        else if (collision.gameObject != Owner)
        {
            DespawnBullet();
        }
    }

    private void DespawnBullet() // Despawn the bullet
    {
        if (IsServer)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
