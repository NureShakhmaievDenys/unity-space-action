using UnityEngine;

public class SectorBoundary : MonoBehaviour
{
    public Transform player;
    public float radius = 2600f;

    void Update()
    {
        if (player == null) return;

        float dist = player.position.magnitude;

        if (dist > radius)
        {
            Vector3 dir = player.position.normalized;

            // мягко возвращаем назад
            player.position = dir * radius;
        }
    }
}