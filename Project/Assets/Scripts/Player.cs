using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int health = 5;
    public float moveSpeed = 5f;
    public float jumpForce = 10f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    public float variableJumpMultiplier = 0.5f;
     // Som de pulo
    public AudioClip jumpClip;
    [Range(0f,1f)] public float jumpVolume = 1f;
    AudioSource audioSource;
    // final som de pulo

    //som de dano
    public AudioClip hitClip;
    [Range(0f,1f)] public float hitVolume = 1f;
    public float invulnerabilityTime = 1f;
    public Vector2 damageKnockback = new Vector2(0f, 6f);
    bool isInvulnerable = false;
    //final som de dano

    // som de corrida
    public AudioClip runClip;
    [Range(0f,1f)] public float runVolume = 1f;
    public float runStartThreshold = 0.1f;
    AudioSource runAudioSource;
    // fim som de corrida

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        runAudioSource = gameObject.AddComponent<AudioSource>();
        runAudioSource.playOnAwake = false;
        runAudioSource.loop = true;
        runAudioSource.spatialBlend = 1f;
        runAudioSource.volume = runVolume;
    }


    void Update()
    {
        // Movimento na horizontal
        float moveInput = Input.GetAxis("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        bool isMoving = Mathf.Abs(moveInput) > runStartThreshold && isGrounded;
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

        // Pulo inicial
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            // parar som de corrida ao pular
            if (runAudioSource.isPlaying) runAudioSource.Stop();

            if (jumpClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(jumpClip, jumpVolume);
            }
        }

        // Pulo variável
        if (Input.GetKeyUp(KeyCode.Space) && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * variableJumpMultiplier);
        }
    }

    private void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Damage")
        {
            TakeDamage(1);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            if (health <= 0)
            {
                Die();
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isInvulnerable) return;

        health -= damage;
        if (hitClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitClip, hitVolume);
        }

        if (runAudioSource != null && runAudioSource.isPlaying) runAudioSource.Stop();

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, damageKnockback.y);

        StartCoroutine(Invulnerability());

        if (health <= 0)
        {
            Die();
        }
    }

    // blinkRead esta sendo substituido por Invulnerability
    private IEnumerator BlinkRed()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;

    }

    // Invulnerabilidade ao tomar dano
    private IEnumerator Invulnerability()
{
    isInvulnerable = true;
    float elapsed = 0f;

    while (elapsed < invulnerabilityTime)
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        elapsed += 0.2f;
    }

    spriteRenderer.color = Color.white;
    isInvulnerable = false;
}

    private void Die()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }
}