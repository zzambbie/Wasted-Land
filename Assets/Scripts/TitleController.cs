using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 타이틀 씬 전체 관리.
/// Canvas 안의 자식 오브젝트를 Transform.Find()로 자동 탐색.
/// Inspector 연결 불필요.
/// 
/// 구조:
///   Canvas
///     ├─ TitleText
///     ├─ StoryModeButton
///     ├─ MultiplayButton
///     ├─ FrenzyButton
///     ├─ SettingsButton
///     ├─ QuitButton
///     ├─ SettingsPanel (볼륨/카메라 설정)
///     ├─ QuitPanel (종료 확인)
///     └─ PreparingPopup
/// </summary>
public class TitleController : MonoBehaviour
{
    GameObject settingsPanel;
    GameObject quitPanel;
    GameObject preparingPopup;
    TextMeshProUGUI preparingText;

    Slider volumeSlider;
    Slider cameraDistSlider;
    TextMeshProUGUI volumeValueText;
    TextMeshProUGUI cameraDistValueText;

    void Start()
    {
        // ★ "Canvas"라는 이름의 캔버스를 정확히 찾음
        // (PauseCanvas 등 다른 캔버스가 있으면 FindFirstObjectByType이 잘못된 캔버스를 반환할 수 있음)
        Canvas canvas = null;
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in allCanvases)
        {
            if (c.gameObject.name == "Canvas")
            {
                canvas = c;
                break;
            }
        }
        if (canvas == null)
        {
            // fallback: 아무 캔버스나 사용
            canvas = FindFirstObjectByType<Canvas>();
        }
        if (canvas == null) return;
        Transform root = canvas.transform;

        // 패널 탐색
        settingsPanel = FindChild(root, "SettingsPanel");
        quitPanel = FindChild(root, "QuitPanel");

        Transform popT = root.Find("PreparingPopup");
        if (popT != null)
        {
            preparingPopup = popT.gameObject;
            preparingText = popT.GetComponentInChildren<TextMeshProUGUI>();
        }

        // 메인 버튼 5개 (전부 Canvas 직속 자식)
        BindBtn(root, "StoryModeButton", OnClickStoryMode);
        BindBtn(root, "MultiplayButton", OnClickMultiplay);
        BindBtn(root, "FrenzyButton", OnClickFrenzy);
        BindBtn(root, "SettingsButton", ShowSettings);
        BindBtn(root, "QuitButton", ShowQuitConfirm);

        // 환경설정 패널 내부
        if (settingsPanel != null)
        {
            Transform sp = settingsPanel.transform;
            BindBtn(sp, "SettingsBackButton", HideSettings);

            Transform volT = sp.Find("VolumeSlider");
            if (volT != null)
            {
                volumeSlider = volT.GetComponent<Slider>();
                volumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }
            Transform camT = sp.Find("CameraDistSlider");
            if (camT != null)
            {
                cameraDistSlider = camT.GetComponent<Slider>();
                cameraDistSlider.value = PlayerPrefs.GetFloat("CameraDistance", 0.5f);
                cameraDistSlider.onValueChanged.AddListener(OnCameraDistChanged);
            }
            volumeValueText = FindTMP(sp, "VolumeValue");
            cameraDistValueText = FindTMP(sp, "CameraDistValue");
            UpdateVolumeText();
            UpdateCameraDistText();
        }

        // 종료 확인 패널 내부
        if (quitPanel != null)
        {
            BindBtn(quitPanel.transform, "QuitYesButton", QuitGame);
            BindBtn(quitPanel.transform, "QuitNoButton", HideQuitConfirm);
        }

        // 패널 숨기기 (바인딩 후!)
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (quitPanel != null) quitPanel.SetActive(false);
        if (preparingPopup != null) preparingPopup.SetActive(false);
    }

    // ============ 유틸 ============
    GameObject FindChild(Transform p, string n)
    {
        Transform t = p.Find(n);
        return t != null ? t.gameObject : null;
    }
    void BindBtn(Transform p, string n, UnityEngine.Events.UnityAction a)
    {
        Transform t = p.Find(n);
        if (t != null)
        {
            Button btn = t.GetComponent<Button>();
            if (btn != null) { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(a); }
        }
    }
    TextMeshProUGUI FindTMP(Transform p, string n)
    {
        Transform t = p.Find(n);
        return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }

    // ============ 메인 버튼 ============
    void OnClickStoryMode() { SceneManager.LoadScene("StoryMapScene"); }
    void OnClickMultiplay() { StartCoroutine(ShowPreparing("멀티플레이는 아직 준비단계입니다!")); }
    void OnClickFrenzy() { StartCoroutine(ShowPreparing("광란의 질주는 아직 준비단계입니다!")); }

    // ============ 환경설정 ============
    public void ShowSettings() { if (settingsPanel != null) settingsPanel.SetActive(true); }
    public void HideSettings() { if (settingsPanel != null) settingsPanel.SetActive(false); }

    void OnVolumeChanged(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat("MasterVolume", v);
        UpdateVolumeText();
    }
    void OnCameraDistChanged(float v)
    {
        PlayerPrefs.SetFloat("CameraDistance", v);
        UpdateCameraDistText();
    }
    void UpdateVolumeText()
    {
        if (volumeValueText != null && volumeSlider != null)
            volumeValueText.text = Mathf.RoundToInt(volumeSlider.value * 100) + "%";
    }
    void UpdateCameraDistText()
    {
        if (cameraDistValueText != null && cameraDistSlider != null)
            cameraDistValueText.text = Mathf.RoundToInt(cameraDistSlider.value * 100) + "%";
    }

    // ============ 종료 ============
    public void ShowQuitConfirm() { if (quitPanel != null) quitPanel.SetActive(true); }
    public void HideQuitConfirm() { if (quitPanel != null) quitPanel.SetActive(false); }
    void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ============ 준비단계 팝업 ============
    IEnumerator ShowPreparing(string msg)
    {
        if (preparingPopup != null)
        {
            if (preparingText != null) preparingText.text = msg;
            preparingPopup.SetActive(true);
            yield return new WaitForSeconds(2f);
            preparingPopup.SetActive(false);
        }
    }
}