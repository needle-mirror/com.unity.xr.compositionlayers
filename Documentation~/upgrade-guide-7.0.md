---
uid: xr-layers-upgrade-guide-7.0
---
# Upgrade to Composition Layers 7.0

Upgrade your project to Composition Layers 7.0.

Composition Layers 7.0 contains breaking changes which require action to successfully upgrade your project when you [upgrade the package](xref:um-upm-ui-update) from previous versions.

The following sections outline the changes you need to make to your project to use Composition Layers 7.0.

## Updated OpenXR Plugin version requirement

This release requires OpenXR Plugin 7.0.0-pre.1 or newer. That OpenXR Plugin release contains the matching composition layer provider update, and older OpenXR Plugin versions don't compile against this release. Update the OpenXR Plugin before you update Composition Layers. For the full compatibility table, refer to [Install Composition Layers](installation.md).

## Updated XR Interaction Toolkit version requirement

Composition Layer UI interaction now requires XR Interaction Toolkit 3.6.1 or newer. If your project uses an XR Interaction Toolkit version older than 3.6.1, `InteractableUIMirror` doesn't include its interaction implementation, and Composition Layer UI stops responding to input. XR Interaction Toolkit remains optional, and projects that don't use it compile as before.

To upgrade, [update XR Interaction Toolkit](xref:um-upm-ui-update) to 3.6.1 or newer in the Package Manager. The Interactable UI Mirror inspector also detects an XR Interaction Toolkit version older than 3.6.1 and offers to upgrade it.

## Deprecated direct calls to CompositionLayerExtension lifecycle methods

`CompositionLayerExtension.Awake`, `OnEnable`, `OnDisable` and `OnDestroy` are Unity callbacks that the package drives. In 7.0 they are marked obsolete and will become `protected` in a future release.

- Don't call these methods directly on an extension. Unity drives the extension lifecycle, and calling the methods by hand could register or unregister the extension twice.
- If you have a custom extension that overrides one of these methods, it keeps working. The compiler warns that the override targets an obsolete member. Add the same `[Obsolete]` attribute to your override to clear the warning and pass the notice on to anything that calls your subclass directly. No other change is needed until the methods become `protected`, at which point the override changes from `public` to `protected`.

## Removed ReportStateChange

The `ReportStateChange` callback on `CompositionLayerExtension` and `LayerData` is removed from the public API. Subclasses that assign or invoke it produce a compile error.

To upgrade, report property changes through `UpdateValue`. When your subclass modifies state in place, for example a collection or texture it references, call the new `protected ReportChange()` method instead.

## CompositionOutline does nothing

`CompositionLayer` now draws the Scene view outline of Quad and Cylinder layers from its own gizmo. Earlier versions added a hidden `CompositionOutline` component to every layer and saved it in the scene. In 7.0 the component is obsolete and does nothing. It remains so that scenes and prefabs saved by earlier versions keep loading, and it will be removed in a future release.

- When you open a scene, each layer on an active GameObject removes the `CompositionOutline` component and marks the scene as modified. Save the scene to keep the change.
- A layer on an inactive GameObject removes the component the first time you activate the GameObject in the Editor. Save the scene to keep the change.
- A layer in a prefab instance doesn't remove the component. To remove it from the prefab asset, open the prefab in prefab editing mode and save the prefab.
- If your code references `CompositionOutline`, it compiles with a warning. Remove the reference; there's no replacement, because the outline needs no component.

## Deprecated helper, editor, and validation types

The following types are obsolete in Composition Layers 7.0.0-pre.1 and will be removed in a future release:

- `CompositionLayerAnalyticsBuildProcessor`
- `CompositionLayerProjectValidation`, `CompositionLayerProjectValidationRuleSetup`, `CompositionLayerBuildValidationRule`
- `CompositionLayersRuntimeSettingsEditor`, `UIHandleEditor`, `UIFocusEditor`, `InteractiveUIMirrorEditor`, `InteractableUIMirrorResolutionEditor`, `MirrorViewRendererEditor`
- `ProjectionEyeRigUtil` (the Projection Eye Rig editor window)
- `GraphicsSettingsHelpers`

