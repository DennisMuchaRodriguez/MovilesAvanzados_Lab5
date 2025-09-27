using Unity.Netcode;
using UnityEngine;

public class NetworkEnemy : NetworkBehaviour
{
    public float moveSpeed = 3f;
    public float detectionRange = 10f;
    private Transform targetPlayer;
    private GameManager gameManager;

    private NetworkVariable<int> health = new NetworkVariable<int>(3);

    public void SetGameManager(GameManager manager)
    {
        gameManager = manager;
    }

    void Update()
    {
        if (!IsServer) return;

        FindNearestPlayer();

        if (targetPlayer != null)
        {
            Vector3 targetPos = targetPlayer.position;
            targetPos.y = transform.position.y;

            Vector3 direction = (targetPos - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }

    void FindNearestPlayer()
    {
        float closestDistance = Mathf.Infinity;
        targetPlayer = null;

        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                float distance = Vector3.Distance(transform.position, client.PlayerObject.transform.position);
                if (distance < detectionRange && distance < closestDistance)
                {
                    closestDistance = distance;
                    targetPlayer = client.PlayerObject.transform;
                }
            }
        }
    }

    private void TakeDamage(int damage)
    {
        if (!IsServer) return;

        health.Value -= damage;

        if (health.Value <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (gameManager != null)
        {
            gameManager.EnemyDestroyed(gameObject);
        }

        GetComponent<NetworkObject>().Despawn();
        Destroy(gameObject, 0.1f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            Die();
        }
        else if (other.CompareTag("Projectile"))
        {
            TakeDamage(1);

            NetworkObject projNetObj = other.GetComponent<NetworkObject>();
            if (projNetObj != null)
            {
                projNetObj.Despawn();
                Destroy(other.gameObject, 0.1f);
            }
        }
    }
}