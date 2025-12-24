using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public int index;

    // 매니저를 통해서 총 개수를 알아와야 함 (Start에서 한 번만 가져옴)
    private int totalCheckpoints;

    void Start()
    {
        totalCheckpoints = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None).Length;
    }

    void OnTriggerEnter(Collider other)
    {
        // 부딪힌 게 카트(Player 또는 AI)인가?
        KartController kart = other.GetComponent<KartController>();

        if (kart != null)
        {
            // 그 카트의 내부 기록을 갱신해라!
            kart.PassCheckpoint(index, totalCheckpoints);

            // 리스폰 지점 업데이트도 그 카트에게 직접! (매니저 안 거침)
            // (KartController에 변수 추가가 필요하지만, 일단 기존 GameManager 방식 유지하려면 아래 코드 사용)
            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm != null && !kart.isAI) // 플레이어일 때만 리스폰 지점 저장
            {
                gm.UpdateRespawnPoint(transform.position, transform.rotation);
            }
        }
    }
}