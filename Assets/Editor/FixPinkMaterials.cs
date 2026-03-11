using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 분홍색(핑크) 매테리얼 자동 수정 도구.
/// Unity 메뉴 → Tools → Fix Pink Materials 로 실행.
/// 
/// 이 프로젝트는 Built-in Render Pipeline을 사용하므로 Standard 셰이더로 교체합니다.
/// </summary>
public class FixPinkMaterials : EditorWindow
{
    private Vector2 scrollPos;
    private List<string> logMessages = new List<string>();

    // 문제가 되는 셰이더 GUID 목록 (프로젝트에 없거나 깨진 셰이더)
    private static readonly string[] brokenShaderGuids = new string[]
    {
        "5be750e0d2330b74abe37e6fbd08fb68",  // 누락된 PrimoToon 변형 셰이더
        "0e85370a028ae2247a2ea6508b153691",  // 누락된 셰이더
        "9dc9fdfae07f2c9419b1d6306e9552b6",  // PrimoToon (컴파일 안 될 수 있음)
        "933532a4fcc9baf4fa0491de14d08ed7",  // URP Lit (URP 미설치 프로젝트)
    };

    [MenuItem("Tools/Fix Pink Materials (분홍색 매테리얼 수정)")]
    static void ShowWindow()
    {
        GetWindow<FixPinkMaterials>("Fix Pink Materials");
    }

    void OnGUI()
    {
        GUILayout.Label("🔧 분홍색 매테리얼 자동 수정 도구", EditorStyles.boldLabel);
        GUILayout.Space(5);
        GUILayout.Label(
            "깨진 셰이더 → Standard 셰이더로 교체\n" +
            "같은 이름의 .png 텍스처 자동 매칭",
            EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("1단계: 문제 매테리얼 검사 (미리보기)", GUILayout.Height(30)))
        {
            ScanAndFix(false);
        }

        GUILayout.Space(5);

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("2단계: 자동 수정 실행!", GUILayout.Height(40)))
        {
            ScanAndFix(true);
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(10);
        GUILayout.Label("실행 로그:", EditorStyles.boldLabel);

        scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
        foreach (string msg in logMessages)
        {
            GUILayout.Label(msg, EditorStyles.wordWrappedLabel);
        }
        GUILayout.EndScrollView();
    }

    void ScanAndFix(bool applyFix)
    {
        logMessages.Clear();
        int fixedCount = 0;
        int textureFixCount = 0;
        int problemCount = 0;

        // Built-in Standard 셰이더 찾기
        Shader standardShader = Shader.Find("Standard");
        if (standardShader == null)
        {
            logMessages.Add("ERROR: Standard 셰이더를 찾을 수 없습니다!");
            Repaint();
            return;
        }
        logMessages.Add("OK: Standard 셰이더 발견됨");

        // 프로젝트 내 모든 텍스처를 이름별로 캐싱
        Dictionary<string, string> texturePathByName = new Dictionary<string, string>();
        string[] allTextures = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
        foreach (string guid in allTextures)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (!texturePathByName.ContainsKey(fileName))
            {
                texturePathByName[fileName] = path;
            }
        }
        logMessages.Add($"텍스처 {texturePathByName.Count}개 인덱싱됨");
        logMessages.Add("---");

        // 모든 매테리얼 검사
        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });

        foreach (string guid in matGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            // 서드파티 에셋은 건드리지 않음
            if (path.Contains("TextMesh Pro") || path.Contains("EasyRoads3D") ||
                path.Contains("PrimoToon-main") || path.Contains("TutorialInfo"))
                continue;

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            string matName = Path.GetFileNameWithoutExtension(path);
            bool needsFix = false;
            string reason = "";

            // --- 체크 1: 셰이더가 에러 상태 ---
            if (mat.shader == null ||
                mat.shader.name == "Hidden/InternalErrorShader" ||
                mat.shader.name.Contains("Error"))
            {
                needsFix = true;
                reason = "셰이더 에러 상태";
            }

            // --- 체크 2: YAML에서 깨진 셰이더 GUID 확인 ---
            if (!needsFix)
            {
                string fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    string content = File.ReadAllText(fullPath);
                    foreach (string brokenGuid in brokenShaderGuids)
                    {
                        if (content.Contains(brokenGuid))
                        {
                            needsFix = true;
                            reason = $"깨진 셰이더 GUID ({brokenGuid.Substring(0, 12)}...)";
                            break;
                        }
                    }
                }
            }

            if (!needsFix) continue;

            problemCount++;

            // 매칭 가능한 텍스처 찾기
            string matchedTexPath = FindMatchingTexture(matName, path, texturePathByName);

            if (!applyFix)
            {
                logMessages.Add($"[문제] {matName} - {reason}");
                if (matchedTexPath != null)
                    logMessages.Add($"  -> 매칭: {Path.GetFileName(matchedTexPath)}");
                else
                    logMessages.Add($"  -> 매칭 텍스처 없음");
                continue;
            }

            // === 수정 적용 ===
            mat.shader = standardShader;

            // 텍스처 매칭
            if (matchedTexPath != null)
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(matchedTexPath);
                if (tex != null)
                {
                    mat.SetTexture("_MainTex", tex);
                    textureFixCount++;
                    logMessages.Add($"[수정] {matName} -> Standard + {Path.GetFileName(matchedTexPath)}");
                }
            }
            else
            {
                logMessages.Add($"[수정] {matName} -> Standard (텍스처 없음, 색상만 유지)");
            }

            // Metallic/Smoothness 기본값
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Glossiness", 0.3f);

            EditorUtility.SetDirty(mat);
            fixedCount++;
        }

        if (applyFix)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            logMessages.Add("---");
            logMessages.Add($"완료! {fixedCount}개 매테리얼 수정, {textureFixCount}개 텍스처 매칭");
        }
        else
        {
            logMessages.Add("---");
            logMessages.Add($"검사 결과: {problemCount}개 문제 발견");
        }

        Repaint();
    }

    string FindMatchingTexture(string matName, string matPath, Dictionary<string, string> texturePathByName)
    {
        // 1) 정확히 같은 이름
        if (texturePathByName.ContainsKey(matName))
            return texturePathByName[matName];

        // 2) 숫자 접미사 제거 후 매칭 (예: "쇼파 1" → "쇼파")
        string baseName = System.Text.RegularExpressions.Regex.Replace(matName, @"\s*\d+$", "").Trim();
        if (baseName != matName && texturePathByName.ContainsKey(baseName))
            return texturePathByName[baseName];

        // 3) 부분 문자열 매칭
        foreach (var kvp in texturePathByName)
        {
            if (kvp.Key.Contains(matName) || matName.Contains(kvp.Key))
            {
                // 너무 짧은 이름은 오매칭 방지
                if (kvp.Key.Length >= 2 && matName.Length >= 2)
                    return kvp.Value;
            }
        }

        // 4) 같은 폴더 내 텍스처
        string matDir = Path.GetDirectoryName(matPath);
        if (!string.IsNullOrEmpty(matDir))
        {
            string[] texInDir = AssetDatabase.FindAssets("t:Texture2D", new[] { matDir });
            foreach (string tGuid in texInDir)
            {
                string tPath = AssetDatabase.GUIDToAssetPath(tGuid);
                string tName = Path.GetFileNameWithoutExtension(tPath);
                if (tName.Contains(baseName) || baseName.Contains(tName))
                    return tPath;
            }
        }

        return null;
    }
}
