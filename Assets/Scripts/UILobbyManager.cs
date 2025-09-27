using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UILobbyManager : MonoBehaviour
{
    [Header("Lobby UI References")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private Transform playersContainer;
    [SerializeField] private GameObject playerSlotPrefab;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TextMeshProUGUI lobbyStatusText;

    private Dictionary<ulong, GameObject> playerSlots = new Dictionary<ulong, GameObject>();

    private void Awake()
    {
        readyButton.onClick.AddListener(ToggleReady);
        startGameButton.onClick.AddListener(StartGame);

   
        startGameButton.gameObject.SetActive(NetworkManager.Singleton.IsHost);
    }

    private void Start()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        UpdateLobbyUI();
    }

    private void Update()
    {
        UpdateLobbyUI();
    }

    private void UpdateLobbyUI()
    {
        if (LobbyManager.Instance == null) return;

        var players = LobbyManager.Instance.GetLobbyPlayers();
        foreach (var player in players)
        {
            if (!playerSlots.ContainsKey(player.clientId))
            {
                GameObject slot = Instantiate(playerSlotPrefab, playersContainer);
                playerSlots[player.clientId] = slot;
            }

            GameObject slotObj = playerSlots[player.clientId];
            TextMeshProUGUI nameText = slotObj.transform.Find("PlayerName").GetComponent<TextMeshProUGUI>();
            GameObject readyIndicator = slotObj.transform.Find("ReadyIndicator").gameObject;

            nameText.text = player.playerName.ToString();
            readyIndicator.SetActive(player.isReady);
        }

        List<ulong> toRemove = new List<ulong>();
        foreach (var slot in playerSlots)
        {
            bool playerExists = false;
            foreach (var player in players)
            {
                if (player.clientId == slot.Key)
                {
                    playerExists = true;
                    break;
                }
            }

            if (!playerExists)
            {
                Destroy(slot.Value);
                toRemove.Add(slot.Key);
            }
        }

        foreach (ulong key in toRemove)
        {
            playerSlots.Remove(key);
        }

        
        int readyCount = 0;
        foreach (var player in players)
        {
            if (player.isReady) readyCount++;
        }

        lobbyStatusText.text = $"Jugadores: {players.Count}/5 - Listos: {readyCount}/{players.Count}";
        
        startGameButton.gameObject.SetActive(NetworkManager.Singleton.IsHost && players.Count > 1);

        startGameButton.interactable = (players.Count > 1 && readyCount == players.Count);
    }

    private void ToggleReady()
    {
        if (LobbyManager.Instance == null) return;

        LobbyManager.Instance.ToggleReadyServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    private void StartGame()
    {
        if (LobbyManager.Instance == null || !NetworkManager.Singleton.IsHost) return;

        LobbyManager.Instance.StartGameServerRpc();
    }

    public void ShowLobby()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
    }

    public void HideLobby()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
    }
}