Nothing changes in the Editor or at runtime. If your code references one of these types directly, it compiles with a warning. Remove any references to these types. There's no replacement.

<a id="move-the-runtime-settings-out-of-resources"></a>
## Moved the runtime settings asset out of Resources

Composition Layers no longer loads its settings asset, `CompositionLayersRuntimeSettings.asset`, from a `Resources` folder. Earlier versions created the asset in `Assets/CompositionLayers/UserSettings/Resources/`. Composition Layers 7.0 keeps the asset in `Assets/XR/Settings/`, adds it to **Preloaded Assets** (**Edit** > **Project Settings** > **Player** > **Other Settings** > **Optimization**), and stores the references to the emulation shaders in it. To edit the settings, go to **Edit** > **Project Settings** > **XR Plug-in Management** > **Composition Layers**. For more information, refer to [Composition Layers settings (XR Plug-in Management) reference](xref:xr-layers-project-settings).

If the old asset turns on the splash screen or **Emulation in Standalone**, player builds fail until you migrate it, so players don't lose those settings. Otherwise, builds log a warning until you migrate it. Builds don't change your project, so you choose when the move happens. To upgrade your project:

1. Go to **Edit** > **Project Settings** > **XR Plug-in Management** > **Composition Layers**.
1. The page displays an error that `CompositionLayersRuntimeSettings.asset` needs to be migrated. Select **Migrate Settings Asset**. **Project Validation** displays the same error, and its **Fix** button does the same. Composition Layers moves the asset out of its `Resources` folder to `Assets/XR/Settings/`, references the emulation shaders, and adds the asset to **Preloaded Assets**. The asset keeps its GUID and its settings, and Composition Layers deletes the `Resources` folder if it's then empty.
1. Commit the moved asset and the **Player Settings** change to version control.

If the active build profile overrides **Player Settings**, it has its own **Preloaded Assets** list. Select **Set Up Settings Asset** on the **Composition Layers** page, or **Fix** in **Project Validation**, with that build profile active.

If a settings asset already exists outside a `Resources` folder, Composition Layers uses it and ignores the old asset, but the old asset is still included in builds because everything in a `Resources` folder is. Project Validation displays a warning that an old copy of `CompositionLayersRuntimeSettings.asset` is still in a `Resources` folder. **Fix** deletes the old asset and its **Preloaded Assets** entry. Before you select **Fix** or **Fix All**, make sure the asset in `Assets/XR/Settings/` has the settings you want. To find the old asset, search for `CompositionLayersRuntimeSettings` in the **Project** window. Select it to compare: its **Inspector** shows its settings, read-only, and names the asset in use. Copy any settings you want to keep to the new asset on the **Composition Layers** settings page.

<a id="runtime-settings-api"></a>
## Changed runtime settings base class and deprecated Instance

`CompositionLayersRuntimeSettings` now derives from `ScriptableObject` instead of the XR Core Utilities `ScriptableSettings<T>` class. Code that uses members inherited from `ScriptableSettings<T>` on this class no longer compiles.

`CompositionLayersRuntimeSettings.Instance` is obsolete in Composition Layers 7.0.0-pre.1 and will be removed in a future release. Code that uses it compiles with a warning. It returns the project's settings, or the default settings if the project has no settings asset. Unlike earlier versions, it never creates the asset.

To read the settings from code, add a serialized `CompositionLayersRuntimeSettings` field to your component, and assign `Assets/XR/Settings/CompositionLayersRuntimeSettings.asset` to it in the **Inspector**. The splash screen properties, such as `EnableSplashScreen` and `SplashDuration`, and `PassthroughSplashScreenEnabled` are unchanged.

## Deprecated ImageFilters members

All `ImageFilters` members, other than `ImageFilters.AlphaMode`, are now obsolete. The `RenderPipeline` enum, the parameter structs and the `Blit` methods keep working and will be removed in a future release.

`MirrorViewRenderer.AlphaMode` is obsolete. Use the `AlphaBlendMode` property, which reads and writes the same value. The field remains so code that passes it by `ref` or `out` keeps compiling, and it will be removed in a future release.

## Renamed public API

