using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ChromaFrontier.Editor
{
    /// <summary>
    /// CLI 빌드 진입점 (-executeMethod). 산출물은 저장소 루트의 build/ 아래.
    ///   Unity.exe -batchmode -quit -projectPath client -executeMethod ChromaFrontier.Editor.BuildScript.BuildWebGL
    /// </summary>
    public static class BuildScript
    {
        private static string BuildRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "build"));

        [MenuItem("Chroma Frontier/Build/WebGL (dev)")]
        public static void BuildWebGL()
        {
            // 압축을 끄면 아무 정적 서버로나 띄울 수 있다.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            Build(BuildTarget.WebGL, Path.Combine(BuildRoot, "webgl"), BuildOptions.Development);
        }

        /// <summary>
        /// 배포용 WebGL (GitHub Pages 등 정적 호스팅) → build/webgl-release.
        /// Gzip 압축 + 압축 해제 대체: 서버가 Content-Encoding 헤더를 안 붙여도 로더가 브라우저에서 직접 푼다.
        /// 개발 빌드가 아니므로 개발용 단축(?unlock, ?reset, U 키)도 꺼진다.
        /// </summary>
        [MenuItem("Chroma Frontier/Build/WebGL (release, GitHub Pages)")]
        public static void BuildWebGLRelease()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            Build(BuildTarget.WebGL, Path.Combine(BuildRoot, "webgl-release"), BuildOptions.None);
        }

        [MenuItem("Chroma Frontier/Build/Android APK (dev)")]
        public static void BuildAndroid()
        {
            EditorUserBuildSettings.buildAppBundle = false;
            Build(BuildTarget.Android, Path.Combine(BuildRoot, "android", "ChromaFrontier.apk"), BuildOptions.Development);
        }

        private static void Build(BuildTarget target, string output, BuildOptions options)
        {
            ProjectSetup.Run();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.MainScenePath },
                locationPathName = output,
                target = target,
                options = options,
            });

            var summary = report.summary;
            Debug.Log($"[ChromaFrontier] {target} build {summary.result}: {output} ({summary.totalSize / (1024 * 1024)} MB, {summary.totalTime})");
            if (summary.result != BuildResult.Succeeded)
            {
                if (Application.isBatchMode) EditorApplication.Exit(1);
                else throw new Exception($"{target} build failed");
            }
        }
    }
}
