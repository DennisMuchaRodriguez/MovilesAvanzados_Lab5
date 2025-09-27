using Unity.Netcode;
using UnityEngine;

public class RandomBuff : NetworkBehaviour
{
    void Start()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SimplePlayerController player = other.GetComponent<SimplePlayerController>();

            if (player != null)
            {
                int extraDamage = Random.Range(1, 10);

                player.Damage.Value += extraDamage;

             
            }

            GetComponent<NetworkObject>().Despawn(true);
        }
    }

    [Rpc(SendTo.Server)]
    private void AddBuffPlayerRpc(ulong playerID)
    {

        GetComponent<NetworkObject>().Despawn(true);
    }
}