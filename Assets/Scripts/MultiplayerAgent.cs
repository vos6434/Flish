using FishNet.Object;
using FishNet.Connection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MultiplayerAgent : NetworkBehaviour
{
    [SerializeField] private List<Transform> positions = new List<Transform>();

    private NavMeshAgent agent;

    private int positionIndex = 0;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
            NextPosition();
    }

    [ServerRpc (RequireOwnership = false)]
    void NextPosition()
    {
        positionIndex++;
        if (positionIndex >= positions.Count)
            positionIndex = 0;

        agent.SetDestination(positions[positionIndex].position);

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            Debug.Log("Found player object for client: " + player.name);
        }
    }
}
