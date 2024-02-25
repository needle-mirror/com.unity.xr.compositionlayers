using UnityEngine;
using UnityEngine.UIElements;
using Unity.XR.CompositionLayers.Services;
using UnityEditor;

namespace Unity.XR.CompositionLayers.Tests.Editor
{
    [CustomEditor(typeof(UserDefaultLayerTestComponent))]
    class UserDefaultLayerTestComponentEditor : UnityEditor.Editor
    {
        SerializedProperty m_CompositionLayerProperty;

        public override VisualElement CreateInspectorGUI()
        {
            var rootElement = new VisualElement();

            m_CompositionLayerProperty = serializedObject.FindProperty("m_CompositionLayer");

            var setDefaultSceneLayerElement = new Button(OnSetDefaultSceneLayerElement);
            setDefaultSceneLayerElement.text = "Set as Scene Layer";
            rootElement.Add(setDefaultSceneLayerElement);

            var unsetDefaultSceneLayerElement = new Button(OnUnsetDefaultSceneLayerElement);
            unsetDefaultSceneLayerElement.text = "Unset Scene Layer";
            rootElement.Add(unsetDefaultSceneLayerElement);

            return rootElement;
        }

        void OnSetDefaultSceneLayerElement()
        {
            var compositionLayer = m_CompositionLayerProperty.objectReferenceValue as CompositionLayer;

            if (CompositionLayerManager.IsLayerSceneValid(compositionLayer))
                CompositionLayerManager.Instance.SetDefaultSceneCompositionLayer(compositionLayer);
        }

        static void OnUnsetDefaultSceneLayerElement()
        {
            CompositionLayerManager.Instance.ResetDefaultSceneCompositionLayer();
        }
    }
}
