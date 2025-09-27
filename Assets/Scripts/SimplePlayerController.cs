using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class SimplePlayerController : NetworkBehaviour
{
    public NetworkVariable<ulong> PlayerID = new(writePerm: NetworkVariableWritePermission.Server);
    public ulong PlayerID2;

  
    public NetworkVariable<FixedString32Bytes> accountID = new(writePerm: NetworkVariableWritePermission.Server);

    public float speed = 6f;
    private Animator animator;
    private Rigidbody rb;
    public LayerMask groundLayer;
    public float jumpForce = 5f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    public float projectileForce = 20f;

    public NetworkVariable<int> Health = new(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> Damage = new(25, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!IsOwner) return;

        float x = Input.GetAxisRaw("Horizontal") * speed * Time.deltaTime;
        float y = Input.GetAxisRaw("Vertical") * speed * Time.deltaTime;

        if (x != 0 || y != 0)
        {
            MovePlayerServerRpc(x, y);
        }

        CheckGroundRpc();

        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            AnimatorSetTriggerRpc("Jump");
        }

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, ~0))
            {
                Vector3 lookDirection = (hit.point - transform.position);
                lookDirection.y = 0f;
                if (lookDirection.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(lookDirection);

                    Vector3 shootDirection = lookDirection.normalized;
                    ShootServerRpc(shootDirection);
                }
            }
        }
    }

    [ServerRpc]
    public void TakeDamageServerRpc(int damage)
    {
        if (!IsServer) return;

        Health.Value -= damage;
        if (Health.Value <= 0)
        {
            Respawn();
        }
        SaveMyStateOnServer();
    }

    private void Respawn()
    {
        Health.Value = 100;
        Vector3 randomPos = new Vector3(Random.Range(-7f, 7f), 1f, Random.Range(-7f, 7f));
        transform.position = randomPos;
        SaveMyStateOnServer();
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.1f, groundLayer);
    }

    [Rpc(SendTo.Server)]
    public void AnimatorSetTriggerRpc(string animationName)
    {
        if (rb != null)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        if (animator != null)
            animator.SetTrigger(animationName);
    }

    [ServerRpc]
    private void MovePlayerServerRpc(float x, float y)
    {
        if (rb != null)
        {
            rb.MovePosition(rb.position + new Vector3(x, 0, y));
        }
    }

    [Rpc(SendTo.Server)]
    public void CheckGroundRpc()
    {
        if (animator == null) return;

        if (IsGrounded())
        {
            animator.SetBool("Grounded", true);
            animator.SetBool("FreeFall", false);
        }
        else
        {
            animator.SetBool("Grounded", false);
            animator.SetBool("FreeFall", true);
        }
    }

    public void ApplyDamage(int damage)
    {
        if (!IsServer) return;

        Health.Value -= damage;
        if (Health.Value <= 0)
        {
            Respawn();
        }
        SaveMyStateOnServer();
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 direction)
    {
        if (projectilePrefab == null || firePoint == null) return;

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
        NetworkObject netObj = projectile.GetComponent<NetworkObject>();
        netObj.Spawn(true);

        Projectile proj = projectile.GetComponent<Projectile>();
        if (proj != null)
            proj.OwnerClientId = OwnerClientId;

        Rigidbody prb = projectile.GetComponent<Rigidbody>();
        if (prb != null)
        {
            prb.linearVelocity = direction * projectileForce;
        }
    }

    public void ApplyData(PlayerData data)
    {
        accountID.Value = data.accountID;
        Health.Value = data.health;
        Damage.Value = data.attack;
        transform.position = data.position;
    }

    public void SaveMyStateOnServer()
    {
        if (!IsServer) return;
        GameManager.Instance.SavePlayerState(this);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            SaveMyStateOnServer();
        }
    }
}
