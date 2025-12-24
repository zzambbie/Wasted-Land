using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class StoryMapController : MonoBehaviour
{
    public GameObject popupPanel;     // 팝업창 전체
    public TextMeshProUGUI storyText; // 스토리 내용

    private int tempSelectedStage;    // 잠시 기억할 스테이지 번호

    // 1. 지도 위의 네모 버튼을 누르면 실행
    public void OnClickStage(int stageNum)
    {
        tempSelectedStage = stageNum;
        popupPanel.SetActive(true); // 팝업 켜기

        // 스테이지별 텍스트 (나중엔 파일에서 불러오겠지만 지금은 하드코딩)
        if (stageNum == 1) storyText.text = "Chapter 1\n\n폐차장의 입구입니다.";
        else if (stageNum == 2) storyText.text = "Chapter 2\n\n위험한 프레스기 공장.";
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
}