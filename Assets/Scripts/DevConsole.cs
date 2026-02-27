using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ` 키로 열리는 개발자 콘솔.
/// 패스워드 인증 후 치트 명령어 사용 가능.
/// OnGUI 기반이라 씬에 빈 오브젝트 하나만 만들고 이 스크립트를 붙이면 끝.
/// </summary>
public class DevConsole : MonoBehaviour
{
    // ─── 설정 ───
    private const string PASSWORD = "wasteland777";
    private const int MAX_LOG_LINES = 50;

    // ─── 상태 ───
    private bool isOpen = false;
    private static bool isAuthenticated = false; // static: 씬 전환해도 유지
    private bool isPasswordMode = true;

    private string inputText = "";
    private List<string> logLines = new List<string>();
    private Vector2 scrollPos;

    // 치트 상태
    private bool godMode = false;
    private bool noClip = false;
    private float originalMaxSpeed = -1f;

    // GUI 스타일
    private GUIStyle consoleStyle;
    private GUIStyle inputStyle;
    private GUIStyle logStyle;
    private bool stylesInitialized = false;

    void Awake()
    {
        // 싱글톤: 중복 방지
        DevConsole[] consoles = FindObjectsByType<DevConsole>(FindObjectsSortMode.None);
        if (consoles.Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // ` 키 (BackQuote) 로 콘솔 토글
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            isOpen = !isOpen;
            if (isOpen && !isAuthenticated)
            {
                isPasswordMode = true;
                inputText = "";
            }
        }

        // 콘솔 열려있을 때 게임 입력 차단용
        if (isOpen && godMode)
        {
            ApplyGodMode();
        }
    }

    void LateUpdate()
    {
        // 무적 모드 유지
        if (godMode && !isOpen)
        {
            ApplyGodMode();
        }
    }

    void ApplyGodMode()
    {
        KartController player = FindPlayerKart();
        if (player != null && !player.isShielded)
        {
            player.ActivateShield(999f);
        }
    }

    void InitStyles()
    {
        if (stylesInitialized) return;

        consoleStyle = new GUIStyle(GUI.skin.box);
        consoleStyle.normal.background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.85f));

        inputStyle = new GUIStyle(GUI.skin.textField);
        inputStyle.fontSize = 16;
        inputStyle.normal.textColor = Color.green;
        inputStyle.focused.textColor = Color.green;
        inputStyle.fontStyle = FontStyle.Bold;

        logStyle = new GUIStyle(GUI.skin.label);
        logStyle.fontSize = 14;
        logStyle.normal.textColor = Color.green;
        logStyle.richText = true;
        logStyle.wordWrap = true;

        stylesInitialized = true;
    }

    void OnGUI()
    {
        if (!isOpen) return;

        InitStyles();

        float w = Screen.width * 0.6f;
        float h = Screen.height * 0.5f;
        float x = (Screen.width - w) / 2f;
        float y = 20f;

        // 배경
        GUI.Box(new Rect(x, y, w, h), "", consoleStyle);

        // 제목
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 18;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = new Color(0f, 1f, 0.4f);
        titleStyle.alignment = TextAnchor.MiddleCenter;

        GUI.Label(new Rect(x, y + 5, w, 30), isAuthenticated ? "[ DEV CONSOLE ]" : "[ ACCESS DENIED - ENTER PASSWORD ]", titleStyle);

        // 로그 영역
        float logY = y + 40;
        float logH = h - 80;

        GUILayout.BeginArea(new Rect(x + 10, logY, w - 20, logH));
        scrollPos = GUILayout.BeginScrollView(scrollPos);

        foreach (string line in logLines)
        {
            GUILayout.Label(line, logStyle);
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        // 입력 영역
        float inputY = y + h - 35;

        GUI.Label(new Rect(x + 10, inputY, 20, 25), ">", logStyle);

        GUI.SetNextControlName("ConsoleInput");
        inputText = GUI.TextField(new Rect(x + 25, inputY, w - 40, 25), inputText, inputStyle);
        GUI.FocusControl("ConsoleInput");

        // 엔터키로 입력 처리
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
        {
            if (!string.IsNullOrEmpty(inputText))
            {
                ProcessInput(inputText.Trim());
                inputText = "";
            }
            Event.current.Use();
        }

        // ESC로 닫기
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            isOpen = false;
            Event.current.Use();
        }
    }

    void ProcessInput(string input)
    {
        // 패스워드 모드
        if (!isAuthenticated)
        {
            if (input == PASSWORD)
            {
                isAuthenticated = true;
                isPasswordMode = false;
                Log("<color=yellow>>>> ACCESS GRANTED <<<</color>");
                Log("치트 모드 활성화! <color=cyan>help</color> 를 입력하세요.");
            }
            else
            {
                Log("<color=red>잘못된 패스워드입니다.</color>");
            }
            return;
        }

        // 명령어 처리
        Log("<color=white>> " + input + "</color>");

        string[] parts = input.ToLower().Split(' ');
        string cmd = parts[0];

        switch (cmd)
        {
            case "help":
                ShowHelp();
                break;

            case "boost":
                CmdBoost();
                break;

            case "maxspeed":
                CmdMaxSpeed(parts);
                break;

            case "god":
                CmdGodMode();
                break;

            case "item":
                CmdItem(parts);
                break;

            case "lap":
                CmdLap(parts);
                break;

            case "win":
                CmdWin();
                break;

            case "rank":
                CmdRank();
                break;

            case "tp":
                CmdTeleport(parts);
                break;

            case "allitem":
                CmdAllItem();
                break;

            case "noclip":
                CmdNoClip();
                break;

            case "reset":
                CmdReset();
                break;

            case "clear":
                logLines.Clear();
                break;

            default:
                Log("<color=red>알 수 없는 명령어: " + cmd + "</color>");
                Log("<color=cyan>help</color> 를 입력하면 명령어 목록을 볼 수 있습니다.");
                break;
        }
    }

    // ─── 명령어 구현 ───

    void ShowHelp()
    {
        Log("<color=yellow>=== 명령어 목록 ===</color>");
        Log("<color=cyan>boost</color>        - 강력한 부스트");
        Log("<color=cyan>maxspeed [값]</color> - 최고 속도 변경 (예: maxspeed 50)");
        Log("<color=cyan>god</color>          - 무적 모드 토글");
        Log("<color=cyan>item [이름]</color>  - 아이템 즉시 획득");
        Log("   → mushroom, banana, bomb, fakebox, oil, shield");
        Log("<color=cyan>lap [번호]</color>   - 랩 수 강제 변경");
        Log("<color=cyan>win</color>          - 즉시 완주");
        Log("<color=cyan>rank</color>         - 현재 순위 표시");
        Log("<color=cyan>tp [번호]</color>    - 체크포인트로 텔레포트");
        Log("<color=cyan>allitem</color>      - 모든 AI 아이템 강제 사용");
        Log("<color=cyan>noclip</color>       - 콜라이더 무시 토글");
        Log("<color=cyan>reset</color>        - 모든 치트 해제");
        Log("<color=cyan>clear</color>        - 콘솔 로그 지우기");
    }

    void CmdBoost()
    {
        KartController player = FindPlayerKart();
        if (player == null) { Log("<color=red>플레이어 카트를 찾을 수 없습니다.</color>"); return; }

        player.AddExternalBoost(200f);
        Log("<color=yellow>부스트 발동!</color>");
    }

    void CmdMaxSpeed(string[] parts)
    {
        KartController player = FindPlayerKart();
        if (player == null) { Log("<color=red>플레이어 카트를 찾을 수 없습니다.</color>"); return; }

        if (parts.Length < 2 || !float.TryParse(parts[1], out float newSpeed))
        {
            Log("현재 최고 속도: " + player.maxSpeed);
            Log("사용법: <color=cyan>maxspeed [값]</color>");
            return;
        }

        if (originalMaxSpeed < 0) originalMaxSpeed = player.maxSpeed;
        player.maxSpeed = newSpeed;
        Log("<color=yellow>최고 속도 변경: " + newSpeed + "</color>");
    }

    void CmdGodMode()
    {
        godMode = !godMode;
        KartController player = FindPlayerKart();

        if (!godMode && player != null)
        {
            player.BreakShield();
        }

        Log(godMode
            ? "<color=yellow>무적 모드 ON - 모든 공격을 무시합니다!</color>"
            : "<color=yellow>무적 모드 OFF</color>");
    }

    void CmdItem(string[] parts)
    {
        KartController player = FindPlayerKart();
        if (player == null) { Log("<color=red>플레이어 카트를 찾을 수 없습니다.</color>"); return; }

        InventoryManager inv = player.GetComponent<InventoryManager>();
        if (inv == null) { Log("<color=red>InventoryManager를 찾을 수 없습니다.</color>"); return; }

        if (parts.Length < 2)
        {
            Log("사용법: <color=cyan>item [mushroom|banana|bomb|fakebox|oil|shield]</color>");
            return;
        }

        InventoryManager.ItemType itemType;
        switch (parts[1])
        {
            case "mushroom": itemType = InventoryManager.ItemType.Mushroom; break;
            case "banana": itemType = InventoryManager.ItemType.Banana; break;
            case "bomb": itemType = InventoryManager.ItemType.Bomb; break;
            case "fakebox": itemType = InventoryManager.ItemType.FakeBox; break;
            case "oil": itemType = InventoryManager.ItemType.Oil; break;
            case "shield": itemType = InventoryManager.ItemType.Shield; break;
            default:
                Log("<color=red>알 수 없는 아이템: " + parts[1] + "</color>");
                return;
        }

        inv.currentItem = itemType;
        inv.hasItem = true;
        inv.isRolling = false;
        Log("<color=yellow>아이템 획득: " + itemType + "</color>");
    }

    void CmdLap(string[] parts)
    {
        KartController player = FindPlayerKart();
        if (player == null) { Log("<color=red>플레이어 카트를 찾을 수 없습니다.</color>"); return; }

        if (parts.Length < 2 || !int.TryParse(parts[1], out int lap))
        {
            Log("현재 랩: " + player.currentLap);
            Log("사용법: <color=cyan>lap [번호]</color>");
            return;
        }

        player.currentLap = lap;
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm != null) gm.UpdateLapUI(lap);
        Log("<color=yellow>랩 변경: " + lap + "</color>");
    }

    void CmdWin()
    {
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm == null) { Log("<color=red>GameManager를 찾을 수 없습니다.</color>"); return; }

        gm.UpdateLapUI(gm.totalLaps + 1);
        Log("<color=yellow>즉시 완주!</color>");
        isOpen = false;
    }

    void CmdRank()
    {
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm == null) { Log("<color=red>GameManager를 찾을 수 없습니다.</color>"); return; }

        Log("<color=yellow>=== 현재 순위 ===</color>");
        for (int i = 0; i < gm.sortedKarts.Count; i++)
        {
            var kart = gm.sortedKarts[i];
            string marker = kart.isAI ? "" : " <color=cyan>← YOU</color>";
            Log((i + 1) + "등: " + kart.gameObject.name + marker);
        }
    }

    void CmdTeleport(string[] parts)
    {
        KartController player = FindPlayerKart();
        if (player == null) { Log("<color=red>플레이어 카트를 찾을 수 없습니다.</color>"); return; }

        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm == null || gm.checkpoints == null) { Log("<color=red>체크포인트 정보가 없습니다.</color>"); return; }

        if (parts.Length < 2 || !int.TryParse(parts[1], out int cpIndex))
        {
            Log("사용법: <color=cyan>tp [체크포인트 번호 0~" + (gm.checkpoints.Length - 1) + "]</color>");
            return;
        }

        if (cpIndex < 0 || cpIndex >= gm.checkpoints.Length || gm.checkpoints[cpIndex] == null)
        {
            Log("<color=red>잘못된 체크포인트 번호입니다. (0~" + (gm.checkpoints.Length - 1) + ")</color>");
            return;
        }

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        player.transform.position = gm.checkpoints[cpIndex].transform.position + Vector3.up * 2f;
        player.transform.rotation = gm.checkpoints[cpIndex].transform.rotation;
        Log("<color=yellow>체크포인트 " + cpIndex + "로 텔레포트!</color>");
    }

    void CmdAllItem()
    {
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm == null) { Log("<color=red>GameManager를 찾을 수 없습니다.</color>"); return; }

        int count = 0;
        foreach (var kart in gm.sortedKarts)
        {
            if (kart.isAI)
            {
                InventoryManager inv = kart.GetComponent<InventoryManager>();
                if (inv != null && inv.hasItem)
                {
                    kart.isItemUseInput = true;
                    count++;
                }
            }
        }
        Log("<color=yellow>" + count + "대의 AI가 아이템을 사용했습니다!</color>");
    }

    void CmdNoClip()
    {
        KartController player = FindPlayerKart();
        if (player == null) { Log("<color=red>플레이어 카트를 찾을 수 없습니다.</color>"); return; }

        noClip = !noClip;

        Collider[] colliders = player.GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = !noClip;
        }

        // NoClip 시 중력 무시
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null) rb.useGravity = !noClip;

        Log(noClip
            ? "<color=yellow>NoClip ON - 벽 통과 가능!</color>"
            : "<color=yellow>NoClip OFF</color>");
    }

    void CmdReset()
    {
        KartController player = FindPlayerKart();

        // 무적 해제
        if (godMode && player != null)
        {
            godMode = false;
            player.BreakShield();
        }

        // 속도 복원
        if (originalMaxSpeed > 0 && player != null)
        {
            player.maxSpeed = originalMaxSpeed;
            originalMaxSpeed = -1f;
        }

        // NoClip 해제
        if (noClip && player != null)
        {
            noClip = false;
            Collider[] colliders = player.GetComponentsInChildren<Collider>();
            foreach (var col in colliders) col.enabled = true;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.useGravity = true;
        }

        godMode = false;
        noClip = false;

        Log("<color=yellow>모든 치트가 해제되었습니다.</color>");
    }

    // ─── 유틸리티 ───

    KartController FindPlayerKart()
    {
        // GameManager에서 먼저 찾기
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm != null && gm.playerKart != null) return gm.playerKart;

        // 없으면 직접 탐색
        KartController[] karts = FindObjectsByType<KartController>(FindObjectsSortMode.None);
        foreach (var k in karts)
        {
            if (!k.isAI) return k;
        }
        return null;
    }

    void Log(string message)
    {
        logLines.Add(message);
        if (logLines.Count > MAX_LOG_LINES)
            logLines.RemoveAt(0);

        // 스크롤을 아래로
        scrollPos = new Vector2(0, float.MaxValue);
    }

    Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
