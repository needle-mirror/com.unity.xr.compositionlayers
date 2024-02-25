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

> [!TIP]
> The main camera in an XR scene is typically a child of the **Camera Offset** GameObject, underneath the **XR Origin**. This camera is the one used to render the default scene layer, which always has a compositing order of zero. Any layers with a negative number as its compositing order are obscured by the default scene layer unless you change the camera properties to as described here.