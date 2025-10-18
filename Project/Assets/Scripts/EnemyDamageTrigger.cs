using UnityEngine;

public class EnemyDamageTrigger : MonoBehaviour
{
    public int damage = 1;
    [Tooltip("Delay em segundos entre danos se o jogador ficar dentro do trigger (0 = apenas uma vez por entrada)")]
    public float damageCooldown = 0f;

    private float lastDamageTime = -999f;

    private void Start()
    {
        // Torna todos os colliders do inimigo triggers para evitar bloqueios físicos (como em DummyDamage)
        var cols = GetComponentsInChildren<Collider2D>();
        foreach (var col in cols)
        {
            col.isTrigger = true;
        }

        // Alerta se houver mais de um Rigidbody2D (pode gerar comportamento físico inesperado)
        var rbs = GetComponentsInChildren<Rigidbody2D>();
        if (rbs != null && rbs.Length > 1)
        {
            Debug.LogWarning($"[EnemyDamageTrigger] vários Rigidbody2D em {gameObject.name}. Certifique-se de que apenas o root tenha Rigidbody2D.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (damageCooldown <= 0f) return;
        if (Time.time - lastDamageTime >= damageCooldown)
        {
            TryDamage(other);
        }
    }

    private void TryDamage(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var player = other.GetComponent<Player>();
        if (player != null)
        {
            // passa a posição deste inimigo como fonte do dano para aplicar knockback direcional
            player.TakeDamage(damage, transform.position);
            lastDamageTime = Time.time;
            Debug.Log($"[EnemyDamageTrigger] damaged player ({damage}) by {gameObject.name}");
            return;
        }

        // fallback para HealthManager se quiser aplicar dano a outros tipos
        var hm = other.GetComponent<HealthManager>();
        if (hm != null)
        {
            hm.TakeDamage(damage);
            lastDamageTime = Time.time;
            Debug.Log($"[EnemyDamageTrigger] damaged HealthManager ({damage}) by {gameObject.name}");
        }
    }
}