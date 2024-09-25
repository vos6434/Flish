using UnityEngine;
using UnityEngine.AI;

public class EnemyNavigation : MonoBehaviour
{

    Vector3 playerPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        ///GetComponent<FieldOfView>().playerRef.transform.position = playerPosition;
        GetComponent<FieldOfView>();
    }

    // Update is called once per frame
    void Update()
    {
        if (GetComponent<FieldOfView>().canSeePlayer)
        {
            playerPosition = GetComponent<FieldOfView>().playerRef.transform.position;
            GetComponent<NavMeshAgent>().SetDestination(playerPosition);
        }
    }
}
