using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; // 재시작을 위해 필수
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

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

    [Header("게임 설정")]
    public KartController[] allKarts;   // 플레이어 + AI 모두 포함
    public KartController playerKart; // 플레이어가 누군지 알아야 UI를 띄움

    
    public Checkpoint[] checkpoints; // 체크포인트들의 위치를 알기 위해 저장
    public int totalLaps = 3;           // 총 바퀴 수

    public List<KartController> sortedKarts = new List<KartController>(); // 실시간으로 등수대로 정렬된 카트 리스트

    [HideInInspector] public int totalCheckpoints;

    public Transform trackPathRoot;

    private float timer = 0f;
    private bool isGameFinished = false;
    private bool isRaceStarted = false;

    // 플레이어 부활 위치 저장용
    private Vector3 lastCheckpointPos;
    private Quaternion lastCheckpointRot;

    void Start()
    {
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
            if (!kart.isAI) playerKart = kart;
        }

        UpdateLapUI(1); // 1바퀴째로 UI 초기화!
        if (finishUI != null) finishUI.SetActive(false);

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
    // 등수 계산 함수
    void CalculateRanking()
    {
        // 1. 리스트 복사
        List<KartController> rankingList = new List<KartController>(allKarts);

        // 2. 정렬 (점수 높은 순)
        rankingList.Sort((KartController a, KartController b) => {

            // 각 카트의 정밀한 주행 점수를 가져옴
            float scoreA = a.GetRaceDistance();
            float scoreB = b.GetRaceDistance();

            // 점수가 큰 사람이 1등 (내림차순 정렬: B - A)
            return scoreB.CompareTo(scoreA);
        });

        // 3. 리스트 갱신
        sortedKarts = rankingList;

        // 4. UI 갱신 (플레이어 등수 찾기)
        if (playerKart != null && rankImage != null && rankSprites.Length > 0)
        {
            int myRankIndex = rankingList.IndexOf(playerKart);

            // 이미지 교체
            if (myRankIndex >= 0 && myRankIndex < rankSprites.Length)
            {
                rankImage.sprite = rankSprites[myRankIndex];
            }
        }
        if(sortedKarts.Count > 0) Debug.Log("현재 1등: " + sortedKarts[0].name);
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
    void FinishGame()
    {
        if (isGameFinished) return;
        isGameFinished = true;

        // 1. 카트 멈추기
        foreach (var kart in allKarts) { if (kart != null) kart.isControlled = false; }

        // 2. 등수 확인 및 저장 로직
        if (playerKart != null)
        {
            // 내 등수 가져오기 (CalculateRanking이 선행되어야 함)
            CalculateRanking();
            int myRank = GetRank(playerKart);

            // 현재 스테이지 번호 (GameData에서 가져옴, 없으면 1)
            int currentStage = (GameData.Instance != null) ? GameData.Instance.currentStage : 1;

            // 3등 이내(1, 2, 3등)라면 다음 스테이지 잠금 해제!
            if (myRank <= 3)
            {
                int nextStage = currentStage + 1;

                // PlayerPrefs는 컴퓨터에 영구 저장하는 유니티 기본 기능
                PlayerPrefs.SetInt("Stage_" + nextStage + "_Unlocked", 1);
                PlayerPrefs.Save(); // 저장 확정

                Debug.Log(currentStage + "탄 클리어! " + nextStage + "탄 해제됨!");
            }
            else
            {
                Debug.Log("패배... 3등 안에 들어야 합니다.");
            }
        }

        // 3. UI 띄우기
        if (finishUI != null)
        {
            finishUI.SetActive(true);
            if (finalTimeText != null) finalTimeText.text = "RECORD: " + FormatTime(timer);
        }

        if (lapText != null) lapText.gameObject.SetActive(false);
        if (timeText != null) timeText.gameObject.SetActive(false);
    }
}