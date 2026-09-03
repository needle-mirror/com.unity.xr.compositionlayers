#if !UNITY_XR_INTERACTION_TOOLKIT && UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
#if LIFECYCLE_APIS_AVAILABLE
using Unity.Scripting.LifecycleManagement;
#endif

namespace Unity.XR.CompositionLayers.UIInteraction.Editor
{
    /// <summary>
    /// Flags a Warningn in the Inspector if the XR Interaction Toolkit is not installed.
    /// Directs Users to install the XR Interaction Toolkit.
    /// </summary>
    [CustomEditor(typeof(InteractableUIMirror))]
    public partial class InteractiveUIMirrorEditor : UnityEditor.Editor
    {
#if LIFECYCLE_APIS_AVAILABLE
        [AutoStaticsCleanup]
#endif
        private static AddRequest addPackageRequest;
#if LIFECYCLE_APIS_AVAILABLE
        [AutoStaticsCleanup]
#endif
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
        /// Prompts users to installs the Unity XRI Package.
        /// </summary>
        public static void InstallXRIPackage()
        {
            isInstalling = true;
            bool userWantsToInstall = EditorUtility.DisplayDialog(
                "Install XR Interaction Toolkit",
                "The XR Interaction Toolkit is required for Composition Layers Interactive UI. Would you like to install it now?",
                "Yes", "No");

            if (userWantsToInstall)
            {
                addPackageRequest = Client.Add("com.unity.xr.interaction.toolkit");
                EditorApplication.update += Progress;
            }
            else
                isInstalling = false;
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
        /// Add a Warning to the Inspector if the XR Interaction Toolkit is not installed.
        /// </summary>
        public override void OnInspectorGUI()
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                DrawDefaultInspector();
                return;
            }

            base.OnInspectorGUI();
            EditorGUILayout.HelpBox("Installing the XR Interaction Toolkit is required for this component.", MessageType.Warning);
            if (GUILayout.Button("Install XRI Package"))
            {
               InstallXRIPackage();
            }
        }
    }
}
#endif

#if UNITY_XR_INTERACTION_TOOLKIT && UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Unity.XR.CompositionLayers.UIInteraction.Editor
{
    /// <summary>
    /// Draws the Interactable UI Mirror inspector and shows the current render texture
    /// resolution, which is driven by the Canvas width and height.
    /// </summary>
    [CustomEditor(typeof(InteractableUIMirror))]
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
