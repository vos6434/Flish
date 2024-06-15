using FishNet.Object;
using FishNet.Connection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportPlayer : NetworkBehaviour
{

    [ServerRpc]
    public void TeleportPlayerServerRpc(Vector3 destination)
    {
        if (!IsServerInitialized)    
            return;

        transform.position = destination;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
