using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    [Header("Stats / Movement")]
    public int health = 5;
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public float variableJumpMultiplier = 0.5f;
    public MonsterMovement monsterMovement;

    [Header("Ground / Wall checks")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public Transform wallCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    [Header("Audio - SFX")]
    public AudioClip jumpClip;
    [Range(0f,1f)] public float jumpVolume = 1f;
    public AudioClip hitClip;
    [Range(0f,1f)] public float hitVolume = 1f;
    public AudioClip runClip;
    [Range(0f,1f)] public float runVolume = 1f;
    public float runStartThreshold = 0.1f;

    [Header("Damage / Invulnerability")]
    public float invulnerabilityTime = 1f;
    public Vector2 damageKnockback = new Vector2(0f, 6f);
    // knockback mais forte quando a fonte do dano é conhecida
    public Vector2 strongDamageKnockback = new Vector2(6f, 8f);
    // duração (segundos) que o jogador fica sem controle horizontal após knockback
    public float knockbackDuration = 0.25f;

    [Header("Dash / WallJump")]
    [SerializeField] private TrailRenderer tr;
    private bool canDash = true;
    private bool isDashing;
    private float dashingPower = 10f;
    private float dashingTime = 0.2f;
    private float dashingCooldown = 1f;
    private bool isWallJumping;
    private float wallJumpingDirection;
    private float wallJumpingTime = 0.2f;
    private float wallJumpingCounter;
    private float wallJumpingDuration = 0.4f;
    private Vector2 wallJumpingPower = new Vector2(6f, 10f);
    public float wallSlideSpeed = 2f;

    // novo: multiplicador para pulo vertical reduzido quando saltando "grudado" ou em direção à parede
    public float smallWallJumpMultiplier = 0.7f;

    // novo: tempo que o jogador fica "grudado" antes de começar a deslizar
    public float wallStickTime = 1f;
    private float wallStickTimer = 0f;
    private bool wasTouchingWall = false;

    // --- debug fields ---
    [Header("Debug")]
    public float debugOverlapRadius = 0.6f; // raio usado no debug de Overlap
    // --------------------

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float horizontal;
    private float vertical;
    private bool isFacingRight = true;
    private bool isGrounded;
    private bool isWallSliding;
    private Vector2 respawnPoint;

    private AudioSource sfxSource;
    private AudioSource runAudioSource;
    private bool isInvulnerable = false;

    // knockback state
    private bool isKnockedBack = false;
    private float knockbackTimer = 0f;

    // double jump
    private bool hasDoubleJumped = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        respawnPoint = transform.position;

        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        runAudioSource = gameObject.AddComponent<AudioSource>();
        runAudioSource.playOnAwake = false;
        runAudioSource.loop = true;
        runAudioSource.spatialBlend = 1f;
        runAudioSource.volume = runVolume;

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
    }

    void Update()
    {
        // DEBUG: pressione L para listar colliders próximos (Overlap) e C para listar contatos atuais do Rigidbody
        if (Input.GetKeyDown(KeyCode.L))
        {
            DebugLogNearbyColliders();
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            DebugLogContacts();
        }

        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        // decrementa timer de knockback
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0f)
            {
                isKnockedBack = false;
                knockbackTimer = 0f;
            }
        }

        // Jump input (ground)
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            hasDoubleJumped = false;
            if (runAudioSource.isPlaying) runAudioSource.Stop();
            if (jumpClip != null && sfxSource != null) sfxSource.PlayOneShot(jumpClip, jumpVolume);
        }

        if (Input.GetKeyUp(KeyCode.Space) && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * variableJumpMultiplier);
        }

        WallSlide();
        WallJump();

        // Double jump (executado após WallJump para não conflitar com wall-jump)
        if (Input.GetKeyDown(KeyCode.Space) && !isGrounded && !isWallJumping && !hasDoubleJumped)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            hasDoubleJumped = true;
            if (runAudioSource.isPlaying) runAudioSource.Stop();
            if (jumpClip != null && sfxSource != null) sfxSource.PlayOneShot(jumpClip, jumpVolume);
        }

        // permite flip mais cedo: agora FlipIfNeeded é aplicado normalmente (controle horizontal pode tomar efeito cedo)
        if (!isWallJumping)
        {
            FlipIfNeeded();
        }
        else
        {
            // mesmo durante wall-jump permita alterarmos facing se o jogador já estiver pressionando direção oposta
            if ((isFacingRight && horizontal < 0f) || (!isFacingRight && horizontal > 0f))
            {
                FlipIfNeeded();
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            StartCoroutine(Dash());
        }

        bool isMoving = Mathf.Abs(horizontal) > runStartThreshold && isGrounded && !isDashing;
        if (isMoving && runClip != null)
        {
            if (!runAudioSource.isPlaying)
            {
                runAudioSource.clip = runClip;
                runAudioSource.volume = runVolume;
                runAudioSource.Play();
            }
        }
        else
        {
            if (runAudioSource.isPlaying) runAudioSource.Stop();
        }
    }

    private void FixedUpdate()
    {
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // reset double jump when grounded
        if (isGrounded)
        {
            hasDoubleJumped = false;
            // reset stick state ao pousar
            wallStickTimer = 0f;
            wasTouchingWall = false;
        }

        if (isDashing) return;

        // Permite ao jogador modificar a direção horizontal mais cedo, mesmo durante um wall-jump.
        // Isso garante que o salto diagonal (ou qualquer wall-jump) possa ser alterado mid-air.
        if (!isKnockedBack)
        {
            rb.linearVelocity = new Vector2(horizontal * moveSpeed, rb.linearVelocity.y);
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

            // ignora triggers (não devem bloquear movimento)
            if (col.isTrigger) continue;

            // ignora inimigos (que possuem HealthManager)
            if (col.GetComponentInParent<HealthManager>() != null || col.GetComponentInChildren<HealthManager>() != null)
                continue;

            // ignora colisor do próprio jogador (por segurança)
            if (col.gameObject == gameObject) continue;

            // log para identificar o que está bloqueando
            Debug.Log($"[IsWalled] collider detected: {col.gameObject.name} layer={LayerMask.LayerToName(col.gameObject.layer)} isTrigger={col.isTrigger}");

            return true;
        }

        return false;
    }

    private void WallSlide()
    {
        // comportamento: quando tocar a parede e estiver no ar, fica "grudado" por wallStickTime segundos (sem deslizar)
        // se o jogador pressionar direção oposta à parede enquanto grudado -> cancela o "stick" e começa a deslizar imediatamente

        bool touchingWall = IsWalled() && !isGrounded;
        float wallDir = 0f;
        if (wallCheck != null) wallDir = Mathf.Sign(wallCheck.position.x - transform.position.x);

        if (touchingWall)
        {
            // novo toque -> inicializa timer
            if (!wasTouchingWall)
            {
                wallStickTimer = wallStickTime;
            }
            wasTouchingWall = true;

            // se o jogador estiver pressionando na direção contrária à parede, quebra o "stick"
            if (Mathf.Abs(horizontal) > 0.1f && wallDir != 0f && Mathf.Sign(horizontal) != wallDir)
            {
                wallStickTimer = 0f;
            }

            if (wallStickTimer > 0f)
            {
                // permanece "grudado" — cancela deslocamento vertical para evitar deslize
                wallStickTimer -= Time.deltaTime;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                isWallSliding = false;
                return;
            }

            // quando o timer expira, começa o sliding normal (limita velocidade de queda)
            isWallSliding = true;
            float cappedY = Mathf.Max(rb.linearVelocity.y, -Mathf.Abs(wallSlideSpeed));
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, cappedY);
            return;
        }
        else
        {
            wasTouchingWall = false;
            wallStickTimer = 0f;
            // padrão: se estiver caindo e não em parede, aplicar clamp normal (já feito por outras rotinas), aqui apenas zera isWallSliding
            isWallSliding = false;
        }
    }

    private void WallJump()
    {
        if (isWallSliding)
        {
            isWallJumping = false;
            wallJumpingDirection = -transform.localScale.x;
            wallJumpingCounter = wallJumpingTime;
            CancelInvoke(nameof(StopWallJumping));
        }
        else
        {
            wallJumpingCounter -= Time.deltaTime;
        }

        // Trata o input de wall-jump (unificado)
        if (Input.GetKeyDown(KeyCode.Space) && wallJumpingCounter > 0f)
        {
            float inputHor = horizontal;
            bool hasDirectionalInput = Mathf.Abs(inputHor) > 0.1f;
            float inputVer = vertical;
            bool lookingUp = inputVer > 0.1f;

            // direção aproximada da parede (se existir)
            float wallDir = 0f;
            if (wallCheck != null) wallDir = Mathf.Sign(wallCheck.position.x - transform.position.x);

            bool touchingWall = IsWalled();
            bool facingTowardWall = wallDir != 0f && Mathf.Sign(transform.localScale.x) == wallDir;

            isWallJumping = true;
            wallJumpingCounter = 0f;
            hasDoubleJumped = false;

            // Caso A: se estiver grudado na parede e estiver olhando para ela ou olhando pra cima -> pulo vertical reduzido
            if (touchingWall && (facingTowardWall || lookingUp))
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * smallWallJumpMultiplier);
            }
            else
            {
                // Caso B: se não houver input horizontal -> salto diagonal na direção contrária à que está olhando
                if (!hasDirectionalInput)
                {
                    float dir = -Mathf.Sign(transform.localScale.x); // oposto ao que está olhando
                    rb.linearVelocity = new Vector2(dir * wallJumpingPower.x, wallJumpingPower.y);
                    // NÃO invertemos o facing automaticamente: jogador continua olhando para onde estava
                }
                else
                {
                    // Caso C: input horizontal presente -> normal wall-jump na direção do input
                    float dir = Mathf.Sign(inputHor);
                    rb.linearVelocity = new Vector2(dir * wallJumpingPower.x, wallJumpingPower.y);

                    // ajusta facing se necessário
                    bool shouldFaceRight = dir > 0f;
                    if ((shouldFaceRight && transform.localScale.x < 0f) || (!shouldFaceRight && transform.localScale.x > 0f))
                    {
                        Vector3 localScale = transform.localScale;
                        localScale.x *= -1f;
                        transform.localScale = localScale;
                        isFacingRight = localScale.x > 0f;
                    }
                }
            }

            // Permite alteração de trajetória cedo (FixedUpdate já aplica horizontal conforme input).
            Invoke(nameof(StopWallJumping), wallJumpingDuration);
        }

        if (IsWalled() && !isGrounded)
        {
            float wallDirTmp = Mathf.Sign(wallCheck.position.x - transform.position.x);
            wallJumpingDirection = -wallDirTmp;
            wallJumpingCounter = wallJumpingTime;
        }
        else
        {
            wallJumpingCounter -= Time.deltaTime;
        }
    }

    private void StopWallJumping()
    {
        isWallJumping = false;
    }

    private void FlipIfNeeded()
    {
        if ((isFacingRight && horizontal < 0f) || (!isFacingRight && horizontal > 0f))
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(transform.localScale.x * dashingPower, 0f);
        if (tr != null) tr.emitting = true;
        yield return new WaitForSeconds(dashingTime);
        if (tr != null) tr.emitting = false;
        rb.gravityScale = originalGravity;
        isDashing = false;
        yield return new WaitForSeconds(dashingCooldown);
        canDash = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Damage"))
        {
            // passa a posição do colisor que causou o dano para aplicar knockback direcional
            TakeDamage(1, collision.transform.position);
        }
    }

    // --- debug helpers ---
    private void DebugLogNearbyColliders()
    {
        Vector2 pos = (wallCheck != null) ? (Vector2)wallCheck.position : (Vector2)transform.position;
        Collider2D[] cols = Physics2D.OverlapCircleAll(pos, debugOverlapRadius);
        Debug.Log($"[DEBUG Overlap] center={pos} radius={debugOverlapRadius} count={cols?.Length ?? 0}");
        if (cols == null || cols.Length == 0) return;
        foreach (var col in cols)
        {
            if (col == null) continue;
            string rbInfo = col.attachedRigidbody != null ? col.attachedRigidbody.bodyType.ToString() : "none";
            Vector2 offset = Vector2.zero;
            if (col is BoxCollider2D bc) offset = bc.offset;
            else if (col is CircleCollider2D cc) offset = cc.offset;
            else if (col is CapsuleCollider2D cap) offset = cap.offset;
            Debug.Log($"[DEBUG Overlap] name={col.gameObject.name} layer={LayerMask.LayerToName(col.gameObject.layer)} isTrigger={col.isTrigger} rb={rbInfo} offset={offset} bounds={col.bounds}");
        }
    }

    private void DebugLogContacts()
    {
        if (rb == null)
        {
            Debug.Log("[DEBUG Contacts] rb == null");
            return;
        }
        ContactPoint2D[] contacts = new ContactPoint2D[16];
        int count = rb.GetContacts(contacts);
        Debug.Log($"[DEBUG Contacts] contactCount={count}");
        for (int i = 0; i < count; i++)
        {
            var c = contacts[i];
            Debug.Log($"[DEBUG Contact] collider={c.collider?.gameObject.name} point={c.point} normal={c.normal} separation={c.separation}");
        }
    }
    // -------------------

    public void TakeDamage(int damage, Vector2? sourcePosition = null)
    {
        if (isInvulnerable) return;

        health -= damage;

        // play hit sound
        if (hitClip != null && sfxSource != null) sfxSource.PlayOneShot(hitClip, hitVolume);

        if (runAudioSource != null && runAudioSource.isPlaying) runAudioSource.Stop();

        // cálculo de knockback direcional:
        float horizKnock = rb.linearVelocity.x; // fallback mantém velocidade atual no X
        float vertKnock = damageKnockback.y;   // fallback vertical antigo

        if (sourcePosition.HasValue)
        {
            Vector2 source = sourcePosition.Value;
            Vector2 delta = source - (Vector2)transform.position;
            float absX = Mathf.Abs(delta.x);
            float absY = Mathf.Abs(delta.y);

            // horizontal forte sempre na direção contrária à que o jogador está olhando
            float oppositeSign = -Mathf.Sign(transform.localScale.x);
            horizKnock = oppositeSign * strongDamageKnockback.x;

            if (absX > absY)
            {
                // dano vindo lateralmente: somente horizontal forte
                vertKnock = 0f;
            }
            else
            {
                // dano vindo de cima/baixo
                if (delta.y < 0f)
                {
                    // fonte abaixo -> lança para cima também
                    vertKnock = strongDamageKnockback.y;
                }
                else
                {
                    // fonte acima -> não lança para baixo, apenas horizontal
                    vertKnock = 0f;
                }
            }
        }
        else
        {
            // sem posição do atacante: comportamento antigo (lança pra cima)
            horizKnock = rb.linearVelocity.x;
            vertKnock = damageKnockback.y;
        }

        rb.linearVelocity = new Vector2(horizKnock, vertKnock);

        // inicia estado de knockback para evitar que FixedUpdate sobrescreva a velocidade
        isKnockedBack = true;
        knockbackTimer = knockbackDuration;

        StartCoroutine(Invulnerability());

        if (health <= 0)
        {
            Die();
        }
    }

    private IEnumerator Invulnerability()
    {
        isInvulnerable = true;
        float elapsed = 0f;
        while (elapsed < invulnerabilityTime)
        {
            if (spriteRenderer != null) spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.2f;
        }
        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        isInvulnerable = false;
    }

    public void Die()
    {
        StartCoroutine(RespawnAfterDelay(0.5f));
        monsterMovement.isChasing = false;
    }

    private IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        transform.position = respawnPoint;
        health = 5;
        rb.linearVelocity = Vector2.zero;
        hasDoubleJumped = false;
    }

    public void UpdateCheckpoint(Vector2 newPosition)
    {
        respawnPoint = newPosition;
        Debug.Log("Checkpoint atualizado para " + respawnPoint);
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
    }
}