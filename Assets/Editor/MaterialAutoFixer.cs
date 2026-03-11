using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Git에서 프로젝트를 받아올 때 매테리얼이 분홍색(Missing Shader)이 되는 문제 자동 해결.
/// Unity 에디터가 열릴 때 자동으로 실행되어 모든 매테리얼의 셰이더를 확인하고 재적용합니다.
/// </summary>
[InitializeOnLoad]
public class MaterialAutoFixer
{
    static MaterialAutoFixer()
    {
        // 에디터 시작 시 1회 실행
        EditorApplication.delayCall += FixMaterialsOnStartup;
    }

    static void FixMaterialsOnStartup()
    {
        // 이미 이번 세션에서 실행했으면 스킵
        if (SessionState.GetBool("MaterialAutoFixer_Done", false)) return;
        SessionState.SetBool("MaterialAutoFixer_Done", true);

        FixAllMaterials();
    }

    [MenuItem("Tools/매테리얼 셰이더 자동 수정")]
    public static void FixAllMaterials()
    {
        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        int fixedCount = 0;

        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null) continue;

            // 분홍색 = 셰이더가 null이거나 "Hidden/InternalErrorShader"
            if (mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader")
            {
                // URP 기본 셰이더로 복구
                Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLit != null)
                {
                    mat.shader = urpLit;
                    EditorUtility.SetDirty(mat);
                    fixedCount++;
                    Debug.Log($"[MaterialAutoFixer] 셰이더 복구: {path}");
                }
            }
        }

        if (fixedCount > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MaterialAutoFixer] 총 {fixedCount}개 매테리얼 셰이더 복구 완료!");
        }
        else
        {
            Debug.Log("[MaterialAutoFixer] 분홍색 매테리얼 없음 - 모두 정상!");
        }
    }
}
