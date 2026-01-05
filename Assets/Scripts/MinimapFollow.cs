using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    public Transform player; // 플레이어 카트

    void LateUpdate()
    {
        if (player != null)
        {
            Vector3 newPos = player.position;
            newPos.y = transform.position.y; // 높이는 유지
            transform.position = newPos;

            // 회전도 따라가게 할 건지 결정 (보통 미니맵은 북쪽 고정이라 회전은 안 따라감)
            // transform.rotation = Quaternion.Euler(90f, player.eulerAngles.y, 0f); // 회전 하려면 주석 해제
        }
    }
}