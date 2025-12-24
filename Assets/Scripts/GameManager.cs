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
        // 1. 모든 카트를 리스트로 만듦 (정렬하기 위해)
        List<KartController> rankingList = new List<KartController>(allKarts);

        // 2. 정렬 (비교 로직: 랩 -> 체크포인트 -> 거리)
        rankingList.Sort((KartController a, KartController b) => {
            // A. 바퀴 수 비교 (높은 게 1등)
            if (a.currentLap != b.currentLap)
                return b.currentLap.CompareTo(a.currentLap);

            // B. 트랙패스 노드 인덱스 비교
            if (a.currentNodeIndex != b.currentNodeIndex)
                return b.currentNodeIndex.CompareTo(a.currentNodeIndex);

            // C. 같은 노드 근처라면? -> 그 노드까지의 거리 비교
            float distA = Vector3.Distance(a.transform.position, a.trackNodes[a.currentNodeIndex].position);
            float distB = Vector3.Distance(b.transform.position, b.trackNodes[b.currentNodeIndex].position);

            return distA.CompareTo(distB); // 완전 똑같음 (거의 없음)
        });

        // 3. 내 등수 찾기 및 UI 갱신
        if (playerKart != null && rankImage != null && rankSprites.Length > 0)
        {
            // 리스트에서 내 위치(Index) 찾기. (0번이 1등)
            int myRankIndex = rankingList.IndexOf(playerKart);

            // 이미지 교체 (0번이면 1st 이미지, 1번이면 2nd 이미지...)
            // 스프라이트 배열 범위를 넘지 않게 체크
            if (myRankIndex < rankSprites.Length)
            {
                rankImage.sprite = rankSprites[myRankIndex];
            }
        }
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
        if (isGameFinished) return; // 이미 끝났으면 무시
        isGameFinished = true;

        Debug.Log("게임 끝! 완주!");

        // 1. 모든 카트 멈춤 (AI 포함)
        foreach (var kart in allKarts)
        {
            if (kart != null) kart.isControlled = false;
        }

        // 2. 결과창 띄우기
        if (finishUI != null)
        {
            finishUI.SetActive(true);

            // 최종 기록 텍스트 설정
            if (finalTimeText != null)
            {
                finalTimeText.text = "RECORD: " + FormatTime(timer);
            }
        }

        // 3. 인게임 UI 숨기기 (선택사항)
        if (lapText != null) lapText.gameObject.SetActive(false);
        if (timeText != null) timeText.gameObject.SetActive(false);
    }

    // 재시작 버튼 기능
    public void OnRetryButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}