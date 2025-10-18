using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMelee : MonoBehaviour
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

    [Header("Hitbox visualization")]
    public bool showHitbox = true;
    public float hitboxDisplayTime = 0.2f;
    [Range(8, 128)] public int hitboxSegments = 40;
    public Color hitboxColor = new Color(1f, 0f, 0f, 0.5f);
    public float hitboxWidth = 0.05f;
    private LineRenderer hitboxRenderer;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (showHitbox)
        {
            CreateHitboxRenderer();
            hitboxRenderer.enabled = false;
        }
    }

    private void CreateHitboxRenderer()
    {
        GameObject go = new GameObject("HitboxRenderer");
        go.transform.SetParent(transform, worldPositionStays: true);
        hitboxRenderer = go.AddComponent<LineRenderer>();
        hitboxRenderer.loop = true;
        hitboxRenderer.useWorldSpace = true;
        hitboxRenderer.positionCount = hitboxSegments + 1;
        hitboxRenderer.material = new Material(Shader.Find("Sprites/Default"));
        hitboxRenderer.widthMultiplier = hitboxWidth;
        hitboxRenderer.startColor = hitboxColor;
        hitboxRenderer.endColor = hitboxColor;
        hitboxRenderer.numCornerVertices = 4;
        hitboxRenderer.numCapVertices = 4;
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

            Vector2 origin = attackOrigin != null ? (Vector2)attackOrigin.position : (Vector2)transform.position;

            Collider2D[] hits;
            if (enemyMask.value != 0)
            {
                hits = Physics2D.OverlapCircleAll(origin, attackRadius, enemyMask);
            }
            else
            {
                hits = Physics2D.OverlapCircleAll(origin, attackRadius);
            }

            var damaged = new HashSet<GameObject>();

            Debug.Log($"[Melee] origin={origin} radius={attackRadius} hits={hits.Length} maskValue={enemyMask.value}");

            foreach (var hit in hits)
            {
                if (hit == null) continue;

                var go = hit.gameObject;
                if (!damaged.Add(go)) continue; // já processado neste ataque

                // tenta HealthManager no próprio objeto ou em parents/children
                var hm = go.GetComponent<HealthManager>() ?? go.GetComponentInParent<HealthManager>() ?? go.GetComponentInChildren<HealthManager>();
                if (hm == null) continue;

                // distância até o transform do HealthManager (protege contra colliders enormes)
                Vector2 targetPos = (Vector2)hm.transform.position;
                float centerDist = Vector2.Distance(origin, targetPos);
                if (centerDist > attackRadius + 0.001f)
                {
                    Debug.Log($"[Melee] skipped (center too far): {go.name} centerDist={centerDist}");
                    continue;
                }

                // checagem de linha de visão: se algo bloquear antes de atingir o inimigo, ignora
                Vector2 dir = (targetPos - origin).normalized;
                float rayDist = Mathf.Max(0.001f, centerDist);
                RaycastHit2D lineHit = Physics2D.Raycast(origin, dir, rayDist);
                if (lineHit.collider != null)
                {
                    // permitido se o primeiro collider atingido é do próprio inimigo (ou contém HealthManager)
                    if (lineHit.collider.gameObject != go && lineHit.collider.GetComponentInParent<HealthManager>() == null)
                    {
                        Debug.Log($"[Melee] blocked by {lineHit.collider.gameObject.name} when trying to hit {go.name}");
                        continue;
                    }
                }

                hm.TakeDamage(attackDamage);
                Debug.Log($"[Melee] damaged -> {go.name}");
            }

            // Mostrar hitbox visual se habilitado
            if (showHitbox && hitboxRenderer != null)
            {
                StartCoroutine(ShowHitbox(origin, attackRadius, hitboxDisplayTime));
            }

            cooldownTimer = cooldownTime;
        }
    }

    private IEnumerator ShowHitbox(Vector2 origin, float radius, float duration)
    {
        if (hitboxRenderer == null) yield break;

        // Atualiza resolução caso tenha sido alterada no Inspector
        hitboxRenderer.positionCount = hitboxSegments + 1;
        hitboxRenderer.widthMultiplier = hitboxWidth;
        hitboxRenderer.startColor = hitboxColor;
        hitboxRenderer.endColor = hitboxColor;

        for (int i = 0; i <= hitboxSegments; i++)
        {
            float angle = (float)i / hitboxSegments * Mathf.PI * 2f;
            Vector3 pos = new Vector3(origin.x + Mathf.Cos(angle) * radius, origin.y + Mathf.Sin(angle) * radius, 0f);
            hitboxRenderer.SetPosition(i, pos);
        }

        hitboxRenderer.enabled = true;
        yield return new WaitForSeconds(duration);
        hitboxRenderer.enabled = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;
        Gizmos.DrawWireSphere(origin, attackRadius);
    }
}