using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MonsterDamage : MonoBehaviour
{
    public int damage;
    public Player playerHealth;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Player")
        {
            playerHealth.TakeDamage(damage);
        }
    }
}
