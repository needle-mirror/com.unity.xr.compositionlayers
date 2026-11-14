using UnityEngine;
using System;
using UnityEngine.Experimental.Rendering;
using Unity.XR.CompositionLayers;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Services;
#if XR_OPENXR_7_0_0_PRE_1_OR_NEWER
using UnityEngine.XR.OpenXR.CompositionLayers;
#endif

/// <summary>
/// Class for displaying an image on the Android surface of this GameObject's composition layer.
/// </summary>
public class TestAndroidImage : MonoBehaviour
{
    [SerializeField, Tooltip("The image to draw on the Android surface. In its import settings, enable Read/Write, " +
        "set Non-Power of 2 to None and use an uncompressed format such as RGBA32. Match the Textures Extension " +
        "resolution to the image size.")]
    Texture2D m_Image;

    bool m_CanDisplayImage;

    /// <summary>
    /// Checks the image is usable, then displays it.
    /// </summary>
    void Start()
    {
        m_CanDisplayImage = CanDisplayImage();
        if (!m_CanDisplayImage)
        {
            enabled = false;
            return;
        }

        DisplayImageAsync();
    }

    /// <summary>
    /// Checks the image can be drawn on the Android surface, and logs what to fix if not.
    /// </summary>
    /// <returns><see langword="true"/> if the image can be drawn.</returns>
    bool CanDisplayImage()
    {
        var prefix = $"{nameof(TestAndroidImage)} on '{name}'";
        if (m_Image == null)
        {
            Debug.LogError($"{prefix} has no Image assigned. Assign a texture.", this);
            return false;
        }

        if (!m_Image.isReadable)
        {
            Debug.LogError($"{prefix} needs Read/Write enabled on Image '{m_Image.name}'.", this);
            return false;
        }

        if (GraphicsFormatUtility.IsCompressedFormat(m_Image.graphicsFormat))
        {
            Debug.LogError(
                $"{prefix} needs an uncompressed format, such as RGBA32, on Image '{m_Image.name}'.", this);
            return false;
        }

        var textures = GetComponent<TexturesExtension>();
        if (textures != null && (textures.Resolution.x != m_Image.width || textures.Resolution.y != m_Image.height))
        {
            Debug.LogWarning(
                $"{prefix}: Image '{m_Image.name}' is {m_Image.width}x{m_Image.height}, but the Textures Extension " +
                $"resolution is {textures.Resolution.x}x{textures.Resolution.y}, so the image may be cut off. " +
                "Set Non-Power of 2 to None on the texture and match the resolution to the image size.", this);
        }

        return true;
    }

    /// <summary>
    /// Waits for the layer's Android surface from OpenXRLayerUtility, then draws the image on it.
    /// Stops waiting if this GameObject is destroyed first.
    /// </summary>
    async void DisplayImageAsync()
    {
        try
        {
            var layer = GetComponent<CompositionLayer>();
            var surface = GetAndroidSurface(layer);
            while (surface == IntPtr.Zero)
            {
                await Awaitable.NextFrameAsync(destroyCancellationToken);
                surface = GetAndroidSurface(layer);
            }

            AndroidTestSurface.InitTestSurface(surface, m_Image);
        }
        catch (OperationCanceledException)
        {
            // This GameObject was destroyed before its surface was ready.
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
    }

    /// <summary>
    /// Gets the Android surface of a composition layer.
    /// </summary>
    /// <param name="layer">The composition layer.</param>
    /// <returns>The surface, or <see cref="IntPtr.Zero"/> if it is not available yet.</returns>
    static IntPtr GetAndroidSurface(CompositionLayer layer)
    {
#if XR_OPENXR_7_0_0_PRE_1_OR_NEWER
        if (CompositionLayerManager.TryGetLayerId(layer, out var layerId))
            return OpenXRLayerUtility.GetLayerAndroidSurfaceObject(layerId);
#endif
        return IntPtr.Zero;
    }

    /// <summary>
    /// Event handler triggered when the application gains or loses focus.
    /// Displays the image again when the application gains focus.
    /// </summary>
    /// <param name="hasFocus">Boolean indicating if the application has focus.</param>
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus || !m_CanDisplayImage)
            return;

        DisplayImageAsync();
    }
}
