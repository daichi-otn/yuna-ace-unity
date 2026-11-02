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

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Build/WebGL",
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
