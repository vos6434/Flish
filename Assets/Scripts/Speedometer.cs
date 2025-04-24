using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Speedometer : MonoBehaviour
{
    [SerializeField] private Rigidbody rb; // Riigidbody component
    private Text text; // Text component

    private void Start()
    {
        text = GetComponent<Text>();   
    }

    private void LateUpdate() {

        if (NetworkManager.Singleton.ConnectedClients.ContainsKey(NetworkManager.Singleton.LocalClientId)) // Check if the client is connected
            rb = NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject.GetComponent<Rigidbody>(); // Get the Rigidbody component of the local player object
        else
            return;

        Vector3 hVel = rb.linearVelocity; // Get the linear velocity of the Rigidbody
        hVel.y = 0; // set the y velocity to 0

        text.text = hVel.magnitude.ToString("0.0"); // Set the text to the magnitude of the velocity
    }
}
