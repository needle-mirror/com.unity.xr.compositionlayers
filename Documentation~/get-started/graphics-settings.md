---
uid: xr-layers-graphics-settings
---
# Configure graphics settings

Configure graphics settings to ensure that the composition layers display correctly in the Unity Editor and at runtime.

XR Composition Layers configures the following graphics settings automatically:

* The shaders it needs are included in every build. Refer to [Shader inclusion](#shader-inclusion).
* If the project uses the Universal Render Pipeline (URP), the [Emulation Layer Renderer Feature](#emulation-layer-renderer-feature) is automatically added.
* If the project uses the High Definition Render Pipeline (HDRP), the [Emulation Layer Custom Pass](#emulation-layer-custom-pass) is automatically added to the global `CustomPassVolume`.

The following sections provide more detail on these settings.

<a id="shader-inclusion"></a>
## Shader inclusion

Composition layer emulation and the mirror view use the `Unlit/XRCompositionLayers/Uber`, `Hidden/XRCompositionLayers/ColorScaleBias`, and `Unlit/XRCompositionLayers/BlitCopyHDR` shaders. You don't need to add them to the **Always Included Shaders** list.

Every build references these shaders from the Composition Layers settings asset, `Assets/XR/Settings/CompositionLayersRuntimeSettings.asset`, which must be in **Preloaded Assets** (**Edit** > **Project Settings** > **Player** > **Other Settings** > **Optimization**). If the active build profile overrides Player Settings, the asset must be in that build profile's list instead. Until the asset is set up, player builds log a warning and emulated layers don't render in players. If the asset turns on the splash screen or **Emulation in Standalone**, builds fail instead, since players would ignore it. To set it up, go to **Edit** > **Project Settings** > **XR Plug-in Management** > **Composition Layers** and select the button the page displays, or select **Fix** on the `CompositionLayersRuntimeSettings.asset` error in **Project Validation**. The page lists the shaders under **Included Shaders**. Commit the asset and the settings change to version control.

<a id="emulation-layer-renderer-feature"></a>
## Emulation Layer Renderer Feature (URP)

To support layer emulation in projects using the Universal Render Pipeline (URP), the `EmulationLayerUniversalScriptableRendererPass` (ScriptableRendererPass) is automatically added with `ScriptableRenderer.EnqueuePass()`.

To support Color Scale and Bias emulation in projects using the Universal Render Pipeline (URP), the `EmulationColorScaleBiasPass` (ScriptableRendererPass) is automatically added with `ScriptableRenderer.EnqueuePass()` when the **Color Scale and Bias** component is added to the Default Layer.

<a id="emulation-layer-custom-pass"></a>
## Emulation Layer Custom Pass (HDRP)

> [!NOTE]
> HDRP support for layer emulation and the Mirror View Renderer is deprecated as of Composition Layers 7.0.0-pre.1 and will be removed in a future release. Use the Universal Render Pipeline (URP).

To support layer emulation in projects using the High Definition Render Pipeline (HDRP), the `EmulationLayerHighDefinitionOverlayCustomPass` and `EmulationLayerHighDefinitionUnderlayCustomPass` are automatically added to the global `CustomPassVolume`.

The GameObject which has CustomPassVolume is hidden in hierarchy.

## Additional resources

* [Layer emulation at runtime](xref:xr-layers-runtime-settings)
