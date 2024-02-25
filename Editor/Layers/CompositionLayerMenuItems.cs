using UnityEditor;

namespace Unity.XR.CompositionLayers.Layers.Editor
{
    static class CompositionLayerMenuItems
    {
        [MenuItem("GameObject/XR/Composition Layers/Quad Layer UI Panel")]
        static void CreateQuadLayerUIPanel()
        {
            CompositionLayerEditorUtils.CreateLayerGameObjectMenuItem(typeof(QuadLayerData));
            CompositionLayerEditorUtils.AddCanvasToActiveGameObject();
        }

        [MenuItem("GameObject/XR/Composition Layers/Quad Layer", false, 10)]
        static void CreateQuadLayer()
        {
            CompositionLayerEditorUtils.CreateLayerGameObjectMenuItem(typeof(QuadLayerData));
        }

        [MenuItem("Component/XR/Composition Layers/Quad Layer", false, 101)]
        static void CreateQuadLayerComponent()
        {
            CompositionLayerEditorUtils.CreateLayerComponentMenuItem(typeof(QuadLayerData));
        }

        [MenuItem("Component/XR/Composition Layers/Quad Layer", true)]
        static bool ValidateCreateQuadLayerComponent()
        {
            return CompositionLayerEditorUtils.ValidateCreateLayerComponentMenuItem();
        }

        [MenuItem("GameObject/XR/Composition Layers/Cylinder Layer UI Panel")]
        static void CreateCylinderLayerUIPanel()
        {
            CompositionLayerEditorUtils.CreateLayerGameObjectMenuItem(typeof(CylinderLayerData));
            CompositionLayerEditorUtils.AddCanvasToActiveGameObject();
        }

        [MenuItem("GameObject/XR/Composition Layers/Cylinder Layer", false, 11)]
        static void CreateCylinderLayer()
        {
            CompositionLayerEditorUtils.CreateLayerGameObjectMenuItem(typeof(CylinderLayerData));
        }

        [MenuItem("Component/XR/Composition Layers/Cylinder Layer", false,102)]
        static void CreateCylinderLayerComponent()
        {
            CompositionLayerEditorUtils.CreateLayerComponentMenuItem(typeof(CylinderLayerData));
        }

        [MenuItem("Component/XR/Composition Layers/Cylinder Layer", true)]
        static bool ValidateCreateCylinderLayerComponent()
        {
            return CompositionLayerEditorUtils.ValidateCreateLayerComponentMenuItem();
        }

        [MenuItem("GameObject/XR/Composition Layers/Equirect Layer", false, 12)]
        static void CreateEquirectMeshLayer()
        {
            CompositionLayerEditorUtils.CreateLayerGameObjectMenuItem(typeof(EquirectMeshLayerData));
        }

        [MenuItem("Component/XR/Composition Layers/Equirect Layer", false, 103)]
        static void CreateEquirectMeshLayerComponent()
        {
            CompositionLayerEditorUtils.CreateLayerComponentMenuItem(typeof(EquirectMeshLayerData));
        }

        [MenuItem("Component/XR/Composition Layers/Equirect Layer", true)]
        static bool ValidateEquirectMeshLayerComponent()
        {
            return CompositionLayerEditorUtils.ValidateCreateLayerComponentMenuItem();
        }

        [MenuItem("GameObject/XR/Composition Layers/Cube Layer", false, 13)]
        static void CreateCubeProjectionLayer()
        {
            CompositionLayerEditorUtils.CreateLayerGameObjectMenuItem(typeof(CubeProjectionLayerData));
        }

        [MenuItem("Component/XR/Composition Layers/Cube Layer", false, 104)]
        static void CreateCubeProjectionLayerComponent()
        {
            CompositionLayerEditorUtils.CreateLayerComponentMenuItem(typeof(CubeProjectionLayerData));
        }

        [MenuItem("Component/XR/Composition Layers/Cube Layer", true)]
        static bool ValidateCubeProjectionLayerComponent()
        {
            return CompositionLayerEditorUtils.ValidateCreateLayerComponentMenuItem();
        }

        [MenuItem("GameObject/XR/Composition Layers/Projection Layer (Stereo)", false, 14)]
        static void CreateProjectionLayer()
        {
            CompositionLayerEditorUtils.CreateLayerGameObjectMenuItem(typeof(ProjectionLayerData));
        }

        [MenuItem("Component/XR/Composition Layers/Projection Layer (Stereo)", false, 105)]
        static void CreateProjectionLayerComponent()
        {
            CompositionLayerEditorUtils.CreateLayerComponentMenuItem(typeof(ProjectionLayerData));
        }

        [MenuItem("Component/XR/Composition Layers/Projection Layer (Stereo)", true)]
        static bool ValidateProjectionLayerComponent()
        {
            return CompositionLayerEditorUtils.ValidateCreateLayerComponentMenuItem();
        }

        [MenuItem("GameObject/XR/Composition Layers/Projection Eye Rig", false, 30)]
        static void CreateProjectionLayerRig()
        {
            CompositionLayerEditorUtils.CreateLayerGameObjectMenuItem(typeof(ProjectionLayerRigData));
        }

        [MenuItem("Component/XR/Composition Layers/Projection Eye Rig", false, 201)]
        static void CreateProjectionLayerRigComponent()
        {
            CompositionLayerEditorUtils.CreateLayerComponentMenuItem(typeof(ProjectionLayerRigData));
        }

        [MenuItem("Component/XR/Composition Layers/Projection Eye Rig", true)]
        static bool ValidateProjectionLayerRigComponent()
        {
            return CompositionLayerEditorUtils.ValidateCreateLayerComponentMenuItem();
        }
    }
}
