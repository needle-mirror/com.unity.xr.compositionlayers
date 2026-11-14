---
uid: xr-layers-project-settings
---
# Composition Layers settings (XR Plug-in Management) reference

Explore the settings to configure the composition layer splash screen and layer emulation in Windows, macOS, and Linux players.

To open these settings, go to **Edit** > **Project Settings** > **XR Plug-in Management** > **Composition Layers**.

XR Composition Layers stores these settings in `Assets/XR/Settings/CompositionLayersRuntimeSettings.asset`, which must be in **Preloaded Assets** (**Edit** > **Project Settings** > **Player** > **Other Settings** > **Optimization**). Until this asset is set up, player builds log a warning and emulated layers don't render in players. If the asset turns on the splash screen or **Emulation in Standalone**, builds fail instead, since players would ignore it. Builds also fail if more than one settings asset is in **Preloaded Assets** and one of them turns these on, since a player might use either. The page displays an error that says what the asset needs and a button that sets it up:

| **Project state** | **Button** | **What the button does** |
| :---------------- | :--------- | :----------------------- |
| No settings asset. The default settings apply. | **Create Settings Asset** | Creates the asset in `Assets/XR/Settings/` and adds it to **Preloaded Assets**. |
| The asset from an earlier version of XR Composition Layers is in a `Resources` folder. The page displays its settings for you to edit, and your changes are kept when you migrate it. | **Migrate Settings Asset** | Moves the asset to `Assets/XR/Settings/`, keeping its settings, and adds it to **Preloaded Assets**. For more information, refer to [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0#move-the-runtime-settings-out-of-resources). |
| The asset isn't in **Preloaded Assets**, or doesn't reference the package shaders. | **Set Up Settings Asset** | References the package shaders and adds the asset to **Preloaded Assets**. |

Each button also references the package shaders, which the page lists, read-only, under **Included Shaders**. **Project Validation** displays the same errors, and its **Fix** button does the same as the page's button. Commit the asset and the **Player Settings** change to version control.

## Composition Layers settings

The **Composition Layers** page contains the following sections.

### Splash Screen

The **Splash Screen** section contains the following settings. For how to set up the splash screen, refer to [Composition layer splash screen](xref:xr-layers-splash-screen).

| **Setting** | **Default** | **Description** |
| :---------- | :---------- | :-------------- |
| **Enable Splash Screen** | Disabled | Displays a splash screen on a composition layer when the application starts. When you enable this setting, XR Composition Layers adds the `CompositionSplash` scene to the start of the scene list in **Build Settings**. When you disable it, XR Composition Layers removes the scene from the list. The other splash screen settings appear only when this setting is enabled. |
| **Splash Image** | None | The texture to display on the splash screen. If you leave this empty, the splash screen displays the Made with Unity logo. |
| **Background Type** | **Solid Color** | The background behind the splash screen. **Solid Color** displays **Background Color**. **Passthrough** displays the passthrough camera image, if the device supports it. When you select **Passthrough**, configure your [HDR settings](xref:xr-layers-transparency#hdr-transparency). |
| **Background Color** | `#231F20` | The color behind the splash screen texture. Appears only when **Background Type** is **Solid Color**. |
| **Splash Duration** | `3` | The time in seconds to display the splash screen, not including the fade in and fade out. |
| **Fade In Duration** | `2` | The time in seconds to fade in the splash screen. |
| **Fade Out Duration** | `1` | The time in seconds to fade out the splash screen. |
| **Follow Speed** | `2` | How fast the splash screen moves to follow the camera. |
| **Follow Distance** | `2` | The distance in meters from the camera to the splash screen. |
| **Lock To Horizon** | Enabled | Keeps the splash screen at eye level and upright when the camera tilts up or down. When disabled, the splash screen follows the tilt of the camera. |
| **Layer Type** | **Quad** | The type of composition layer that displays the splash screen, **Quad** or **Cylinder**. The page then displays the settings of the selected layer type, such as the size of a quad or the radius of a cylinder. |

When the splash screen is enabled, the section also displays any problems with the scene list in **Build Settings** or the splash screen in **Player Settings**.

> [!NOTE]
> XR Composition Layers preloads the settings asset in builds, so the **Splash Image** texture stays in memory while the application runs. If you disable the splash screen, also clear **Splash Image** to keep the texture out of your build.

### Emulation

The **Emulation** section contains the following settings:

| **Setting** | **Default** | **Description** |
| :---------- | :---------- | :-------------- |
| **Emulation In Standalone (Deprecated)** | Disabled | Renders composition layers through emulation in Windows, macOS, and Linux players with no XR display running. This setting is deprecated and will be removed in a future release. For more information, refer to [Layer emulation at runtime](xref:xr-layers-runtime-settings#emulation-in-standalone). |

## Additional resources

* [Composition layer splash screen](xref:xr-layers-splash-screen)
* [Layer emulation at runtime](xref:xr-layers-runtime-settings)
* [Configure graphics settings](xref:xr-layers-graphics-settings)
* [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0)
