using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;

namespace Unity.XR.CompositionLayers.Tests.Layers
{
    /// <summary>
    /// Tests that a <see cref="CompositionLayer"/> with no <see cref="CompositionLayer.LayerData"/>
    /// is not reported to layer providers, is reported once LayerData is assigned, and is removed
    /// when its LayerData is cleared.
    /// </summary>
    class LayerWithoutDataTests : CompositionLayerManagerTestBase
    {
        static readonly Regex k_NoLayerTypeWarning = new Regex("has no Layer Type");

        // Reset assigns a type on AddComponent in Edit Mode, so it is cleared before enabling.
        CompositionLayer CreateActiveLayerWithoutData()
        {
            var layer = CreateLayerGameObject(false);
            layer.ChangeLayerDataType((LayerData)null);
            layer.gameObject.SetActive(true);
            m_TrackedLayers.Add(layer);
            return layer;
        }

        static bool IsManaged(CompositionLayer layer) =>
            CompositionLayerManager.Instance.CompositionLayers.Contains(layer);

        [Test]
        public async Task EnabledLayerWithoutData_IsNotReportedToProviders()
        {
            LogAssert.Expect(LogType.Warning, k_NoLayerTypeWarning);
            var layer = CreateActiveLayerWithoutData();
            await NextManagerUpdate();

            Assert.IsFalse(WasReportedCreated(layer));
            Assert.IsFalse(WasReportedActive(layer));
            Assert.IsFalse(IsManaged(layer));
        }

        [Test]
        public async Task EnabledLayerWithoutData_WarnsOnce()
        {
            LogAssert.Expect(LogType.Warning, k_NoLayerTypeWarning);
            var layer = CreateActiveLayerWithoutData();
            await NextManagerUpdate();

            layer.gameObject.SetActive(false);
            layer.gameObject.SetActive(true);
            await NextManagerUpdate();

            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public async Task LayerDataAssignedRightAfterEnable_IsCreatedWithoutWarning()
        {
            var layer = CreateActiveLayerWithoutData();
            layer.ChangeLayerDataType<QuadLayerData>();
            await NextManagerUpdate();

            Assert.IsTrue(WasReportedCreated(layer));
            Assert.IsTrue(WasReportedActive(layer));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public async Task LayerDataAssignedLater_ReportsCreated()
        {
            LogAssert.Expect(LogType.Warning, k_NoLayerTypeWarning);
            var layer = CreateActiveLayerWithoutData();
            await NextManagerUpdate();

            layer.ChangeLayerDataType<QuadLayerData>();
            await NextManagerUpdate();

            Assert.IsTrue(WasReportedCreated(layer));
            Assert.IsTrue(WasReportedActive(layer));
            Assert.IsTrue(IsManaged(layer));
        }

        [Test]
        public async Task LayerDataCleared_ReportsRemoved()
        {
            var layer = CreateActiveLayer<QuadLayerData>();
            await NextManagerUpdate();
            Assert.IsTrue(CompositionLayerManager.TryGetLayerId(layer, out var layerId));

            LogAssert.Expect(LogType.Warning, k_NoLayerTypeWarning);
            layer.ChangeLayerDataType((LayerData)null);
            await NextManagerUpdate();

            CollectionAssert.Contains(lastRemovedLayers, layerId);
            Assert.IsFalse(WasReportedActive(layer));
            Assert.IsFalse(IsManaged(layer));
        }

#if UNITY_EDITOR
        // In Play Mode the undo rescan keeps known layers, so this relies on OnValidate.
        [Test]
        public async Task UndoingLayerDataChange_ReportsRemovedThenCreated()
        {
            var layer = CreateActiveLayer<QuadLayerData>();
            await NextManagerUpdate();
            Assert.IsTrue(CompositionLayerManager.TryGetLayerId(layer, out var layerId));

            UnityEditor.Undo.IncrementCurrentGroup();
            layer.ChangeLayerDataType((LayerData)null);
            UnityEditor.Undo.FlushUndoRecordObjects();
            LogAssert.Expect(LogType.Warning, k_NoLayerTypeWarning);
            await NextManagerUpdate();

            CollectionAssert.Contains(lastRemovedLayers, layerId);
            Assert.IsFalse(IsManaged(layer));

            UnityEditor.Undo.PerformUndo();
            await NextManagerUpdate();

            Assert.IsInstanceOf<QuadLayerData>(layer.LayerData);
            Assert.IsTrue(WasReportedCreated(layer));
            Assert.IsTrue(IsManaged(layer));

            UnityEditor.Undo.PerformRedo();
            LogAssert.Expect(LogType.Warning, k_NoLayerTypeWarning);
            await NextManagerUpdate();

            Assert.IsNull(layer.LayerData);
            Assert.IsFalse(IsManaged(layer));
        }
#endif

        [Test]
        public async Task SceneScan_DoesNotReportLayerWithoutData()
        {
            LogAssert.Expect(LogType.Warning, k_NoLayerTypeWarning);
            var layer = CreateActiveLayerWithoutData();
            await NextManagerUpdate();

            CompositionLayerManager.Instance.FindAllLayersInScene();
            await NextManagerUpdate();

            Assert.IsFalse(WasReportedCreated(layer));
            Assert.IsFalse(IsManaged(layer));
        }
    }
}
