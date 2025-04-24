using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Healthmeter : MonoBehaviour
{
    [SerializeField] private Health health; // The Health component
    private Text text; // The text component

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        text = GetComponent<Text>(); // Get the text component
    }

    // Update is called once per frame
    private void Update()
    {
        if (NetworkManager.Singleton.ConnectedClients.ContainsKey(NetworkManager.Singleton.LocalClientId)) // Check if the client is connected
            health = NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject.GetComponent<Health>(); // Get the Health component from the player object
        else
            return;
        text.text = $"Health: {health.health.Value.ToString("")}"; // Update the text with the player health value
    }
}
