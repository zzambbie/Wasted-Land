using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public enum ObstacleType { Oil, Bananas }
    public ObstacleType type = ObstacleType.Oil;

    public float effectDuration = 1f; // 1초 동안 돎

    void OnTriggerEnter(Collider other)
    {
        // 플레이어인지 확인
        KartController kart = other.GetComponent<KartController>();

        if (kart != null)
        {
            if (type == ObstacleType.Oil)
            {
                // 카트에게 "돌아라!" 명령
                kart.SlipAndSpin(effectDuration);
            }
        }
    }
}