using FishNet.Object;
using FishNet.Connection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportPlayer : NetworkBehaviour
{

    [ServerRpc]
    public void TeleportPlayerServerRpc(Vector3 destination, NetworkConnection conn = null)
    {
        //if (!IsServerInitialized)    
            //return;

        transform.position = destination;

        TeleportPlayerClientRpc(destination, conn);
    }

    [ObserversRpc]
    public void TeleportPlayerClientRpc(Vector3 destination, NetworkConnection conn = null)
    {
        if (IsOwner)
        {
            transform.position = destination;
        }
    }
}
