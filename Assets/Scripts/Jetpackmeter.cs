using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class Jetpackmeter : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    private Text text;

    private void Start() {
        text = GetComponent<Text>();   
        /*
        if (NetworkManager.Singleton.ConnectedClients.ContainsKey(NetworkManager.Singleton.LocalClientId))
            text = NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject.GetComponent<Text>();

        */
        //text = NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject.GetComponent<Text>();
    }

    private void LateUpdate() {

        if (NetworkManager.Singleton.ConnectedClients.ContainsKey(NetworkManager.Singleton.LocalClientId))
            playerController = NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject.GetComponent<PlayerController>();
        else
            return;

        text.text = $"Fuel: {playerController.JetpackFuel.ToString("0")}";
    }
}
