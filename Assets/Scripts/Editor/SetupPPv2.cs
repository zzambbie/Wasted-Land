using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// 에디터 메뉴에서 Built-in PPv2 포스트 프로세싱 세팅을 자동화하는 스크립트.
/// 메뉴: Tools → Post Processing → PPv2 완전 자동 세팅
/// </summary>
public class SetupPPv2 : EditorWindow
{
    [MenuItem("Tools/Post Processing/PPv2 완전 자동 세팅 (문제가 계속 될 때 클릭!)")]
    public static void Setup()
    {
        bool changed = false;

        // 1. 카메라 설정 (PostProcessLayer 추가 및 HDR 켜기, LayerMask 설정)
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            // HDR 활성화 (블룸이 이쁘게 먹으려면 필수)
            mainCam.allowHDR = true;
            
            // PostProcessLayer 컴포넌트 추가
            var ppLayer = mainCam.GetComponent<PostProcessLayer>();
            if (ppLayer == null)
            {
                ppLayer = mainCam.gameObject.AddComponent<PostProcessLayer>();
            }

            // LayerMask 값을 "Everything" 또는 "Default" 등 필요한 레이어로 설정
            // 기본적으로 Everything(-1)으로 적용해야 어느 레이어의 Volume이든 잘 적용됩니다
            ppLayer.volumeLayer = -1; // Everything

            // 안티앨리어싱 기본값 활성화
            ppLayer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;

            // CRT 셰이더 컴포넌트 추가 및 매테리얼 연결
            var crt = mainCam.GetComponent<CRTPostEffecter>();
            if (crt == null)
            {
                crt = mainCam.gameObject.AddComponent<CRTPostEffecter>();
            }

            var crtMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SimpleCRTShader/material/CRT.mat");
            if (crtMat != null)
            {
                crt.material = crtMat;
                Debug.Log("[PPv2 Setup] CRT 셰이더와 매테리얼 자동 할당 성공");
            }

            EditorUtility.SetDirty(mainCam);
            changed = true;
            Debug.Log("[PPv2 Setup] 메인 카메라에 PostProcessLayer 추가 및 세팅 완료 (HDR Enabled, VolumeLayer Everything)");
        }
        else
        {
            Debug.LogWarning("[PPv2 Setup] MainCamera 태그를 가진 카메라를 찾을 수 없습니다.");
        }

        // 2. Global Volume 설정 (씬 전체에 적용할 범용 볼륨)
        PostProcessVolume globalVol = GameObject.FindFirstObjectByType<PostProcessVolume>();
        if (globalVol == null)
        {
            GameObject volumeGo = new GameObject("Global Volume");
            // 기본 Default 레이어로 설정해도 카메라의 PostProcessLayer가 Everything이므로 상관없음
            globalVol = volumeGo.AddComponent<PostProcessVolume>();
            Debug.Log("[PPv2 Setup] 씬에 Global Volume 오브젝트가 없어서 자동 생성했습니다.");
        }

        globalVol.isGlobal = true; // 필수 (씬 전체 적용)

        // 프로파일 자동 할당 (없으면 생성)
        if (globalVol.profile == null)
        {
            string profilePath = "Assets/Settings/DefaultPPv2Profile.asset";
            // 만약 폴더가 없으면 생성
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            {
                AssetDatabase.CreateFolder("Assets", "Settings");
            }

            var profile = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<PostProcessProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
                
                // 블룸(Bloom) 자동 추가해주기
                var bloom = profile.AddSettings<Bloom>();
                bloom.active = true;
                bloom.intensity.Override(1.5f);
                bloom.threshold.Override(0.9f);
                bloom.diffusion.Override(7f);
                
                // 컬러 그레이딩(톤매핑)
                var colorGrading = profile.AddSettings<ColorGrading>();
                colorGrading.active = true;
                colorGrading.tonemapper.Override(Tonemapper.ACES);
                colorGrading.postExposure.Override(0.5f);

                // 비네트
                var vignette = profile.AddSettings<Vignette>();
                vignette.active = true;
                vignette.intensity.Override(0.3f);
                vignette.smoothness.Override(0.5f);
            }
            
            globalVol.sharedProfile = profile;
        }

        EditorUtility.SetDirty(globalVol.gameObject);
        changed = true;

        if (changed)
        {
            AssetDatabase.SaveAssets();
            Debug.Log("✅ [PPv2 Setup] 포스트 프로세싱 세팅 완료. 씬을 확인해주세요.");
            EditorUtility.DisplayDialog("PPv2 세팅 완료!", 
                "적용이 정상적으로 끝났습니다.\n\n" +
                "1. 카메라에 PostProcessLayer가 부착되고 Layer가 Everything으로 맞춰졌습니다.\n" +
                "2. Global Volume에 PostProcessVolume이 붙고 IsGlobal이 켜졌습니다.\n" +
                "3. 카메라의 HDR이 켜졌습니다.\n\n" +
                "이제 Global Volume의 수치를 올리면 정상 적용될 것입니다!", 
                "확인"
            );
        }
    }
}
