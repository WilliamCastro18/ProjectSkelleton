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

        //detectando proximidade do player
        detectionCollider = GetComponent<CircleCollider2D>();
        if (detectionCollider == null) detectionCollider = gameObject.AddComponent<CircleCollider2D>();
        detectionCollider.isTrigger = true;
        detectionCollider.radius = detectionRadius;

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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var p = collision.gameObject.GetComponent<Player>();
            if (p != null) p.TakeDamage(damage);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        PlayGrowl();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        StopGrowl();
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