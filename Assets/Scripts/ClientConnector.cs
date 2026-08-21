using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using TMPro;

public class ClientConnector : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField m_IpInputField;
    [SerializeField] private GameObject m_ConnectionPanel;
    [SerializeField] private TMP_Text m_StatusText;

    [Header("Defaults")]
    [SerializeField] private string m_DefaultLaptopIP = "192.168.1.100";

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnected;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnected;
        }
    }

    public void ConnectToLaptopServer()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)
            return;

        if (NetworkManager.Singleton.TryGetComponent<UnityTransport>(out var transport))
        {
            string targetIp = string.IsNullOrWhiteSpace(m_IpInputField.text) ? m_DefaultLaptopIP : m_IpInputField.text.Trim();
            transport.ConnectionData.Address = targetIp;

            if (m_StatusText != null) m_StatusText.text = $"Connecting to {targetIp}...";
            NetworkManager.Singleton.StartClient();
        }
    }

    private void OnConnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId && m_ConnectionPanel != null)
        {
            m_ConnectionPanel.SetActive(false); // Hide panel on success
        }
    }

    private void OnDisconnected(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            if (m_ConnectionPanel != null) m_ConnectionPanel.SetActive(true);
            if (m_StatusText != null) m_StatusText.text = "Connection Failed / Disconnected";
        }
    }
}