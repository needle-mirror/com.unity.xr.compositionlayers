using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.XR.CompositionLayers.Editor
{
    /// <summary>
    /// The Composition Layers page in Project Settings, which edits the
    /// <see cref="CompositionLayersRuntimeSettings"/> asset.
    /// </summary>
    class CompositionLayersSettingsProvider : SettingsProvider
    {
        internal const string k_SettingsPath = "Project/XR Plug-in Management/Composition Layers";
        internal const string k_SetupErrorName = "setup-error";
        internal const string k_SetUpButtonName = "set-up-settings-asset";
        const string k_IncludedShadersName = "included-shaders";
        const string k_SettingsContainerName = "settings";
        const string k_EmulationInStandaloneHelpBoxName = "emulation-in-standalone-deprecation";
        internal const string k_EnableSplashToggleName = "enable-splash-screen";

        const string k_EmulationInStandalonePath = "m_EmulationInStandalone";
        const string k_EnableSplashScreenPath = "m_EnableSplashScreen";
        const string k_BackgroundTypePath = "m_BackgroundType";
        const string k_BackgroundColorPath = "m_BackgroundColor";
        const string k_LayerTypePath = "m_LayerType";
        const string k_QuadLayerDataPath = "m_QuadLayerData";
        const string k_CylinderLayerDataPath = "m_CylinderLayerData";
        const string k_ApplyTransformScaleName = "m_ApplyTransformScale";

        const string k_EmulationInStandaloneLabel = "Emulation In Standalone (Deprecated)";
        const string k_EmulationInStandaloneDeprecation = "Emulation In Standalone is deprecated in XR Composition " +
            "Layers 7.0.0-pre.1 and will be removed in a future release. It only affects standalone players without " +
            "an XR display, such as flat PC builds. When an XR display is running, layers fall back to emulation " +
            "regardless. To keep layers out of a flat build, deactivate their GameObjects instead.";

        static readonly string[] k_SplashStylePaths =
        {
            "m_SplashImage", k_BackgroundTypePath, k_BackgroundColorPath, "m_SplashDuration", "m_FadeInDuration",
            "m_FadeOutDuration", "m_FollowSpeed", "m_FollowDistance", "m_LockToHorizon", k_LayerTypePath,
        };

        const string k_IncludedShadersTooltip = "Player builds include these shaders through this settings asset in " +
            RuntimeSettingsMessages.k_PreloadedAssets + ".";

        const string k_IncludedShadersDocsUrl =
            "https://docs.unity3d.com/Packages/com.unity.xr.compositionlayers@7.0/" +
            "manual/get-started/graphics-settings.html#shader-inclusion";

        const float k_HelpIconSize = 16f;
        const float k_PageMargin = 10f;
        const float k_TitleFontSize = 19f;
        const float k_SectionSpacing = 10f;
        const float k_HeaderSpacing = 4f;

        static readonly string[] k_Keywords = { "composition", "layers", "splash", "emulation", "xr" };

        readonly RuntimeSettingsLocations m_Locations;
        readonly Func<Object[]> m_GetPreloadedAssets;
        readonly Action<Object[]> m_SetPreloadedAssets;
        readonly SplashSceneTarget m_SplashScenes;

        VisualElement m_Root;
        string m_SetupFailure;
        SerializedObject m_SerializedSettings;
        Toggle m_EnableSplashToggle;
        VisualElement m_SplashMessages;
        VisualElement m_SplashSettings;
        VisualElement m_BackgroundColorField;
        VisualElement m_LayerDataFields;

        internal CompositionLayersSettingsProvider(
            RuntimeSettingsLocations locations,
            Func<Object[]> getPreloadedAssets,
            Action<Object[]> setPreloadedAssets,
            SplashSceneTarget splashScenes)
            : base(k_SettingsPath, SettingsScope.Project, k_Keywords)
        {
            m_Locations = locations;
            m_GetPreloadedAssets = getPreloadedAssets;
            m_SetPreloadedAssets = setPreloadedAssets;
            m_SplashScenes = splashScenes;
        }

        [SettingsProvider]
        internal static SettingsProvider Create()
        {
            return new CompositionLayersSettingsProvider(
                RuntimeSettingsLocations.Default,
                PlayerSettings.GetPreloadedAssets,
                PlayerSettings.SetPreloadedAssets,
                SplashSceneTarget.Project);
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_Root = rootElement;
            m_SetupFailure = null;
            Rebuild();
        }

        public override void OnDeactivate()
        {
            m_Root = null;
            m_SerializedSettings = null;
        }

        void Rebuild()
        {
            m_Root.Clear();
            var page = new ScrollView(ScrollViewMode.Vertical);
            page.style.marginLeft = k_PageMargin;
            page.style.marginRight = k_PageMargin;
            m_Root.Add(page);

            var title = new Label("Composition Layers");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = k_TitleFontSize;
            title.style.marginBottom = k_SectionSpacing;
            page.Add(title);

            var preloadedAssets = m_GetPreloadedAssets();
            if (RuntimeSettingsLookup.TryFindNewAsset(m_Locations, preloadedAssets, out var settings))
            {
                if (!RuntimeSettingsSetup.IsSetUp(m_Locations, preloadedAssets))
                    AddSetupError(page, RuntimeSettingsMessages.k_NotPreloaded, RuntimeSettingsMessages.k_SetUpButton);

                AddSettings(page, settings);
            }
            else if (RuntimeSettingsLookup.TryFindOldAsset(m_Locations, out settings))
            {
                AddSetupError(
                    page, RuntimeSettingsMessages.NeedsMigrating(m_Locations), RuntimeSettingsMessages.k_MigrateButton);
                AddSettings(page, settings);
            }
            else
            {
                AddSetupError(
                    page, RuntimeSettingsMessages.Missing(m_Locations), RuntimeSettingsMessages.k_CreateButton);
            }
        }

        // Shows the same message as the Project Validation error, and a button that runs the same setup as its Fix.
        void AddSetupError(VisualElement page, string message, string buttonText)
        {
            page.Add(new HelpBox(message, HelpBoxMessageType.Error) { name = k_SetupErrorName });
            if (m_SetupFailure != null)
                page.Add(new HelpBox(m_SetupFailure, HelpBoxMessageType.Error));

            page.Add(new Button(SetUpSettings) { name = k_SetUpButtonName, text = buttonText });
        }

        void SetUpSettings()
        {
            m_SetupFailure = null;
            try
            {
                if (RuntimeSettingsSetup.SetUp(m_Locations, m_GetPreloadedAssets(), out _, out var preloadedAssets))
                    m_SetPreloadedAssets(preloadedAssets);
            }
            catch (BuildFailedException exception)
            {
                m_SetupFailure = exception.Message;
            }

            Rebuild();
        }

        void AddSettings(VisualElement page, CompositionLayersRuntimeSettings settings)
        {
            m_SerializedSettings = new SerializedObject(settings);
            var container = new VisualElement { name = k_SettingsContainerName };
            container.Add(CreateIncludedShadersSection(settings));
            container.Add(CreateSplashSection());
            container.Add(CreateEmulationSection());
            page.Add(container);
            container.Bind(m_SerializedSettings);
            RefreshSplash();
        }

        VisualElement CreateSplashSection()
        {
            var section = CreateSection("Splash Screen");

            // Unbound, so only a user click raises a change event. Undo or a reverted asset updates the toggle without
            // one and leaves the scene list alone.
            var enableSplashScreen = m_SerializedSettings.FindProperty(k_EnableSplashScreenPath);
            m_EnableSplashToggle = new Toggle(enableSplashScreen.displayName)
            {
                name = k_EnableSplashToggleName,
                tooltip = enableSplashScreen.tooltip,
            };
            m_EnableSplashToggle.AddToClassList(BaseField<bool>.alignedFieldUssClassName);
            m_EnableSplashToggle.SetValueWithoutNotify(enableSplashScreen.boolValue);
            m_EnableSplashToggle.RegisterValueChangedCallback(OnSplashToggled);
            m_EnableSplashToggle.TrackPropertyValue(enableSplashScreen, _ => RefreshSplash());
            section.Add(m_EnableSplashToggle);

            m_SplashMessages = new VisualElement();
            section.Add(m_SplashMessages);

            m_SplashSettings = new VisualElement();
            foreach (var path in k_SplashStylePaths)
            {
                var field = new PropertyField(m_SerializedSettings.FindProperty(path));
                if (path == k_BackgroundColorPath)
                    m_BackgroundColorField = field;

                m_SplashSettings.Add(field);
            }

            m_LayerDataFields = new VisualElement();
            m_SplashSettings.Add(m_LayerDataFields);
            section.Add(m_SplashSettings);

            section.TrackPropertyValue(m_SerializedSettings.FindProperty(k_BackgroundTypePath), _ => RefreshSplash());
            section.TrackPropertyValue(m_SerializedSettings.FindProperty(k_LayerTypePath), _ => RefreshSplash());
            return section;
        }

        VisualElement CreateEmulationSection()
        {
            var section = CreateSection("Emulation");
            var property = m_SerializedSettings.FindProperty(k_EmulationInStandalonePath);
            section.Add(new PropertyField(property, k_EmulationInStandaloneLabel));
            section.Add(new HelpBox(k_EmulationInStandaloneDeprecation, HelpBoxMessageType.Warning)
            {
                name = k_EmulationInStandaloneHelpBoxName,
            });
            return section;
        }

        // The shaders are why builds preload the asset. Setup assigns them, so they're read-only, and the explanation
        // is background, so it's a tooltip.
        static VisualElement CreateIncludedShadersSection(CompositionLayersRuntimeSettings settings)
        {
            var section = CreateSection("Included Shaders", k_IncludedShadersTooltip);
            section.name = k_IncludedShadersName;
            section[0].Add(CreateHelpIcon(k_IncludedShadersDocsUrl));
            section.Add(CreateShaderField("Uber", settings.UberShader));
            section.Add(CreateShaderField("Color Scale Bias", settings.ColorScaleBiasShader));
            section.Add(CreateShaderField("Blit Copy HDR", settings.BlitCopyHDRShader));
            return section;
        }

        static ObjectField CreateShaderField(string label, Shader shader)
        {
            var field = new ObjectField(label)
            {
                objectType = typeof(Shader),
                tooltip = k_IncludedShadersTooltip,
            };
            field.SetValueWithoutNotify(shader);
            field.SetEnabled(false);
            return field;
        }

        // An image rather than a Button, whose padding shrinks a small icon to a few pixels.
        static Image CreateHelpIcon(string url)
        {
            var icon = new Image
            {
                image = EditorGUIUtility.IconContent("_Help").image,
                scaleMode = ScaleMode.ScaleToFit,
                tooltip = "Open the documentation.",
            };
            icon.style.width = k_HelpIconSize;
            icon.style.height = k_HelpIconSize;
            icon.style.marginLeft = k_HeaderSpacing;
            icon.AddManipulator(new Clickable(() => Application.OpenURL(url)));
            return icon;
        }

        static VisualElement CreateSection(string title, string tooltip = null)
        {
            var section = new VisualElement();
            section.style.marginBottom = k_SectionSpacing;

            // A row, so a section can add a help button beside its title.
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = k_HeaderSpacing;
            var titleLabel = new Label(title) { tooltip = tooltip };
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(titleLabel);
            section.Add(header);
            return section;
        }

        void OnSplashToggled(ChangeEvent<bool> change)
        {
            m_SerializedSettings.Update();
            m_SerializedSettings.FindProperty(k_EnableSplashScreenPath).boolValue = change.newValue;
            m_SerializedSettings.ApplyModifiedProperties();
            m_SplashScenes.SetScenes(SplashSceneSetup.UpdateScenes(
                change.newValue, m_SplashScenes.GetScenes(), m_SplashScenes.Folder));
            RefreshSplash();
        }

        void RefreshSplash()
        {
            if (m_SerializedSettings == null)
                return;

            m_SerializedSettings.Update();
            var splashEnabled = m_SerializedSettings.FindProperty(k_EnableSplashScreenPath).boolValue;
            m_EnableSplashToggle.SetValueWithoutNotify(splashEnabled);
            var isPassthrough = m_SerializedSettings.FindProperty(k_BackgroundTypePath).intValue ==
                (int)CompositionLayersRuntimeSettings.SplashBackgroundType.Passthrough;

            m_SplashSettings.style.display = splashEnabled ? DisplayStyle.Flex : DisplayStyle.None;
            m_BackgroundColorField.style.display = isPassthrough ? DisplayStyle.None : DisplayStyle.Flex;
            RefreshSplashMessages(splashEnabled);
            RefreshLayerDataFields();
        }

        void RefreshSplashMessages(bool splashEnabled)
        {
            m_SplashMessages.Clear();
            var messages = SplashSceneSetup.GetMessages(splashEnabled, SplashSceneSetup.IsPlayerSplashShown,
                m_SplashScenes.GetScenes(), m_SplashScenes.Folder);
            foreach (var (text, type) in messages)
            {
                m_SplashMessages.Add(new HelpBox(text, type));
            }
        }

        // Shows the fields of the selected layer type's data, without Apply Transform Scale, which a splash ignores.
        void RefreshLayerDataFields()
        {
            m_LayerDataFields.Clear();
            var isCylinder = m_SerializedSettings.FindProperty(k_LayerTypePath).intValue ==
                (int)CompositionLayersRuntimeSettings.Layer.Cylinder;
            var layerDataPath = isCylinder ? k_CylinderLayerDataPath : k_QuadLayerDataPath;
            var layerData = m_SerializedSettings.FindProperty(layerDataPath);

            foreach (var child in GetVisibleChildren(layerData))
            {
                if (child.name == k_ApplyTransformScaleName)
                    continue;

                m_LayerDataFields.Add(new PropertyField(child));
            }

            m_LayerDataFields.Bind(m_SerializedSettings);
        }

        static List<SerializedProperty> GetVisibleChildren(SerializedProperty parent)
        {
            var children = new List<SerializedProperty>();
            var child = parent.Copy();
            var end = parent.GetEndProperty();
            if (!child.NextVisible(true))
                return children;

            while (!SerializedProperty.EqualContents(child, end))
            {
                children.Add(child.Copy());
                if (!child.NextVisible(false))
                    break;
            }

            return children;
        }
    }
}
