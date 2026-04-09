using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 레이스 중 일시정지.
/// ESC → 일시정지 패널, 환경설정은 인게임 설정 패널로 연결.
/// </summary>
public class PauseManager : MonoBehaviour
{
    [Header("일시정지 UI")]
    public GameObject pausePanel;

    [Header("인게임 환경설정 패널")]
    public GameObject settingsPanel;
    public Slider volumeSlider;
    public Slider cameraDistSlider;
    public TextMeshProUGUI volumeValueText;
    public TextMeshProUGUI cameraDistValueText;

    [Header("버튼 (Inspector에서 연결)")]
    public Button resumeButton;
    public Button restartButton;
    public Button settingsButton;
    public Button quitButton;
    public Button settingsBackButton;

    private bool isPaused = false;
    private GraphicRaycaster canvasRaycaster; // PauseCanvas의 레이캐스터

    void Start()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // ★ PauseCanvas의 GraphicRaycaster를 가져와서 평소에 꺼둠
        // → 이걸 안 하면 PauseCanvas가 기존 Canvas 위에서 클릭을 가로챔!
        canvasRaycaster = GetComponent<GraphicRaycaster>();
        if (canvasRaycaster == null)
            canvasRaycaster = GetComponentInParent<GraphicRaycaster>();
        if (canvasRaycaster != null)
            canvasRaycaster.enabled = false;

        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (quitButton != null) quitButton.onClick.AddListener(QuitRace);
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(CloseSettings);

        // 환경설정 슬라이더 초기화
        if (volumeSlider != null)
        {
            volumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            UpdateVolumeText();
        }
        if (cameraDistSlider != null)
        {
            cameraDistSlider.value = PlayerPrefs.GetFloat("CameraDistance", 0.5f);
            cameraDistSlider.onValueChanged.AddListener(OnCameraDistChanged);
            UpdateCameraDistText();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm != null && gm.IsGameFinished) return;

            // 설정 패널이 열려있으면 설정 닫기
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
                return;
            }

            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (canvasRaycaster != null) canvasRaycaster.enabled = true; // ★ 레이캐스터 켜기
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (canvasRaycaster != null) canvasRaycaster.enabled = false; // ★ 레이캐스터 끄기
    }

    // 처음부터 다시 시작 - timeScale 반드시 1로!
    public void RestartGame()
    {
        isPaused = false;
        Time.timeScale = 1f;  // ★ 이게 없으면 카운트다운 WaitForSeconds가 안 돌아감!
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 환경설정 열기 (일시정지 패널 위에 겹침)
    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    // 환경설정 닫기
    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    // 레이스 종료
    public void QuitRace()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("StoryMapScene");
    }

    // --- 환경설정 기능 ---
    void OnVolumeChanged(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat("MasterVolume", v);
        PlayerPrefs.Save();
        UpdateVolumeText();
    }
    void OnCameraDistChanged(float v)
    {
        PlayerPrefs.SetFloat("CameraDistance", v);
        PlayerPrefs.Save();
        UpdateCameraDistText();

        // 실시간으로 카메라 거리 반영
        KartCamera cam = FindFirstObjectByType<KartCamera>();
        if (cam != null) cam.defaultDistance = Mathf.Lerp(2.5f, 6.0f, v);
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
}
