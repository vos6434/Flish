using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartSceneTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other) {
        NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject.GetComponent<PlayerController>().transform.position = new Vector3(0, 2, 0);
    }
}
