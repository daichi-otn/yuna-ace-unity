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
        [MenuItem("Tools/YNA ACE/Build WebGL")]
        private static void BuildWebGlFromMenu() => PerformBuild();

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

            PatchResponsiveWebPlayer(outputPath);
        }

        private static void PatchResponsiveWebPlayer(string outputPath)
        {
            var indexPath = Path.Combine(outputPath, "index.html");
            var html = File.ReadAllText(indexPath);
            html = html.Replace("<html lang=\"en-us\">", "<html lang=\"ja\">");
            html = html.Replace(
                "<meta charset=\"utf-8\">",
                "<meta charset=\"utf-8\">\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1, viewport-fit=cover\">");
            html = html.Replace(
                "if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {",
                "if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent) || window.innerWidth <= 960) {");
            File.WriteAllText(indexPath, html);

            var stylePath = Path.Combine(outputPath, "TemplateData", "style.css");
            var css = File.ReadAllText(stylePath);
            css = css.Replace(
                "body { padding: 0; margin: 0 }",
                "html, body { width: 100%; height: 100%; padding: 0; margin: 0; overflow: hidden }");
            css = css.Replace(
                "#unity-container.unity-mobile { position: fixed; width: 100%; height: 100% }",
                "#unity-container.unity-mobile { position: fixed; inset: 0; width: 100vw; height: 100%; height: 100dvh }");
            File.WriteAllText(stylePath, css);
            // Unityが出力フォルダーを作り直してもGitHub Pages向け設定を維持します。
            File.WriteAllText(Path.Combine(outputPath, ".nojekyll"), "\n");
        }
    }
}
