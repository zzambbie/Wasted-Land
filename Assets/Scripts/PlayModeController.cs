using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayModeController : MonoBehaviour
{
    [Header("플레이모드 선택 패널")]
    public GameObject playModePanel;     // 플레이모드 선택 전체 패널

    [Header("준비 중 팝업")]
    public GameObject preparingPopup;    // "아직 준비단계입니다" 팝업
    public TextMeshProUGUI preparingText;

    [Header("버튼")]
    public Button storyModeButton;       // 스토리 모드
    public Button multiplayButton;       // 멀티플레이
    public Button frenzyButton;          // 광란의 질주
    public Button backButton;            // 뒤로가기

    [Header("버튼 이미지 (일반/선택시)")]
    public Image storyBtnImage;
    public Sprite storyNormal;
    public Sprite storyHover;

    public Image multiBtnImage;
    public Sprite multiNormal;
    public Sprite multiHover;

    public Image frenzyBtnImage;
    public Sprite frenzyNormal;
    public Sprite frenzyHover;

    void Start()
    {
        if (playModePanel != null)
            playModePanel.SetActive(false);
        if (preparingPopup != null)
            preparingPopup.SetActive(false);

        // 버튼 이벤트 연결
        if (storyModeButton != null)
            storyModeButton.onClick.AddListener(OnClickStoryMode);
        if (multiplayButton != null)
            multiplayButton.onClick.AddListener(OnClickMultiplay);
        if (frenzyButton != null)
            frenzyButton.onClick.AddListener(OnClickFrenzy);
        if (backButton != null)
            backButton.onClick.AddListener(OnClickBack);
    }

    // 타이틀에서 "플레이" 버튼을 누르면 호출
    public void ShowPlayModePanel()
    {
        if (playModePanel != null)
            playModePanel.SetActive(true);
    }

    // 스토리 모드 → 스토리맵으로 이동
    public void OnClickStoryMode()
    {
        SceneManager.LoadScene("StoryMapScene");
    }

    // 멀티플레이 → 준비 중 팝업 표시
    public void OnClickMultiplay()
    {
        StartCoroutine(ShowPreparingPopup("멀티플레이는 아직 준비단계입니다!"));
    }

    // 광란의 질주 → 준비 중 팝업 표시
    public void OnClickFrenzy()
    {
        StartCoroutine(ShowPreparingPopup("광란의 질주는 아직 준비단계입니다!"));
    }

    // 뒤로가기 → 패널 닫기
    public void OnClickBack()
    {
        if (playModePanel != null)
            playModePanel.SetActive(false);
    }

    // "준비단계" 팝업을 2초간 표시 후 자동으로 사라짐
    IEnumerator ShowPreparingPopup(string message)
    {
        if (preparingPopup != null)
        {
            if (preparingText != null)
                preparingText.text = message;

            preparingPopup.SetActive(true);
            yield return new WaitForSeconds(2.0f);
            preparingPopup.SetActive(false);
        }
        else
        {
            // 팝업이 없으면 최소한 로그 출력
            Debug.Log(message);
            yield break;
        }
    }

    // --- 호버 이벤트 (EventTrigger 또는 코드로 연결) ---
    public void OnStoryHoverEnter()
    {
        if (storyBtnImage != null && storyHover != null)
            storyBtnImage.sprite = storyHover;
    }
    public void OnStoryHoverExit()
    {
        if (storyBtnImage != null && storyNormal != null)
            storyBtnImage.sprite = storyNormal;
    }

    public void OnMultiHoverEnter()
    {
        if (multiBtnImage != null && multiHover != null)
            multiBtnImage.sprite = multiHover;
    }
    public void OnMultiHoverExit()
    {
        if (multiBtnImage != null && multiNormal != null)
            multiBtnImage.sprite = multiNormal;
    }

    public void OnFrenzyHoverEnter()
    {
        if (frenzyBtnImage != null && frenzyHover != null)
            frenzyBtnImage.sprite = frenzyHover;
    }
    public void OnFrenzyHoverExit()
    {
        if (frenzyBtnImage != null && frenzyNormal != null)
            frenzyBtnImage.sprite = frenzyNormal;
    }
}
