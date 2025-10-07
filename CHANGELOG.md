---
uid: xr-layers-changelog
---
# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

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
