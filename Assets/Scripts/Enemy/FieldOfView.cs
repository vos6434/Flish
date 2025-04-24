using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    public float radius; // The radius of the field of view
    [Range(0,360)] 
    public float angle; // The angle of the field of view

    public GameObject playerRef; // The player object

    public LayerMask targetMask; // The layer mask for the player target
    public LayerMask obstructionMask; // The layer mask for obstructions

    public bool canSeePlayer; // Whether the player is in the field of view

    private void Start()
    {
        StartCoroutine(FOVRoutine()); // Start the field of view check routine
    }

    private IEnumerator FOVRoutine() // The field of view routine
    {
        WaitForSeconds wait = new WaitForSeconds(0.2f); // Wait time between checks

        while (true) 
        {
            yield return wait; // Wait for the specified time
            FieldOfViewCheck();
        }
    }

    private void FieldOfViewCheck() // Check the field of view
    {
        Collider[] rangeChecks = Physics.OverlapSphere(transform.position, radius, targetMask); // Check for colliders within the radius

        if (rangeChecks.Length != 0) // If the range checks are not empty
        {
            Transform target = rangeChecks[0].transform; // Get the target transform
            Vector3 directionToTarget = (target.position - transform.position).normalized; // Calculate the direction to the target

            if (Vector3.Angle(transform.forward, directionToTarget) < angle / 2) // Check if the target is within the angle
            {
                float distanceToTarget = Vector3.Distance(transform.position, target.position); // Calculate the distance to the target

                if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstructionMask)) // Check for obstructions
                { 
                    canSeePlayer = true; // The player is seen
                    playerRef = target.gameObject.tag == "Player" ? target.gameObject : playerRef; // Set playerRef to the target if it is a player
                }
                else
                    canSeePlayer = false; // The player is not seen
            }
            else
                canSeePlayer = false; 
        }
        else if (canSeePlayer)
            canSeePlayer = false;
    }

}
