using UnityEngine;

namespace Portfolio.CoreSystem
{
    /// <summary>
    /// 바나나(설치형 함정) 아이템 전략 구현체
    /// </summary>
    [CreateAssetMenu(fileName = "New Banana Strategy", menuName = "Portfolio/ItemSystem/Banana Strategy")]
    public class BananaStrategySO : ItemStrategySO
    {
        [Header("바나나 고유 설정")]
        [Tooltip("설치될 바나나 프리팹 (가급적 풀링 시스템과 연동하는 것을 권장)")]
        public GameObject bananaPrefab;
        
        [Tooltip("카트 뒤쪽 몇 m 지점에 설치할 것인지 지정")]
        public float spawnDistanceBehind = 2.0f;

        public override void Execute(KartController caster, Transform spawnPoint)
        {
            if (bananaPrefab == null)
            {
                Debug.LogWarning($"[{itemName}] 바나나 프리팹이 할당되지 않았습니다.");
                return;
            }

            // 트랜스폼 매트릭스 계산으로 즉시 뒤쪽 좌표 연산
            Vector3 spawnPos = spawnPoint.position - (spawnPoint.forward * spawnDistanceBehind);
            spawnPos.y = spawnPoint.position.y; // 바닥 높이 보정 로직을 추가할 수 있음

            // 팩토리나 풀에 객체 요청 (현재는 예제를 위해 Instantiate 사용)
            Instantiate(bananaPrefab, spawnPos, spawnPoint.rotation);
            Debug.Log($"[{itemName}] 뒤쪽에 바나나 설치 완료!");
        }
    }
}
