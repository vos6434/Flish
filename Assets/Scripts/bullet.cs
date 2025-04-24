using Unity.Netcode;
using UnityEngine;

public class bullet : NetworkBehaviour
{

    public float lifeTime = 5f;
    public float damage = 10f;
    public GameObject Owner {get; set;}

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Destroy(gameObject, lifeTime);
        if (IsServer)
        {
            Invoke(nameof(DespawnBullet), lifeTime);
        }
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("Owner: " + Owner.name);
    }
    void OnTriggerEnter(Collider collision)
    {
        if (!IsServer) return; // Only the server should handle collisions

        if (collision.gameObject.CompareTag("Player") && collision.gameObject != Owner)
        {
            Health playerHealth = collision.gameObject.GetComponentInParent<Health>();
            if (playerHealth != null)
            {
                playerHealth.Damage(damage);
                Debug.Log("Target health: " + playerHealth.health.Value);
            }
            //Destroy(gameObject);
            DespawnBullet();
        }
        else if (collision.gameObject.CompareTag("Enemy") && collision.gameObject != Owner)
        {
            //Destroy(gameObject);

            Health enemyHealth = collision.gameObject.GetComponentInParent<Health>();
            if (enemyHealth != null)
            {
                enemyHealth.Damage(damage);
                Debug.Log("Target health: " + enemyHealth.health.Value);
            }
            DespawnBullet();
        }
        else if (collision.gameObject != Owner)
        {
            DespawnBullet();
        }
    }

    private void DespawnBullet()
    {
        if (IsServer)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
