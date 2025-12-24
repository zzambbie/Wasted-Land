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

    private GameObject currentModel;
    private int currentIndex = 0;

    void Start()
    {
        ShowKart(0);
    }

    void Update()
    {
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

    // --- [신규] 오른쪽 그리드 버튼용 함수 (직접 선택) ---
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

        SceneManager.LoadScene("Track_" + stage);
    }
}