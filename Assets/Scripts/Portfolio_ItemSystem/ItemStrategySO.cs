using UnityEngine;

namespace Portfolio.CoreSystem
{
    /// <summary>
    /// 전략 패턴(Strategy Pattern) 기반 아이템 시스템의 최상위 추상 클래스입니다.
    /// ScriptableObject를 상속받아 데이터(아이콘 등)와 행위(Execute)를 캡슐화합니다.
    /// 
    /// [포트폴리오 설계 의도]
    /// 기존의 switch-case 기반 아이템 처리 로직을 교체하여 OCP(개방-폐쇄 원칙)를 준수했습니다.
    /// 새로운 아이템 추가 시, 기존 코드를 수정할 필요 없이 이 클래스를 상속받는 새로운 데이터 애셋만 생성하면 됩니다.
    /// 메모리 최적화를 위해 싱글톤 메커니즘을 흉내내는 ScriptableObject를 사용해 인스턴스 중복 생성을 방지했습니다.
    /// </summary>
    public abstract class ItemStrategySO : ScriptableObject
    {
        [Header("아이템 기본 정보")]
        [Tooltip("인게임 UI 등에 표시될 아이템의 고유 이름")]
        public string itemName;

        [Tooltip("슬롯에 표시될 아이템 스프라이트")]
        public Sprite itemIcon;

        /// <summary>
        /// 다형성이 적용된 아이템 사용 인터페이스
        /// </summary>
        /// <param name="caster">아이템을 사용하는 주체 (카트)</param>
        /// <param name="spawnPoint">투사체 발사 시 사용할 기준 위치</param>
        public abstract void Execute(KartController caster, Transform spawnPoint);
    }
}
