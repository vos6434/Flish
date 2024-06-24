using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiplayerManager : NetworkBehaviour
{

    private NetworkManager networkManager;

    private void Awake()
    {
        networkManager = FindObjectOfType<NetworkManager>();

        if (networkManager == null)
        {
            Debug.LogError("No NetworkManager found in scene.");
            return;
        }

        networkManager.ServerManager.OnRemoteConnectionState += OnPlayerConnected;
    }

    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.ServerManager.OnRemoteConnectionState -= OnPlayerConnected;
        }
    }

    private void OnPlayerConnected(NetworkConnection conn, FishNet.Transporting.RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == FishNet.Transporting.RemoteConnectionState.Started)
        {
            Debug.Log("Player connected: " + conn.ClientId);

            /*
            GameObject playerObject = FindPlayerObject(conn.ClientId);
            if (playerObject != null)
            {
                playerObject.name = "Player (" + conn.ClientId + ")";
                Debug.Log("Renamed player object to: " + playerObject.name);
            }
            */
            StartCoroutine(RenamePlayerObjectWhenAvailable(conn.ClientId));
        }
    }

    private IEnumerator RenamePlayerObjectWhenAvailable(int clientId)
    {
        GameObject playerObject = null;
        while (playerObject == null)
        {
            playerObject = FindPlayerObject(clientId);
            yield return null; // Wait for the next frame
        }

        playerObject.name = "Player (" + clientId + ")";
        Debug.Log("Renamed player object to: " + playerObject.name);
    }

    private GameObject FindPlayerObject(int clientId)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            NetworkObject netObj = player.GetComponent<NetworkObject>();
            if (netObj != null && netObj.Owner.ClientId == clientId)
            {
                Debug.Log("Found player object for client: " + clientId);
                return player;
            }
        }

        return null;
    }

    public override void OnStartServer()
    {
        //Debug.Log("Player Joined?");
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
