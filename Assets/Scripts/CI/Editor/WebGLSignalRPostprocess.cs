using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CI.Editor
{
    public sealed class WebGLSignalRPostprocess : IPostprocessBuildWithReport
    {
        private const string SignalRScriptTag =
            "<script src=\"https://cdn.jsdelivr.net/npm/@microsoft/signalr@7.0.14/dist/browser/signalr.min.js\"></script>";

        public int callbackOrder => 1000;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.WebGL)
            {
                return;
            }

            var outputPath = report.summary.outputPath;
            var indexPath = Path.Combine(outputPath, "index.html");
            if (!File.Exists(indexPath))
            {
                Debug.LogWarning($"[WebGLSignalRPostprocess] index.html not found at {indexPath}");
                return;
            }

            var html = File.ReadAllText(indexPath);
            if (html.Contains("signalr.min.js"))
            {
                return;
            }

            var insertIndex = html.LastIndexOf("</body>", System.StringComparison.OrdinalIgnoreCase);
            if (insertIndex < 0)
            {
                Debug.LogWarning("[WebGLSignalRPostprocess] </body> tag not found, cannot inject SignalR script.");
                return;
            }

            html = html.Insert(insertIndex, $"    {SignalRScriptTag}\n");
            File.WriteAllText(indexPath, html);
            Debug.Log("[WebGLSignalRPostprocess] Injected SignalR script into WebGL build.");
        }
    }
}
