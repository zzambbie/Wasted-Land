using UnityEngine;
using System.Collections;

public class ItemBox : MonoBehaviour
{
    public float respawnTime = 3.0f; // 박스 재생성 시간
    public GameObject boxVisual;     // 박스 모델 (큐브)

    private Collider boxCollider;
    private Renderer boxRenderer;

    void Start()
    {
        boxCollider = GetComponent<Collider>();
        boxRenderer = GetComponent<Renderer>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // 플레이어 태그 확인
        {
            InventoryManager inventory = other.GetComponent<InventoryManager>();

            if (inventory != null)
            {
                // 박스 사라지는 건 무조건 실행! (시각적 피드백)
                StartCoroutine(RespawnRoutine());

                // 아이템 뽑기는 없을 때만 실행
                // (아이템이 있는데 또 먹으면 룰렛 안 돌고 그냥 박스만 사라짐 -> 마리오 카트 방식)
                if (!inventory.hasItem && !inventory.isRolling)
                {
                    inventory.StartItemRoulette();
                }
            }
        }
    }

    IEnumerator RespawnRoutine()
    {
        // 모습과 충돌을 끈다 (안 보이게)
        if (boxRenderer != null) boxRenderer.enabled = false;
        if (boxCollider != null) boxCollider.enabled = false;

        // 자식 오브젝트(시각효과)가 있다면 그것도 꺼주는 게 좋음
        // transform.GetChild(0).gameObject.SetActive(false); // 예시

        // 대기
        yield return new WaitForSeconds(respawnTime);

        // 다시 켠다 (리스폰)
        if (boxRenderer != null) boxRenderer.enabled = true;
        if (boxCollider != null) boxCollider.enabled = true;

        // 뿅! 하고 나타나는 이펙트가 있으면 여기서 재생
    }
}