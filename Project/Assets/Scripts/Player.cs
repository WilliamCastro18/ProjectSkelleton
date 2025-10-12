// ...existing code...
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

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float horizontal;
    private bool isFacingRight = true;
    private bool isGrounded;
    private bool isWallSliding;
    private Vector2 respawnPoint;

    private AudioSource sfxSource;
    private AudioSource runAudioSource;
    private bool isInvulnerable = false;

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
        horizontal = Input.GetAxis("Horizontal");

        // Jump input
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            if (runAudioSource.isPlaying) runAudioSource.Stop();
            if (jumpClip != null && sfxSource != null) sfxSource.PlayOneShot(jumpClip, jumpVolume);
        }

        if (Input.GetKeyUp(KeyCode.Space) && rb.velocity.y > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * variableJumpMultiplier);
        }

        WallSlide();
        WallJump();

        if (!isWallJumping)
        {
            FlipIfNeeded();
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

        if (isDashing) return;

        if (!isWallJumping)
        {
            rb.velocity = new Vector2(horizontal * moveSpeed, rb.velocity.y);
        }
    }

    private bool IsWalled()
    {
        if (wallCheck == null) return false;
        return Physics2D.OverlapCircle(wallCheck.position, 0.2f, wallLayer);
    }

    private void WallSlide()
    {
        if (IsWalled() && !isGrounded && Mathf.Abs(horizontal) > 0f)
        {
            isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, -Mathf.Abs(wallJumpingPower.y) * 0.2f);
        }
        else
        {
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

        if (Input.GetKeyDown(KeyCode.Space) && wallJumpingCounter > 0f)
        {
            isWallJumping = true;
            rb.velocity = new Vector2(wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);
            wallJumpingCounter = 0f;

            if (transform.localScale.x != wallJumpingDirection)
            {
                isFacingRight = !isFacingRight;
                Vector3 localScale = transform.localScale;
                localScale.x *= -1f;
                transform.localScale = localScale;
            }

            Invoke(nameof(StopWallJumping), wallJumpingDuration);
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
        rb.velocity = new Vector2(transform.localScale.x * dashingPower, 0f);
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
            TakeDamage(1);
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isInvulnerable) return;

        health -= damage;

        // play hit sound
        if (hitClip != null && sfxSource != null) sfxSource.PlayOneShot(hitClip, hitVolume);

        if (runAudioSource != null && runAudioSource.isPlaying) runAudioSource.Stop();

        rb.velocity = new Vector2(rb.velocity.x, damageKnockback.y);

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

    private void Die()
    {
        StartCoroutine(RespawnAfterDelay(0.5f));
    }

    private IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        transform.position = respawnPoint;
        health = 5;
        rb.velocity = Vector2.zero;
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