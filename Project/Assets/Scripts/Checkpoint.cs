using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private SpriteRenderer sr;
    public Color activeColor = Color.green;  // cor do checkpoint ativo
    private bool activated = false;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !activated)
        {
            Player player = collision.GetComponent<Player>();
            if (player != null)
            {
                player.UpdateCheckpoint(transform.position);
                ActivateCheckpoint();
            }
        }
    }

    private void ActivateCheckpoint()
    {
        activated = true;
        if (sr != null)
        {
            sr.color = activeColor;
        }
    }
}
