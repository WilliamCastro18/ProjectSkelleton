using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyDamage : MonoBehaviour
{
    public int damage = 1;

    public AudioClip growlClip;
    [Range(0f,1f)] public float growlVolume = 1f;
    public float detectionRadius = 5f;
    AudioSource audioSource;
    CircleCollider2D detectionCollider;
    Transform playerTransform;

    private void Start()
    {
        // Audio loop
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 1f;
        audioSource.volume = growlVolume;

        // detectando proximidade do player
        detectionCollider = GetComponent<CircleCollider2D>();
        if (detectionCollider == null) detectionCollider = gameObject.AddComponent<CircleCollider2D>();
        detectionCollider.isTrigger = true;
        detectionCollider.radius = detectionRadius;

        // Torna todos os outros colliders do inimigo triggers também (evita bloqueio físico)
        foreach (var col in GetComponents<Collider2D>())
        {
            if (col == detectionCollider) continue;
            col.isTrigger = true;
        }

        var pgo = GameObject.FindGameObjectWithTag("Player");
        if (pgo != null) playerTransform = pgo.transform;
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float dist = Vector2.Distance(playerTransform.position, transform.position);
        if (dist <= detectionRadius)
        {
            PlayGrowl();
        }
        else
        {
            StopGrowl();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Verifica interseção entre bounds do player e dos colliders "de corpo" do inimigo
        var bodyColliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in bodyColliders)
        {
            if (col == detectionCollider) continue;

            // ignora se o collider estiver desativado
            if (!col.enabled) continue;

            // usa Intersects para detectar sobreposição mesmo em bordas
            if (col.bounds.Intersects(other.bounds))
            {
                var p = other.GetComponent<Player>();
                if (p != null)
                {
                    // passa a posição deste inimigo como fonte do dano
                    p.TakeDamage(damage, transform.position);
                }
                return;
            }
        }

        // Se não aplicou dano, provavelmente foi apenas a zona de detecção a tocar growl
        PlayGrowl();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // se saiu da zona de detecção, parar growl
        if (detectionCollider != null && !detectionCollider.bounds.Contains(other.bounds.center))
        {
            StopGrowl();
        }
    }

    private void PlayGrowl()
    {
        if (growlClip == null || audioSource == null) return;
        if (!audioSource.isPlaying)
        {
            audioSource.clip = growlClip;
            audioSource.volume = growlVolume;
            audioSource.Play();
        }
    }

    private void StopGrowl()
    {
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
    }
}