using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIGameManager : MonoBehaviour
{
    [Header("Login UI refs")]
    [SerializeField] private GameObject loginPanel;    // BackGround
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button submitButton;
    [Header("Lobby UI refs")]
    [SerializeField] private GameObject lobbyPanel;
    private UILobbyManager uiLobbyManager;
    void Awake()
    {
        if (submitButton != null) submitButton.onClick.AddListener(OnSubmitName);
        if (loginPanel != null) loginPanel.SetActive(false);
    }

    void OnEnable()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }

    void OnDisable()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
    }

    // Host: cuando levanta el server, este mismo proceso es también cliente local
    private void OnServerStarted()
    {
        ShowLoginAndReset();
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton != null &&
            clientId == NetworkManager.Singleton.LocalClientId)
        {
            // Verificar si estamos en escena de lobby o juego
            if (SceneManager.GetActiveScene().name == "LobbyScene")
            {
                ShowLobby();
            }
            else
            {
                ShowLoginAndReset();
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton != null &&
            clientId == NetworkManager.Singleton.LocalClientId)
        {
            // Al parar Client/Host, limpia y oculta
            ResetLoginUI();
            if (loginPanel != null) loginPanel.SetActive(false);
        }
    }
    private void ShowLobby()
    {
        if (lobbyPanel != null) 
        { 
            lobbyPanel.SetActive(true);
        }
        if (loginPanel != null)
        {
            loginPanel.SetActive(false);
        }

    }

    private void ShowLoginAndReset()
    {
        // Solo mostrar login si no estamos en el lobby
        if (SceneManager.GetActiveScene().name != "LobbyScene")
        {
            if (loginPanel != null) loginPanel.SetActive(true);
            ResetLoginUI();
        }
    }
    private void ResetLoginUI()
    {
        if (inputField != null) { inputField.text = ""; inputField.interactable = true; }
        if (submitButton != null) submitButton.interactable = true;
    }

    private void DisableLoginInputs()
    {
        if (inputField != null) inputField.interactable = false;
        if (submitButton != null) submitButton.interactable = false;
    }

    public void OnSubmitName()
    {
        if (inputField == null || GameManager.Instance == null || NetworkManager.Singleton == null)
        {
            return;
        }
         

        string accountID = inputField.text.Trim();
        if (string.IsNullOrEmpty(accountID))
        {
            return;
        }
        GameManager.Instance.RegisterPlayerServerRpc(accountID, NetworkManager.Singleton.LocalClientId);

        DisableLoginInputs();
        if (loginPanel != null)
        {
            loginPanel.SetActive(false);
        }

    }
}