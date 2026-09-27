using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace KingdomsOfBharat.Editor
{
    // Batch: -executeMethod KingdomsOfBharat.Editor.ArtReferenceBuild.Build -artBuildOut <path.app>
    public static class ArtReferenceBuild
    {
        public static void Build()
        {
            string[] args = Environment.GetCommandLineArgs();
            string output = "Builds/ArtReference/ArtReference.app";
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-artBuildOut") output = args[i + 1];
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" }, locationPathName = output,
                target = BuildTarget.StandaloneOSX, options = BuildOptions.Development,
            });
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
