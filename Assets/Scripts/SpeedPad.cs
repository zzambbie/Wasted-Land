using UnityEngine;

public class SpeedPad : MonoBehaviour
{
    public float boostAmount = 40f; // 발판이 주는 속도 (Impulse 힘)

    void OnTriggerEnter(Collider other)
    {
        // 닿은 물체가 플레이어인지 확인 (Tag가 Player여야 함)
        // 혹은 KartController 컴포넌트가 있는지 확인
        KartController kart = other.GetComponent<KartController>();

        if (kart != null)
        {
            // 카트에게 "부스터 써!" 명령
            kart.AddExternalBoost(boostAmount);
        }
    }
}