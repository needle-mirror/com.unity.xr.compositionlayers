using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.XR.CompositionLayers.Emulation;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;
using UnityEditor;
using UnityEngine;
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
            var layer = CreateLayerGameObject(false, assignLayerType: false);

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

        // Undo, redo and prefab revert set m_LayerData like this, without calling the setter.
        static void SetLayerDataThroughSerialization(CompositionLayer layer, LayerData layerData)
        {
            var serializedObject = new SerializedObject(layer);
            serializedObject.FindProperty("m_LayerData").managedReferenceValue = layerData;
            serializedObject.ApplyModifiedProperties();
        }

        [Test]
        public void LayerDataClearedThroughSerialization_StopsManagingLayer()
        {
            var layer = CreateActiveLayer<QuadLayerData>();
            Assume.That(CompositionLayerManager.Instance.CompositionLayers, Has.Member(layer));

            SetLayerDataThroughSerialization(layer, null);

            Assert.IsNull(layer.LayerData);
            CollectionAssert.DoesNotContain(CompositionLayerManager.Instance.CompositionLayers, layer);
        }

        [Test]
        public void LayerDataAssignedThroughSerialization_ManagesLayer()
        {
            var layer = CreateLayerGameObject(false, assignLayerType: false);
            m_TrackedLayers.Add(layer);
            layer.ChangeLayerDataType((LayerData)null);
            layer.gameObject.SetActive(true);
            Assume.That(CompositionLayerManager.Instance.CompositionLayers, Has.No.Member(layer));

            SetLayerDataThroughSerialization(layer, new QuadLayerData());

            Assert.IsInstanceOf<QuadLayerData>(layer.LayerData);
            CollectionAssert.Contains(CompositionLayerManager.Instance.CompositionLayers, layer);
        }

        // The editor's undo handler rescans the scene after an undo. The provider must still
        // receive the removal queued before the rescan, or the emulated layer keeps being drawn.
        [Test]
        public void UndoingLayerDataChange_ReportsRemovedAndCreated()
        {
            var noLayerTypeWarning = new Regex("has no Layer Type");
            var layer = CreateActiveLayer<QuadLayerData>();
            var manager = CompositionLayerManager.Instance;
            var previousProvider = manager.LayerProvider;
            manager.LayerProvider = this;
            try
            {
                manager.Update();
                Assert.IsTrue(WasReportedCreated(layer));
                Assert.IsTrue(CompositionLayerManager.TryGetLayerId(layer, out var layerId));

                Undo.IncrementCurrentGroup();
                layer.ChangeLayerDataType((LayerData)null);
                Undo.FlushUndoRecordObjects();
                LogAssert.Expect(LogType.Warning, noLayerTypeWarning);
                manager.Update();
                CollectionAssert.Contains(lastRemovedLayers, layerId);
                CollectionAssert.DoesNotContain(manager.CompositionLayers, layer);

                Undo.PerformUndo();
                manager.Update();
                Assert.IsInstanceOf<QuadLayerData>(layer.LayerData);
                Assert.IsTrue(WasReportedCreated(layer));
                Assert.IsTrue(CompositionLayerManager.TryGetLayerId(layer, out layerId));

                Undo.PerformRedo();
                LogAssert.Expect(LogType.Warning, noLayerTypeWarning);
                manager.Update();
                Assert.IsNull(layer.LayerData);
                CollectionAssert.Contains(lastRemovedLayers, layerId);
                CollectionAssert.DoesNotContain(manager.CompositionLayers, layer);
            }
            finally
            {
                manager.LayerProvider = previousProvider;
            }
        }

        // A destroyed layer must stop listening to its LayerData, and a layer whose data changes must stop
        // listening to the old one. Otherwise LayerData shared between layers, like the splash settings,
        // keeps dead layers subscribed and invokes their handlers on later changes.
        [Test]
        public void LayerData_StateChangeSubscription_FollowsLayerLifetime()
        {
            var sharedData = CompositionLayerUtils.CreateLayerData(typeof(QuadLayerData).FullName);
            var first = CreateLayerGameObject(false, assignLayerType: false);
            var second = CreateLayerGameObject(false, assignLayerType: false);
            m_TrackedLayers.Add(second);
            first.ChangeLayerDataType(sharedData);
            second.ChangeLayerDataType(sharedData);
            first.gameObject.SetActive(true);
            second.gameObject.SetActive(true);

            CollectionAssert.AreEquivalent(new object[] { first, second }, StateChangeSubscribers(sharedData));

            UnityObject.DestroyImmediate(first.gameObject);
            CollectionAssert.AreEquivalent(new object[] { second }, StateChangeSubscribers(sharedData));

            second.ChangeLayerDataType(CompositionLayerUtils.CreateLayerData(typeof(CylinderLayerData).FullName));
            CollectionAssert.IsEmpty(StateChangeSubscribers(sharedData));
        }

        static List<object> StateChangeSubscribers(LayerData layerData)
        {
            var subscribers = new List<object>();
            var field = typeof(LayerData).GetField("ReportStateChange", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field?.GetValue(layerData) is not Delegate handlers)
            {
                return subscribers;
            }

            foreach (var handler in handlers.GetInvocationList())
            {
                subscribers.Add(handler.Target);
            }

            return subscribers;
        }

        // A rescan caused by another layer changing must not report an untyped layer that was already reported.
        [Test]
        public void RescanAfterAnotherLayerChanges_DoesNotWarnAgain()
        {
            // Emulation logs a one-time warning on the first typed layer when the project has no URP asset.
            // That warning is not what this test checks, so treat it as already shown.
            EmulatedLayerProvider.s_WarnUnsupportedRenderPipeline = false;

            var layer = CreateLayerGameObject(false, assignLayerType: false);
            m_TrackedLayers.Add(layer);
            layer.ChangeLayerDataType((LayerData)null);
            layer.gameObject.SetActive(true);

            LogAssert.Expect(LogType.Warning, new Regex("has no Layer Type"));
            CompositionLayerManager.Instance.Update();

            CreateActiveLayer<QuadLayerData>();
            CompositionLayerManager.Instance.OccupiedLayersDirty = true;
            CompositionLayerManager.Instance.Update();

            LogAssert.NoUnexpectedReceived();
        }

        // Saving a prefab in Prefab Mode can give an untyped layer a type while its Order is held by the scene instance.
        // Moving it to a free Order runs OnValidate in the Editor, which must not register the layer a second time.
        [Test]
        public void TypedLayerWithOccupiedOrder_IsRegisteredOnce()
        {
            var typedLayer = CreateActiveLayer<QuadLayerData>();

            var layer = CreateLayerGameObject(false, assignLayerType: false);
            m_TrackedLayers.Add(layer);
            layer.ChangeLayerDataType((LayerData)null);
            layer.gameObject.SetActive(true);
            Assume.That(CompositionLayerManager.Instance.CompositionLayers, Has.No.Member(layer));

            // Like a prefab save, the LayerData arrives without going through the setter.
            layer.m_LayerData = new QuadLayerData();
            layer.SetLayerOrderInternal(typedLayer.Order);

            Assert.DoesNotThrow(() => CompositionLayerManager.Instance.CompositionLayerCreated(layer));
            CollectionAssert.Contains(CompositionLayerManager.Instance.CompositionLayers, layer);
            Assert.AreNotEqual(typedLayer.Order, layer.Order);
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
