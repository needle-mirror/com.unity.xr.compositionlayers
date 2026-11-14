using System.Collections.Generic;
using NUnit.Framework;
using Unity.XR.CompositionLayers.Emulation;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Layers.Editor;
using UnityEditor;
using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace Unity.XR.CompositionLayers.Tests.Editor
{
    /// <summary>
    /// Specifies how a projection rig's cameras are recorded when it is created, removed when its type changes, and
    /// reported when they are missing (UUM-160120).
    /// </summary>
    class ProjectionRigTypeChangeTests : CompositionLayerManagerTestBase
    {
        // ProjectionEyeRigUtil assigns the rig a user layer named after it; any non-default layer stands in for that.
        const int k_RigGameObjectLayer = 8;
        const HideFlags k_HiddenTexturesExtension = HideFlags.HideInInspector | HideFlags.HideInHierarchy;

        Camera m_MainCamera;

        protected override void Setup()
        {
            base.Setup();
            m_MainCamera = new GameObject("Main Camera").AddComponent<Camera>();
            m_MainCamera.tag = "MainCamera";
        }

        protected override void Teardown()
        {
            UnityObject.DestroyImmediate(m_MainCamera.gameObject);
            base.Teardown();
        }

        // Drives the emulated layer directly, because an open Editor that is not the active application skips the
        // edit mode player loop, so waiting for frames would never reach emulation in agent runs.
        [Test]
        public void RigTypeChange_RemovesRigCameras([Values] bool emulated)
        {
            var layer = CreateRigFromMenu();
            var otherChild = new GameObject("Other Child");
            otherChild.transform.SetParent(layer.transform, false);
            var (leftCamera, rightCamera) = GetChildCameras(layer);
            var emulatedLayer = emulated ? CreateEmulatedLayer(layer) : null;
            if (emulated)
                Assert.IsTrue(leftCamera.targetTexture, "Precondition: the rig is not being emulated.");

            Undo.IncrementCurrentGroup();
            layer.ChangeLayerDataType<QuadLayerData>();
            emulatedLayer?.ModifyLayer();
            emulatedLayer?.Dispose();

            var texturesExtension = layer.GetComponent<TexturesExtension>();
            Assert.IsFalse(leftCamera, "The Left Camera was not removed.");
            Assert.IsFalse(rightCamera, "The Right Camera was not removed.");
            Assert.IsTrue(otherChild, "A child that is not a rig camera was removed.");
            Assert.AreEqual(HideFlags.None, texturesExtension.hideFlags);
            Assert.IsNull(texturesExtension.LeftTexture);
            Assert.IsNull(texturesExtension.RightTexture);
            Assert.AreEqual(0, layer.gameObject.layer);
        }

        [Test]
        public void RigTypeChange_Undo_RestoresRig()
        {
            var layer = CreateRigFromMenu();
            Undo.IncrementCurrentGroup();
            layer.ChangeLayerDataType<QuadLayerData>();
            Undo.FlushUndoRecordObjects();
            Assert.IsEmpty(layer.GetComponentsInChildren<Camera>(true), "Precondition: cameras kept.");
            Assert.AreEqual(0, layer.gameObject.layer, "Precondition: the type change kept the rig's layer.");

            Undo.PerformUndo();

            var rigData = layer.LayerData as ProjectionLayerRigData;
            Assert.IsNotNull(rigData, "Undo did not restore the rig layer type.");
            var (leftCamera, rightCamera) = GetChildCameras(layer);
            Assert.AreSame(leftCamera, rigData.LeftCamera, "The rig did not keep its camera references.");
            Assert.AreSame(rightCamera, rigData.RightCamera, "The rig did not keep its camera references.");
            Assert.AreEqual(k_HiddenTexturesExtension, layer.GetComponent<TexturesExtension>().hideFlags);
            Assert.AreEqual(k_RigGameObjectLayer, layer.gameObject.layer);
        }

        [Test]
        public void RigLayerData_HidesCameraReferencesInInspector()
        {
            var layer = CreateRigFromMenu();
            var layerData = new SerializedObject(layer).FindProperty(nameof(CompositionLayer.m_LayerData));
            var end = layerData.GetEndProperty();
            var visibleNames = new List<string>();
            for (var hasNext = layerData.NextVisible(true);
                 hasNext && !SerializedProperty.EqualContents(layerData, end);
                 hasNext = layerData.NextVisible(false))
            {
                visibleNames.Add(layerData.name);
            }

            CollectionAssert.DoesNotContain(visibleNames, "m_LeftCamera");
            CollectionAssert.DoesNotContain(visibleNames, "m_RightCamera");
            CollectionAssert.DoesNotContain(visibleNames, "m_CamerasAssigned");
        }

        [Test]
        public void RigWithMissingCameras_ShowsWarning()
        {
            var completeRig = CreateRigFromMenu();
            var deletedCameraRig = CreateRigFromMenu();
            UnityObject.DestroyImmediate(GetChildCameras(deletedCameraRig).leftCamera.gameObject);
            var childlessRig = CreateTrackedLayer(typeof(ProjectionLayerRigData));

            Assert.IsNull(CompositionLayerSceneValidation.GetCameraConfigWarning(completeRig));
            Assert.AreEqual(
                ProjectionLayerRigData.k_MissingCamerasMessage,
                CompositionLayerSceneValidation.GetCameraConfigWarning(deletedCameraRig));
            Assert.AreEqual(
                ProjectionLayerRigData.k_MissingCamerasMessage,
                CompositionLayerSceneValidation.GetCameraConfigWarning(childlessRig));
        }

        [Test]
        public void RigWithAnotherRigsCameraReferences_DoesNotUseThem()
        {
            var sourceRig = CreateRigFromMenu();
            var targetRig = CreateRigFromMenu();
            var (sourceLeftCamera, sourceRightCamera) = GetChildCameras(sourceRig);
            var targetRigData = (ProjectionLayerRigData)targetRig.LayerData;
            targetRigData.SetCameras(sourceLeftCamera, sourceRightCamera);

            targetRig.ChangeLayerDataType<QuadLayerData>();

            Assert.IsTrue(sourceLeftCamera, "Another rig's Left Camera was removed.");
            Assert.IsTrue(sourceRightCamera, "Another rig's Right Camera was removed.");
        }

        [Test]
        public void RigWithBothCamerasDeleted_DoesNotAdoptOtherChildCameras()
        {
            var layer = CreateRigFromMenu();
            var (leftCamera, rightCamera) = GetChildCameras(layer);
            UnityObject.DestroyImmediate(leftCamera.gameObject);
            UnityObject.DestroyImmediate(rightCamera.gameObject);
            new GameObject("User Camera A", typeof(Camera)).transform.SetParent(layer.transform, false);
            new GameObject("User Camera B", typeof(Camera)).transform.SetParent(layer.transform, false);

            Assert.AreEqual(
                ProjectionLayerRigData.k_MissingCamerasMessage,
                CompositionLayerSceneValidation.GetCameraConfigWarning(layer));
        }

        [Test]
        public void UnderlayRig_KeepsClearFlagsWarning()
        {
            var layer = CreateRigFromMenu();
            layer.Order = -1;
            m_MainCamera.clearFlags = CameraClearFlags.Skybox;

            var warning = CompositionLayerSceneValidation.GetCameraConfigWarning(layer);
            Assert.IsNotNull(warning, "A complete underlay rig lost its clear flags warning.");
            StringAssert.Contains("underlay", warning);
        }

        // Goes through the GameObject menu path, then stands in for the name window, which sets the rig's layer.
        CompositionLayer CreateRigFromMenu()
        {
            CompositionLayerEditorUtils.CreateLayerGameObjectMenuItem(typeof(ProjectionLayerRigData));
#pragma warning disable CS0618
            ProjectionEyeRigUtil.CloseWindow();
#pragma warning restore CS0618

            var layer = Selection.activeGameObject.GetComponent<CompositionLayer>();
            layer.gameObject.layer = k_RigGameObjectLayer;
            m_TrackedLayers.Add(layer);
            return layer;
        }

        static EmulatedCompositionLayer CreateEmulatedLayer(CompositionLayer layer)
        {
            var emulatedLayer = new EmulatedCompositionLayer { CompositionLayer = layer };
            emulatedLayer.ChangeEmulatedLayerDataType(
                EmulatedCompositionLayerUtils.GetEmulatedLayerDataType(layer.LayerData.GetType()));
            emulatedLayer.UpdateLayer();
            return emulatedLayer;
        }

        static (Camera leftCamera, Camera rightCamera) GetChildCameras(CompositionLayer layer)
        {
            var cameras = layer.GetComponentsInChildren<Camera>(true);
            Assert.AreEqual(2, cameras.Length, "The rig does not have exactly two cameras.");
            return (cameras[0], cameras[1]);
        }
    }
}
