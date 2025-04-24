using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class Jetpackmeter : MonoBehaviour
{
    [SerializeField] private PlayerController playerController; // Playercontroller script
    private Text text; // Text component

    private void Start()
    {
        text = GetComponent<Text>();   // Get the text component
    }

    private void LateUpdate() {

        if (NetworkManager.Singleton.ConnectedClients.ContainsKey(NetworkManager.Singleton.LocalClientId)) // Check if the client is connected
            playerController = NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject.GetComponent<PlayerController>(); // Get the PlayerController component from the player object
        else
            return;

        text.text = $"Fuel: {playerController.JetpackFuel.ToString("0")}"; // Update the text with the current fuel amount
    }
}
