    using System;
    using System.Collections.Generic;
    using Unity.Netcode;
    using UnityEngine;
using UnityEngine.SceneManagement;

    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; } 

        [SerializeField] private Transform player;
        [SerializeField] private GameObject buffP;
        [SerializeField] private GameObject enemyPrefab;

        public float spawnCount = 4f;
        public float currentCount = 0;

        public float enemySpawnInterval = 8f;
        public float enemySpawnTimer = 0f;
        public int maxEnemies = 5;
        private int currentEnemies = 0;
        public float enemySpawnRadius = 15f;

        private readonly List<GameObject> activeEnemies = new();
        private readonly HashSet<ulong> spawnedClients = new();

   
        private readonly Dictionary<string, PlayerData> playerStates = new();

    
        public event Action OnLocalClientConnected; 

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                if (NetworkManager.Singleton != null)
                {
                    NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                    NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected; 
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton != null &&
            clientId == NetworkManager.Singleton.LocalClientId)
        {
           
            if (SceneManager.GetActiveScene().name == "SampleScene") { }

        }
    }

    private void OnClientDisconnected(ulong clientId)
        {

            if (IsServer)
            {
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var netClient))
                {
                    var obj = netClient?.PlayerObject;
                    if (obj != null)
                    {
                        var ctrl = obj.GetComponent<SimplePlayerController>();
                        if (ctrl != null) SavePlayerState(ctrl);
                    }
                }
                spawnedClients.Remove(clientId);
            }
        }

        void Update()
        {
            if (IsServer && NetworkManager.Singleton.ConnectedClients.Count >= 1)
            {
                currentCount += Time.deltaTime;
                if (currentCount >= spawnCount)
                {
                    SpawnBuff();
                    currentCount = 0;
                }

                enemySpawnTimer += Time.deltaTime;
                if (enemySpawnTimer >= enemySpawnInterval && currentEnemies < maxEnemies)
                {
                    SpawnEnemy();
                    enemySpawnTimer = 0f;
                }
            }
        }

        void SpawnBuff()
        {

        if (buffP == null)
        {
            return;
        }
            Vector3 randomPos = new Vector3(UnityEngine.Random.Range(-12,12), 0.5f, UnityEngine.Random.Range(-10, 10));
            GameObject buff = Instantiate(buffP, randomPos, Quaternion.identity);
            var no = buff.GetComponent<NetworkObject>();
        if (no != null)
        {
            no.Spawn(true);
        }

        }

        void SpawnEnemy()
        {
            if (enemyPrefab == null) return;

            Vector3 spawnPos = GetRandomSpawnPosition();
            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            NetworkObject enemyNetObj = enemy.GetComponent<NetworkObject>();
            enemyNetObj.Spawn(true);

            NetworkEnemy networkEnemy = enemy.GetComponent<NetworkEnemy>();
            if (networkEnemy != null)
            {
                networkEnemy.SetGameManager(this);
            }

            activeEnemies.Add(enemy);
            currentEnemies++;

            Debug.Log($"Enemigo generado. Total: {currentEnemies}");
        }

        Vector3 GetRandomSpawnPosition()
        {
            Vector3 spawnPos;
            bool validPosition = false;
            int attempts = 0;

            do
            {
                spawnPos = new Vector3(
                    UnityEngine.Random.Range(-enemySpawnRadius, enemySpawnRadius),
                    0.5f,
                    UnityEngine.Random.Range(-enemySpawnRadius, enemySpawnRadius)
                );

                validPosition = IsPositionValid(spawnPos);
                attempts++;

                if (attempts > 10)
                {
                    validPosition = true;
                    Debug.LogWarning("No se encontró posición ideal para enemigo");
                }

            } while (!validPosition);

            return spawnPos;
        }

        bool IsPositionValid(Vector3 position)
        {
            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.PlayerObject != null)
                {
                    float distance = Vector3.Distance(position, client.PlayerObject.transform.position);
                    if (distance < 5f)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void EnemyDestroyed(GameObject enemy)
        {
            if (activeEnemies.Contains(enemy))
            {
                activeEnemies.Remove(enemy);
                currentEnemies--;
                Debug.Log($"Enemigo destruido. Total: {currentEnemies}");
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                foreach (GameObject enemy in activeEnemies)
                {
                    if (enemy != null)
                    {
                        enemy.GetComponent<NetworkObject>().Despawn();
                        Destroy(enemy);
                    }
                }

                activeEnemies.Clear();
                currentEnemies = 0;
            }
        }

    public override void OnNetworkSpawn()
    {
        Debug.Log("Current players: " + NetworkManager.Singleton.ConnectedClients.Count);
        Debug.Log("Local Client ID: " + NetworkManager.Singleton.LocalClientId);

        
        if (IsServer && SceneManager.GetActiveScene().name == "SampleScene")
        {
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                if (!spawnedClients.Contains(client.Key))
                {
                   
                    RegisterPlayerServerRpc($"player_{client.Key}", client.Key);
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
        public void InstancePlayerRpc(ulong ownerID)
        {
            if (spawnedClients.Contains(ownerID)) return;

            var playerP = Instantiate(player);
            var netObj = playerP.GetComponent<NetworkObject>();
            netObj.SpawnAsPlayerObject(ownerID, true);

            var ctrl = playerP.GetComponent<SimplePlayerController>();
            if (ctrl != null)
            {
                ctrl.PlayerID.Value = ownerID;
              
                if (string.IsNullOrEmpty(ctrl.accountID.Value.ToString()))
                {
                    ctrl.accountID.Value = new Unity.Collections.FixedString32Bytes($"guest_{ownerID}");
                    SavePlayerState(ctrl); 
                }
            }

            spawnedClients.Add(ownerID);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RegisterPlayerServerRpc(string accountID, ulong clientID)
        {
        if (spawnedClients.Contains(clientID))
        {
            return;
        }

            var playerP = Instantiate(player);
            var netObj = playerP.GetComponent<NetworkObject>();
            netObj.SpawnAsPlayerObject(clientID, true);

            var ctrl = playerP.GetComponent<SimplePlayerController>();
            if (ctrl != null)
            {
                ctrl.PlayerID.Value = clientID;

                
                ctrl.accountID.Value = accountID;

                if (playerStates.TryGetValue(accountID, out var data))
                {
                    ctrl.ApplyData(data);
                }
                else
                {
                    var startData = new PlayerData(accountID, playerP.position, 100, 25);
                    ctrl.ApplyData(startData);
                    playerStates[accountID] = startData;
                }
            }

            spawnedClients.Add(clientID);
        }

        
        public void SavePlayerState(SimplePlayerController ctrl)
        {
            var accID = ctrl.accountID.Value.ToString();
            if (string.IsNullOrEmpty(accID)) return;

            playerStates[accID] = new PlayerData(
                accID,
                ctrl.transform.position,
                ctrl.Health.Value,
                ctrl.Damage.Value
            );
        }
    }

    [System.Serializable]
    public struct PlayerData
    {
        public string accountID;
        public Vector3 position;
        public int health;
        public int attack;

        public PlayerData(string id, Vector3 pos, int hp, int atk)
        {
            accountID = id;
            position = pos;
            health = hp;
            attack = atk;
        }
    }
