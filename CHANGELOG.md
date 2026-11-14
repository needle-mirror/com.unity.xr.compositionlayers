---
uid: xr-layers-changelog
---
# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [7.0.0-pre.1] - 2026-11-14

### Added

* Added the `MirrorViewRenderer.AlphaBlendMode` property, which reads and writes the value of the `AlphaMode` field.
* Added the `CompositionLayerDataAttribute.SuggestedExtensionTypes` property, which reads and writes the value of the misspelled `SuggestedExtenstionTypes` field and works as an attribute named argument.
* Added the protected `LayerUIScale` properties `TexturesExtension`, `CanvasRect`, `CanvasSizeX`, `CanvasSizeY`, `CanvasScaleX` and `CanvasScaleY`, which read and write the values of the fields they replace.
* Added `protected ReportChange()` to `CompositionLayerExtension` and `LayerData`. Subclasses call it to report a change to the layer provider when they modify state in place, for example a collection or texture they reference, which `UpdateValue` can't detect.

### Changed

* Changed the log when an emulated layer can't find the Uber shader from a warning to an error, since the layer doesn't render.
* Changed `CompositionLayer` to draw the Scene view outline of Quad and Cylinder layers itself. Earlier versions added a hidden `CompositionOutline` component to every layer and saved it in the scene. A layer now removes that component from its GameObject the next time the scene opens in the Unity Editor, and the package's own scenes no longer contain it.
* Updated Universal Render Pipeline (URP) support to require the URP 17 render graph system, and removed support for URP Compatibility Mode.
* Changed the minimum XR Interaction Toolkit version for Composition Layer UI interaction to 3.6.1. Projects that rely on UI interaction with an older XR Interaction Toolkit must upgrade to 3.6.1 or newer. XR Interaction Toolkit remains an optional package and projects without it compile as before. The Interactable UI Mirror inspector distinguishes a missing XR Interaction Toolkit from an installed version older than 3.6.1, and offers to upgrade it. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0).
* Changed the `ReportStateChange` callback on `CompositionLayerExtension` and `LayerData` from `protected internal` to `internal`. The package manages it, and it was never meant to be assigned or invoked by subclasses. Report property changes through `UpdateValue`, or through the new `protected ReportChange()` when state is modified in place. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0).
* Changed the minimum OpenXR Plugin version for composition layers at runtime to 7.0.0-pre.1.
* Changed the package version from 2.6.1 to 7.0.0-pre.1 to align the package major version with Unity 7.
* Changed the minimum Unity version from 6000.0 to 7000.0.
* Renamed public API members to follow C# and Unity conventions. Previous names are now obsolete members that forward to the new equivalent. Unity's script updater rewrites references to the new names the next time the project is opened in the Unity Editor. Renamed the following members:
  * `TexturesExtension.sourceTexture` is now `SourceTexture`.
  * `CompositionLayerManager.mainCameraCache` is now `MainCameraCache`.
* Renamed the External Surface sample's assembly from `AndriodSurfaceSample` to `AndroidSurfaceSample` and fixed the sample's display name. The sample imports into a new folder named after the corrected display name, so delete the previously imported sample from `Assets/Samples/XR Composition Layers` before importing it again. Otherwise the two copies conflict on their `.meta` file GUIDs.
* Changed XR Composition Layers to include the Uber, Color Scale Bias, and Blit Copy HDR shaders in every build through the Composition Layers settings asset in **Preloaded Assets** in the active **Player Settings**. Until the asset is set up on the **Composition Layers** settings page or with **Fix** in Project Validation, player builds log a warning and emulated layers don't render in players. If the asset turns on the splash screen or **Emulation in Standalone**, builds fail instead, since players would ignore it. Commit the asset and the **Player Settings** change. For more information, refer to [Shader inclusion](xref:xr-layers-graphics-settings#shader-inclusion).
* Changed the location of the Composition Layers settings to **Project Settings** > **XR Plug-in Management** > **Composition Layers**, which edits the settings asset and sets it up for builds. `CompositionLayersRuntimeSettings` now derives from `ScriptableObject` instead of `ScriptableSettings<T>`, and its asset is kept at `Assets/XR/Settings/CompositionLayersRuntimeSettings.asset` and preloaded in builds instead of loaded from a `Resources` folder. If an asset from an earlier version in a `Resources` folder turns on the splash screen or **Emulation in Standalone**, player builds fail until it's moved out with **Migrate Settings Asset** on the settings page or **Fix** in Project Validation. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0#move-the-runtime-settings-out-of-resources).
* Changed the composition layer emulation provider to initialize on every player platform instead of only the Editor and standalone players, so layers can fall back to emulation on any XR device.
* Changed composition layers to render through emulation until a provider plug-in starts to composite them. This applies on XR devices, in Play mode, and in Windows, macOS, and Linux players with an XR display running. Windows, macOS, and Linux players with no XR display running keep following the deprecated **Emulation in Standalone** setting. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0#emulation-without-provider).
* Changed projection rig layers (`ProjectionLayerRigData`) to render through emulation too, with each eye's projection applied to the rig's cameras. Rig content such as controllers lines up with the eye render, draws above other emulated layers, and blends premultiplied as it does when composited natively, whatever the layer's blend type.
* Changed the Default Scene Layer's Color Scale and Bias to apply through emulation in Play mode until a provider plug-in starts to composite layers, including with AR Foundation's XR Simulation. Edit mode and the Scene view preview it as before.
* Changed the sample descriptions in the Package Manager to use one short summary followed by a list of each sample's dependencies, matching the other XR packages.
* Changed Projection Eye Rig layers to keep a reference to their **Left Camera** and **Right Camera**, which emulation uses to render the rig. Rigs created with an earlier version use their first two child GameObjects, if both have a Camera component. Refer to [Projection Eye Rig](xref:xr-layers-projection-eye-rig).
* Changed the Editor to remove a Projection Eye Rig's cameras when a script changes the layer to another layer type, whether the layer renders natively or through emulation. You can undo the change.

