using System.Collections;
using NUnit.Framework;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;
using UnityEditor;
using UnityEngine.TestTools;
using UnityObject = UnityEngine.Object;

namespace Unity.XR.CompositionLayers.Tests.Editor
{
    class LayerComponentTests : CompositionLayerManagerTestBase
    {
        [Test]
        public void AddingLayerAddsExpectedExtensions()
        {
            var layer = CreateLayerGameObject();

            layer.ChangeLayerDataType((LayerData)null);
            layer.AddSuggestedExtensions();
            var texExt = layer.gameObject.GetComponent<TexturesExtension>();
            Assert.IsNull(texExt);

            var layerId = typeof(CylinderLayerData).FullName;
            var layerData = CompositionLayerUtils.CreateLayerData(layerId);
            layer.ChangeLayerDataType(layerData);
            layer.AddSuggestedExtensions();

            texExt = layer.gameObject.GetComponent<TexturesExtension>();
            Assert.IsNotNull(texExt);
        }

        [Test]
        public void NewLayerDefaultsToQuadLayerData()
        {
            var layer = CreateLayerGameObject(false);

            Assert.IsInstanceOf<QuadLayerData>(layer.LayerData);
        }

        [Test]
        public void Reset_PreservesLayerTypeAndOrder()
        {
            var layer = CreateActiveLayer(typeof(CylinderLayerData), order: 3);
            var expectedOrder = layer.Order; // The manager may adjust the requested order on activation.

            layer.SendMessage("OnValidate");

            // The SerializeReference LayerData is cleared and the order is clobbered.
            layer.m_LayerData = null;
            layer.SetLayerOrderInternal(expectedOrder + 100);

            layer.SendMessage("Reset");

            Assert.IsInstanceOf<CylinderLayerData>(layer.LayerData);
            Assert.AreEqual(expectedOrder, layer.Order);
        }

        [Test]
        public void Reset_DoesNotLeaveLayerWithoutData()
        {
            var layer = CreateActiveLayer(typeof(CylinderLayerData));
            layer.SendMessage("OnValidate");

            layer.m_LayerData = null;
            layer.SendMessage("Reset");

            Assert.IsNotNull(layer.LayerData);
        }

        // Regression test for UUM-147688: undoing a layer type change caused a
        // "SerializedObject of SerializedProperty has been Disposed" error.
        [UnityTest]
        public IEnumerator UndoAfterInspectorDisposedDoesNotThrow()
        {
            var layer = CreateLayerGameObject(false);
            layer.gameObject.SetActive(true);

            Undo.IncrementCurrentGroup();
            layer.ChangeLayerDataType(typeof(CylinderLayerData));
            Undo.FlushUndoRecordObjects();

            Undo.IncrementCurrentGroup();
            layer.ChangeLayerDataType(typeof(QuadLayerData));
            Undo.FlushUndoRecordObjects();

            // Opening the inspector subscribes CompositionLayerEditor to Undo.undoRedoPerformed and
            // initializes the serialized properties the queued callback reads.
            var editor = UnityEditor.Editor.CreateEditor(layer);
            editor.CreateInspectorGUI();

            // Two undos before an editor tick fire UndoRedoPerformed twice becuase the callback must
            // still be queued only once so OnDisable can remove it completely.
            Undo.PerformUndo();
            Undo.PerformUndo();
            UnityObject.DestroyImmediate(editor);

            // Activating a layer while a Default Scene Layer exists emits a benign
            // "Layer Order already in use" Log that is unrelated to this regression.
            yield return null;
            yield return null;
        }
    }
}
