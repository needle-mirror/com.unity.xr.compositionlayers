#if UNITY_EDITOR
using System;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Provider;
using Unity.XR.CompositionLayers.Services;
using UnityEngine;

namespace Unity.XR.CompositionLayers
{
    /// <summary>
    /// Draws the Scene view outline of a Quad or Cylinder <see cref="CompositionLayer"/> from its
    /// <c>OnDrawGizmos</c> callback. The outline shows the layer's bounds, which is useful when a child
    /// Canvas has a different aspect ratio than the layer and leaves empty space.
    /// </summary>
    static class CompositionLayerOutlineGizmo
    {
        const int k_CylinderStepCount = 32;

        internal static void Draw(CompositionLayer layer)
        {
            switch (layer.LayerData)
            {
                case QuadLayerData quad:
                    DrawQuadOutline(layer, quad);
                    break;
                case CylinderLayerData cylinder:
                    DrawCylinderOutline(layer, cylinder);
                    break;
            }
        }

        static void DrawQuadOutline(CompositionLayer layer, QuadLayerData data)
        {
            var transform = layer.transform;

            // The inverse of the layer's scale lets the outline size change independently of the transform
            // when ApplyTransformScale is false.
            var adjustmentScale = data.ApplyTransformScale
                ? Vector2.one
                : new Vector2(1 / transform.localScale.x, 1 / transform.lossyScale.y);

            var halfWidth = data.Size.x / 2 * adjustmentScale.x;
            var halfHeight = data.Size.y / 2 * adjustmentScale.y;
            var matrix = GetTransformMatrix(layer);

            var bottomLeft = matrix.MultiplyPoint(new Vector3(-halfWidth, -halfHeight, 0f));
            var topLeft = matrix.MultiplyPoint(new Vector3(-halfWidth, halfHeight, 0f));
            var topRight = matrix.MultiplyPoint(new Vector3(halfWidth, halfHeight, 0f));
            var bottomRight = matrix.MultiplyPoint(new Vector3(halfWidth, -halfHeight, 0f));

            Gizmos.DrawLine(bottomLeft, topLeft);
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
        }

        static void DrawCylinderOutline(CompositionLayer layer, CylinderLayerData data)
        {
            var transform = layer.transform;
            var lossyScale = transform.lossyScale;

            // Scaling a cylinder layer doesn't stretch the GameObject. It adjusts the radius, height and central
            // angle instead, so the transform scale is removed from the vertices and re-applied to those values.
            var inverseScale = new Vector3(1 / lossyScale.x, 1 / lossyScale.y, 1 / lossyScale.z);
            var adjustmentScale = data.ApplyTransformScale ? lossyScale : Vector3.one;

            var radius = data.Radius * adjustmentScale.z;
            var height = data.GetHeight() * adjustmentScale.y;
            var centralAngle = data.CentralAngle * adjustmentScale.x / adjustmentScale.z;

            var matrix = GetTransformMatrix(layer);
            var yTop = height * 0.5f * inverseScale.y;
            var yBottom = height * -0.5f * inverseScale.y;
            var stepAngleDelta = centralAngle / k_CylinderStepCount;
            var currentAngle = -centralAngle / 2.0f;

            var previousTop = matrix.MultiplyPoint(ArcPoint(currentAngle, radius, yTop, inverseScale));
            var previousBottom = matrix.MultiplyPoint(ArcPoint(currentAngle, radius, yBottom, inverseScale));
            Gizmos.DrawLine(previousTop, previousBottom);

            for (var i = 0; i < k_CylinderStepCount; i++)
            {
                currentAngle += stepAngleDelta;
                var top = matrix.MultiplyPoint(ArcPoint(currentAngle, radius, yTop, inverseScale));
                var bottom = matrix.MultiplyPoint(ArcPoint(currentAngle, radius, yBottom, inverseScale));

                Gizmos.DrawLine(previousTop, top);
                Gizmos.DrawLine(previousBottom, bottom);

                previousTop = top;
                previousBottom = bottom;
            }

            Gizmos.DrawLine(previousTop, previousBottom);
        }

        static Vector3 ArcPoint(float angle, float radius, float y, Vector3 scale)
        {
            var x = (float)Math.Sin(angle) * radius;
            var z = (float)Math.Cos(angle) * radius;
            return new Vector3(x * scale.x, y, z * scale.z);
        }

        static Matrix4x4 GetTransformMatrix(CompositionLayer layer)
        {
            var provider = PlatformManager.ActivePlatformProvider;
            if (provider == null || provider.GetSelectedCoordinateSystem(layer) == PlatformProvider.DefaultCoordinateSystem)
            {
                return layer.transform.localToWorldMatrix;
            }

            var cameraData = new CustomTransformCameraData
            {
                MainCamera = CompositionLayerUtils.GetStereoMainCamera(),
                IsSceneView = true
            };
            return provider.GetCustomTransformData(layer, cameraData).Matrix;
        }
    }
}
#endif
