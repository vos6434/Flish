using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Speedometer : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
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
            rb = NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject.GetComponent<Rigidbody>();
        else
            return;

        //rb = NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject.GetComponent<Rigidbody>();

        Vector3 hVel = rb.linearVelocity;
        hVel.y = 0;

        text.text = hVel.magnitude.ToString("0.0");
    }
}
