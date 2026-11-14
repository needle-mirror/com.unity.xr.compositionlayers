---
uid: xr-layers-whats-new
---
# What's new in version 7.0

Learn about the latest features, improvements, and fixes.

Composition Layers 7.0.0-pre.1 is a major release that raises the package's minimum requirements and deprecates API that will be removed in a future release. For the changes that can require edits to your project, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0).

This release includes the following significant changes:

## Updated minimum requirements

- The minimum supported Unity Editor version is now 7000.0.
- Composition layers at runtime require OpenXR Plugin 7.0.0-pre.1 or newer.
- In-Editor layer emulation and the Mirror View Renderer require the Universal Render Pipeline (URP) 17 render graph system. Support for URP Compatibility Mode and for the Built-in Render Pipeline in those features is removed, and High Definition Render Pipeline (HDRP) support is deprecated.
- UI interaction on composition layers requires XR Interaction Toolkit 3.6.1 or newer.

## Layer emulation without a provider plug-in

Composition layers now render through emulation until a provider plug-in starts to composite them. Layers appear on XR devices, in Play mode, and in Windows, macOS, and Linux players with an XR display running, even when no provider plug-in composites layers. Projection rig layers also render through emulation, aligned with each eye. For more information, refer to [Layer emulation at runtime](xref:xr-layers-runtime-settings).

The **Emulation in Standalone** runtime setting is deprecated, and now applies only to Windows, macOS, and Linux players with no XR display running. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0).

## Composition Layers preferences removed

The **Composition Layers** page of the **Preferences** window is removed, along with its **Emulation In Play Mode**, **Emulation in Scene**, and **Display Angles As** settings. The Scene view always shows layers through emulation, and the Inspector always shows angles in degrees. Saved angles don't change. Project Validation offers to delete the leftover preferences asset. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0#composition-layers-preferences).

## Emulation shaders in every build

The shaders that layer emulation and the mirror view use are now included in every build once the Composition Layers settings asset is set up, so you no longer need to add them to the **Always Included Shaders** list. For more information, refer to [Shader inclusion](xref:xr-layers-graphics-settings#shader-inclusion).

`GraphicsSettingsLoader` is deprecated and does nothing. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0#graphics-settings-loader).

## Settings asset in Preloaded Assets

The splash screen and emulation settings are now on their own page, **Edit** > **Project Settings** > **XR Plug-in Management** > **Composition Layers**. Composition Layers stores them in `CompositionLayersRuntimeSettings.asset` outside any `Resources` folder, and the asset reaches player builds through **Preloaded Assets** along with the emulation shaders it references.

Set up the asset once per project with the button on the **Composition Layers** page, or with **Fix** in **Project Validation**. If your project has the asset in a `Resources` folder from an earlier version, the same button migrates it and keeps its settings. Commit the asset and the **Player Settings** change to version control. For more information, refer to [Composition Layers settings (XR Plug-in Management) reference](xref:xr-layers-project-settings) and [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0#move-the-runtime-settings-out-of-resources).

## Breaking changes

This release contains breaking changes. For the changes you need to make to your project, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0).

## Other changes

- `CompositionLayerExtension` and `LayerData` have a new protected `ReportChange()` method for reporting state that is modified in place.
- The `ReportStateChange` callback on `CompositionLayerExtension` and `LayerData` is now `internal`.

For a full list of changes in this version including backwards-compatible bugfixes, refer to the package [changelog](xref:xr-layers-changelog).
