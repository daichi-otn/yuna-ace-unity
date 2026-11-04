using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace YnaAce.Editor
{
    /// <summary>ブラウザ配布用の WebGL 出力を Build/WebGL に作成します。</summary>
    public static class WebGlBuild
    {
        public static void PerformBuild()
        {
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new BuildFailedException("ビルド対象のシーンがありません。");
            }

            // GitHub PagesではContent-Encodingヘッダーを付けられないため、
            // Unityの圧縮済み(.br/.gz)成果物ではなく非圧縮ファイルを公開します。
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            const string outputPath = "Build/WebGL";
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException("WebGL ビルドに失敗しました: " + report.summary.result);
            }
        }
    }
}
