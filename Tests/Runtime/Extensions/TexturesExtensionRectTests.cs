using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Tests;

namespace Unity.XR.CompositionLayers.Extensions.Tests
{
    /// <summary>
    /// Tests the source/destination rect behavior of <see cref="TexturesExtension"/>.
    /// The rect getters return the default full rect (0,0,1,1) unless
    /// <c>CustomRects</c> is enabled, then they return the stored values.
    /// </summary>
    class TexturesExtensionRectTests : CompositionLayerManagerTestBase
    {
        static readonly Rect k_DefaultRect = new Rect(0f, 0f, 1f, 1f);
        static readonly Rect k_CustomRect = new Rect(0.1f, 0.2f, 0.3f, 0.4f);

        // Public so they can be used as parameter types on the public [Test] methods.
        public enum RectKind { LeftSource, RightSource, LeftDestination, RightDestination }
        public enum ToggleProperty { CustomRects, CropToAspect }

        static Rect GetRect(TexturesExtension ext, RectKind kind) => kind switch
        {
            RectKind.LeftSource => ext.LeftEyeSourceRect,
            RectKind.RightSource => ext.RightEyeSourceRect,
            RectKind.LeftDestination => ext.LeftEyeDestinationRect,
            _ => ext.RightEyeDestinationRect,
        };

        static void SetRect(TexturesExtension ext, RectKind kind, Rect value)
        {
            switch (kind)
            {
                case RectKind.LeftSource: ext.LeftEyeSourceRect = value; break;
                case RectKind.RightSource: ext.RightEyeSourceRect = value; break;
                case RectKind.LeftDestination: ext.LeftEyeDestinationRect = value; break;
                default: ext.RightEyeDestinationRect = value; break;
            }
        }

        [Test]
        public void RectGetter_ReturnsDefault_WhenCustomRectsFalse([Values] RectKind kind)
        {
            var ext = CreateTrackedLayer().gameObject.AddComponent<TexturesExtension>();
            ext.CustomRects = false;

            SetRect(ext, kind, k_CustomRect);

            Assert.AreEqual(k_DefaultRect, GetRect(ext, kind));
        }

        [Test]
        public void RectGetter_ReturnsStored_WhenCustomRectsTrue([Values] RectKind kind)
        {
            var ext = CreateTrackedLayer().gameObject.AddComponent<TexturesExtension>();
            ext.CustomRects = true;

            SetRect(ext, kind, k_CustomRect);

            Assert.AreEqual(k_CustomRect, GetRect(ext, kind));
        }

        [Test]
        public void CustomRectsAndCropToAspect_DefaultFalse()
        {
            var ext = CreateTrackedLayer().gameObject.AddComponent<TexturesExtension>();

            Assert.IsFalse(ext.CustomRects);
            Assert.IsFalse(ext.CropToAspect);
        }

        [Test]
        public async Task TogglingRectProperty_ReportsModified([Values] ToggleProperty property)
        {
            var layer = CreateActiveLayer<QuadLayerData>(addSuggestedExtensions: true);
            await new WaitForUpdateCall(this);
            var ext = GetExtension<TexturesExtension>(layer);
            Assert.IsNotNull(ext);
            await new WaitForUpdateCall(this);

            if (property == ToggleProperty.CustomRects)
                ext.CustomRects = true;
            else
                ext.CropToAspect = true;

            await new WaitForUpdateCall(this);
            Assert.IsTrue(hasModifiedLayers);
        }
    }
}
