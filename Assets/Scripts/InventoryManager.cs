using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InventoryManager : MonoBehaviour
{
    public enum ItemType { None, Mushroom, Banana, Bomb }

    [Header("UI 연결")]
    public Image itemSlotImage;
    public Sprite defaultIcon;

    [Header("아이템 데이터")]
    public ItemType[] itemTypes;
    public Sprite[] itemIcons;

    [Header("아이템 프리팹")]
    public GameObject bananaPrefab;
    public GameObject missilePrefab;

    [Header("상태")]
    public ItemType currentItem = ItemType.None;
    public bool hasItem = false;
    public bool isRolling = false;

    private KartController kart;

    void Start()
    {
        kart = GetComponent<KartController>();
        UpdateUI(null); // 시작할 땐 빈 슬롯
    }

    void Update()
    {
        // Player: 키보드 입력
        if (!kart.isAI && hasItem && !isRolling && Input.GetKeyDown(KeyCode.LeftControl))
        {
            UseItem();
        }
        // AI: 입력 신호 확인
        if (kart.isAI && hasItem && !isRolling && kart.isItemUseInput)
        {
            UseItem();
            kart.isItemUseInput = false;
        }
    }

    public void StartItemRoulette()
    {
        if (hasItem || isRolling) return;
        StartCoroutine(RouletteRoutine());
    }

    IEnumerator RouletteRoutine()
    {
        isRolling = true;

        float duration = 2.0f;
        float elapsed = 0f;
        float switchTime = 0.1f;

        while (elapsed < duration)
        {
            int randomIndex = Random.Range(0, itemIcons.Length);

            // 룰렛 돌릴 때 색깔을 하얗게(불투명) 해줘야 보임!
            if (!kart.isAI && itemSlotImage != null)
            {
                itemSlotImage.color = Color.white;
                itemSlotImage.sprite = itemIcons[randomIndex];
            }

            yield return new WaitForSeconds(switchTime);
            elapsed += switchTime;

            if (elapsed > duration * 0.7f) switchTime += 0.05f;
        }

        //1등 체크 및 아이템 결정
        int finalIndex = 0;

        // 매니저에게 등수 물어보기
        GameManager gm = FindFirstObjectByType<GameManager>();
        int myRank = gm != null ? gm.GetRank(kart) : 99;

        if (myRank == 1)
        {
            // 1등이면: Bomb이나 None이 아닐 때까지 계속 뽑음 (재추첨)
            do
            {
                finalIndex = Random.Range(0, itemTypes.Length);
            }
            while (itemTypes[finalIndex] == ItemType.Bomb || itemTypes[finalIndex] == ItemType.None);

            // (디버깅용 로그)
            // Debug.Log(gameObject.name + "는 1등이라서 공격 아이템 제외됨.");
        }
        else
        {
            // 1등 아니면: 그냥 랜덤
            finalIndex = Random.Range(0, itemTypes.Length);
        }

        currentItem = itemTypes[finalIndex];

        if (!kart.isAI)
        {
            UpdateUI(itemIcons[finalIndex]);
        }

        hasItem = true;
        isRolling = false;

        if (kart.isAI)
        {
            Invoke("UseItem", Random.Range(1.0f, 3.0f));
        }

        Debug.Log("아이템 획득: " + currentItem);
    }

    void UseItem()
    {
        switch (currentItem)
        {
            case ItemType.Mushroom:
                kart.AddExternalBoost(50f);
                break;

            case ItemType.Banana:
                SpawnBanana();
                break;

            case ItemType.Bomb:
                SpawnMissile();
                break;
        }

        currentItem = ItemType.None;
        hasItem = false;
        if (!kart.isAI) UpdateUI(null);
    }

    void SpawnBanana()
    {
        if (bananaPrefab != null)
        {
            Vector3 spawnPos = transform.position - (transform.forward * 2.0f);
            spawnPos.y = transform.position.y;
            Instantiate(bananaPrefab, spawnPos, transform.rotation);
        }
    }

    //미사일 발사 시 정보 전달 수정
    void SpawnMissile()
    {
        if (missilePrefab != null)
        {
            // 위치: 카트 앞쪽 위
            Vector3 spawnPos = transform.position + (transform.forward * 3.0f) + (Vector3.up * 1.2f);

            GameObject missileObj = Instantiate(missilePrefab, spawnPos, transform.rotation);

            Missile missileScript = missileObj.GetComponent<Missile>();
            if (missileScript != null)
            {
                // GameObject 대신 KartController 스크립트 자체를 넘겨줌 (GameManager가 등수 찾을 때 씀)
                missileScript.ownerScript = kart;
            }

            Debug.Log("미사일 발사!");
        }
    }

    void UpdateUI(Sprite sprite)
    {
        if (itemSlotImage != null)
        {
            if (sprite == null)
            {
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