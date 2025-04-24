using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Healthmeter : MonoBehaviour
{
    [SerializeField] private Health health;
    private Text text;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        text = GetComponent<Text>();   
    }

    // Update is called once per frame
    private void Update()
    {
        if (NetworkManager.Singleton.ConnectedClients.ContainsKey(NetworkManager.Singleton.LocalClientId))
            health = NetworkManager.Singleton.ConnectedClients[NetworkManager.Singleton.LocalClientId].PlayerObject.GetComponent<Health>();
        else
            return;
        text.text = $"Health: {health.health.Value.ToString("")}";
    }
}
