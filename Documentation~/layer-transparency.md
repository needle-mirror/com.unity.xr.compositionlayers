---
uid: xr-layers-transparency
---

# Set composition layer transparency

Composition layers are blended according to their alpha channel. A completely opaque area of a channel obscures any layers already drawn. When you render objects to a Projection layer -- including the default scene layer -- you must make sure that the camera renders with a transparent background if you want layers behind it to be visible.

To set the main scene camera in an XR scene to render with a transparent background:

1. Select the **Camera** GameObject in the scene Hierarchy to view its properties in the Inspector.
2. Select from the following options. The options differ based on the render pipeline you're using:
    * Universal Render Pipeline (URP): In the **Environment** section, set the **Background Type** to **Solid Color**.
    * Built-In Render Pipeline: Set **Clear Flags** to **Solid Color**.
3. Select the **Background** color to open the color picker.
4. Set the color's **A** value to 0.
5. If you are using High Dynamic Range Rendering (HDR), make sure that your graphics  **Quality** or **Tier** settings are set to allow the scene to be rendered with an alpha channel. Refer to [Render texture format](#render-texture-format) for more infommation.
6. If you are using post-processing effects, make sure that **Alpha Processing** is enabled. Refer to [Post processing](#post-processing) for more information.
> [!TIP]
> The main camera in an XR scene is typically a child of the **Camera Offset** GameObject, underneath the **XR Origin**. This camera is the one used to render the default scene layer, which always has a compositing order of zero. Any layers with a negative number as its compositing order are obscured by the default scene layer unless you change the camera properties to as described here.

## Render Texture format

In order to store information to the alpha channel, the format of the render texture the scene is rendered to must contain an alpha channel. Some High Dynamic Range (HDR) rendering formats drop alpha channels to help improve performance. Configure the following settings to make sure composition layer transparency is preserved under HDR:

* Universal Render Pipeline (URP): Make sure that if you are using HDR rendering (**Quality**->**HDR** is enabled) that the **Quality**->**HDR Precision** is set to **64 Bits** to allow for an alpha channel to be included.
* Built-In Render Pipeline: make sure that if you are using HDR rendering (**Project Settings**->**Graphics**->**Tier Settings**->**Use HDR**) that the **Project Settings**->**Graphics**->**Tier Settings**->**HDR Mode** is set to **FP16**

## Post Processing

Post processing effects may discard alpha channel information depending on the implementation. If you use post processing effects in your scene, you may need to enable additional options to allow the alpha channel information to be preserved:

* Universal Render Pipeline (URP): Make sure that **Post-processing**->**Alpha Processing** is enabled on the active URP Render Pipeline Asset.