Renamed public API members to follow C# and Unity conventions. Previous names are now obsolete members that forward to the new equivalent. Unity's script updater rewrites references to the new names the next time the project is opened in the Unity Editor. The following table outlines the previous API name and its equivalent replacement.

| **Previous name** | **Replacement name** |
| :--- | :--- |
| `TexturesExtension.sourceTexture` | `SourceTexture` |
| `CompositionLayerManager.mainCameraCache` | `MainCameraCache` |

`CompositionLayerDataAttribute.SuggestedExtenstionTypes` is obsolete. Use the `SuggestedExtensionTypes` property, which reads and writes the same value and works as a named argument in `[CompositionLayerData(...)]`. The field remains so existing attribute usages compile, and it will be removed in a future release. The script updater can't rewrite it, because a field can't be rewritten to a property, so rename it yourself.

Subclasses of `LayerUIScale`: the protected fields `texturesExtension`, `canvasRect`, `canvasSizeX`, `canvasSizeY`, `canvasScaleX` and `canvasScaleY` are obsolete. Use the new protected properties `TexturesExtension`, `CanvasRect`, `CanvasSizeX`, `CanvasSizeY`, `CanvasScaleX` and `CanvasScaleY`, which read and write the same values. The fields remain so subclasses compile, and they will be removed in a future release. The script updater can't rewrite these either.

## EmulationLayerUniversalScriptableRendererFeature is obsolete

`EmulationLayerUniversalScriptableRendererFeature` does nothing in Composition Layers 7.0. In 2.6, code that referenced it compiled with a warning. In 7.0, code that references it produces a compile error.

To upgrade, delete the feature from your URP Renderer assets, and remove any code references to it.

## Deprecated High Definition Render Pipeline support

High Definition Render Pipeline (HDRP) support for in-Editor layer emulation and the Mirror View Renderer is deprecated, and will be removed in a future release. HDRP still works in this release, and the package logs a warning when HDRP is active. `EmulationLayerHighDefinitionUnderlayCustomPass`, `EmulationLayerHighDefinitionOverlayCustomPass`, `ImageFilters.RenderPipeline.HighDefinition` and `RenderPipelineUtility.IsHDRenderPipeline` are marked obsolete, and code that references them compiles with a warning.

To upgrade, use the Universal Render Pipeline (URP) for layer emulation and the Mirror View Renderer.

## Removed Built-in Render Pipeline and URP Compatibility Mode support

In-Editor layer emulation, Color Scale and Bias emulation, and the Mirror View Renderer no longer support the Built-in Render Pipeline or URP Compatibility Mode. These features require the URP 17 render graph system. `EmulationLayerUniversalScriptableRendererPass.Execute`, which was deprecated in 2.2, is removed.

To upgrade, switch your project to URP. URP always uses the render graph system in the Unity versions that Composition Layers 7.0 supports, so there's no setting to enable. If a scene saved the Emulated Color Scale Bias component on its main camera, the component now does nothing, and you can remove it.

<a id="emulation-without-provider"></a>
## Layer emulation without a provider plug-in

Composition layers now render through emulation until a provider plug-in starts to composite them. In earlier versions, layers didn't render in the following situations. They now render through emulation:

- An XR device where the provider plug-in doesn't composite layers, such as when its composition layer support is disabled.
- A Windows, macOS, or Linux player with an XR display running and no provider plug-in compositing layers, such as a Windows build that runs on a virtual reality headset.

If your project relied on layers not rendering in these situations, deactivate the layers' GameObjects instead. For more information, refer to [Keep layers out of builds without XR](xref:xr-layers-enable#keep-layers-out-of-builds-without-xr) and [Layer emulation at runtime](xref:xr-layers-runtime-settings).

<a id="emulation-in-standalone"></a>
## Deprecated Emulation in Standalone setting

The **Emulation in Standalone** runtime setting, `CompositionLayersRuntimeSettings.EmulationInStandalone`, is deprecated and will be removed in a future release.

The setting now applies only to Windows, macOS, and Linux players with no XR display running, such as the desktop version of a project that also ships on an XR device. In these players, the setting still decides whether layers render through emulation. It's disabled by default, so these players don't render composition layers unless you enable it. When an XR display is running, the setting has no effect.

