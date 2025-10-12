using UnityEngine;

public class CameraFollowSmooth : MonoBehaviour
{
    public Transform target;       // Referência ao jogador
    public Vector3 offset;         // Distância entre player e câmera
    public float smoothSpeed = 5f; // Velocidade da suavização

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        desiredPosition.z = -10; // mantém a câmera no plano 2D

        // Movimento suave da câmera
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }
}
