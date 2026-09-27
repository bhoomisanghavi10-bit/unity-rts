using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace KingdomsOfBharat.Editor
{
    public static class RepresentativeBenchmarkBuild
    {
        public const string OutputPath = "Builds/RepresentativeBenchmark/KingdomsOfBharatBenchmark.app";

        [MenuItem("BharatRTS/Build Representative Benchmark Player")]
        public static void Build()
        {
            string[] scenes = { "Assets/Scenes/Main.unity" };
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development | BuildOptions.ConnectWithProfiler | BuildOptions.AllowDebugging,
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Representative benchmark player build failed: " + report.summary.result);
            UnityEngine.Debug.Log("Representative benchmark Development player built at " + OutputPath);
        }
    }
}
