using UnityEngine;

namespace Portfolio.CoreSystem
{
    /// <summary>
    /// 미사일(유도탄) 아이템 전략 구현체
    /// 
    /// [설계 의도] 
    /// 무기 로직(Missile 발사)을 매니저 밖으로 빼내 책임을 분리(SRP 준수)했습니다.
    /// 각 아이템이 필요로 하는 프리팹은 Manager가 아닌 SO 객체 자체가 가짐으로써 메모리 측면에서도 클린합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "New Missile Strategy", menuName = "Portfolio/ItemSystem/Missile Strategy")]
    public class MissileStrategySO : ItemStrategySO
    {
        [Header("미사일 발사 설정")]
        public GameObject missilePrefab;
        public float forwardOffset = 3.0f;
        public float upwardOffset = 1.2f;

        public override void Execute(KartController caster, Transform spawnPoint)
        {
            if (missilePrefab == null) return;

            Vector3 spawnPos = spawnPoint.position + (spawnPoint.forward * forwardOffset) + (Vector3.up * upwardOffset);
            GameObject missileObj = Instantiate(missilePrefab, spawnPos, spawnPoint.rotation);

            // 기존 Missile.cs의 의존성을 스크립트에 주입 (주인 기록)
            Missile missileScript = missileObj.GetComponent<Missile>();
            if (missileScript != null)
            {
                missileScript.ownerScript = caster;
            }

            Debug.Log($"[{itemName}] 락온! 미사일 발사!");
        }
    }
}
