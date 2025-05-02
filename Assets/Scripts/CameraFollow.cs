using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player; // Player im Inspector zuweisen

    void Update()
    {
        if (player == null) return;

        // Position leicht versetzt auf Höhe Kopf
        transform.position = player.position + new Vector3(0f, 0f, 0f);

        // Kamera dreht sich mit dem Spieler
        transform.eulerAngles = new Vector3(0f, player.eulerAngles.y, 0f);
    }
}
