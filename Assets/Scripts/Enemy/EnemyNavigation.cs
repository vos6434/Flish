using UnityEngine;
using UnityEngine.AI;

public class EnemyNavigation : MonoBehaviour
{

    Vector3 playerPosition; // The position of the player

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>(); // The navmeshagent component of the enemy
        GetComponent<FieldOfView>(); // The field of view component of the enemy
    }

    // Update is called once per frame
    void Update()
    {
        if (GetComponent<FieldOfView>().canSeePlayer) // If the enemy can see the player
        {
            playerPosition = GetComponent<FieldOfView>().playerRef.transform.position; // Get the position of the player
            GetComponent<NavMeshAgent>().SetDestination(playerPosition); // Set the destination of the enemy to the player position
        }
    }
}
