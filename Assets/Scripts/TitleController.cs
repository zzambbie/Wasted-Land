using UnityEngine;
using UnityEngine.SceneManagement; // 씬 이동을 위해 필수!

public class TitleController : MonoBehaviour
{
    // 1. 스토리 모드 버튼
    public void OnClickStoryMode()
    {
        // 스토리 맵 씬으로 이동
        // (Build Settings에 이 이름이 등록되어 있어야 함)
        SceneManager.LoadScene("StoryMapScene");
    }
    // 2. 멀티 플레이 버튼 (준비 중)
    public void OnClickMultiplay()
    {
        Debug.Log("멀티 플레이는 아직 공사 중입니다!");
        // 나중에 멀티 로비 씬으로 연결할 예정
    }
    // 3. 옵션 버튼
    public void OnClickOption()
    {
        Debug.Log("옵션 창은 나중에 만들 거예요!");
        // 나중에 여기에 optionPanel.SetActive(true); 넣으면 됨
    }
    // 4. 게임 종료 버튼
    public void OnClickQuit()
    {
        Debug.Log("게임 종료!");
        Application.Quit(); // 빌드된 게임(exe/apk)에서만 꺼짐. 에디터에선 안 꺼짐.
    }
}