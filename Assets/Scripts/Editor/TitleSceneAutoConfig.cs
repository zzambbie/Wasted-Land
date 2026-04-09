using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TitleScene UI 전체 자동 생성
/// 구조: 5개 메인 버튼(이미지) + 환경설정 패널 + 종료 확인 패널
/// 모든 패널은 ACTIVE 상태로 생성 (TitleController.Start()에서 숨김)
/// </summary>
public class TitleSceneAutoConfig : EditorWindow
{
    [MenuItem("Tools/UI 자동 셋업/5. TitleScene 전체 자동 세팅")]
    static void AutoSetup()
    {
        // Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject co = new GameObject("Canvas");
            canvas = co.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = co.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            co.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(co, "Canvas");
        }
        Transform root = canvas.transform;

        // EventSystem
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(es, "EventSystem");
        }

        // 기존 정리
        string[] toKill = { "PlayModePanel", "SettingsPanel", "QuitPanel",
            "Btn_Play", "Btn_Option", "Btn_Quit", "TitleText", "PreparingPopup",
            "StoryModeButton", "MultiplayButton", "FrenzyButton",
            "SettingsButton", "QuitButton" };
        foreach (string n in toKill) Kill(root, n);

        // =========================================
        //  타이틀
        // =========================================
        MakeTMP("TitleText", root, "WASTED LAND", 72, Color.white,
            new Vector2(0.5f, 1f), new Vector2(700, 120), new Vector2(0, -60));

        // =========================================
        //  5개 메인 버튼 (UI 이미지 사용!)
        // =========================================
        // 1. 스토리 모드
        GameObject storyBtn = ImgBtn("StoryModeButton", root,
            "Assets/ui이미지/플레이모드ui/스토리모드.png",
            new Vector2(500, 100), new Vector2(0, 80));
        ImgIcon("StoryIcon", storyBtn.transform,
            "Assets/ui이미지/플레이모드ui/스토리모드 아이콘.png", true);

        // 2. 멀티플레이
        GameObject multiBtn = ImgBtn("MultiplayButton", root,
            "Assets/ui이미지/플레이모드ui/멀티플레이.png",
            new Vector2(500, 100), new Vector2(0, -40));
        ImgIcon("MultiIcon", multiBtn.transform,
            "Assets/ui이미지/플레이모드ui/멀티플레이 아이콘.png", false);

        // 3. 광란의 질주
        GameObject frenzyBtn = ImgBtn("FrenzyButton", root,
            "Assets/ui이미지/플레이모드ui/광란의 질주 버튼.png",
            new Vector2(500, 100), new Vector2(0, -160));
        ImgIcon("FrenzyIcon", frenzyBtn.transform,
            "Assets/ui이미지/플레이모드ui/광란의 질주 아이콘.png", true);

        // 4. 환경설정
        ImgBtn("SettingsButton", root,
            "Assets/ui이미지/일시정지 ui/환경설정.png",
            new Vector2(250, 80), new Vector2(-130, -290));

        // 5. 게임 종료
        ImgBtn("QuitButton", root,
            "Assets/ui이미지/일시정지 ui/레이스 종료.png",
            new Vector2(250, 80), new Vector2(130, -290));

        // =========================================
        //  준비단계 팝업 (Canvas 직속)
        // =========================================
        GameObject popup = new GameObject("PreparingPopup", typeof(RectTransform));
        popup.transform.SetParent(root, false);
        SetRect(popup, new Vector2(650, 100), new Vector2(0, 150));
        Image popBg = popup.AddComponent<Image>();
        popBg.color = new Color(0.08f, 0.08f, 0.08f, 0.93f);
        MakeTMPChild(popup.transform, "아직 준비단계입니다!", 36);
        // Active 상태로 남김 - TitleController가 숨김

        // =========================================
        //  환경설정 패널
        // =========================================
        MakeSettingsPanel(root);

        // =========================================
        //  종료 확인 패널
        // =========================================
        MakeQuitPanel(root);

        // TitleController 확인
        GameObject tm = GameObject.Find("TitleManager");
        if (tm != null && tm.GetComponent<TitleController>() == null)
            tm.AddComponent<TitleController>();

        // 이전 PlayModeCanvas 정리
        PlayModeController old = Object.FindFirstObjectByType<PlayModeController>();
        if (old != null) Object.DestroyImmediate(old.gameObject);

        // DevConsole (치트 엔진) 자동 생성 - DontDestroyOnLoad이라 타이틀씬에만 있으면 됨
        if (Object.FindFirstObjectByType<DevConsole>() == null)
        {
            GameObject devConsole = new GameObject("DevConsole");
            devConsole.AddComponent<DevConsole>();
            Undo.RegisterCreatedObjectUndo(devConsole, "DevConsole");
            Debug.Log("✅ DevConsole(치트 엔진) 생성 완료 - ` 키로 열기");
        }

        Debug.Log("🎉 TitleScene UI 완료!\n" +
            "  스토리모드 / 멀티플레이 / 광란의질주 / 환경설정 / 게임종료\n" +
            "  재생 버튼 눌러서 테스트! Ctrl+S로 저장!");
    }

    // ================================================================
    //  환경설정 패널
    // ================================================================
    static void MakeSettingsPanel(Transform root)
    {
        GameObject panel = MakeOverlayPanel("SettingsPanel", root);

        // 배경
        GameObject bg = ImgObj("BG", panel.transform, "Assets/ui이미지/환경설정 ui/배경2.png");
        Stretch(bg);

        // 프레임
        ImgAt("Frame", panel.transform, "Assets/ui이미지/환경설정 ui/테두리.png",
            new Vector2(900, 550), Vector2.zero);

        // 설정 타이틀
        ImgAt("Title", panel.transform, "Assets/ui이미지/환경설정 ui/text설정.png",
            new Vector2(160, 55), new Vector2(-310, 210));

        // 볼륨
        ImgAt("VolLabel", panel.transform, "Assets/ui이미지/환경설정 ui/text볼륨 크기.png",
            new Vector2(200, 45), new Vector2(30, 90));
        MakeSlider("VolumeSlider", panel.transform, new Vector2(30, 30), new Vector2(420, 25));
        MakeTMP("VolumeValue", panel.transform, "100%", 28, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(80, 40), new Vector2(290, 30));

        // 카메라 거리
        ImgAt("CamLabel", panel.transform, "Assets/ui이미지/환경설정 ui/text카메라 거리.png",
            new Vector2(200, 45), new Vector2(30, -50));
        MakeSlider("CameraDistSlider", panel.transform, new Vector2(30, -110), new Vector2(420, 25));
        MakeTMP("CameraDistValue", panel.transform, "50%", 28, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(80, 40), new Vector2(290, -110));

        // 뒤로가기
        GameObject back = ImgBtn("SettingsBackButton", panel.transform,
            "Assets/ui이미지/플레이모드ui/뒤로가기 버튼.png",
            new Vector2(80, 80), new Vector2(60, -60));
        SetAnchor(back, 0, 1);
    }

    // ================================================================
    //  종료 확인 패널
    // ================================================================
    static void MakeQuitPanel(Transform root)
    {
        GameObject panel = MakeOverlayPanel("QuitPanel", root);

        // 배경
        GameObject bg = ImgObj("BG", panel.transform, "Assets/ui이미지/일시정지 ui/배경2.png");
        Stretch(bg);

        // 질문
        MakeTMP("QuitText", panel.transform, "정말 종료하시겠습니까?", 52, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(700, 100), new Vector2(0, 60));

        // 예/아니오 (일시정지 UI 이미지)
        ImgBtn("QuitYesButton", panel.transform,
            "Assets/ui이미지/일시정지 ui/레이스 종료.png",
            new Vector2(250, 100), new Vector2(-150, -80));
        ImgBtn("QuitNoButton", panel.transform,
            "Assets/ui이미지/일시정지 ui/레이스 재개.png",
            new Vector2(250, 100), new Vector2(150, -80));
    }

    // ================================================================
    //  유틸
    // ================================================================
    static void Kill(Transform p, string n)
    {
        Transform t = p.Find(n);
        if (t != null) Object.DestroyImmediate(t.gameObject);
    }

    static GameObject MakeOverlayPanel(string name, Transform parent)
    {
        GameObject p = new GameObject(name, typeof(RectTransform));
        p.transform.SetParent(parent, false);
        Stretch(p);
        Image img = p.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.6f);
        Undo.RegisterCreatedObjectUndo(p, name);
        // ★ ACTIVE 상태 유지!
        return p;
    }

    static GameObject ImgBtn(string name, Transform parent, string path, Vector2 size, Vector2 pos)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        Sprite s = Spr(path);
        if (s != null) { img.sprite = s; img.preserveAspect = true; }
        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.85f, 0.85f, 0.85f);
        cb.pressedColor = new Color(0.65f, 0.65f, 0.65f);
        btn.colors = cb;
        SetRect(obj, size, pos);
        Undo.RegisterCreatedObjectUndo(obj, name);
        return obj;
    }

    static void ImgIcon(string name, Transform parent, string path, bool left)
    {
        if (parent == null) return;
        GameObject obj = ImgObj(name, parent, path);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(left ? 0 : 1, 0.5f);
        rt.anchorMax = new Vector2(left ? 0 : 1, 0.5f);
        rt.sizeDelta = new Vector2(60, 60);
        rt.anchoredPosition = new Vector2(left ? 50 : -50, 0);
    }

    static GameObject ImgObj(string name, Transform parent, string path)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        Sprite s = Spr(path);
        if (s != null) { img.sprite = s; img.preserveAspect = true; }
        return obj;
    }

    static GameObject ImgAt(string name, Transform parent, string path, Vector2 size, Vector2 pos)
    {
        GameObject obj = ImgObj(name, parent, path);
        SetRect(obj, size, pos);
        return obj;
    }

    static GameObject MakeTMP(string name, Transform parent,
        string text, float fontSize, Color color,
        Vector2 anchor, Vector2 size, Vector2 pos)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        // 한국어 폰트 런타임 폴백으로 처리 (TMP 기본 폰트 사용)
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        return obj;
    }

    static void MakeTMPChild(Transform parent, string text, float fontSize)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        Stretch(obj);
    }

    static void MakeSlider(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        SetRect(obj, size, pos);
        Slider slider = obj.AddComponent<Slider>();
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = 0.5f;

        GameObject bgObj = new GameObject("Background", typeof(RectTransform));
        bgObj.transform.SetParent(obj.transform, false);
        bgObj.AddComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f);
        Stretch(bgObj);

        GameObject fa = new GameObject("Fill Area", typeof(RectTransform));
        fa.transform.SetParent(obj.transform, false);
        RectTransform faRT = fa.GetComponent<RectTransform>();
        faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
        faRT.offsetMin = new Vector2(5, 0); faRT.offsetMax = new Vector2(-5, 0);

        GameObject fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fa.transform, false);
        fill.AddComponent<Image>().color = new Color(1f, 0f, 0.8f);
        Stretch(fill);

        GameObject ha = new GameObject("Handle Slide Area", typeof(RectTransform));
        ha.transform.SetParent(obj.transform, false);
        RectTransform haRT = ha.GetComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
        haRT.offsetMin = new Vector2(10, -5); haRT.offsetMax = new Vector2(-10, 5);

        GameObject handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(ha.transform, false);
        Image hImg = handle.AddComponent<Image>();
        hImg.color = Color.white;
        handle.GetComponent<RectTransform>().sizeDelta = new Vector2(25, 25);

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.targetGraphic = hImg;
    }

    static void SetRect(GameObject obj, Vector2 size, Vector2 pos)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
    }

    static void SetAnchor(GameObject obj, float x, float y)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(x, y);
        rt.anchorMax = new Vector2(x, y);
    }

    static void Stretch(GameObject obj)
    {
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static Sprite Spr(string path)
    {
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null && imp.textureType != TextureImporterType.Sprite)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
