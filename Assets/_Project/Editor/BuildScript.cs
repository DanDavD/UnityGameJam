using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GameJam.Editor
{
    public static class BuildScript
    {
        public static void BuildWebGL()
        {
            Debug.Log("[BuildScript] Starting WebGL Build...");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_Project/Scenes/Match.unity" },
                locationPathName = "Builds/WebGL",
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] WebGL Build succeeded: {summary.totalSize} bytes in {summary.totalTime.TotalSeconds:F1}s");
                EditorApplication.Exit(0);
            }
            else
            {
                Debug.LogError($"[BuildScript] WebGL Build failed: {summary.result}");
                EditorApplication.Exit(1);
            }
        }
    }
}
