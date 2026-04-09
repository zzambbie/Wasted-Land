using UnityEngine;

namespace Portfolio.CoreSystem
{
    /// <summary>
    /// 버섯(부스터) 아이템 전략 구현체
    /// </summary>
    [CreateAssetMenu(fileName = "New Mushroom Strategy", menuName = "Portfolio/ItemSystem/Mushroom Strategy")]
    public class MushroomStrategySO : ItemStrategySO
    {
        [Header("버섯 고유 설정")]
        [Tooltip("순간적으로 가해지는 부스터 힘의 크기")]
        public float boostAmount = 50f;

        public override void Execute(KartController caster, Transform spawnPoint)
        {
            // 의존성 주입된 caster(카트)에 직접적으로 물리력이나 상태값 변경 지시
            caster.AddExternalBoost(boostAmount);
            Debug.Log($"[{itemName}] 사용! 강력한 부스터 발동: {boostAmount} 파워");
        }
    }
}
