using Unity.XR.CompositionLayers.Extensions;
using UnityEngine;
using Unity.XR.CompositionLayers.Layers;

namespace Unity.XR.CompositionLayers.Emulation.Implementations
{
    [EmulatedLayerDataType(typeof(QuadLayerData))]
    internal class QuadEmulatedMeshLayerData : EmulatedMeshLayerData
    {
        Vector2 m_Size;

        public override bool IsSupported(Camera camera) => true;

        protected override void UpdateMesh(ref Mesh mesh)
        {
            var lossyScale = Transform.lossyScale;
            var size = GetMeshScale(lossyScale);
            if (mesh == null || m_Size != size)
            {
                m_Size = size;
                GeneratePlaneMesh(ref mesh, size);
            }
        }

        Vector2 GetMeshScale(Vector3 lossyScale)
        {
            var size = Vector2.one;
            if (LayerData is QuadLayerData quadLayer)
            {
                size = quadLayer.GetScaledSize(lossyScale);
                foreach (var extension in CompositionLayer.Extensions)
                {
                    if (extension is TexturesExtension textureExt)
                    {
                        if (textureExt.CropToAspect)
                        {
                            var requestedSize = size;
                            float reqSizeRatio = (float)requestedSize.x / (float)requestedSize.y;
                            float texRatio = 1f;
                            if (textureExt.sourceTexture == TexturesExtension.SourceTextureEnum.LocalTexture)
                            {
                                if (textureExt.LeftTexture == null)
                                    break;
                                texRatio = (float)textureExt.LeftTexture.width / (float)textureExt.LeftTexture.height;
                            }
                            else
                            {
                                texRatio = (float)textureExt.Resolution.x / (float)textureExt.Resolution.y;
                            }

                            if (reqSizeRatio > texRatio)
                            {
                                // too wide
                                requestedSize.x = requestedSize.y * texRatio;
                            }
                            else if (reqSizeRatio < texRatio)
                            {
                                // too narrow
                                requestedSize.y = requestedSize.x / texRatio;
                            }

                            return requestedSize * 0.5f;
                        }
                    }
                }
            }

            return size * 0.5f;
        }
    }
}
