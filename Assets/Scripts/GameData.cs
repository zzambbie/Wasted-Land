using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData Instance; // 어디서든 접근 가능하게

    public int selectedKartIndex = 0; // 선택한 카트 번호 (0, 1, 2...)
    public int currentStage = 1;     // 현재 도전할 스테이지 번호

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 나를 파괴하지 마!
        }
        else
        {
            Destroy(gameObject); // 이미 있으면 나는 꺼진다 (중복 방지)
        }
    }
}