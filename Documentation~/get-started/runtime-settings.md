---
uid: xr-layers-runtime-settings
---
# Enable layer emulation in a standalone build

Learn how to enable layer emulation in a standalone build.

To enable layer emulation in a standalone **Win, Mac, Linux** build:

1. Navigate to the `Assets/CompositionLayers/UserSettings/Resources` folder in your **Project** window. (Unity creates this folder automatically when you import the XR Composition Layers package.)
1. Select the `CompositionLayersRuntimeSettings` asset to view its properties in the **Inspector**.
1. Enable the **Emulation in Standalone** option.

> [!TIP]
> You must also add the **Uber** shader to the [Always Include Shaders list](xref:xr-layers-graphics-settings#always-include-shaders-list) to make sure that this shader, which is needed to render emulated layers, is included in your standalone builds.

## Additional resources

* [Configure graphics settings](xref:xr-layers-graphics-settings)
