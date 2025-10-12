using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMeele : MonoBehaviour
{
    public Transform attackOrigin;
    public float attackRadius = 1f;
    public LayerMask enemyMask;

    public int attackDamage = 1;

        // áudio de ataque
    public AudioClip attackClip;
    [Range(0f,1f)] public float attackVolume = 1f;
    private AudioSource audioSource;
    // fim áudio

    public float cooldownTime = 1f;
    private float cooldownTimer = 3f;
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }
    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            if (attackClip != null && audioSource != null)
            {
                audioSource.PlayOneShot(attackClip, attackVolume);
            }

            Collider2D[] enemiesInRange = Physics2D.OverlapCircleAll(attackOrigin.position, attackRadius, enemyMask);
            foreach (var enemy in enemiesInRange)
            {
                var hm = enemy.GetComponent<HealthManager>();
                if (hm != null) hm.TakeDamage(attackDamage);
            }

            cooldownTimer = cooldownTime;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(attackOrigin.position, attackRadius);
    }
}