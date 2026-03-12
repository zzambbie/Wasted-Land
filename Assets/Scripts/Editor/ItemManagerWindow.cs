using UnityEditor;
using UnityEngine;

public class ItemManagerWindow : EditorWindow
{
    private string path = "Assets/Resources/Items";
    private KartItemData selectedItem;
    private Vector2 scrollPos;
    private Texture2D headerLogo;

    [MenuItem("Tools/MS Item Tool")]
    public static void ShowWindow()
    {
        GetWindow<ItemManagerWindow>("MS Item Tool");
    }

    void OnEnable()
    {
        headerLogo = Resources.Load<Texture2D>("Logo");
    }

    void OnGUI()
    {
        // --- 상단 배너 ---
        GUILayout.BeginHorizontal("box");
        {
            if (headerLogo != null) GUILayout.Label(headerLogo, GUILayout.Height(60), GUILayout.Width(60));
            GUILayout.BeginVertical();
            GUILayout.Space(10);
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 20;
            titleStyle.normal.textColor = new Color(1.0f, 0.5f, 0.0f);
            GUILayout.Label("MS Cheat & Data Tool", titleStyle);
            GUILayout.Label("아이템 데이터 관리 및 인벤토리 치트", EditorStyles.miniLabel);
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        // --- 메인 영역 ---
        GUILayout.BeginHorizontal();

        // [좌측] 리스트
        GUILayout.BeginVertical("box", GUILayout.Width(150));
        GUILayout.Label("아이템 목록", EditorStyles.boldLabel);
        scrollPos = GUILayout.BeginScrollView(scrollPos);

        string[] guids = AssetDatabase.FindAssets("t:KartItemData");
        foreach (string guid in guids)
        {
            // [수정된 부분] GetAssetPath -> GUIDToAssetPath로 변경
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            KartItemData item = AssetDatabase.LoadAssetAtPath<KartItemData>(assetPath);

            // 삭제된 아이템이 리스트에 남는 에러 방지
            if (item == null) continue;

            GUI.backgroundColor = (selectedItem == item) ? Color.cyan : Color.white;
            if (GUILayout.Button(item.itemName))
            {
                selectedItem = item;
                GUI.FocusControl(null);
            }
            GUI.backgroundColor = Color.white;
        }
        GUILayout.EndScrollView();

        if (GUILayout.Button("새 아이템 생성")) CreateNewItem();
        GUILayout.EndVertical();

        // [우측] 상세 설정
        GUILayout.BeginVertical("box");
        if (selectedItem != null)
        {
            GUILayout.Label("아이템 정보 수정", EditorStyles.boldLabel);

            selectedItem.itemName = EditorGUILayout.TextField("이름", selectedItem.itemName);

            // [신규] 아이템 타입 선택 (Enum 드롭다운)
            selectedItem.itemType = (InventoryManager.ItemType)EditorGUILayout.EnumPopup("아이템 타입", selectedItem.itemType);

            selectedItem.icon = (Sprite)EditorGUILayout.ObjectField("아이콘", selectedItem.icon, typeof(Sprite), false);
            selectedItem.prefab = (GameObject)EditorGUILayout.ObjectField("프리팹 (선택)", selectedItem.prefab, typeof(GameObject), false);
            selectedItem.isAttackType = EditorGUILayout.Toggle("공격형", selectedItem.isAttackType);
            selectedItem.description = EditorGUILayout.TextArea(selectedItem.description, GUILayout.Height(40));

            if (GUILayout.Button("파일 이름 동기화"))
            {
                string assetPath = AssetDatabase.GetAssetPath(selectedItem);
                AssetDatabase.RenameAsset(assetPath, selectedItem.itemName);
                AssetDatabase.SaveAssets();
            }
            if (GUI.changed) EditorUtility.SetDirty(selectedItem);

            GUILayout.Space(20);

            // =========================================================
            // [기능 1] 인벤토리 주입 (치트)
            // =========================================================
            GUILayout.Label("🔴 실시간 테스트 (치트)", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                GUI.backgroundColor = new Color(0.6f, 1f, 0.6f); // 연두색 버튼

                if (GUILayout.Button("내 인벤토리에 넣기 (Click!)", GUILayout.Height(40)))
                {
                    InjectItemToInventory(selectedItem);
                }

                GUI.backgroundColor = Color.white;
            }
            else
            {
                EditorGUILayout.HelpBox("게임을 실행하면 인벤토리 주입 버튼이 나타납니다.", MessageType.Info);
            }

            GUILayout.Space(20);

            // =========================================================
            // [기능 2] 아이템 삭제
            // =========================================================
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f); // 빨간색 버튼
            if (GUILayout.Button("이 아이템 삭제하기"))
            {
                // 실수 방지를 위한 확인 팝업
                if (EditorUtility.DisplayDialog("아이템 삭제",
                    $"정말 '{selectedItem.itemName}' 데이터를 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다.", "삭제", "취소"))
                {
                    DeleteItem();
                }
            }
            GUI.backgroundColor = Color.white;
        }
        else
        {
            GUILayout.Label("왼쪽에서 아이템을 선택하세요.");
        }
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }
    void InjectItemToInventory(KartItemData data)
    {
        KartController realPlayer = null;

        // 1. 게임 매니저를 먼저 찾는다.
        GameManager gm = FindFirstObjectByType<GameManager>();

        // 2. 매니저가 알고 있는 '현재 조종 중인 플레이어'를 가져온다. (이게 제일 정확함)
        if (gm != null && gm.playerKart != null)
        {
            realPlayer = gm.playerKart;
        }
        else
        {
            // 3. (비상용) 매니저가 없을 때만 태그로 찾는다 (테스트 씬 등)
            GameObject taggedObj = GameObject.FindGameObjectWithTag("Player");
            if (taggedObj != null) realPlayer = taggedObj.GetComponent<KartController>();
        }

        // 4. 주입 로직
        if (realPlayer != null)
        {
            InventoryManager inv = realPlayer.GetComponent<InventoryManager>();

            if (inv != null)
            {
                inv.currentItem = data.itemType;

                inv.hasItem = true;
                inv.isRolling = false;

                // UI 업데이트 (UI가 연결되어 있다면)
                if (inv.itemSlotImage != null)
                {
                    inv.itemSlotImage.color = Color.white;
                    inv.itemSlotImage.sprite = data.icon;
                }

                Debug.Log($"[치트 성공] {realPlayer.name}에게 {data.itemName} 주입 완료!");
            }
            else
            {
                Debug.LogError($"찾은 플레이어({realPlayer.name})에게 InventoryManager가 없습니다.");
            }
        }
        else
        {
            Debug.LogError("플레이어를 찾을 수 없습니다! (게임을 실행 중인가요?)");
        }
    }

    // 아이템 삭제 함수
    void DeleteItem()
    {
        string assetPath = AssetDatabase.GetAssetPath(selectedItem);
        AssetDatabase.DeleteAsset(assetPath); // 파일 삭제
        selectedItem = null; // 선택 해제
        AssetDatabase.Refresh(); // 새로고침
    }

    void CreateNewItem()
    {
        KartItemData newItem = CreateInstance<KartItemData>();
        newItem.itemName = "New Item";
        if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
        string uniquePath = AssetDatabase.GenerateUniqueAssetPath(path + "/NewItem.asset");
        AssetDatabase.CreateAsset(newItem, uniquePath);
        AssetDatabase.SaveAssets();
        selectedItem = newItem;
    }
}
