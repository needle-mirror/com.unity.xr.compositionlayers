#if !UNITY_XR_INTERACTION_TOOLKIT_3_6_1_OR_NEWER && UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using Unity.Scripting.LifecycleManagement;

namespace Unity.XR.CompositionLayers.UIInteraction.Editor
{
    /// <summary>
    /// Flags a warning in the Inspector if the XR Interaction Toolkit is missing or older than the
    /// version Composition Layers requires, and offers to install or upgrade it.
    /// </summary>
    [CustomEditor(typeof(InteractableUIMirror))]
    [System.Obsolete("InteractiveUIMirrorEditor is not intended for use outside the Composition Layers package. It is obsolete as of Composition Layers 7.0.0-pre.1 and will be removed in a future release.")]
    public partial class InteractiveUIMirrorEditor : UnityEditor.Editor
    {
        const string k_PackageName = "com.unity.xr.interaction.toolkit";
        const string k_MinimumVersion = "3.6.1";
#if UNITY_XR_INTERACTION_TOOLKIT
        const bool k_OlderVersionInstalled = true;
#else
        const bool k_OlderVersionInstalled = false;
#endif

        [AutoStaticsCleanup]
        private static AddRequest addPackageRequest;
        [AutoStaticsCleanup]
        private static bool isInstalling = false;

        void Awake()
        {
            if (!isInstalling)
            {
                var allInteractiveUI = Resources.FindObjectsOfTypeAll<InteractableUIMirror>();
                if (allInteractiveUI.Length > 0)
                {
                    InstallXRIPackage();
                }
            }

        }

        /// <summary>
        /// Prompts users to install the XR Interaction Toolkit, or to upgrade it when an older version is installed.
        /// </summary>
        public static void InstallXRIPackage()
        {
            isInstalling = true;
            var title = k_OlderVersionInstalled ? "Upgrade XR Interaction Toolkit" : "Install XR Interaction Toolkit";
            var message = k_OlderVersionInstalled
                ? $"Composition Layers Interactive UI requires XR Interaction Toolkit {k_MinimumVersion} or newer. Version {GetInstalledVersion()} is installed. Would you like to upgrade it now?"
                : "The XR Interaction Toolkit is required for Composition Layers Interactive UI. Would you like to install it now?";
            var userWantsToInstall = EditorUtility.DisplayDialog(title, message, "Yes", "No");

            if (userWantsToInstall)
            {
                addPackageRequest = Client.Add(k_OlderVersionInstalled ? $"{k_PackageName}@{k_MinimumVersion}" : k_PackageName);
                EditorApplication.update += Progress;
            }
            else
                isInstalling = false;
        }

        static string GetInstalledVersion()
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForPackageName(k_PackageName);
            return package != null ? package.version : "unknown";
        }

        /// <summary>
        /// Tracks the progress of a Package installation.
        /// </summary>
        private static void Progress()
        {
            if (addPackageRequest.IsCompleted)
            {
                if (addPackageRequest.Status == StatusCode.Success)
                    Debug.Log("XRI Package installed successfully");
                else if (addPackageRequest.Status >= StatusCode.Failure)
                    Debug.LogError("Failed to install XRI Package: " + addPackageRequest.Error.message);

                EditorApplication.update -= Progress;
                isInstalling = false;
            }
        }

        /// <summary>
        /// Adds a warning to the Inspector if the XR Interaction Toolkit is missing or too old.
        /// </summary>
        public override void OnInspectorGUI()
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                DrawDefaultInspector();
                return;
            }

            base.OnInspectorGUI();
            var helpText = k_OlderVersionInstalled
                ? $"XR Interaction Toolkit {k_MinimumVersion} or newer is required for this component. Version {GetInstalledVersion()} is installed."
                : "Installing the XR Interaction Toolkit is required for this component.";
            EditorGUILayout.HelpBox(helpText, MessageType.Warning);
            if (GUILayout.Button(k_OlderVersionInstalled ? "Upgrade XRI Package" : "Install XRI Package"))
            {
               InstallXRIPackage();
            }
        }
    }
}
#endif

#if UNITY_XR_INTERACTION_TOOLKIT_3_6_1_OR_NEWER && UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Unity.XR.CompositionLayers.UIInteraction.Editor
{
    /// <summary>
    /// Draws the Interactable UI Mirror inspector and shows the current render texture
    /// resolution, which is driven by the Canvas width and height.
    /// </summary>
    [CustomEditor(typeof(InteractableUIMirror))]
    [System.Obsolete("InteractableUIMirrorResolutionEditor is not intended for use outside the Composition Layers package. It is obsolete as of Composition Layers 7.0.0-pre.1 and will be removed in a future release.")]
    public class InteractableUIMirrorResolutionEditor : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint() => true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (serializedObject.isEditingMultipleObjects)
                return;

            var resolution = ((InteractableUIMirror)target).CurrentResolution;
            var resolutionLabel = resolution == Vector2Int.zero
                ? "Not generated yet"
                : $"{resolution.x} x {resolution.y}";

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.LabelField("Render Texture Resolution", resolutionLabel);

            EditorGUILayout.HelpBox(
                "Canvas Width & Height control resolution, not physical size. Increase them for a sharper texture. Size is set by the layer shape.",
                MessageType.Info);
        }
    }
}
#endif