### Deprecated

* Deprecated High Definition Render Pipeline (HDRP) support for in-Editor layer emulation and the Mirror View Renderer. HDRP still works in this release, and the package logs a warning when HDRP is active. The Universal Render Pipeline is the only fully supported pipeline, and HDRP support will be removed in a future release. `EmulationLayerHighDefinitionUnderlayCustomPass`, `EmulationLayerHighDefinitionOverlayCustomPass`, `ImageFilters.RenderPipeline.HighDefinition` and `RenderPipelineUtility.IsHDRenderPipeline` are marked obsolete and keep working. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0).
* Deprecated `ImageFilters.RenderPipeline.Builtin`. The value is kept, with its original numeric value, so existing references compile with a warning, and `Universal` and `HighDefinition` keep their numeric values too. Use `Universal`. It will be removed in a future release. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0).
* Marked `EmulationLayerUniversalScriptableRendererFeature` as obsolete. Code that references it now produces compile errors. This type has no effect and remains only so existing Universal Render Pipeline (URP) Renderer assets continue to load. Remove it from your URP Renderer assets, as it will be removed in a future release. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0).
* Deprecated calling `CompositionLayerExtension.Awake`, `OnEnable`, `OnDisable`, and `OnDestroy` directly. Unity invokes these callbacks itself, and calling them by hand can register or unregister an extension twice. They are marked obsolete and will become `protected` in a future release. Overrides keep working and compile with a warning that they override an obsolete member. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0).
* Deprecated the following types, which are helpers, custom editors, and build machinery that Unity discovers by reflection: `CompositionLayerAnalyticsBuildProcessor`, `CompositionLayerProjectValidation`, `CompositionLayerProjectValidationRuleSetup`, `CompositionLayerBuildValidationRule`, `CompositionLayersRuntimeSettingsEditor`, `UIHandleEditor`, `UIFocusEditor`, `InteractiveUIMirrorEditor`, `InteractableUIMirrorResolutionEditor`, `MirrorViewRendererEditor`, `ProjectionEyeRigUtil` and `GraphicsSettingsHelpers`. They keep working exactly as before, and code that references them compiles with a warning. They will be removed in a future release. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0).
* Deprecated `CompositionOutline`. `CompositionLayer` draws its own outline, so the component does nothing. It remains only so scenes and prefabs saved by earlier versions keep loading, and it will be removed in a future release. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0).
* Deprecated the `ImageFilters` members that only the package and its platform providers use: the `RenderPipeline` enum, the `HDRParams`, `BlitParams`, `TargetParams` and `MirrorViewParams` structs, and the `Blit`, `BlitMirrorView`, `BeginBatch` and `EndBatch` methods. `ImageFilters.AlphaMode`, which `MirrorViewRenderer` exposes, is unchanged. They keep working and compile with a warning, and will be removed in a future release. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0).
* Deprecated the `MirrorViewRenderer.AlphaMode` field. Use the `AlphaBlendMode` property instead. The field remains so code that passes it by `ref` or `out` keeps compiling, and it will be removed in a future release. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0).
* Deprecated `CompositionLayersRuntimeSettings.Instance`. To read the settings from code, reference the settings asset from a serialized field. `Instance` keeps returning the project's settings, or the defaults when the project has no settings asset, and no longer creates the asset. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0#runtime-settings-api).
* Deprecated the `CompositionLayerDataAttribute.SuggestedExtenstionTypes` field. Use the `SuggestedExtensionTypes` property instead. The field remains so existing attribute usages keep compiling, and it will be removed in a future release. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0).
* Deprecated the protected `LayerUIScale` fields `texturesExtension`, `canvasRect`, `canvasSizeX`, `canvasSizeY`, `canvasScaleX` and `canvasScaleY`. Use the new protected properties instead. The fields remain so subclasses keep compiling, and they will be removed in a future release. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0).
* Deprecated `CompositionLayersRuntimeSettings.EmulationInStandalone`. It only applies to Windows, macOS, and Linux players with no XR display running, where it still decides whether layers render through emulation. To keep layers out of these players, deactivate them instead. It will be removed in a future release. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0#emulation-in-standalone).
* Deprecated `GraphicsSettingsLoader`. It does nothing and will be removed in a future release. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0#graphics-settings-loader).

