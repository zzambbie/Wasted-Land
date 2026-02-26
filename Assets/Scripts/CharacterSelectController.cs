using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class CharacterSelectController : MonoBehaviour
{
    [Header("3D 모델링")]
    public GameObject[] kartPrefabs; // 카트 프리팹 4개
    public Transform spawnPoint;     // 단상 위치

    [Header("UI 연결")]
    public TextMeshProUGUI statusText; // 스탯 표시

    [Header("회전 연출")]
    public float rotateSpeed = 30f; // 초당 회전 각도 (360 / 30 = 약 12초에 한 바퀴)

    private GameObject currentModel;
    private int currentIndex = 0;

    void Start()
    {
        ShowKart(0);
    }

    void Update()
    {
        // 카트 모델 천천히 회전 (전신 보여주기)
        if (currentModel != null)
        {
            currentModel.transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);
        }

        // 1. 키보드 방향키 입력 (Prev/Next)
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            PrevKart();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            NextKart();
        }

        // 2. 엔터키로 게임 시작
        if (Input.GetKeyDown(KeyCode.Return))
        {
            OnClickStartGame();
        }
    }

    // --- 화살표 버튼용 함수 ---
    public void NextKart()
    {
        currentIndex++;
        if (currentIndex >= kartPrefabs.Length) currentIndex = 0;
        ShowKart(currentIndex);
    }

    public void PrevKart()
    {
        currentIndex--;
        if (currentIndex < 0) currentIndex = kartPrefabs.Length - 1;
        ShowKart(currentIndex);
    }

    // --- 오른쪽 그리드 버튼용 함수 (직접 선택) ---
    public void SelectKartBtn(int index)
    {
        // 범위를 벗어나지 않게 안전장치
        if (index >= 0 && index < kartPrefabs.Length)
        {
            currentIndex = index; // 현재 번호를 누른 버튼 번호로 갱신
            ShowKart(currentIndex);
        }
    }

    // --- 공통: 카트 보여주기 함수 ---
    void ShowKart(int index)
    {
        if (currentModel != null) Destroy(currentModel);

        if (kartPrefabs == null || kartPrefabs.Length <= index || kartPrefabs[index] == null) return;

        currentModel = Instantiate(kartPrefabs[index], spawnPoint.position, spawnPoint.rotation);

        Rigidbody rb = currentModel.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        KartController kart = currentModel.GetComponent<KartController>();
        if (kart)
        {
            kart.enabled = false;

            // 이펙트/사운드 끄기
            ParticleSystem[] particles = currentModel.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem p in particles) { p.Stop(); p.gameObject.SetActive(false); }
            AudioSource[] audios = currentModel.GetComponentsInChildren<AudioSource>();
            foreach (AudioSource a in audios) { a.Stop(); a.enabled = false; }

            if (statusText != null)
            {
                statusText.text = $"Speed: {kart.maxSpeed}\nAccel: {kart.acceleration}\nWeight: {kart.weight}";
            }
        }
    }

    public void OnClickStartGame()
    {
        if (GameData.Instance != null)
            GameData.Instance.selectedKartIndex = currentIndex;

        int stage = 1;
        if (GameData.Instance != null) stage = GameData.Instance.currentStage;

        string targetScene = "Track_" + stage;

        // 해당 트랙 씬이 빌드에 없으면 Track_1로 폴백
        if (SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + targetScene + ".unity") == -1
            && SceneUtility.GetBuildIndexByScenePath(targetScene) == -1)
        {
            Debug.LogWarning(targetScene + " 씬이 아직 없습니다. Track_1으로 이동합니다.");
            targetScene = "Track_1";
        }

        SceneManager.LoadScene(targetScene);
    }
}