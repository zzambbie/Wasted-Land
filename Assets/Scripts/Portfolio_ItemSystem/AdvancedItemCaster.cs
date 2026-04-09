using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.CoreSystem
{
    /// <summary>
    /// 전략 패턴을 구동하는 컨텍스트(Context) 역할을 수행하는 컴포넌트입니다.
    /// 
    /// [포트폴리오 설계 의도 - 스파게티 코드 방지]
    /// 기존 InventoryManager의 switch-case가 지닌 한계(로직 비대화)를 제거했습니다.
    /// 이 클래스는 현재 뽑은 '아이템 전략(ItemStrategySO)'이 무엇인지 알 필요가 없습니다.
    /// 오직 currentStrategy.Execute()만 호출함으로써 객체 간 결합도를 최소화했습니다.
    /// </summary>
    public class AdvancedItemCaster : MonoBehaviour
    {
        [Header("의존성 구성")]
        [Tooltip("아이템 사용 주체 컨트롤러")]
        private KartController kart;
        
        [Tooltip("아이템 발사 기준 위치 (보안상 트랜스폼 직접 참조)")]
        [SerializeField] private Transform itemSpawnPoint;

        [Header("룰렛 시스템 호환성 풀")]
        public List<ItemStrategySO> possibleItems;
        public Image uiSlotImage;

        private ItemStrategySO currentStrategy;
        private bool isRolling = false;

        void Start()
        {
            kart = GetComponent<KartController>();
            if (itemSpawnPoint == null) itemSpawnPoint = transform; // 미 지정시 자기 자신
        }

        void Update()
        {
            // 사용자의 입력 혹은 AI의 신호를 감지하고 디커플링된 메서드를 호출
            bool fireCommand = (!kart.isAI && Input.GetKeyDown(KeyCode.LeftControl)) || 
                               (kart.isAI && kart.isItemUseInput);

            if (fireCommand && HasItem() && !isRolling)
            {
                CastItem();
                if (kart.isAI) kart.isItemUseInput = false;
            }
        }

        public bool HasItem() => currentStrategy != null;

        /// <summary>
        /// 전략 객체의 다형성을 이용한 핵심 실행 메서드입니다.
        /// 구체적으로 어떤 아이템인지 타입 캐스팅이나 조건문 검사를 하지 않습니다.
        /// </summary>
        public void CastItem()
        {
            if (currentStrategy == null) return;

            // 전략 패턴: 추상체만 바라봄
            currentStrategy.Execute(kart, itemSpawnPoint);

            // 사용 후 상태 클리어
            currentStrategy = null;
            UpdateUI();
        }

        // ============================================
        // 룰렛 시뮬레이션 (기존 코드와의 인터페이스 유지를 위해 남김)
        // ============================================
        public void StartItemRoulette()
        {
            if (HasItem() || isRolling) return;
            StartCoroutine(RouletteRoutine());
        }

        private IEnumerator RouletteRoutine()
        {
            isRolling = true;
            // 로직 간결화를 위해 연출 생략 (포트폴리오용이므로 코어 아키텍처에 집중)
            yield return new WaitForSeconds(1.0f);

            // 임의의 전략 체택 (GameManager의 등수 기반 가중치를 별도 컴포넌트로 분리하는 것이 이상적임)
            int rnd = Random.Range(0, possibleItems.Count);
            currentStrategy = possibleItems[rnd];
            
            UpdateUI();
            isRolling = false;

            if (kart.isAI)
            {
                Invoke(nameof(CastItem), Random.Range(1.0f, 3.0f));
            }
        }

        private void UpdateUI()
        {
            if (uiSlotImage != null)
            {
                if (currentStrategy != null)
                {
                    uiSlotImage.sprite = currentStrategy.itemIcon;
                    uiSlotImage.color = Color.white;
                }
                else
                {
                    uiSlotImage.sprite = null;
                    uiSlotImage.color = Color.clear;
                }
            }
        }
    }
}