After the setting is removed, these players render composition layers through emulation. To keep composition layers out of these players now and after the removal, deactivate the layers' GameObjects. For more information, refer to [Keep layers out of builds without XR](xref:xr-layers-enable#keep-layers-out-of-builds-without-xr).

If your code reads `EmulationInStandalone`, it compiles with a warning. Remove the reference.

<a id="emulation-in-play-mode"></a>
## Removed Emulation In Play Mode preference

The **Emulation In Play Mode** preference is removed. In Play mode, composition layers render through emulation until a provider plug-in starts to composite them.

If you disabled the preference to hide composition layers in Play mode, deactivate the layers' GameObjects instead. For more information, refer to [Enable and disable layers](xref:xr-layers-enable).

<a id="composition-layers-preferences"></a>
## Removed the Composition Layers page of the Preferences window

Composition Layers 7.0 removes the **Composition Layers** page from the Unity **Preferences** window, because the package no longer needs its settings. The **Emulation In Play Mode** setting is covered in [Removed Emulation In Play Mode preference](#emulation-in-play-mode). The other two settings are also removed:

- The **Display Angles As** setting: the Inspector always shows angles in degrees, like the rest of the Unity Editor. Scripts can set angles in degrees with the `InDegrees` properties, such as `CylinderLayerData.CentralAngleInDegrees`.
- The **Emulation in Scene** setting: the **Scene** view always shows composition layers through emulation. To hide a layer in the **Scene** view, turn off its visibility in the **Hierarchy** window. For more information, refer to [Hide layers in the Scene view](xref:xr-layers-enable#hide-in-scene-view).

All three settings were stored in a project asset, `Assets/CompositionLayers/UserSettings/CompositionLayersPreferences.asset` by default, which your project might have committed to version control. The package no longer uses this asset. To delete it:

1. Open **Project Settings** > **XR Plug-in Management** > **Project Validation**.
1. Find the warning about the leftover `CompositionLayersPreferences` asset and select **Fix**.
1. Commit the deletion to version control.

<a id="shader-inclusion"></a>
## Emulation shaders included in every build

Composition Layers now includes the shaders that layer emulation and the mirror view use in every build. The Composition Layers settings asset, `Assets/XR/Settings/CompositionLayersRuntimeSettings.asset`, references the shaders and is in **Preloaded Assets** in **Player Settings**, or in the active build profile if it overrides **Player Settings**. Until the asset is set up, player builds log a warning and emulated layers don't render in players. If the asset turns on the splash screen or **Emulation in Standalone**, builds fail instead, since players would ignore it. If your settings asset is in a `Resources` folder, refer to [Moved the runtime settings asset out of Resources](#move-the-runtime-settings-out-of-resources). To upgrade your project:

1. Go to **Edit** > **Project Settings** > **XR Plug-in Management** > **Composition Layers**, and select the button the page displays. Alternatively, select **Fix** on the `CompositionLayersRuntimeSettings.asset` error in **Project Validation**.
1. Commit the settings asset and the settings change to version control.
1. Remove any Composition Layers shaders that you added to the **Always Included Shaders** list by hand, unless your project uses them for its own purposes.

For more information, refer to [Shader inclusion](xref:xr-layers-graphics-settings#shader-inclusion).

<a id="graphics-settings-loader"></a>
## Deprecated GraphicsSettingsLoader

`GraphicsSettingsLoader` is obsolete and does nothing, because you no longer need to add the emulation shaders to the **Always Included Shaders** list. If your code calls `GraphicsSettingsLoader`, it compiles with a warning. Remove the calls. There's no replacement.

## Renamed External Surface sample assembly

The External Surface sample's assembly is now `AndroidSurfaceSample`, and the sample's display name is corrected to match. The Package Manager imports the sample into a folder named after its display name, so a project that imported the sample from an earlier version has it under the old, misspelled name. Delete that folder from `Assets/Samples/XR Composition Layers` before you import the sample again. If both copies are present, every file in the sample reports a `.meta` file GUID conflict.

## Additional resources

- [Changelog](xref:xr-layers-changelog)
- [What's new in version 7.0](xref:xr-layers-whats-new)
