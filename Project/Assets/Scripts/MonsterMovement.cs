using Unity.VisualScripting;
using UnityEngine;

public class MonsterMovement : MonoBehaviour
{
    public Transform[] patrolPoints;
    public Player player;
    public float moveSpeed;
    public int patrolDestination;
    public Transform playerTransform;
    public bool isChasing = true;
    public float chaseDistance;

    void Update()
    {
        if (isChasing)
        {
            if(transform.position.x > playerTransform.position.x)
            {
                transform.localScale = new Vector3(1.5f, 1.5f, 1);
                transform.position += Vector3.left * moveSpeed * Time.deltaTime;
            }
            if (transform.position.x < playerTransform.position.x)
            {
                transform.localScale = new Vector3(-1.5f, 1.5f, 1);
                transform.position += Vector3.right * moveSpeed * Time.deltaTime;
            }
            if ((Vector2.Distance(transform.position, playerTransform.position) >= chaseDistance))
            {
                isChasing = false;
            }
        }
        else
        {
            if (Vector2.Distance(transform.position, playerTransform.position) < chaseDistance)
            {
                isChasing = true;
            }


            if (patrolDestination == 0)
            {
                transform.position = Vector2.MoveTowards(transform.position, patrolPoints[0].position, moveSpeed * Time.deltaTime);
                if (Vector2.Distance(transform.position, patrolPoints[0].position) < 0.2f)
                {
                    transform.localScale = new Vector3(-1.5f, 1.5f, 1);
                    patrolDestination = 1;
                }
            }

            if (patrolDestination == 1)
            {
                transform.position = Vector2.MoveTowards(transform.position, patrolPoints[1].position, moveSpeed * Time.deltaTime);
                if (Vector2.Distance(transform.position, patrolPoints[1].position) < 0.2f)
                {
                    transform.localScale = new Vector3(1.5f, 1.5f, 1);
                    patrolDestination = 0;
                }
            }
        }
    }

}
