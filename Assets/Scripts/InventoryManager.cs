using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InventoryManager : MonoBehaviour
{
    // 아이템 종류 정의
    public enum ItemType { None, Mushroom, Banana, Bomb }

    [Header("UI 연결")]
    public Image itemSlotImage;     // 화면의 ItemSlot 이미지
    public Sprite defaultIcon;      // 아이템 없을 때 (투명하거나 물음표)

    [Header("아이템 데이터")]
    // 이 두 배열의 순서가 맞아야 함! (0번: 버섯, 1번: 바나나...)
    public ItemType[] itemTypes;    // [Mushroom, Banana, Bomb]
    public Sprite[] itemIcons;      // [버섯그림, 바나나그림, 폭탄그림]

    [Header("아이템 프리팹")]
    public GameObject bananaPrefab; // 바나나 프리팹 연결용 변수

    [Header("상태")]
    public ItemType currentItem = ItemType.None; // 현재 가진 아이템
    public bool hasItem = false;    // 아이템이 있는가?
    public bool isRolling = false;  // 룰렛 도는 중인가?

    private KartController kart;

    void Start()
    {
        kart = GetComponent<KartController>();

        // 시작할 땐 빈 슬롯
        UpdateUI(null);
    }

    void Update()
    {
        // 아이템 사용 (Ctrl 키)
        if (!kart.isAI && hasItem && !isRolling && Input.GetKeyDown(KeyCode.LeftControl))
        {
            UseItem();
        }
        // AI: 입력 신호(isItemUseInput) 확인
        if (kart.isAI && hasItem && !isRolling && kart.isItemUseInput)
        {
            UseItem();
            kart.isItemUseInput = false; // 한 번 썼으니 신호 끄기
        }
    }

    // 외부(ItemBox)에서 호출하는 함수
    public void StartItemRoulette()
    {
        if (hasItem || isRolling) return;
        StartCoroutine(RouletteRoutine());
    }

    // 마리오카트처럼 다다다닥 바뀌다가 멈추는 연출
    IEnumerator RouletteRoutine()
    {
        isRolling = true;

        // 1. 룰렛 돌리기 (빠르게 아이콘 변경)
        float duration = 2.0f; // 2초 동안 돎
        float elapsed = 0f;
        float switchTime = 0.1f; // 0.1초마다 그림 바뀜

        while (elapsed < duration)
        {
            // 랜덤한 아이콘을 잠깐 보여줌
            int randomIndex = Random.Range(0, itemIcons.Length);

            if (!kart.isAI)
            {
                itemSlotImage.sprite = itemIcons[randomIndex];
            }

            // 소리 재생하고 싶으면 여기서 (띡, 띡, 띡)

            yield return new WaitForSeconds(switchTime);
            elapsed += switchTime;

            // 시간이 지날수록 조금씩 느려지게 하면 더 리얼함 (선택사항)
            if (elapsed > duration * 0.7f) switchTime += 0.05f;
        }

        // 2. 최종 아이템 결정 (완전 랜덤)
        int finalIndex = Random.Range(0, itemTypes.Length);

        currentItem = itemTypes[finalIndex]; // 아이템 타입 저장

        if (!kart.isAI)
        {
            UpdateUI(itemIcons[finalIndex]); // UI 확정
        }     

        hasItem = true;
        isRolling = false;

        if (kart.isAI)
        {          
            Invoke("UseItem", Random.Range(1.0f, 3.0f)); // AI는 아이템 얻고 1~3초 뒤에 사용하게 예약
        }

        // 아이템 획득 소리 (띠링!)
        Debug.Log("아이템 획득: " + currentItem);
    }

    void UseItem()
    {
        Debug.Log("아이템 사용: " + currentItem);

        switch (currentItem)
        {
            case ItemType.Mushroom:
                // 버섯: 부스터 발동 (kart 스크립트 활용)
                // 아이템용 부스터 함수 호출
                kart.AddExternalBoost(50f);
                break;

            case ItemType.Banana:
                // 바나나: 뒤로 던지기
                SpawnBanana(); // 바나나 설치 함수 호출
                break;
        }

        // 사용 후 초기화
        currentItem = ItemType.None;
        hasItem = false;
        if (!kart.isAI) UpdateUI(null); // 슬롯 비우기
    }

    // 바나나 설치 함수
    void SpawnBanana()
    {
        if (bananaPrefab != null)
        {
            // 1. 설치 위치 계산: 내 카트보다 2미터 뒤, 높이는 바닥에 붙게
            Vector3 spawnPos = transform.position - (transform.forward * 2.0f);
            spawnPos.y = transform.position.y; // 높이는 카트와 같게 (혹은 약간 아래)

            // 2. 바나나 생성 (Instantiate)
            Instantiate(bananaPrefab, spawnPos, transform.rotation);

            Debug.Log("바나나 설치 완료!");
        }
    }

        // UI 이미지를 바꿔주는 함수
        void UpdateUI(Sprite sprite)
        {
            if (itemSlotImage != null)
            {
                if (sprite == null)
                {
                    // 이미지가 없으면 투명하게 하거나 기본 아이콘(물음표)으로
                    if (defaultIcon != null)
                    {
                        itemSlotImage.sprite = defaultIcon;
                        itemSlotImage.color = Color.white;
                    }
                    else
                    {
                        itemSlotImage.color = Color.clear; // 투명
                    }
                }
                else
                {
                    itemSlotImage.sprite = sprite;
                    itemSlotImage.color = Color.white; // 불투명
                }
            }
        }
    }