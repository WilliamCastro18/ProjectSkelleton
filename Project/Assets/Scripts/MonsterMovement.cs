using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MonsterMovement : MonoBehaviour
{
    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float moveSpeed = 2f;
    public int patrolDestination;
    public float arrivalTolerance = 0.2f;
    public bool useVerticalMovement = false; // se true segue Y do patrolPoint, senão mantém Y atual

    [Header("Ground / Checks")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public Transform wallCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    private bool isGrounded;

    [Header("Physics")]
    public bool requireGroundedToMove = false;
    private Rigidbody2D rb;

    [Header("Debug")]
    public bool verboseLogs = false;

    private bool warnedAboutMissingGroundLayer = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (groundCheck == null)
        {
            Transform t = transform.Find("GroundCheck");
            if (t != null) groundCheck = t;
            else
            {
                GameObject g = new GameObject("GroundCheck");
                g.transform.SetParent(transform);
                g.transform.localPosition = new Vector3(0f, -0.5f, 0f);
                groundCheck = g.transform;
            }
        }

        if (wallCheck == null)
        {
            Transform t = transform.Find("WallCheck");
            if (t != null) wallCheck = t;
            else
            {
                GameObject w = new GameObject("WallCheck");
                w.transform.SetParent(transform);
                w.transform.localPosition = new Vector3(0.5f, 0f, 0f);
                wallCheck = w.transform;
            }
        }

        ValidatePatrolPoints();
        patrolDestination = Mathf.Clamp(patrolDestination, 0, Mathf.Max(0, patrolPoints?.Length - 1 ?? 0));
    }

    void Update()
    {
        if (!HasValidPatrol()) return;

        Vector2 currentPos = new Vector2(transform.position.x, transform.position.y);
        Vector2 targetPos = GetTargetPositionPreserveY(currentPos);

        if (Vector2.Distance(currentPos, targetPos) < arrivalTolerance)
        {
            AdvanceToNextValidPoint();
            if (verboseLogs) Debug.Log($"[MonsterMovement] chegada detectada -> novo destino {patrolDestination}");
        }
    }

    private void FixedUpdate()
    {
        if (!HasValidPatrol()) return;

        // ground check (compatível com Player.cs)
        bool groundCheckResult = true;
        if (groundCheck != null)
        {
            if ((int)groundLayer == 0)
            {
                groundCheckResult = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius) != null;
                if (requireGroundedToMove && !warnedAboutMissingGroundLayer)
                {
                    warnedAboutMissingGroundLayer = true;
                    Debug.LogWarning("[MonsterMovement] requireGroundedToMove=true mas groundLayer não configurada. Ignorando bloqueio até configurar groundLayer.");
                }
            }
            else
            {
                groundCheckResult = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
            }
        }
        isGrounded = groundCheckResult;
        bool effectiveRequireGrounded = requireGroundedToMove && (int)groundLayer != 0;
        if (effectiveRequireGrounded && !isGrounded)
        {
            if (verboseLogs) Debug.Log("[MonsterMovement] Não se move porque não está grounded.");
            return;
        }

        // movimento em direção ao patrol point (respeita opção vertical)
        Vector2 currentPos = rb != null ? rb.position : (Vector2)transform.position;
        Vector2 targetPos2 = GetTargetPositionPreserveY(currentPos);
        Vector2 next = Vector2.MoveTowards(currentPos, targetPos2, moveSpeed * Time.fixedDeltaTime);

        if (rb != null)
            rb.MovePosition(next);
        else
            transform.position = new Vector3(next.x, next.y, transform.position.z);

        bool walled = IsWalled();
        if (verboseLogs)
        {
            string rbInfo = rb != null ? $"rb.bodyType={rb.bodyType} rb.gravity={rb.gravityScale}" : "rb=null";
            Debug.Log($"[MonsterMovement - DEBUG] cur={currentPos} target={targetPos2} next={next} grounded={isGrounded} walled={walled} {rbInfo}");
        }

        if (walled)
        {
            AdvanceToNextValidPoint();
            if (verboseLogs) Debug.Log("[MonsterMovement] Parede detectada -> invertendo direção.");
        }
    }

    private Vector2 GetTargetPositionPreserveY(Vector2 currentPos)
    {
        Transform tp = patrolPoints[patrolDestination];
        if (tp == null) return currentPos;
        Vector2 target = tp.position;
        if (!useVerticalMovement) target.y = currentPos.y;
        return target;
    }

    private void AdvanceToNextValidPoint()
    {
        if (!HasValidPatrol()) return;

        int attempts = 0;
        int len = patrolPoints.Length;
        do
        {
            patrolDestination = (patrolDestination + 1) % len;
            attempts++;
            if (attempts > len) break; // protecção contra loop infinito
        } while (patrolPoints[patrolDestination] == null || !patrolPoints[patrolDestination].gameObject.activeInHierarchy);
    }

    private bool HasValidPatrol()
    {
        if (patrolPoints == null || patrolPoints.Length < 1) 
        {
            if (verboseLogs) Debug.LogWarning("[MonsterMovement] patrolPoints não atribuído ou vazio.");
            return false;
        }
        foreach (var p in patrolPoints)
        {
            if (p != null && p.gameObject.activeInHierarchy) return true;
        }
        if (verboseLogs) Debug.LogWarning("[MonsterMovement] nenhum patrolPoint válido (todos nulos ou inativos).");
        return false;
    }

    private void ValidatePatrolPoints()
    {
        if (patrolPoints == null) return;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null && verboseLogs)
                Debug.LogWarning($"[MonsterMovement] patrolPoints[{i}] é nulo.");
            else if (patrolPoints[i] != null && !patrolPoints[i].gameObject.activeInHierarchy && verboseLogs)
                Debug.LogWarning($"[MonsterMovement] patrolPoints[{i}] inativo: {patrolPoints[i].name}");
        }
    }

    private bool IsWalled()
    {
        if (wallCheck == null) return false;

        Collider2D[] cols = Physics2D.OverlapCircleAll(wallCheck.position, 0.2f, wallLayer);
        if (cols == null || cols.Length == 0) return false;

        foreach (var col in cols)
        {
            if (col == null) continue;
            if (col.isTrigger) continue;
            if (col.GetComponentInParent<HealthManager>() != null || col.GetComponentInChildren<HealthManager>() != null) continue;
            if (col.gameObject == gameObject) continue;
            if (verboseLogs) Debug.Log($"[IsWalled] collider detected: {col.gameObject.name} layer={LayerMask.LayerToName(col.gameObject.layer)} isTrigger={col.isTrigger}");
            return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
        if (wallCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(wallCheck.position, 0.2f);
        }

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null) Gizmos.DrawSphere(patrolPoints[i].position, 0.05f);
            }
        }
    }
}
