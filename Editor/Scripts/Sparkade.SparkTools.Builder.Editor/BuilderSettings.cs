namespace Sparkade.SparkTools.Builder.Editor
{
    using System;
    using System.Collections.Generic;
    using Sparkade.SparkTools.CustomProjectSettings;
    using Sparkade.SparkTools.CustomProjectSettings.Editor;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Settings for multiple build targets.
    /// </summary>
    public class BuilderSettings : SettingsAsset
    {
        /// <summary>
        /// Callback for when the settings are reset.
        /// </summary>
        internal event Action OnReset;

        /// <summary>
        /// Gets an array of platform settings.
        /// </summary>
        [field: SerializeField]
        public Platform[] Platforms { get; private set; }

        /// <summary>
        /// Gets the default scenes that will be included in builds.
        /// </summary>
        [field: SerializeField]
        public SceneAsset[] IncludedScenes { get; private set; }

        /// <summary>
        /// Gets the default output folder for builds.
        /// </summary>
        [field: SerializeField]
        public string OutputFolder { get; private set; }

        /// <summary>
        /// Gets the default output pattern to use within the output folder.
        /// </summary>
        [field: SerializeField]
        public string OutputPattern { get; private set; }

        /// <summary>
        /// Gets a value indicating whether by default builds will be marked as development builds.
        /// </summary>
        [field: SerializeField]
        public bool DevelopmentBuild { get; private set; }

        /// <summary>
        /// Gets a value indicating whether by default to open the build folder when a build finishes.
        /// </summary>
        [field: SerializeField]
        public bool OpenBuildFolder { get; private set; }

        /// <inheritdoc/>
        public override void Reset()
        {
            this.OutputFolder = "{project}/Builds";
            this.OutputPattern = "{platform}/{product}-{version}/{product}";
            this.DevelopmentBuild = false;
            this.OpenBuildFolder = true;
            this.Platforms = new Platform[] { new Platform(EditorUserBuildSettings.activeBuildTarget) };
            this.OnReset?.Invoke();
        }

        [MenuItem("SparkTools/Builder/Settings")]
        private static void InspectOrCreateSettings()
        {
            EditorSettingsManager.InspectOrCreateSettings<BuilderSettings>();
        }

        /// <summary>
        /// Settings for a build target.
        /// </summary>
        [Serializable]
        public struct Platform
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Platform"/> struct.
            /// </summary>
            /// <param name="target">The build target for this platform.</param>
            public Platform(BuildTarget target)
            {
                this.Target = target;
                this.FileExtension = (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64) ? "exe" : string.Empty;
                this.OutputPattern = OverrideSetting.Default;
                this.OverridePattern = string.Empty;
                this.ExtraScenes = new SceneAsset[0];
                this.ExcludedScenes = new SceneAsset[0];
                this.AdditionalSymbols = new List<string>();
                this.DevelopmentBuild = BoolSetting.Default;
            }

            /// <summary>
            /// A boolean platform option.
            /// </summary>
            public enum BoolSetting
            {
                /// <summary>
                /// Use the default value.
                /// </summary>
                Default = 0,

                /// <summary>
                /// Use true.
                /// </summary>
                Yes = 1,

                /// <summary>
                /// Use false.
                /// </summary>
                No = 2,
            }

            /// <summary>
            /// A platform option whether to override the default.
            /// </summary>
            public enum OverrideSetting
            {
                /// <summary>
                /// Use the default value.
                /// </summary>
                Default = 0,

                /// <summary>
                /// Use the platform's value.
                /// </summary>
                Override = 1,
            }

            /// <summary>
            /// Gets the platform's build target.
            /// </summary>
            [field: SerializeField]
            public BuildTarget Target { get; private set; }

            /// <summary>
            /// Gets the platform's build target.
            /// </summary>
            [field: SerializeField]
            public string FileExtension { get; private set; }

            /// <summary>
            /// Gets whether the platform uses its own output pattern.
            /// </summary>
            [field: SerializeField]
            public OverrideSetting OutputPattern { get; private set; }

            /// <summary>
            /// Gets the output pattern used when overriding the default.
            /// </summary>
            [field: SerializeField]
            public string OverridePattern { get; private set; }

            /// <summary>
            /// Gets scenes not in the default to be included in this platform's build.
            /// </summary>
            [field: SerializeField]
            public SceneAsset[] ExtraScenes { get; private set; }

            /// <summary>
            /// Gets scenes from the default to be excluded from this platform's build.
            /// </summary>
            [field: SerializeField]
            public SceneAsset[] ExcludedScenes { get; private set; }

            /// <summary>
            /// Gets additional scripting symbols to add to this platform's build.
            /// </summary>
            [field: SerializeField]
            public List<string> AdditionalSymbols { get; private set; }

            /// <summary>
            /// Gets whether the platform will be flagged as a development build, or inherit the default.
            /// </summary>
            [field: SerializeField]
            public BoolSetting DevelopmentBuild { get; private set; }
        }
    }
}