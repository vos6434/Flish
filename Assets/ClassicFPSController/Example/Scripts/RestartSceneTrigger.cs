using FishNet.Object;
using FishNet.Connection;
using UnityEngine;
using UnityEngine.SceneManagement;
using FishNet;

public class RestartSceneTrigger : NetworkBehaviour
{

    [SerializeField] private Transform teleportDestination;

    private void OnTriggerEnter(Collider other) {

        // Check if the colliding object has network object script
        NetworkObject networkObject = other.GetComponent<NetworkObject>();
        if (networkObject != null )
        {
            // Check if player object has teleport component
            TeleportPlayer teleportPlayer = networkObject.GetComponent<TeleportPlayer>();
            if (teleportPlayer != null)
            {
                Debug.Log("Teleport Player");
                teleportPlayer.TeleportPlayerServerRpc(teleportDestination.position);
                teleportPlayer.TeleportPlayerClientRpc(teleportDestination.position);
            }
        }
    }
}
