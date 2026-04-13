using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 적 카트(Enemy_Kart)를 Car 프리팹 기반으로 교체하는 에디터 유틸리티.
/// Unity 메뉴: Tools > 적 카트를 자동차로 교체
/// 
/// 사용법:
/// 1. Track_1 씬을 열고
/// 2. 메뉴에서 Tools > 적 카트를 자동차로 교체 실행
/// 3. 모든 Enemy_Kart 인스턴스가 P_Racing Car 프리팹으로 교체됨 (Enemy_Car_Mat 적용)
/// </summary>
public class EnemyCarSwapper : EditorWindow
{
    // 기본값 (Inspector에서 변경 가능)
    private GameObject sourcePrefab;    // 교체할 Car 프리팹 (P_Racing Car 등)
    private Material enemyMaterial;     // 적 차량 메테리얼 (Enemy_Car_Mat)

    [MenuItem("Tools/적 카트를 자동차로 교체")]
    static void ShowWindow()
    {
        var window = GetWindow<EnemyCarSwapper>("적 카트 교체");
        window.minSize = new Vector2(400, 250);

        // 기본 에셋 자동 탐색
        window.AutoFindAssets();
    }

    void AutoFindAssets()
    {
        // P_Racing Car 프리팹 자동 탐색
        string[] prefabGuids = AssetDatabase.FindAssets("P_Racing Car t:Prefab");
        if (prefabGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[0]);
            sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        // Enemy_Car_Mat 메테리얼 자동 탐색
        string[] matGuids = AssetDatabase.FindAssets("Enemy_Car_Mat t:Material");
        if (matGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(matGuids[0]);
            enemyMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
        }
    }

    void OnGUI()
    {
        GUILayout.Label("적 카트 → 자동차 교체 도구", EditorStyles.boldLabel);
        GUILayout.Space(10);

        sourcePrefab = (GameObject)EditorGUILayout.ObjectField(
            "Car 프리팹 (원본)", sourcePrefab, typeof(GameObject), false);

        enemyMaterial = (Material)EditorGUILayout.ObjectField(
            "적 차량 메테리얼", enemyMaterial, typeof(Material), false);

        GUILayout.Space(15);

        // 현재 씬의 Enemy_Kart 개수 표시
        KartController[] allKarts = FindObjectsByType<KartController>(FindObjectsSortMode.None);
        int enemyCount = 0;
        foreach (var kart in allKarts)
        {
            if (kart.isAI && kart.gameObject.name.Contains("Enemy_Kart"))
                enemyCount++;
        }
        EditorGUILayout.HelpBox($"현재 씬에서 발견된 Enemy_Kart: {enemyCount}개", MessageType.Info);

        GUILayout.Space(10);

        // 교체 버튼
        GUI.enabled = sourcePrefab != null && enemyMaterial != null;
        if (GUILayout.Button("교체 실행!", GUILayout.Height(40)))
        {
            SwapEnemyKarts();
        }
        GUI.enabled = true;

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "이 도구는 씬의 모든 Enemy_Kart를 선택한 Car 프리팹으로 교체합니다.\n" +
            "- 위치/회전 유지\n" +
            "- KartController, AIController 등 컴포넌트 유지\n" +
            "- 메테리얼을 Enemy_Car_Mat으로 변경\n" +
            "- GameManager의 allKarts 배열 자동 갱신",
            MessageType.None);
    }

    void SwapEnemyKarts()
    {
        if (sourcePrefab == null || enemyMaterial == null)
        {
            EditorUtility.DisplayDialog("오류", "Car 프리팹과 적 메테리얼을 모두 지정해주세요!", "확인");
            return;
        }

        // Undo 그룹 시작
        Undo.SetCurrentGroupName("적 카트를 자동차로 교체");
        int undoGroup = Undo.GetCurrentGroup();

        KartController[] allKarts = FindObjectsByType<KartController>(FindObjectsSortMode.None);
        int swapCount = 0;

        // GameManager 찾기
        GameManager gm = FindFirstObjectByType<GameManager>();

        foreach (var kart in allKarts)
        {
            if (!kart.isAI) continue;
            if (!kart.gameObject.name.Contains("Enemy_Kart")) continue;

            GameObject oldObj = kart.gameObject;
            Vector3 pos = oldObj.transform.position;
            Quaternion rot = oldObj.transform.rotation;
            Vector3 scale = oldObj.transform.localScale;
            Transform parent = oldObj.transform.parent;

            // 기존 AI 설정 백업
            AIController oldAI = oldObj.GetComponent<AIController>();
            Transform oldPathRoot = oldAI != null ? oldAI.pathRoot : null;
            float oldSpeedFactor = oldAI != null ? oldAI.speedFactor : 0.82f;
            float oldSensorLength = oldAI != null ? oldAI.sensorLength : 12.0f;
            LayerMask oldObstacleLayer = oldAI != null ? oldAI.obstacleLayer : 0;

            // 새 Car 프리팹 인스턴스화
            GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
            Undo.RegisterCreatedObjectUndo(newObj, "새 적 자동차 생성");

            newObj.transform.SetParent(parent);
            newObj.transform.position = pos;
            newObj.transform.rotation = rot;
            newObj.transform.localScale = scale;
            newObj.name = oldObj.name.Replace("Kart", "Car");

            // KartController 설정
            KartController newKart = newObj.GetComponent<KartController>();
            if (newKart != null)
            {
                newKart.isAI = true;

                // 기존 KartController의 trackNodes 복사 (런타임에 GameManager가 설정)
                KartController oldKart = oldObj.GetComponent<KartController>();
                if (oldKart != null)
                {
                    newKart.trackNodes = oldKart.trackNodes;
                }
            }

            // AIController 설정
            AIController newAI = newObj.GetComponent<AIController>();
            if (newAI == null)
            {
                newAI = newObj.AddComponent<AIController>();
            }
            newAI.pathRoot = oldPathRoot;
            newAI.speedFactor = oldSpeedFactor;
            newAI.sensorLength = oldSensorLength;
            newAI.obstacleLayer = oldObstacleLayer;

            // 메테리얼 변경 - 모든 Renderer에 적용
            Renderer[] renderers = newObj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                // ParticleSystemRenderer는 건너뜀 (이펙트용)
                if (renderer is ParticleSystemRenderer) continue;

                Material[] mats = renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = enemyMaterial;
                }
                renderer.sharedMaterials = mats;
            }

            // GameManager의 allKarts 배열 갱신
            if (gm != null && newKart != null)
            {
                SerializedObject gmSO = new SerializedObject(gm);
                SerializedProperty kartsProp = gmSO.FindProperty("allKarts");

                for (int i = 0; i < kartsProp.arraySize; i++)
                {
                    var element = kartsProp.GetArrayElementAtIndex(i);
                    if (element.objectReferenceValue == kart)
                    {
                        element.objectReferenceValue = newKart;
                        break;
                    }
                }
                gmSO.ApplyModifiedProperties();
            }

            // 기존 오브젝트 삭제
            Undo.DestroyObjectImmediate(oldObj);
            swapCount++;
        }

        Undo.CollapseUndoOperations(undoGroup);

        // 씬 dirty 표시
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("완료!",
            $"Enemy_Kart {swapCount}개를 자동차로 교체했습니다!\n\n" +
            "씬을 저장하세요 (Ctrl+S).",
            "확인");

        Debug.Log($"[EnemyCarSwapper] {swapCount}개의 적 카트를 자동차로 교체 완료!");
    }
}
