using Unity.Netcode;
using UnityEngine;

public class ShootPlayer : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private float damageRate = 1f; // Bullets per second

    private Transform playerTransform;
    private float damageCooldown = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
         FieldOfView fieldOfView = GetComponent<FieldOfView>();
        if (fieldOfView != null)
        {
            if (fieldOfView.playerRef != null)
                playerTransform = fieldOfView.playerRef.transform;
            else
                playerTransform = null; // Reset playerTransform if playerRef is null
        }
        //fireCooldown -= Time.deltaTime;
        if (fieldOfView.canSeePlayer)
        {
            playerTransform = fieldOfView.playerRef.transform;
            ShootRpc();
        }
        damageCooldown -= Time.deltaTime; // Decrease cooldown timer
    }
    
    [ServerRpc]
    private void ShootRpc()
    {
        if (playerTransform == null) return; // Check if playerTransform is null

        if (damageCooldown <= 0f)
        {
            GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
            bullet.GetComponent<bullet>().Owner = gameObject;
            bullet.GetComponent<NetworkObject>().Spawn();

            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                Vector3 direction = (playerTransform.position - bulletSpawnPoint.position).normalized;
                bulletRb.AddForce(direction * 10f, ForceMode.Impulse); // Adjust the force as needed
            }
            
            damageCooldown = 1f / damageRate; // Reset cooldown timer
        }
    }

    private void OggerEnter(Collider other)
    {
        
        if (other.CompareTag("Player"))
        {
            playerTransform = other.transform;
            ShootRpc();
        }
    }
}
