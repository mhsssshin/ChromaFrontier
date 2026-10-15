using ChromaFrontier.Client;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChromaFrontier.Editor
{
    /// <summary>
    /// 씬/플레이어 설정을 코드로 보장한다. ProjectSettings 를 손으로 고치지 말고 여기에 추가할 것.
    /// 에디터를 열면(또는 CLI 빌드 시) 같은 상태가 된다.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        public const string MainScenePath = "Assets/Scenes/Main.unity";
        public const string AppId = "com.chromafrontier.game";

        static ProjectSetup()
        {
            if (Application.isBatchMode) return; // CLI 빌드는 BuildScript 가 직접 호출
            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) == null) Run();
            };
        }

        [MenuItem("Chroma Frontier/Setup Project")]
        public static void Run()
        {
            EnsurePlayerSettings();
            EnsureMainScene();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[ChromaFrontier] project setup done");
        }

        private static void EnsurePlayerSettings()
        {
            PlayerSettings.companyName = "ChromaFrontier";
            PlayerSettings.productName = "Chroma Frontier";
            PlayerSettings.bundleVersion = "0.1.0";

            // 모바일 세로 모드 (컨셉 시안 9:16)
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // WebGL: Portrait 템플릿(Assets/WebGLTemplates)은 9:16 비율로 브라우저 창에 맞춘다.
            PlayerSettings.WebGL.template = "PROJECT:Portrait";
            PlayerSettings.defaultWebScreenWidth = 540;
            PlayerSettings.defaultWebScreenHeight = 960;
            PlayerSettings.runInBackground = true;

            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, AppId);
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.WebGL, AppId);
        }

        private static void EnsureMainScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) != null) return;

            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            new GameObject("GameBootstrap").AddComponent<GameBootstrap>();

            EditorSceneManager.SaveScene(scene, MainScenePath);
        }
    }
}
