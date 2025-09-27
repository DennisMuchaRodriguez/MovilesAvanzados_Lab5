using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class Player2 : NetworkBehaviour
{
    public NetworkVariable<FixedString32Bytes> accoundID = new();
    public NetworkVariable<int> health = new();
    public NetworkVariable<int> attack = new();
    public void SetData(PlayerData2 playerData)
    {
        accoundID.Value = playerData.accoundID;
        health.Value = playerData.health;
        attack.Value = playerData.attack;
        transform.position = playerData.position;
    }

    public override void OnNetworkDespawn()
    {
        //-> guardado?
        print("Me eh desconectado " + NetworkManager.Singleton.LocalClientId);
    }
}
public class PlayerData2
{
    public string accoundID;
    public Vector3 position;
    public int health;
    public int attack;

    public PlayerData2(string id, Vector3 pos, int hp, int atk)
    {
        accoundID = id;
        position = pos;
        health = hp;
        attack = atk;
    }
}
