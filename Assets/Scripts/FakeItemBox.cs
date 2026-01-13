using UnityEngine;

public class FakeItemBox : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // 트리거끼리 충돌은 무시 (다른 아이템 박스 등)
        if (other.isTrigger && !other.CompareTag("Player")) return;

        // 카트가 닿았는지 확인
        KartController target = other.GetComponent<KartController>();

        if (target != null)
        {
            Debug.Log("가짜 박스에 걸렸다! 쾅!");

            // 미사일 맞았을 때랑 똑같은 효과(공중부양 & 구르기)
            target.HitByMissile();

            // 폭발 이펙트가 있다면 여기서 생성
            // Instantiate(explosionEffect, transform.position, Quaternion.identity);

            // 박스 삭제
            Destroy(gameObject);
        }
    }
}