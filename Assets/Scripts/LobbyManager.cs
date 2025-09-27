using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Collections;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private int maxPlayers = 5;

    private NetworkList<LobbyPlayerData> lobbyPlayers;
    private NetworkVariable<bool> isGameStarting = new NetworkVariable<bool>(false);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            lobbyPlayers = new NetworkList<LobbyPlayerData>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        // Si es cliente, solicita unirse al lobby
        if (IsClient)
        {
            JoinLobbyServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void JoinLobbyServerRpc(ulong clientId)
    {
        if (lobbyPlayers.Count >= maxPlayers)
        {
            // Lobby lleno, desconectar cliente
            NetworkManager.Singleton.DisconnectClient(clientId);
            return;
        }

        var playerData = new LobbyPlayerData
        {
            clientId = clientId,
            isReady = false,
            playerName = $"Player_{clientId}"
        };

        lobbyPlayers.Add(playerData);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleReadyServerRpc(ulong clientId)
    {
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].clientId == clientId)
            {
                var data = lobbyPlayers[i];
                data.isReady = !data.isReady;
                lobbyPlayers[i] = data;
                break;
            }
        }

        CheckAllReady();
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc()
    {
        if (!IsServer || isGameStarting.Value) return;

        // Solo el host puede iniciar el juego
        if (NetworkManager.Singleton.LocalClientId != 0) return;

        if (CheckAllReady())
        {
            isGameStarting.Value = true;
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
    }

    private bool CheckAllReady()
    {
        if (lobbyPlayers.Count < 2) return false; 

        foreach (var player in lobbyPlayers)
        {
            if (!player.isReady) return false;
        }

        return true;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Cliente {clientId} conectado al lobby");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // Remover jugador desconectado
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].clientId == clientId)
            {
                lobbyPlayers.RemoveAt(i);
                break;
            }
        }
    }

    public List<LobbyPlayerData> GetLobbyPlayers()
    {
        var players = new List<LobbyPlayerData>();
        foreach (var player in lobbyPlayers)
        {
            players.Add(player);
        }
        return players;
    }

    public bool IsPlayerReady(ulong clientId)
    {
        foreach (var player in lobbyPlayers)
        {
            if (player.clientId == clientId)
                return player.isReady;
        }
        return false;
    }
}

public struct LobbyPlayerData : INetworkSerializable, System.IEquatable<LobbyPlayerData>
{
    public ulong clientId;
    public bool isReady;
    public FixedString32Bytes playerName;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref isReady);
        serializer.SerializeValue(ref playerName);
    }

    public bool Equals(LobbyPlayerData other)
    {
        return clientId == other.clientId &&
               isReady == other.isReady &&
               playerName.Equals(other.playerName);
    }

    public override bool Equals(object obj)
    {
        return obj is LobbyPlayerData other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = clientId.GetHashCode();
            hashCode = (hashCode * 397) ^ isReady.GetHashCode();
            hashCode = (hashCode * 397) ^ playerName.GetHashCode();
            return hashCode;
        }
    }
}