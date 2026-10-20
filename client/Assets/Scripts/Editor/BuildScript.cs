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
            PlayerSettings.WebGL.nameFilesAsHashes = false;
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
            // 빌드 파일 이름을 내용 해시로: 이름이 매번 같으면 이전 버전을 받아 둔 브라우저가 옛 파일과 새 파일을 섞어
            // 실행해 터진다 (실제로 겪음: RangeError Maximum call stack size exceeded).
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            var output = Path.Combine(BuildRoot, "webgl-release");
            Build(BuildTarget.WebGL, output, BuildOptions.None);
            HashNetScript(output);
        }

        /// <summary>
        /// 템플릿의 net.js(온라인 기능)도 내용 해시 이름으로 Build/ 에 옮긴다. index.html 과 빌드 파일이 함께 바뀌도록
        /// (배포 스크립트는 직전 index.html 이 가리키던 Build/ 파일을 남겨 두므로 옛 index.html 도 제 짝을 찾는다).
        /// </summary>
        private static void HashNetScript(string output)
        {
            var src = Path.Combine(output, "net.js");
            var index = Path.Combine(output, "index.html");
            if (!File.Exists(src) || !File.Exists(index)) return;
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = BitConverter.ToString(md5.ComputeHash(File.ReadAllBytes(src))).Replace("-", "").Substring(0, 12).ToLowerInvariant();
            var name = $"Build/net.{hash}.js";
            File.Copy(src, Path.Combine(output, name), true);
            File.Delete(src);
            File.WriteAllText(index, File.ReadAllText(index).Replace("src=\"net.js\"", $"src=\"{name}\""));
            Debug.Log($"[ChromaFrontier] net.js -> {name}");
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