### Removed

* Removed Built-in Render Pipeline support for in-Editor layer emulation, Color Scale and Bias emulation, and the Mirror View Renderer. Those features now require the Universal Render Pipeline. Scenes that saved the `EmulatedColorScaleBias` component on their main camera keep loading, and the component now does nothing and can be removed. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0).
* Removed `EmulationLayerUniversalScriptableRendererPass.Execute`, which was deprecated in 2.2, along with URP Compatibility Mode support. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0).
* Removed the **Emulation In Play Mode** preference. In Play mode, composition layers render through emulation until a provider plug-in starts to composite them. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0#emulation-in-play-mode).
* Removed the **Emulation in Scene** setting from the **Composition Layers** page of the **Preferences** window. The Scene view always shows composition layers through emulation. To hide a layer in the Scene view, turn off its Scene visibility in the Hierarchy window. A Project Validation warning offers to delete the leftover `CompositionLayersPreferences` asset. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0#composition-layers-preferences).
* Removed the **Display Angles As** setting from the **Composition Layers** page of the **Preferences** window. The Inspector always shows Cylinder and Equirect layer angles in degrees. Scripts can set them in degrees with the `InDegrees` properties, such as `CylinderLayerData.CentralAngleInDegrees`. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0#composition-layers-preferences).

### Fixed

