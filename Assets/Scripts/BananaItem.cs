using UnityEngine;

public class BananaItem : MonoBehaviour
{
    public float slipDuration = 1.0f; // 미끄러지는 시간

    void OnTriggerEnter(Collider other)
    {
        // 플레이어인지 확인 (혹은 나중에 AI도 포함)
        KartController kart = other.GetComponent<KartController>();

        if (kart != null)
        {
            // 1. 카트 미끄러지게 하기 (기름 웅덩이 때 만든 함수 재사용!)
            kart.SlipAndSpin(slipDuration);

            // 2. 바나나는 밟히면 사라져야 함
            Destroy(gameObject);
        }
    }
}