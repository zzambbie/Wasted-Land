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
    public float rotateSpeed = 30f; // 초당 회전 각도 (약 12초에 한 바퀴)

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

        // === 캐릭터 선택 화면 전용 보정 ===
        string kartName = kartPrefabs[index].name;

        if (kartName.Contains("Bettery"))
        {
            // 배터리 카: 게임용 세팅이라 선택화면에서 뒤를 보므로 Y축 180도 보정
            currentModel.transform.Rotate(0f, 180f, 0f);
        }
        else if (kartName.Contains("Racing"))
        {
            // 레이싱 카: 뒤를 보고 도는 문제 → Y축 180도 보정
            currentModel.transform.Rotate(0f, 180f, 0f);
        }
        else if (kartName.Contains("Doll"))
        {
            // 인형 카트: Cube 자식 회전 보정 + 뒤를 보므로 Y축 180도 보정
            Transform cubeChild = currentModel.transform.Find("Cube");
            if (cubeChild != null)
            {
                cubeChild.localRotation = Quaternion.Euler(0f, -90f, 0f);
            }
            currentModel.transform.Rotate(0f, 180f, 0f);
        }
        else if (kartName.Contains("Cookie"))
        {
            // 쿠키 카트: 뒤를 보므로 Y축 180도 보정
            currentModel.transform.Rotate(0f, 180f, 0f);
        }

        Rigidbody rb = currentModel.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        // === 이펙트/사운드 끄기 (kart 유무와 관계없이 항상 실행) ===
        ParticleSystem[] particles = currentModel.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem p in particles) { p.Stop(); p.gameObject.SetActive(false); }
        AudioSource[] audios = currentModel.GetComponentsInChildren<AudioSource>(true);
        foreach (AudioSource a in audios) { a.Stop(); a.enabled = false; }

        // 이펙트/유틸리티 오브젝트 비활성화 (분홍색 이펙트 방지)
        // Cube와 Car(모델) 이외의 MeshRenderer를 가진 자식은 모두 비활성화
        MeshRenderer[] renderers = currentModel.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer mr in renderers)
        {
            // 루트 오브젝트 자체는 건드리지 않음
            if (mr.gameObject == currentModel) continue;
            
            string objName = mr.gameObject.name;
            // Cube(메시 모델)와 Car(중첩 프리팹 모델)만 남기고 나머지 비활성화
            if (objName != "Cube" && objName != "Car")
            {
                mr.gameObject.SetActive(false);
            }
        }

        KartController kart = currentModel.GetComponent<KartController>();
        if (kart)
        {
            kart.enabled = false;

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