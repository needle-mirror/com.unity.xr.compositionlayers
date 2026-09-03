---
uid: xr-layers-whats-new
---
# What's new in version 2.6

Learn about the latest features, improvements, and fixes.

This release includes the following significant changes:

## Mipmap support for local textures

The [Source Textures component](xref:xr-layers-source-textures) has a new **Mip Mode** option that controls how a Local Texture's mip levels are written into the layer's swapchain image:

- **None** (default): the swapchain is created with a single mip level.
- **Copy From Source**: every authored mip level is copied directly from the source texture. Use this when the source format is compatible with `Graphics.CopyTexture`.
- **Auto Generate**: mip level 0 is blitted into the swapchain and the lower mip levels are generated automatically. Use this for sources that cannot be copied directly, such as compressed or cross-format textures.

> [!NOTE]
> Mip Mode requires Unity 6000.7.0a4 or newer and OpenXR Plugin 1.19.0-pre.2 or later.

## Dynamic textures

The Source Textures component has a new **Dynamic Texture** option. When enabled, the layer's swapchain image is rewritten every frame, so textures whose contents are updated externally on the GPU (such as native plugin or `AHardwareBuffer`-backed `Texture2D` objects) no longer appear static.

> [!NOTE]
> Dynamic Texture requires OpenXR Plugin 1.19.0-pre.2 or later.

## Equirect vertical angles are now measured from the equator

The Equirect layer's **Upper** and **Lower Vertical Angle** are now measured from the equator (positive above the equator, negative below). A full sphere now uses an Upper angle of +90 and a Lower angle of -90, instead of the previous +90 and +90. Existing layers are upgraded automatically.

## Other changes

- The minimum supported Unity Editor version is now 6000.0.
- The Default Scene Layer is now serialized into the scene (persisted in the scene file, still excluded from builds) instead of being recreated on load.

For a full list of changes in this version including backwards-compatible bugfixes, refer to the package [changelog](xref:xr-layers-changelog).
