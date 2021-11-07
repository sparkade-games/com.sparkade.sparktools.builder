namespace Sparkade.SparkTools.Builder.Editor
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Sparkade.SparkTools.CustomProjectSettings.Editor;
    using UnityEditor;
    using UnityEditor.Build.Reporting;
    using UnityEngine;

    /// <summary>
    /// Initiates builds based on the builder and platform settings.
    /// </summary>
    public static class Builder
    {
        /// <summary>
        /// Builds a platform defined in the builder settings.
        /// </summary>
        /// <param name="platform">Platform from the builder settings to be built.</param>
        /// <param name="autoRun">Whether the build should run after being built.</param>
        /// <returns>A report of the build.</returns>
        public static BuildReport Build(BuilderSettings.Platform platform, bool autoRun = false)
        {
            BuilderSettings settings = EditorSettingsManager.LoadOrCreateSettings<BuilderSettings>();

            EditorSettingsManager.SaveCachedSettings();
            BuildReport report = BuildPlayer(platform, autoRun);

            if (report.summary.result == BuildResult.Succeeded && settings.OpenBuildFolder && !autoRun)
            {
                EditorUtility.RevealInFinder(report.summary.outputPath);
            }

            return report;
        }

        /// <summary>
        /// Builds multiple platforms defined in the builder settings.
        /// </summary>
        /// <param name="platforms">Platforms from the builder settings to be built.</param>
        public static void Build(BuilderSettings.Platform[] platforms)
        {
            BuilderSettings settings = EditorSettingsManager.LoadOrCreateSettings<BuilderSettings>();

            EditorSettingsManager.SaveCachedSettings();
            BuildReport report = null;
            for (int i = 0; i < platforms.Length; i += 1)
            {
                report = BuildPlayer(platforms[i]);
                if (report.summary.result == BuildResult.Failed)
                {
                    return;
                }
            }

            if (settings.OpenBuildFolder)
            {
                int outputPathLevels = OutputFolderPatternToPath(settings.OutputFolder).Split(Path.DirectorySeparatorChar).Length;
                string outputPath = report.summary.outputPath.Replace('/', Path.DirectorySeparatorChar);
                string folderPath = string.Join(Path.DirectorySeparatorChar.ToString(), outputPath.Split(Path.DirectorySeparatorChar).Take(outputPathLevels + 1));
                EditorUtility.RevealInFinder(folderPath);
            }
        }

        /// <summary>
        /// Builds the active build target using platform settings defined in the builder settings.
        /// </summary>
        [MenuItem("SparkTools/Build", false, 100)]
        internal static void BuildActiveTarget()
        {
            Build(GetPlatform(EditorUserBuildSettings.activeBuildTarget));
        }

        /// <summary>
        /// Builds and runs the active build target using platform settings defined in the builder settings.
        /// </summary>
        [MenuItem("SparkTools/Build and Run", false, 101)]
        internal static void BuildAndRunActiveTarget()
        {
            Build(GetPlatform(EditorUserBuildSettings.activeBuildTarget), true);
        }

        /// <summary>
        /// Builds all platforms defined in the builder settings.
        /// </summary>
        [MenuItem("SparkTools/Build All", false, 102)]
        internal static void BuildAll()
        {
            BuilderSettings settings = EditorSettingsManager.LoadOrCreateSettings<BuilderSettings>();
            Build(settings.Platforms);
        }

        /// <summary>
        /// Converts an output pattern to a path.
        /// </summary>
        /// <param name="pattern">The pattern to be parsed.</param>
        /// <param name="platform">Target platform from the builder settings.</param>
        /// <returns>A parsed string.</returns>
        internal static string OutputPatternToPath(string pattern, BuilderSettings.Platform platform)
        {
            pattern = pattern.Replace('/', Path.DirectorySeparatorChar);
            pattern = pattern.
                Replace("{platform}", BuildPipeline.GetBuildTargetName(platform.Target)).
                Replace("{product}", Application.productName).
                Replace("{company}", Application.companyName).
                Replace("{identifier}", Application.identifier).
                Replace("{version}", Application.version).
                Replace("{unityversion}", Application.unityVersion);
            pattern = Path.ChangeExtension(pattern, !string.IsNullOrEmpty(platform.FileExtension) ? platform.FileExtension : null);
            return pattern;
        }

        /// <summary>
        /// Converts an output folder pattern to a path.
        /// </summary>
        /// <param name="pattern">The pattern to be parsed.</param>
        /// <returns>A parsed string.</returns>
        internal static string OutputFolderPatternToPath(string pattern)
        {
            pattern = pattern.Replace('/', Path.DirectorySeparatorChar);
            pattern = pattern.Replace("{project}", Path.Combine(Application.dataPath.Replace('/', Path.DirectorySeparatorChar), ".."));
            return pattern;
        }

        /// <summary>
        /// Gets platform settings from the builder settings.
        /// </summary>
        /// <param name="buildTarget">The platform's build target.</param>
        /// <returns>The platform settings from the builder settings, or a default platform settings for the given build target if none exists.</returns>
        internal static BuilderSettings.Platform GetPlatform(BuildTarget buildTarget)
        {
            BuilderSettings settings = EditorSettingsManager.LoadOrCreateSettings<BuilderSettings>();
            for (int i = 0; i < settings.Platforms.Length; i += 1)
            {
                if (settings.Platforms[i].Target == buildTarget)
                {
                    return settings.Platforms[i];
                }
            }

            return new BuilderSettings.Platform(buildTarget);
        }

        private static BuildReport BuildPlayer(BuilderSettings.Platform platform, bool autoRun = false)
        {
            BuilderSettings settings = EditorSettingsManager.LoadOrCreateSettings<BuilderSettings>();

            List<string> scenes = new List<string>();
            for (int i = 0; i < settings.IncludedScenes.Length; i += 1)
            {
                if (!platform.ExcludedScenes.Contains(settings.IncludedScenes[i]))
                {
                    scenes.Add(AssetDatabase.GetAssetPath(settings.IncludedScenes[i]));
                }
            }

            for (int i = 0; i < platform.ExtraScenes.Length; i += 1)
            {
                scenes.Add(AssetDatabase.GetAssetPath(platform.ExtraScenes[i]));
            }

            BuildOptions buildOptions = BuildOptions.None;
            if ((platform.DevelopmentBuild == BuilderSettings.Platform.BoolSetting.Default && settings.DevelopmentBuild == true) || platform.DevelopmentBuild == BuilderSettings.Platform.BoolSetting.Yes)
            {
                buildOptions |= BuildOptions.Development;
            }

            if (autoRun)
            {
                buildOptions |= BuildOptions.AutoRunPlayer;
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                target = platform.Target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(platform.Target),
                locationPathName = Path.Combine(OutputFolderPatternToPath(settings.OutputFolder), OutputPatternToPath(platform.OutputPattern == BuilderSettings.Platform.OverrideSetting.Default ? settings.OutputPattern : platform.OverridePattern, platform)),
                scenes = scenes.ToArray(),
                options = buildOptions,
            };

            HashSet<string> prevSymbols = null;
            if (platform.AdditionalSymbols.Count > 0)
            {
                prevSymbols = GetSymbols(platform.Target);
                HashSet<string> platformSymbols = new HashSet<string>(platform.AdditionalSymbols);
                platformSymbols.Union(prevSymbols);
                SetSymbols(platform.Target, platformSymbols);
            }

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

            if (platform.AdditionalSymbols.Count > 0)
            {
                SetSymbols(platform.Target, prevSymbols);
            }

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"{BuildPipeline.GetBuildTargetName(platform.Target)} build completed in ({report.summary.totalTime}) with a result of {report.summary.result}.");
            }
            else
            {
                Debug.LogError($"{BuildPipeline.GetBuildTargetName(platform.Target)} build completed in ({report.summary.totalTime}) with a result of {report.summary.result} ({report.summary.totalErrors}) errors.");
            }

            return report;
        }

        private static HashSet<string> GetSymbols(BuildTarget target)
        {
            string[] symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(target)).Split(';');
            return new HashSet<string>(symbols);
        }

        private static void SetSymbols(BuildTarget target, HashSet<string> symbols)
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildPipeline.GetBuildTargetGroup(target), string.Join(";", symbols));
        }
    }
}