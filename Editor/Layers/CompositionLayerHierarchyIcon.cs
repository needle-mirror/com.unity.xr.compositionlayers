using UnityEditor;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace Unity.XR.CompositionLayers.Layers.Editor
{
    [InitializeOnLoad]
    static class CompositionLayerHierarchyIcon
    {
        const float k_IconSize = 16f;
        const float k_IconPadding = 2f;

        [NoAutoStaticsCleanup]
        static GUIContent s_WarningIcon;

        static CompositionLayerHierarchyIcon()
        {
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= OnHierarchyItemGUI;
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyItemGUI;
        }

        static void OnHierarchyItemGUI(EntityId entityId, Rect selectionRect)
        {
            DrawIcon(EditorUtility.EntityIdToObject(entityId) as GameObject, selectionRect);
        }

        static void DrawIcon(GameObject go, Rect selectionRect)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            if (go == null)
                return;

            var layer = go.GetComponent<CompositionLayer>();
            if (layer == null)
                return;

            var warning = CompositionLayerSceneValidation.GetCameraConfigWarning(layer);
            if (warning == null)
                return;

            if (s_WarningIcon == null)
                s_WarningIcon = new GUIContent(EditorGUIUtility.IconContent("console.warnicon.sml"));
            s_WarningIcon.tooltip = warning;

            var iconRect = new Rect(
                selectionRect.xMax - k_IconSize - k_IconPadding,
                selectionRect.y,
                k_IconSize,
                selectionRect.height);

            GUI.Label(iconRect, s_WarningIcon);
        }
    }
}
