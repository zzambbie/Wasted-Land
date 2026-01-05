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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}