* Fixed Projection Eye Rig layers so they keep their cameras and continue to render when composition layer emulation restarts. ([UUM-160120](https://issuetracker.unity3d.com/product/unity/issues/guid/UUM-160120))
* Fixed Projection Eye Rig layers whose cameras are missing so they log an error each time emulation starts, instead of an exception every frame, and show a warning in the **Inspector** and **Hierarchy** windows.
* Fixed `NullReferenceException` errors thrown every frame by layer providers such as OpenXR when the scene contained a Composition Layer with no Layer Type. Composition Layers without a Layer Type are now ignored, with a warning, and are not included in `CompositionLayerManager.CompositionLayers` until a Layer Type is assigned.
* Fixed deprecation warnings about `OnPreRender` and `OnPostRender` being found on `CompositionSplash`, and `CanvasLayerCameraController` by removing the usages of those methods.
* Fixed a `FileNotFoundException` thrown when this package is loaded in CoreCLR Editor.
* Fixed emulated composition layers being drawn into secondary cameras under the High Definition Render Pipeline. Emulated layers are now restricted to the cameras selected for emulation, matching the Universal Render Pipeline. They no longer appear in secondary cameras such as UI, capture, and projection rig cameras.
* Fixed a per-frame managed allocation in mirror view blitting.
* Fixed emulated composition layers discarding pixels with alpha below 0.5. The emulation shader no longer applies an alpha test, so translucent and soft-edged content blends as it does when composited natively.
* Fixed `ProjectionLayerData` serialization. It is now marked `[Serializable]` like the other layer types, so Projection layers serialize without a warning.
* Fixed component help links in the Inspector pointing to the 0.6 documentation when the package version can't be resolved. They now fall back to the 7.0 documentation.
* Fixed the Sample External Android Surface Project so that its image is a texture assigned in the scene, which keeps working after a package version change and is no longer copied into every Android build in the project.
* Fixed `CompositionLayerUtils.CreateLayerData` to return `null` for an unknown layer data type, as documented, instead of throwing an `ArgumentNullException` or `NullReferenceException`. `CompositionLayer.ChangeLayerDataType` logs a warning and leaves the layer data unchanged for an unknown type instead of throwing. ([UUM-159113](https://issuetracker.unity3d.com/product/unity/issues/guid/UUM-159113))

## [2.6.1] - 2026-10-16

### Removed
* Removed the **Window > XR > Composition Layers > Project Validation** menu item. It opened the same Project Validation page that XR Plug-in Management provides under **Project Settings > XR Plug-in Management > Project Validation**.

### Fixed
* Fixed the Composition Layer component's Reset (inspector menu) which cleared the layer's `LayerData`, and left the layer with no type/data and collapsed the Composition Layer and Source Textures inspectors. ([UUM-147687](https://issuetracker.unity3d.com/product/unity/issues/guid/UUM-147687))

* Fixed a `NullReferenceException` ("SerializedObject of SerializedProperty has been Disposed") that was thrown when undoing a layer type change in the Composition Layer inspector. ([UUM-147688](https://issuetracker.unity3d.com/product/unity/issues/guid/UUM-147688))

* Fixed `InteractableUIMirror` so that a UI Panel layer created before installing the XR Interaction Toolkit finishes its setup and renders once the XR Interaction Toolkit is installed. ([UUM-144204](https://issuetracker.unity3d.com/product/unity/issues/guid/UUM-144204))

* Fixed `InteractableUIMirror` rendering blank and not responding to input when its scene was created by a script or in batch mode.
* Fixed an obsolete API warning (CS0618) for `EditorUtility.InstanceIDToObject` from the Composition Layer Hierarchy window icon in Unity 6.3.

## [2.6.0] - 2026-09-03

### Added
* Added a Dynamic Texture option to the source texture component. When enabled, the layer's swapchain image is rewritten every frame, so textures whose contents are updated externally on the GPU (such as native plugin or AHardwareBuffer-backed `Texture2D` objects) no longer appear static.
* Added a `Mip Mode` option to the source texture component that controls how a Local Texture's mip levels reach the layer swapchain: `CopyFromSource`, `AutoGenerate`, or `None` (default). See the [Source Textures component](xref:xr-layers-source-textures) documentation for details.
* Added an inspector warning to the Equirect layer when the Upper Vertical Angle is at or below the Lower Vertical Angle.

### Fixed
* Fixed per-frame managed allocations in `CompositionLayerManager` caused by `CompositionLayerManager.LayerInfo` not implementing `IEquatable<LayerInfo>`.
* Fixed `InteractableUIMirror` so that XRI input continues to work correctly when switching a composition layer's type at runtime via `ChangeLayerDataType`.
* Fixed `InteractableUIMirror` so that interactive UI controls like sliders, scrollbars, scroll views, and joystick scrolling work correctly on composition layer canvases.
* Fixed the Default Scene Layer so it is reliably found after domain reload, scene reopening, and other editor lifecycle events, and no longer produces an error when duplicated.
* Fixed documentation to clarify that single-pass instanced (SPI) rendering and GPU Instancing are supported for projection layers.
* Fixed static cleanup analyzer warnings in Unity 6000.7.
* Fixed Equirect layer emulation so that the Editor preview matches the headset output.

### Changed
* Raised the minimum supported Unity Editor version to 6000.0.
* The Default Scene Layer is now serialized into the scene (persisted in the scene file, still excluded from builds) instead of being recreated on load.
* Changed how the Equirect layer's Upper and Lower Vertical Angles are represented. They are now measured from the equator (positive above, negative below), so a full sphere uses Upper +90 and Lower -90 instead of the previous +90 and +90. Existing layers are upgraded automatically.

## [2.5.0] - 2026-06-15

### Added
* Added hierarchy window warning icon and inspector warnings for camera clear flags or background alpha using underlay or passthrough composition layers.
* Added the current render texture resolution to the `InteractableUIMirror` inspector.
* Added Individual, Left and Right Target Eye option to the source texture component. Allowing per-eye or single eye textures for standard Quad, Cylinder, Equirect, and Cube layers.

### Fixed
* Fixed Color Scale and Bias emulation in the Editor and Standalone builds.
* Fixed composition layer emulation not rendering in Editor Play Mode or Standalone builds.
* Fixed `CompositionLayer` to successfully open prefabs from scenes that do not contain any composition layers.
* Fixed `EmulatedLayerData` so it no longer logs an error related to a destroyed CompositionLayer when changing scenes from within prefab mode.
* Fixed `InteractableUIMirror` so that its fields are correctly serialized, resolving a warning that was previously logged to the console in Unity 6000.6.0a5 or newer.
* Fixed `CameraTargetTextureFactory` so that it no longer logs an error related to releasing render texture that is set as Camera.targetTexture.

### Changed
* Emulation in Play mode and Standalone builds previously depended on the now obsolete `com.unity.modules.vr` and now depends on`com.unity.modules.xr` and falls back to always emulating when the XR module is not present.

## [2.4.0] - 2026-03-12

### Added
* Added `CompositionLayerManager.TryGetLayerId(CompositionLayer layer, out int layerId)` to get a layer's unique id instead of `GetInstanceID()` or `GetEntityID()`.

### Fixed
* Fixed `InteractableUIMirror` to support creating or duplicating them during runtime such as with prefabs or asset bundles.
* Fixed `CompositionLayersRuntimeSettings` so that the `Script` property is no longer modifiable from the inspector.
* Fixed `InteractableUIMirror` so cameras can be added at runtime without camera layer issues.
* Fixed `ScriptableSingleton` warning log when first launching a project with the composition layers package.

### Changed
* Changed `InteractactableUIMirror` Cameras so they are visible in the inspector
* Changed how CompositionLayerManager initializes `LayerInfo.Id` to use its own generated unique ids instead of `GetInstanceID()` or `GetEntityID()`.

## [2.3.0] - 2025-12-15

### Added
* Added Background Type option to the Composition Layer Splash Screen settings to allow using Passthrough as the background.

### Fixed
* Fixed `DefaultLayerData` so it no longer logs a warning related to the `SerializeReferenceAttribute` when you install this package for the first time.
* Fixed internal code so that inspecting a `CompositionLayersRuntimeSettings` object soon after package import no longer causes a warning to be logged to the console.

## [2.2.0] - 2025-10-07

### Deprecated
* Deprecated `EmulationLayerUniversalScriptableRendererPass.Execute`. This method depends on URP Compatibility Mode, which has been deprecated since Unity 6.0 and is removed in Unity 6.4.

## [2.1.1] - 2025-09-30

### Added
* Added Right-eye configuration support to the Stereo Projection Layer.

### Fixed
* Fixed `CompositionLayer`'s `LayerType` property to not ask the user twice to add an extension when set to anything other than `[Empty]` if the user chooses `No`.
* Fixed `NullReferenceException` when adding a Source Textures component to an empty GameObject.
* Fixed shader warnings.
* Fixed a build warning by updating `SceneEmulatedProjectionRig` to no longer implement `IPreprocessBuildWithReport`.

## [2.1.0] - 2025-06-25

### Added
* Added Game view and Standalone color scale bias emulation for Default Scene Layer for the Built-in Render Pipeline and Universal Render Pipeline.
* Added Project Validation rules for Composition Layer Transparency.

### Fixed
* Fixed `CompositionLayerManager` so that it correctly sets layers to be visible when they are enabled after previously being disabled.

## [2.0.0] - 2024-12-10

### Fixed
* Fixed Composition Layers not hiding or showing correctly when using Scene Visibility in the hierarchy.
* Fixed an issue where non-cubemap textures could be assigned to the Cube Projection Layer.

### Changed
* Moved `ProjectionEyeRigUtil` to the `Unity.XR.CompositionLayers.Layers.Editor` namespace.
* Moved `CompositionSplash` to the `Unity.XR.CompositionLayers` namespace.

## [1.0.0] - 2024-10-09

### Added
* Added Default Layer Support allows for the creation of a default layer and rearragment of the default layer order.
* Added Composition Layer Sample Scene to demonstrate the use of Composition Layers in a scene.
* Added Composition Layer Android Sample Surface Scene to demonstrate the use of Andriods External Surface in a scene.
* Added supporing HDR Tonemapping extension. It allows to set HDR parameters for each layer.
* Added supporting MirrorViewRenderer. It provides to draw mirror view rendering with layers on XR.
* Added supporting automatically renderer feature settings on URP and HDRP.

### Fixed
* Fixed Interactable UI Scalings are now consistent between editor and build.
* Fixed Projection Eye Rig Emulation for URP and Unity6.

### Changed
* Removed EmulationLayerUniversalScriptableRendererFeature.(Manual configuration is no longer required.)

## [0.6.0] - 2024-06-26

### Added
* Added a projection validation to check if EmulationLayerUniversalScriptableRendererFeature is added to the current pipeline for URP. Click "Fix" button will automatically add the emulation render feature to enable URP Editor emulation.
* Added composition layer splash screen support. See Composition Layer Splash Screen section in documentation for details.

### Changed
* Emulation In Playmode or Standalone now is only available when no XR provider is active or no headset is connected for visual approximation and preview purposes.

### Fixed
* Fixed error spamming issue when creating a UI canvas and drag it into a quad layer.

## [0.5.0] - 2024-02-25

### This is the first experimental release of *Unity Package XR CompositionLayers \<com.unity.xr.compositionlayers\>*.
