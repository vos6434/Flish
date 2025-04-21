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
            Shoot();
        }
        damageCooldown -= Time.deltaTime; // Decrease cooldown timer
    }
    private void Shoot()
    {
        Debug.Log("Shooting at player: " + playerTransform.name);

        Health playerHealth = playerTransform.GetComponentInParent<Health>();
        if (playerHealth == null)
        {
            playerHealth = playerTransform.GetComponent<Health>();
            //Debug.Log("Player health component not found in parent, checking player object directly.");
        }
        if (playerHealth != null)
        {
            if (damageCooldown <= 0f)
            {
            playerHealth.Damage(10f); // Adjust the damage value as needed
            Debug.Log("DAMAGING PLAYER");
            damageCooldown = 1f; // Reset cooldown
            }
        }
        
        
    }

    private void OggerEnter(Collider other)
    {
        
        if (other.CompareTag("Player"))
        {
            playerTransform = other.transform;
            Shoot();
        }
    }
}
