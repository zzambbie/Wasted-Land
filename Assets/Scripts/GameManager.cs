using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; // 재시작을 위해 필수
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public KartController[] kartPrefabs; // 캐릭터 선택용 프리팹 리스트 (Inspector에서 채워야 함)
    public Transform startPoint;         // 플레이어 생성 위치

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

    [Header("일시정지 UI")]
    public GameObject pausePanel;
    private bool isPaused = false; // 지금 멈췄는지 확인용

    [Header("게임 설정")]
    public KartController[] allKarts;   // 플레이어 + AI 모두 포함
    public KartController playerKart; // 플레이어가 누군지 알아야 UI를 띄움

    
    public Checkpoint[] checkpoints; // 체크포인트들의 위치를 알기 위해 저장
    public int totalLaps = 3;           // 총 바퀴 수

    public List<KartController> sortedKarts = new List<KartController>(); // 실시간으로 등수대로 정렬된 카트 리스트
    public List<KartController> finishedKarts = new List<KartController>(); // 결승선 통과한 카트들을 순서대로 저장하는 명단

    [HideInInspector] public int totalCheckpoints;

    public Transform trackPathRoot;

    public Image itemSlotUI;

    private float timer = 0f;
    private bool isGameFinished = false;
    private bool isRaceStarted = false;

    // 플레이어 부활 위치 저장용
    private Vector3 lastCheckpointPos;
    private Quaternion lastCheckpointRot;

    void Start()
    {
        // === 선택한 캐릭터로 플레이어 카트 교체 ===
        if (GameData.Instance != null && kartPrefabs != null && kartPrefabs.Length > 0)
        {
            int selectedIndex = Mathf.Clamp(GameData.Instance.selectedKartIndex, 0, kartPrefabs.Length - 1);

            // 기존 플레이어 카트 찾기 & 제거
            for (int i = 0; i < allKarts.Length; i++)
            {
                if (allKarts[i] != null && !allKarts[i].isAI)
                {
                    Vector3 pos = allKarts[i].transform.position;
                    Quaternion rot = allKarts[i].transform.rotation;

                    Destroy(allKarts[i].gameObject);

                    // 선택한 카트 스폰
                    KartController newKart = Instantiate(kartPrefabs[selectedIndex], pos, rot);
                    newKart.isAI = false;
                    allKarts[i] = newKart;
                    playerKart = newKart;

                    // 카메라 타겟 재설정
                    KartCamera cam = FindAnyObjectByType<KartCamera>();
                    if (cam != null) cam.targetKart = newKart;

                    break;
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

        // ESC 키 입력 감지
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }

        if (isRaceStarted)
        {
            timer += Time.deltaTime;

            CalculateRanking();
        }

        // 실시간 타이머 표시
        if (timeText != null) timeText.text = FormatTime(timer);
    }

    // 게임 멈추기
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // [핵심] 시간을 멈춤! (모든 물리, 이동 정지)
        if (pausePanel != null) pausePanel.SetActive(true); // UI 켜기
    }

    // 게임 재개
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // [핵심] 시간 다시 흐름
        if (pausePanel != null) pausePanel.SetActive(false); // UI 끄기
    }

    // 재시작 (버튼용)
    public void OnClickRestart()
    {
        // 시간은 다시 흐르게 해줘야 함 (안 그러면 멈춘 채로 재시작됨)
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 나가기 (버튼용)
    public void OnClickExit()
    {
        Time.timeScale = 1f; // 시간 정상화
        SceneManager.LoadScene("StoryMapScene"); // 맵 선택 화면으로
    }

    // 등수 계산 함수
    void CalculateRanking()
    {
        // 1. 아직 달리고 있는 카트들만 리스트에 담음
        List<KartController> racingList = new List<KartController>();
        foreach (var kart in allKarts)
        {
            // 이미 도착 명단에 있는 애들은 뺌
            if (!finishedKarts.Contains(kart))
            {
                racingList.Add(kart);
            }
        }

        // 2. 달리는 애들끼리는 '점수'로 등수 매김 (기존 방식)
        racingList.Sort((KartController a, KartController b) => {
            float scoreA = a.GetRaceDistance();
            float scoreB = b.GetRaceDistance();
            return scoreB.CompareTo(scoreA);
        });

        // 3. [최종 명단 합체] (도착한 애들) + (달리는 애들)
        // sortedKarts를 새로 구성함
        sortedKarts.Clear();
        sortedKarts.AddRange(finishedKarts); // 1, 2등 먼저 넣고
        sortedKarts.AddRange(racingList);    // 나머지 뒤에 붙임

        // 4. UI 갱신 (기존 코드 유지)
        if (playerKart != null && rankImage != null && rankSprites.Length > 0)
        {
            int myRankIndex = sortedKarts.IndexOf(playerKart);
            if (myRankIndex >= 0 && myRankIndex < rankSprites.Length)
            {
                rankImage.sprite = rankSprites[myRankIndex];
            }
        }
    }
    // 카트가 완주했을 때 호출하는 함수 (도착 도장 쾅!)
    public void RegisterFinish(KartController kart)
    {
        // 이미 명단에 없으면 추가
        if (!finishedKarts.Contains(kart))
        {
            finishedKarts.Add(kart);
            Debug.Log(kart.name + " 완주! 현재 순위: " + finishedKarts.Count + "등");

            // 만약 플레이어라면 게임 종료 처리
            if (kart == playerKart)
            {
                FinishGame();
            }
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

        // 1. 모든 카트 멈춤
        foreach (var kart in allKarts)
        {
            if (kart != null) kart.isControlled = false;
        }

        // 2. 클리어 여부 저장 로직
        if (playerKart != null)
        {
            // 마지막으로 등수 확실하게 계산
            CalculateRanking();
            int myRank = GetRank(playerKart);

            // 3등 안에 들어야 클리어! (조건은 변경 가능)
            if (myRank <= 3)
            {
                // 현재 몇 탄인지 가져옴 (GameData가 없으면 1탄으로 가정)
                int currentStage = (GameData.Instance != null) ? GameData.Instance.currentStage : 1;

                // 다음 스테이지 번호
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

        // 3. 결과창 UI 띄우기
        if (finishUI != null)
        {
            finishUI.SetActive(true);

            if (finalTimeText != null)
                finalTimeText.text = "RECORD: " + FormatTime(timer);

            // (선택) 등수도 텍스트로 보여주기
            if (finalRankText != null)
            {
                int rank = GetRank(playerKart);
                finalRankText.text = rank + (rank == 1 ? "st" : (rank == 2 ? "nd" : (rank == 3 ? "rd" : "th")));
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
}