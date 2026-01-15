using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StoryMapController : MonoBehaviour
{
    public GameObject popupPanel;     // 팝업창 전체
    public TextMeshProUGUI storyText; // 스토리 내용

    public Button[] stageButtons;

    private int tempSelectedStage;    // 잠시 기억할 스테이지 번호

    void Start()
    {
        // 시작하자마자 잠금 상태 확인
        CheckStageLocks();

        popupPanel.SetActive(false);
    }
    void CheckStageLocks()
    {
        for (int i = 0; i < stageButtons.Length; i++)
        {
            int stageNum = i + 1; // 버튼 인덱스 0 -> 스테이지 1

            // 1탄은 무조건 열림
            if (stageNum == 1)
            {
                stageButtons[i].interactable = true; // 클릭 가능
                continue;
            }

            // 저장된 데이터 확인 (1이면 해제된 것, 0이면 잠긴 것)
            int isUnlocked = PlayerPrefs.GetInt("Stage_" + stageNum + "_Unlocked", 0);

            if (isUnlocked == 1)
            {
                stageButtons[i].interactable = true; // 해제됨 -> 클릭 가능
                // (이미지 색을 밝게 하거나 자물쇠 아이콘을 없애는 로직 추가 가능)
            }
            else
            {
                stageButtons[i].interactable = false; // 잠김 -> 클릭 불가
                // (이미지 색을 어둡게 하거나 자물쇠 아이콘 띄우기)
            }
        }
    }

    // 1. 지도 위의 네모 버튼을 누르면 실행
    public void OnClickStage(int stageNum)
    {
        tempSelectedStage = stageNum;
        popupPanel.SetActive(true); // 팝업 켜기
        storyText.text = "Chapter " + stageNum + "\n\n도전을 시작합니다.";
    }

    // 2. 팝업 안의 '확인(캐릭터 선택)' 버튼 누르면 실행
    public void OnClickGoToCharSelect()
    {
        // GameData에 "나 1탄 할거야"라고 저장
        if (GameData.Instance != null)
            GameData.Instance.currentStage = tempSelectedStage;

        // 캐릭터 선택 씬으로 이동!
        SceneManager.LoadScene("CharSelectScene");
    }

    // 팝업 닫기 버튼용
    public void ClosePopup()
    {
        popupPanel.SetActive(false);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
        { 
            PlayerPrefs.DeleteAll();
            Debug.Log("저장 데이터 초기화됨!");
            CheckStageLocks(); // 다시 잠금
        }
    }
}