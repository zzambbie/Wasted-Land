using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// 최소한의 에디터 도구. 한글 폰트 + PauseCanvas 복사만 담당.
/// </summary>
public class UISetupTool : EditorWindow
{
    // ========================================
    // 1. 한글 폰트 (생성 + 적용 한방)
    // ========================================
    [MenuItem("Tools/한글 폰트 생성 + 적용")]
    static void CreateAndApplyKoreanFont()
    {
        string fontPath = "Assets/Fonts/MalgunGothicBold.ttf";
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        if (sourceFont == null)
        {
            Debug.LogError("폰트를 찾을 수 없습니다: " + fontPath);
            return;
        }

        string savePath = "Assets/Fonts/MalgunGothicBold SDF.asset";

        // 기존 깨진 에셋 삭제
        if (AssetDatabase.LoadAssetAtPath<Object>(savePath) != null)
            AssetDatabase.DeleteAsset(savePath);

        // Dynamic 모드 폰트 생성 (한글 글리프를 런타임에 자동 생성)
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont, 44, 4, GlyphRenderMode.SDFAA, 2048, 2048);

        if (fontAsset == null)
        {
            Debug.LogError("폰트 에셋 생성 실패!");
            return;
        }

        fontAsset.name = "MalgunGothicBold SDF";
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

        // ★ 아틀라스 텍스처 + 머터리얼을 서브 에셋으로 정확히 저장
        AssetDatabase.CreateAsset(fontAsset, savePath);

        if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
        {
            fontAsset.atlasTextures[0].name = "MalgunGothicBold SDF Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
        }
        if (fontAsset.material != null)
        {
            fontAsset.material.name = "MalgunGothicBold SDF Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // TMP 기본 폰트로 설정
        TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(
            "Assets/TextMesh Pro/Resources/TMP Settings.asset");
        if (settings != null)
        {
            var field = typeof(TMP_Settings).GetField("m_defaultFontAsset",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(settings, fontAsset);
                EditorUtility.SetDirty(settings);
            }
        }

        // 현재 씬의 모든 TMP 텍스트 폰트 교체
        int count = 0;
        foreach (var tmp in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(tmp, "Korean Font");
            tmp.font = fontAsset;
            EditorUtility.SetDirty(tmp);
            count++;
        }
        foreach (var tmp in Object.FindObjectsByType<TextMeshPro>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(tmp, "Korean Font");
            tmp.font = fontAsset;
            EditorUtility.SetDirty(tmp);
            count++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log("✅ 한글 폰트 생성 + 기본 설정 + " + count + "개 텍스트 교체 완료!");
        Selection.activeObject = fontAsset;
    }

    // ========================================
    // 2. PauseCanvas Prefab 저장
    // ========================================
    [MenuItem("Tools/PauseCanvas → Prefab 저장")]
    static void SavePauseCanvasPrefab()
    {
        GameObject pc = GameObject.Find("PauseCanvas");
        if (pc == null) { Debug.LogError("현재 씬에 PauseCanvas가 없습니다!"); return; }

        if (!AssetDatabase.IsValidFolder("Assets/prefeb"))
            AssetDatabase.CreateFolder("Assets", "prefeb");

        PrefabUtility.SaveAsPrefabAsset(pc, "Assets/prefeb/PauseCanvas.prefab");
        Debug.Log("✅ PauseCanvas Prefab 저장 완료! → 다른 씬에서 Prefab을 드래그해서 배치하세요.");
    }
}
