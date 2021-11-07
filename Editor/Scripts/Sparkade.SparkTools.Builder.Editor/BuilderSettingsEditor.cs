namespace Sparkade.SparkTools.Builder.Editor
{
    using System.IO;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    /// <summary>
    /// Custom editor for BuilderSettings.
    /// </summary>
    [CustomEditor(typeof(BuilderSettings))]
    public class BuilderSettingsEditor : Editor
    {
        private BuilderSettings builderSettings;
        private VisualElement rootElement;
        private VisualTreeAsset platformVisualTree;
        private TextField outputFolderTextField;
        private TextField outputPatternTextField;
        private HelpBox patternHelpBox;
        private GroupBox platformsContainer;
        private SerializedProperty platformsProperty;

        /// <inheritdoc/>
        public override VisualElement CreateInspectorGUI()
        {
            // setup
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.sparkade.sparktools.builder/Editor/Templates/BuilderSettingsEditor.uxml");
            this.platformVisualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.sparkade.sparktools.builder/Editor/Templates/Platform.uxml");
            StyleSheet stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.sparkade.sparktools.builder/Editor/Stylesheets/BuilderSettingsEditor.uss");
            this.rootElement = new VisualElement();
            this.rootElement.styleSheets.Add(stylesheet);
            visualTree.CloneTree(this.rootElement);

            // platforms
            this.platformsContainer = this.rootElement.Q<GroupBox>(className: "platforms-container");
            this.platformsProperty = this.serializedObject.FindProperty("<Platforms>k__BackingField");
            this.UpdatePlatforms();
            this.rootElement.Q<Button>(className: "add-platform-button").clicked += () =>
            {
                this.serializedObject.FindProperty("<Platforms>k__BackingField").arraySize += 1;
                this.serializedObject.ApplyModifiedProperties();
                this.UpdatePlatforms();
            };

            // scenes
            this.rootElement.Q<PropertyField>(className: "included-scenes").BindProperty(this.serializedObject.FindProperty("<IncludedScenes>k__BackingField"));

            // output folder
            SerializedProperty outputFolderProperty = this.serializedObject.FindProperty("<OutputFolder>k__BackingField");
            this.outputFolderTextField = this.rootElement.Q<TextField>(className: "output-folder");
            this.outputFolderTextField.BindProperty(outputFolderProperty);
            this.outputFolderTextField.RegisterValueChangedCallback((s) => this.UpdatePatternHelpBoxText());
            this.rootElement.Q<Button>(className: "output-folder-button").clicked += () =>
            {
                string newPath = EditorUtility.OpenFolderPanel("Output Folder", Builder.OutputFolderPatternToPath(outputFolderProperty.stringValue), string.Empty);
                if (!string.IsNullOrEmpty(newPath))
                {
                    outputFolderProperty.stringValue = newPath;
                }
            };

            // output pattern
            this.outputPatternTextField = this.rootElement.Q<TextField>(className: "output-pattern");
            this.outputPatternTextField.BindProperty(this.serializedObject.FindProperty("<OutputPattern>k__BackingField"));
            this.outputPatternTextField.RegisterValueChangedCallback((s) => this.UpdatePatternHelpBoxText());

            // pattern help box
            this.rootElement.Q<Button>(className: "pattern-help-button").clicked += this.TogglePatternHelpBox;
            this.patternHelpBox = new HelpBox();
            this.patternHelpBox.style.display = DisplayStyle.None;
            this.rootElement.Add(this.patternHelpBox);
            this.patternHelpBox.PlaceInFront(this.outputPatternTextField);

            // options
            this.rootElement.Q<Toggle>(className: "development-build-toggle").BindProperty(this.serializedObject.FindProperty("<DevelopmentBuild>k__BackingField"));
            this.rootElement.Q<Toggle>(className: "open-build-folder-toggle").BindProperty(this.serializedObject.FindProperty("<OpenBuildFolder>k__BackingField"));

            // build buttons
            this.rootElement.Q<Button>(className: "build-button").clicked += Builder.BuildActiveTarget;
            this.rootElement.Q<Button>(className: "build-and-run-button").clicked += Builder.BuildAndRunActiveTarget;
            this.rootElement.Q<Button>(className: "build-all-button").clicked += Builder.BuildAll;

            // initial current path update
            this.UpdatePatternHelpBoxText();

            return this.rootElement;
        }

        private void UpdatePlatforms()
        {
            this.serializedObject.Update();
            this.platformsContainer.Clear();
            for (int i = 0; i < this.platformsProperty.arraySize; i += 1)
            {
                SerializedProperty platformProperty = this.platformsProperty.GetArrayElementAtIndex(i);
                VisualElement platform = new VisualElement();
                this.platformVisualTree.CloneTree(platform);
                platform.Q<TextField>(className: "file-extension").BindProperty(platformProperty.FindPropertyRelative("<FileExtension>k__BackingField"));
                platform.Q<EnumField>(className: "development-build").BindProperty(platformProperty.FindPropertyRelative("<DevelopmentBuild>k__BackingField"));
                platform.Q<PropertyField>(className: "extra-scenes").BindProperty(platformProperty.FindPropertyRelative("<ExtraScenes>k__BackingField"));
                platform.Q<PropertyField>(className: "excluded-scenes").BindProperty(platformProperty.FindPropertyRelative("<ExcludedScenes>k__BackingField"));
                platform.Q<PropertyField>(className: "additional-symbols").BindProperty(platformProperty.FindPropertyRelative("<AdditionalSymbols>k__BackingField"));

                TextField overridePattern = platform.Q<TextField>(className: "override-pattern");
                SerializedProperty outputPatternProperty = platformProperty.FindPropertyRelative("<OutputPattern>k__BackingField");
                overridePattern.BindProperty(platformProperty.FindPropertyRelative("<OverridePattern>k__BackingField"));
                overridePattern.style.display = outputPatternProperty.intValue == (int)BuilderSettings.Platform.OverrideSetting.Override ? DisplayStyle.Flex : DisplayStyle.None;

                EnumField outputPattern = platform.Q<EnumField>(className: "output-pattern");
                outputPattern.BindProperty(outputPatternProperty);
                outputPattern.RegisterValueChangedCallback((e) =>
                {
                    if (e.newValue == null)
                    {
                        return;
                    }

                    if ((BuilderSettings.Platform.OverrideSetting)e.newValue == BuilderSettings.Platform.OverrideSetting.Override)
                    {
                        overridePattern.style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        overridePattern.style.display = DisplayStyle.None;
                    }
                });

                int n = i;
                Button switchButton = platform.Q<Button>(className: "switch-button");
                SerializedProperty targetProperty = platformProperty.FindPropertyRelative("<Target>k__BackingField");
                switchButton.style.display = targetProperty.intValue != (int)EditorUserBuildSettings.activeBuildTarget ? DisplayStyle.Flex : DisplayStyle.None;
                switchButton.clicked += () =>
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup((BuildTarget)targetProperty.intValue), (BuildTarget)targetProperty.intValue);
                    this.UpdatePlatforms();
                };

                Button deleteButton = platform.Q<Button>(className: "delete-button");
                deleteButton.style.display = this.platformsProperty.arraySize > 1 ? DisplayStyle.Flex : DisplayStyle.None;
                deleteButton.clicked += () =>
                {
                    this.platformsProperty.DeleteArrayElementAtIndex(n);
                    this.serializedObject.ApplyModifiedProperties();
                    this.UpdatePlatforms();
                };

                EnumField target = platform.Q<EnumField>(className: "build-target");
                target.BindProperty(targetProperty);
                target.RegisterValueChangedCallback((e) =>
                {
                    if (e.newValue == null)
                    {
                        return;
                    }

                    if ((BuildTarget)e.newValue != EditorUserBuildSettings.activeBuildTarget)
                    {
                        switchButton.style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        switchButton.style.display = DisplayStyle.None;
                    }
                });

                this.platformsContainer.Add(platform);
            }
        }

        private void TogglePatternHelpBox()
        {
            this.patternHelpBox.style.display = this.patternHelpBox.style.display == DisplayStyle.Flex ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void UpdatePatternHelpBoxText()
        {
            this.patternHelpBox.text =
                "Current Path:\n" +
                $"{Path.Combine(Builder.OutputFolderPatternToPath(this.outputFolderTextField.value), Builder.OutputPatternToPath(this.outputPatternTextField.value, Builder.GetPlatform(EditorUserBuildSettings.activeBuildTarget)))}\n\n" +
                "Output Folder Keywords:\n" +
                "{project} - Path to project folder, one level above the Assets folder.\n\n" +
                "Output Pattern Keywords:\n" +
                "{platform} - Name of the platform being built.\n" +
                "{product} - Name of the project set in project settings.\n" +
                "{company} - Name of the company set in project settings.\n" +
                "{identifier} - Mobile app identifier set in project settings.\n" +
                "{version} - Player version set in project settings.\n" +
                "{unityversion} - Unity editor version.";
        }

        private void OnEnable()
        {
            this.builderSettings = (BuilderSettings)this.target;
            this.builderSettings.OnReset += this.UpdatePlatforms;
        }

        private void OnDisable()
        {
            this.builderSettings.OnReset -= this.UpdatePlatforms;
        }
    }
}