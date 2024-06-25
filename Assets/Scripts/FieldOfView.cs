using FishNet.Connection;
using FishNet.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FieldOfView : NetworkBehaviour
{
    public float radius;
    [Range(0,360)]
    public float angle;

    public GameObject playerRef;

    public LayerMask targetMask;
    public LayerMask obstructionMask;

    public bool canSeePlayer;

    private NavMeshAgent agent;

    /*
    private void Start()
    {
        playerRef = GameObject.FindGameObjectWithTag("Player");
        StartCoroutine(FOVRoutine());
    }
    */

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    //public override void OnStartClient()
    public override void OnStartServer()
    //public override void OnStartNetwork()
    //public override void OnOwnershipServer(NetworkConnection prevOwner)
    {
        //base.OnOwnershipServer(prevOwner);
        //if (base.Owner.IsLocalClient)
        //playerRef = GameObject.FindWithTag("Player");
        //playerRef = GameObject.FindGameObjectWithTag("Player");
        StartCoroutine(FOVRoutine());
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        //playerRef = GameObject.FindGameObjectWithTag("Player");
        this.enabled = false;
    }


    private IEnumerator FOVRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.2f);

        while (true)
        {
            yield return wait;
            FieldOfViewCheck();
        }
    }

    private void FindClosestPlayerToAgent()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Debug.Log("Number of players in scene: " + players.Length);
        float shortestDistance = Mathf.Infinity;
        GameObject closestPlayer = null;

        foreach (GameObject player in players)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (distanceToPlayer < shortestDistance)
            {
                shortestDistance = distanceToPlayer;
                closestPlayer = player;
            }
        }

        playerRef = closestPlayer;
    }

    private void FieldOfViewCheck()
    {
        Collider[] rangeChecks = Physics.OverlapSphere(transform.position, radius, targetMask);

        if (rangeChecks.Length != 0)
        {
            Transform target = rangeChecks[0].transform;
            Vector3 directionToTarget = (target.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, directionToTarget) < angle / 2)
            {
                float distanceToTarget = Vector3.Distance(transform.position, target.position);

                if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstructionMask))
                {
                    FindClosestPlayerToAgent();
                    canSeePlayer = true;
                    //Debug.Log("FOUND PLAYER");
                    Debug.Log("Following " + playerRef.gameObject.name);
                    // Set agent destination to player
                    agent.SetDestination(playerRef.gameObject.transform.position);
                }
                else
                    canSeePlayer = false;
            }
            else
                canSeePlayer = false;
        }
        else if (canSeePlayer)
            canSeePlayer = false;
    }
}
