using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; // 재시작을 위해 필수
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    [Header("UI 연결")]
    public TextMeshProUGUI lapText;     // 게임 중 상단 Lap 표시
    public TextMeshProUGUI timeText;    // 게임 중 상단 시간 표시
    public TextMeshProUGUI countText;   // 3, 2, 1 카운트

    [Header("등수 UI")]
    public Image rankImage;       // 등수를 보여줄 이미지
    public Sprite[] rankSprites; // 1st, 2nd, 3rd... 이미지들

    [Header("결과 화면 UI")]
    public GameObject finishUI;         // 게임 끝나면 켜질 패널 (ResultPanel)
    public TextMeshProUGUI finalTimeText; // 결과창에 뜰 최종 기록 텍스트
    public TextMeshProUGUI finalRankText; // 결과창에도 등수 나오게

    [Header("결과 화면 이미지 UI")]
    public Image resultMedalImage;      // 메달 이미지 (금/은/동)
    public Sprite[] medalSprites;       // 0=금, 1=은, 2=동
    public Image[] rankSlotImages;      // 등수란 이미지 (최대 8칸)
    public TextMeshProUGUI[] rankSlotTexts; // 각 등수란의 텍스트 (이름/시간)
    public Image playerRankSlotImage;   // 플레이어 등수란 (하이라이트용)

    [Header("게임 설정")]
    public KartController[] allKarts;   // 플레이어 + AI 모두 포함
    public KartController playerKart;

    [Header("kartPrefabs")]
    public GameObject[] kartPrefabs; // 플레이어가 누군지 알아야 UI를 띄움

    [Header("아이템 UI (플레이어용)")]
    public Image itemSlotUI;           // 씬에 있는 아이템 슬롯 이미지
    public Image itemSlotBG;           // 아이템 슬롯 배경 이미지 (아이템 상자)
    public Sprite itemDefaultIcon;     // 아이템 없을 때 기본 아이콘 (비워도 됨)
    public GameObject itemUseEffectUI; // 아이템 사용 시 "NOW!!" 화살표 이펙트

    
    public Checkpoint[] checkpoints; // 체크포인트들의 위치를 알기 위해 저장
    public int totalLaps = 3;           // 총 바퀴 수

    public List<KartController> sortedKarts = new List<KartController>(); // 실시간으로 등수대로 정렬된 카트 리스트

    [HideInInspector] public int totalCheckpoints;

    public Transform trackPathRoot;

    private float timer = 0f;
    private bool isGameFinished = false;
    public bool IsGameFinished => isGameFinished;
    private bool isRaceStarted = false;

    // 등수 확정용: 완주한 카트를 순서대로 기록
    private List<KartController> finishOrder = new List<KartController>();

    // 플레이어 부활 위치 저장용
    private Vector3 lastCheckpointPos;
    private Quaternion lastCheckpointRot;

    void Awake()
    {
        // 결과 패널을 가장 먼저 비활성화 (화면 깜빡임 방지)
        if (finishUI != null) finishUI.SetActive(false);

        // ResultPanel 레이아웃을 코드에서 올바르게 배치
        SetupResultPanelLayout();

        // ★ 아이템 화살표 위치 보정 (살짝 왼쪽으로)
        AdjustItemArrowPosition();
    }

    void Start()
    {
        // CanvasScaler는 Unity 에디터에서 직접 설정 (코드에서 건드리면 클릭 좌표 어긋남!)
        // 0. 캐릭터 선택 씬에서 골라서 넘어온 카트로 교체
        if (GameData.Instance != null && kartPrefabs != null && kartPrefabs.Length > 0)
        {
            int selectedIndex = GameData.Instance.selectedKartIndex;
            if (selectedIndex >= 0 && selectedIndex < kartPrefabs.Length)
            {
                // 기존 플레이어 카트가 있으면 그 자리에, 없으면 빈 슬롯(null)에 새 카트 생성
                int targetSlot = -1;
                Vector3 spawnPos = Vector3.zero;
                Quaternion spawnRot = Quaternion.identity;
                Transform spawnParent = null;

                // 1) 먼저 기존 플레이어 카트(isAI==false) 찾기
                for (int i = 0; i < allKarts.Length; i++)
                {
                    if (allKarts[i] != null && !allKarts[i].isAI)
                    {
                        targetSlot = i;
                        spawnPos = allKarts[i].transform.position;
                        spawnRot = allKarts[i].transform.rotation;
                        spawnParent = allKarts[i].transform.parent;
                        Destroy(allKarts[i].gameObject);
                        break;
                    }
                }

                // ★ 씬에 남아있는 여분의 플레이어 카트 찌꺼기 강제 삭제 (만약 inspector에 등록 안 된 경우)
                KartController[] sceneKarts = FindObjectsByType<KartController>(FindObjectsSortMode.None);
                foreach (var k in sceneKarts)
                {
                    if (!k.isAI)
                    {
                        if (targetSlot < 0)
                        {
                            // targetSlot을 못 찾았다면, 이 찌꺼기 카트의 위치라도 시작 위치로 쓴다
                            spawnPos = k.transform.position;
                            spawnRot = k.transform.rotation;
                            spawnParent = k.transform.parent;
                            targetSlot = 0; // 강제로 플레이어 슬롯 배정
                        }
                        if (k.gameObject != this.gameObject) // GameManager 자체 방어용
                            Destroy(k.gameObject);
                    }
                }

                // 2) 기존 플레이어가 없으면 → null인 첫 번째 슬롯 사용
                if (targetSlot < 0)
                {
                    for (int i = 0; i < allKarts.Length; i++)
                    {
                        if (allKarts[i] == null)
                        {
                            targetSlot = i;
                            break;
                        }
                    }
                    // 스폰 위치: 첫 번째 AI 카트 옆 (시작 라인 근처)
                    for (int i = 0; i < allKarts.Length; i++)
                    {
                        if (allKarts[i] != null)
                        {
                            spawnPos = allKarts[i].transform.position + allKarts[i].transform.right * 3f;
                            spawnRot = allKarts[i].transform.rotation;
                            spawnParent = allKarts[i].transform.parent;
                            break;
                        }
                    }
                }

                // 3) 카트 생성
                if (targetSlot >= 0)
                {
                    GameObject newKartObj = Instantiate(kartPrefabs[selectedIndex], spawnPos, spawnRot, spawnParent);
                    KartController newKart = newKartObj.GetComponent<KartController>();
                    newKart.isAI = false;

                    // AI 컨트롤러가 있으면 즉시 제거
                    AIController aiComp = newKartObj.GetComponent<AIController>();
                    if (aiComp != null) DestroyImmediate(aiComp);

                    allKarts[targetSlot] = newKart;
                    playerKart = newKart;

                    // 카메라도 새 카트를 따라가도록 갱신
                    KartCamera kartCam = FindFirstObjectByType<KartCamera>();
                    if (kartCam != null)
                    {
                        kartCam.targetKart = newKart;
                    }

                    // 아이템 UI 연결 (프리팹 클론은 씬 UI 참조가 끊어지므로 다시 연결)
                    InventoryManager inv = newKartObj.GetComponent<InventoryManager>();
                    if (inv != null)
                    {
                        inv.itemSlotImage = itemSlotUI;
                        inv.defaultIcon = itemDefaultIcon;
                        inv.itemUseEffectUI = itemUseEffectUI;
                        inv.UpdateUI(null); // 초기 상태로 설정
                    }
                }
            }
        }

        // 1. 트랙패스의 모든 점을 가져옴
        List<Transform> nodes = new List<Transform>();
        if (trackPathRoot != null)
        {
            foreach (Transform child in trackPathRoot) nodes.Add(child);
        }
        Transform[] nodeArray = nodes.ToArray();

        // 2. 모든 카트에게 "이게 트랙 지도야"라고 알려줌
        foreach (var kart in allKarts)
        {
            if (kart != null) kart.trackNodes = nodeArray;
        }

        // 씬에 있는 체크포인트들을 순서대로(Index순) 정렬해서 가져옴
        Checkpoint[] unsortedPoints = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);
        checkpoints = new Checkpoint[unsortedPoints.Length];

        foreach (var cp in unsortedPoints)
        {
            if (cp.index < checkpoints.Length)
                checkpoints[cp.index] = cp;
        }

        // 플레이어 찾기 (allKarts 중에서 isAI가 false인 녀석)
        foreach (var kart in allKarts)
        {
            if (kart != null && !kart.isAI) playerKart = kart;
        }

        // Fallback: 플레이어 카트가 없으면 (캐릭터 선택 씬을 거치지 않았거나 모든 카트가 AI인 경우)
        // kartPrefabs에서 첫 번째 카트를 빈 슬롯에 생성
        if (playerKart == null && kartPrefabs != null && kartPrefabs.Length > 0)
        {
            // 빈 슬롯 찾기
            int targetSlot = -1;
            for (int i = 0; i < allKarts.Length; i++)
            {
                if (allKarts[i] == null) { targetSlot = i; break; }
            }

            if (targetSlot >= 0)
            {
                // 스폰 위치: 첫 번째 AI 카트 근처
                Vector3 spawnPos = Vector3.zero;
                Quaternion spawnRot = Quaternion.identity;
                for (int i = 0; i < allKarts.Length; i++)
                {
                    if (allKarts[i] != null)
                    {
                        spawnPos = allKarts[i].transform.position + allKarts[i].transform.right * 3f;
                        spawnRot = allKarts[i].transform.rotation;
                        break;
                    }
                }

                GameObject newKartObj = Instantiate(kartPrefabs[0], spawnPos, spawnRot);
                KartController newKart = newKartObj.GetComponent<KartController>();
                newKart.isAI = false;

                // AI 컨트롤러가 있으면 제거
                AIController aiComp = newKartObj.GetComponent<AIController>();
                if (aiComp != null) DestroyImmediate(aiComp);

                allKarts[targetSlot] = newKart;
                playerKart = newKart;

                // 카메라 연결
                KartCamera kartCam = FindFirstObjectByType<KartCamera>();
                if (kartCam != null) kartCam.targetKart = playerKart;

                // 아이템 UI 연결
                InventoryManager inv = newKartObj.GetComponent<InventoryManager>();
                if (inv != null)
                {
                    inv.itemSlotImage = itemSlotUI;
                    inv.defaultIcon = itemDefaultIcon;
                    inv.itemUseEffectUI = itemUseEffectUI;
                    inv.UpdateUI(null);
                }

                Debug.Log("플레이어 카트 자동 생성: " + playerKart.name);
            }
        }

        UpdateLapUI(1); // 1바퀴째로 UI 초기화!
        // finishUI 비활성화는 Awake()에서 이미 처리됨

        // (리스폰 지점 초기화 코드 생략)
        if (checkpoints.Length > 0)
        {
            lastCheckpointPos = checkpoints[0].transform.position;
            lastCheckpointRot = checkpoints[0].transform.rotation;
        }

        StartCoroutine(StartCountdownRoutine());
    }

    IEnumerator StartCountdownRoutine()
    {
        // 1. 모든 카트 얼음!
        foreach (var kart in allKarts)
        {
            if (kart != null) kart.isControlled = false;
        }
        isRaceStarted = false;

        // 카운트다운
        if (countText != null) { countText.gameObject.SetActive(true); countText.text = "3"; }
        yield return new WaitForSeconds(1.0f);

        if (countText != null) countText.text = "2";
        yield return new WaitForSeconds(1.0f);

        if (countText != null) countText.text = "1";
        yield return new WaitForSeconds(1.0f);

        if (countText != null) countText.text = "GO!";

        // 2. 출발!
        foreach (var kart in allKarts)
        {
            if (kart != null) kart.isControlled = true;
        }
        isRaceStarted = true;

        yield return new WaitForSeconds(1.0f);
        if (countText != null) countText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (isGameFinished) return;

        if (isRaceStarted)
        {
            timer += Time.deltaTime;

            CalculateRanking();
        }

        // 실시간 타이머 표시
        if (timeText != null) timeText.text = FormatTime(timer);
    }
    // 등수 계산 함수 (실시간 인게임 순위 표시용)
    void CalculateRanking()
    {
        // 1. 리스트 복사 (삭제된 카트 = null 제거)
        List<KartController> rankingList = new List<KartController>();
        foreach (var k in allKarts) { if (k != null) rankingList.Add(k); }

        // 2. 정렬 (점수 높은 순)
        rankingList.Sort((KartController a, KartController b) => {
            float scoreA = a.GetRaceDistance();
            float scoreB = b.GetRaceDistance();
            return scoreB.CompareTo(scoreA);
        });

        // 3. 리스트 갱신
        sortedKarts = rankingList;

        // 4. UI 갱신 (플레이어 등수 찾기)
        if (playerKart != null && rankImage != null && rankSprites.Length > 0)
        {
            int myRankIndex = rankingList.IndexOf(playerKart);
            if (myRankIndex >= 0 && myRankIndex < rankSprites.Length)
            {
                rankImage.sprite = rankSprites[myRankIndex];
            }
        }
        if(sortedKarts.Count > 0) Debug.Log("현재 1등: " + sortedKarts[0].name);
    }

    /// <summary>
    /// 최종 등수 계산 (게임 종료 시 사용)
    /// 플레이어가 완주하는 순간 currentLap이 이미 증가하여 GetRaceDistance가 부풀려지므로,
    /// 완주 직전 순위(마지막 CalculateRanking 결과)를 활용하여 올바른 순위를 계산합니다.
    /// </summary>
    void CalculateFinalRanking()
    {
        // 플레이어의 currentLap을 임시로 1 줄여서 점수를 공정하게 만듦
        int originalLap = 0;
        if (playerKart != null)
        {
            originalLap = playerKart.currentLap;
            playerKart.currentLap = totalLaps; // 완주 = totalLaps (totalLaps+1로 증가된 것을 원복)
        }

        // 공정한 상태에서 다시 정렬
        List<KartController> rankingList = new List<KartController>();
        foreach (var k in allKarts) { if (k != null) rankingList.Add(k); }

        rankingList.Sort((KartController a, KartController b) => {
            float scoreA = a.GetRaceDistance();
            float scoreB = b.GetRaceDistance();
            return scoreB.CompareTo(scoreA);
        });

        sortedKarts = rankingList;

        // 원복
        if (playerKart != null)
        {
            playerKart.currentLap = originalLap;
        }

        Debug.Log("최종 등수 계산 완료!");
        for (int i = 0; i < sortedKarts.Count; i++)
        {
            Debug.Log((i+1) + "등: " + sortedKarts[i].name + " (점수: " + sortedKarts[i].GetRaceDistance() + ")");
        }
    }
    // 내 앞 등수(타겟)를 찾아주는 함수
    public KartController GetTargetFor(KartController shooter)
    {
        if (sortedKarts.Count == 0) return null;

        int myIndex = sortedKarts.IndexOf(shooter);

        // 내가 1등(인덱스 0)이거나 리스트에 없으면 타겟 없음
        if (myIndex <= 0) return null;

        // 내 바로 앞 등수(인덱스 - 1) 리턴
        return sortedKarts[myIndex - 1];
    }

    // 내가 몇 등인지 알려주는 함수
    public int GetRank(KartController kart)
    {
        if (sortedKarts.Contains(kart))
            return sortedKarts.IndexOf(kart) + 1; // 1등부터 시작
        return 99;
    }

    // 시간 포맷을 예쁘게 바꿔주는 함수
    string FormatTime(float t)
    {
        int minutes = Mathf.FloorToInt(t / 60F);
        int seconds = Mathf.FloorToInt(t % 60F);
        int milliseconds = Mathf.FloorToInt((t * 100F) % 100F);
        return string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
    }
    // 1. 체크포인트가 리스폰 위치 업데이트 요청
    public void UpdateRespawnPoint(Vector3 pos, Quaternion rot)
    {
        lastCheckpointPos = pos;
        lastCheckpointRot = rot;
        Debug.Log("부활 지점 저장됨!");
    }

    // 2. 플레이어(KartController)가 추락했을 때 구조 요청
    public void RespawnPlayer(KartController player)
    {
        Debug.Log("구조 중...");
        player.transform.position = lastCheckpointPos;
        player.transform.rotation = lastCheckpointRot;
        player.ResetStatus();
    }

    // 3. 플레이어(KartController)가 랩이 올랐을 때 UI 갱신 요청
    public void UpdateLapUI(int currentLap)
    {
        // 아직 완주 전이라면
        if (currentLap <= totalLaps)
        {
            if (lapText != null)
                lapText.text = currentLap + " / " + totalLaps;
        }
        // 완주했다면
        else
        {
            FinishGame();
        }
    }

    // 게임 종료 처리
    // 게임 종료 처리
    void FinishGame()
    {
        if (isGameFinished) return;
        isGameFinished = true;

        Debug.Log("게임 끝! 완주!");

        // ★ 1. 최종 등수 계산! (플레이어 lap 보정 포함)
        CalculateFinalRanking();
        int myRank = playerKart != null ? GetRank(playerKart) : 99;
        Debug.Log("최종 등수: " + myRank + "등");

        // 2. 등수 확정 후 모든 카트 멈춤
        foreach (var kart in allKarts)
        {
            if (kart != null) kart.isControlled = false;
        }

        // 3. 클리어 여부 저장 로직
        if (playerKart != null)
        {
            // 3등 안에 들어야 클리어!
            if (myRank <= 3)
            {
                int currentStage = (GameData.Instance != null) ? GameData.Instance.currentStage : 1;
                int nextStage = currentStage + 1;

                PlayerPrefs.SetInt("Stage_" + nextStage + "_Unlocked", 1);
                PlayerPrefs.Save();

                Debug.Log(currentStage + "탄 클리어! " + nextStage + "탄 해제됨! (등수: " + myRank + ")");
            }
            else
            {
                Debug.Log("패배... 다음 스테이지 해제 실패. (등수: " + myRank + ")");
            }
        }

        // 4. 결과창 UI 띄우기
        if (finishUI != null)
        {
            finishUI.SetActive(true);

            if (finalTimeText != null)
                finalTimeText.text = "RECORD: " + FormatTime(timer);

            if (finalRankText != null)
            {
                finalRankText.text = myRank + (myRank == 1 ? "st" : (myRank == 2 ? "nd" : (myRank == 3 ? "rd" : "th")));
            }

            // ★ 메달 이미지 표시 (1~3등만)
            if (resultMedalImage != null && medalSprites != null)
            {
                if (myRank >= 1 && myRank <= 3 && myRank - 1 < medalSprites.Length)
                {
                    resultMedalImage.sprite = medalSprites[myRank - 1];
                    resultMedalImage.gameObject.SetActive(true);
                }
                else
                {
                    resultMedalImage.gameObject.SetActive(false);
                }
            }

            // ★ 등수란 채우기 (항상 8칸 모두 표시, 빈 칸은 빈 바만)
            if (rankSlotImages != null && rankSlotTexts != null)
            {
                for (int i = 0; i < rankSlotImages.Length && i < 8; i++)
                {
                    // 항상 바를 표시 (비어있어도)
                    if (rankSlotImages[i] != null)
                        rankSlotImages[i].gameObject.SetActive(true);

                    if (i < sortedKarts.Count && sortedKarts[i] != null)
                    {
                        // 참가 카트가 있는 칸: 이름 표시
                        if (rankSlotTexts[i] != null)
                        {
                            string kartName = sortedKarts[i].name.Replace("(Clone)", "").Trim();
                            // ★ 1등 표기는 흰색 영역(좌측)에, 이름은 보라색 영역(우측)에 오도록 간격을 띄움
                            rankSlotTexts[i].text = (i + 1) + "등<pos=30%>" + kartName;
                        }

                        // 플레이어 카트면 하이라이트
                        if (playerRankSlotImage != null && sortedKarts[i] == playerKart)
                        {
                            playerRankSlotImage.gameObject.SetActive(true);
                            playerRankSlotImage.rectTransform.anchoredPosition =
                                rankSlotImages[i].rectTransform.anchoredPosition;
                        }
                    }
                    else
                    {
                        // 참가 카트가 없는 칸: 텍스트 비움 (바는 유지)
                        if (rankSlotTexts[i] != null)
                            rankSlotTexts[i].text = "";
                    }
                }
            }
        }

        // 인게임 UI 숨기기
        if (lapText != null) lapText.gameObject.SetActive(false);
        if (timeText != null) timeText.gameObject.SetActive(false);
    }
    public void OnClickReturnMap()
    {
        SceneManager.LoadScene("StoryMapScene");
    }

    /// <summary>
    /// ResultPanel의 모든 자식 UI를 참조 디자인에 맞게 프로그래밍적으로 배치합니다.
    /// 각 등수란은 개별 그라디언트 바로 표시됩니다 (등수란.png가 5줄 통합 이미지이므로).
    /// </summary>
    void SetupResultPanelLayout()
    {
        if (finishUI == null) return;

        RectTransform panelRect = finishUI.GetComponent<RectTransform>();
        if (panelRect == null) return;

        // ResultPanel 자체: 전체 화면을 덮도록
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = Vector2.zero;

        // --- ResultBG (상단 헤더 바) ---
        Transform resultBG = finishUI.transform.Find("ResultBG");
        if (resultBG != null)
        {
            RectTransform rt = resultBG.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.45f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(0, 0);
            rt.sizeDelta = new Vector2(0, 150);
        }

        // --- MedalImage (우상단 코너) ---
        Transform medalImg = finishUI.transform.Find("MedalImage");
        if (medalImg != null)
        {
            RectTransform rt = medalImg.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-20, -10);
            rt.sizeDelta = new Vector2(120, 150);
        }

        // --- PlayerRankSlot ("1st" 등수 표시 + FinalRankText) ---
        Transform playerRankSlot = finishUI.transform.Find("PlayerRankSlot");
        if (playerRankSlot != null)
        {
            RectTransform rt = playerRankSlot.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-140, -10);
            rt.sizeDelta = new Vector2(500, 140);

            // FinalRankText (PlayerRankSlot 자식)
            Transform finalRankTxt = playerRankSlot.Find("FinalRankText");
            if (finalRankTxt != null)
            {
                RectTransform frt = finalRankTxt.GetComponent<RectTransform>();
                frt.anchorMin = Vector2.zero;
                frt.anchorMax = Vector2.one;
                frt.anchoredPosition = Vector2.zero;
                frt.sizeDelta = Vector2.zero;
            }
        }

        // --- FinalTimeText (화면 좌하단 영역) ---
        Transform finalTimeTxt = finishUI.transform.Find("FinalTimeText");
        if (finalTimeTxt != null)
        {
            RectTransform rt = finalTimeTxt.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(30, 80);
            rt.sizeDelta = new Vector2(400, 50);
            rt.localScale = Vector3.one;
        }

        // --- RankSlot 1~8 등수란 배치 + '등수란.png'에서 바 한 줄만 잘라서 적용 ---
        float slotStartY = -170f;
        float slotHeight = 55f;
        float slotGap = 10f;
        float slotWidth = 500f;
        float slotRightMargin = -40f;

        // '등수란.png'의 첫 번째 바 영역을 잘라서 스프라이트 생성
        Sprite singleBarSprite = null;
        // 첫 번째 RankSlot의 기존 스프라이트에서 텍스처 추출
        Transform firstSlot = finishUI.transform.Find("RankSlot_1");
        if (firstSlot != null)
        {
            UnityEngine.UI.Image firstImg = firstSlot.GetComponent<UnityEngine.UI.Image>();
            if (firstImg != null && firstImg.sprite != null)
            {
                Texture2D tex = firstImg.sprite.texture;
                if (tex != null && tex.isReadable)
                {
                    // 등수란.png (1920x1080) 에서 첫 번째 바 영역 (Unity 좌하단 원점 기준)
                    // 이미지 상단 기준: y=373~482, x=1113~1825
                    // Unity 텍스처 좌표 (y 뒤집기): y_unity = 1080 - 482 = 598, height = 110
                    float barX = 1113f;
                    float barY = 1080f - 482f; // = 598
                    float barW = 1825f - 1113f; // = 712
                    float barH = 482f - 373f;   // = 109

                    Rect barRect = new Rect(barX, barY, barW, barH);
                    Vector2 pivot = new Vector2(0.5f, 0.5f);
                    singleBarSprite = Sprite.Create(tex, barRect, pivot, 100f);

                    Debug.Log("등수란.png에서 바 한 줄 잘라냄: " + barRect);
                }
                else
                {
                    Debug.LogWarning("등수란.png 텍스처가 읽기 불가 (Read/Write 활성화 필요)");
                }
            }
        }

        for (int i = 1; i <= 8; i++)
        {
            Transform slot = finishUI.transform.Find("RankSlot_" + i);
            if (slot != null)
            {
                RectTransform rt = slot.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(slotRightMargin, slotStartY - (i - 1) * (slotHeight + slotGap));
                rt.sizeDelta = new Vector2(slotWidth, slotHeight);

                // ★ 등수란.png에서 잘라낸 단일 바 스프라이트 적용
                UnityEngine.UI.Image slotImage = slot.GetComponent<UnityEngine.UI.Image>();
                if (slotImage != null)
                {
                    if (singleBarSprite != null)
                    {
                        slotImage.sprite = singleBarSprite;
                        slotImage.color = Color.white; // 원본 색상 유지
                    }
                    else
                    {
                        // 펴백: 스프라이트 읽기 실패 시 단색
                        slotImage.sprite = null;
                        slotImage.color = new Color(0.75f, 0.45f, 1f, 0.85f);
                    }
                    slotImage.type = UnityEngine.UI.Image.Type.Simple;
                    slotImage.preserveAspect = false;
                }

                // 자식 텍스트 배치 (슬롯 내부에 꽉 차게)
                if (slot.childCount > 0)
                {
                    RectTransform textRt = slot.GetChild(0).GetComponent<RectTransform>();
                    if (textRt != null)
                    {
                        textRt.anchorMin = Vector2.zero;
                        textRt.anchorMax = Vector2.one;
                        textRt.anchoredPosition = new Vector2(20, 0); // 좌측 여백 살짝 (흰색 영역)
                        textRt.sizeDelta = new Vector2(-40, -6);
                    }

                    TMPro.TextMeshProUGUI txt = slot.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();
                    if (txt != null)
                    {
                        txt.color = Color.black; 
                        txt.alignment = TMPro.TextAlignmentOptions.MidlineLeft; // ★ 좌측 중앙 정렬로 변경
                        txt.richText = true; // <pos=> 태그 사용을 위해 활성화
                    }
                }
            }
        }

        // --- ReturnMapButton (하단 중앙) ---
        Transform returnBtn = finishUI.transform.Find("ReturnMapButton");
        if (returnBtn != null)
        {
            RectTransform rt = returnBtn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0, 30);
            rt.sizeDelta = new Vector2(250, 50);
        }

        Debug.Log("ResultPanel 레이아웃 재배치 완료!");
    }

    /// <summary>
    /// 아이템 사용 화살표(NOW!! 이펙트)의 위치를 약간 왼쪽으로 이동
    /// </summary>
    void AdjustItemArrowPosition()
    {
        if (itemUseEffectUI != null)
        {
            RectTransform rt = itemUseEffectUI.GetComponent<RectTransform>();
            if (rt != null)
            {
                // 현재 위치에서 X를 왼쪽으로 약간 이동
                Vector2 pos = rt.anchoredPosition;
                pos.x -= 40f; // 40px 왼쪽으로
                rt.anchoredPosition = pos;
                Debug.Log("아이템 화살표 위치 보정: " + rt.anchoredPosition);
            }
        }
    }
}