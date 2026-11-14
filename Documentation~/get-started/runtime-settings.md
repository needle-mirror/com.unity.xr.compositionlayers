---
uid: xr-layers-runtime-settings
---
# Layer emulation at runtime

Understand when composition layers render through emulation in Play mode and in built players.

XR Composition Layers renders composition layers through emulation until a provider plug-in starts to composite them, so the layers still appear. Built Windows, macOS, and Linux players with no XR display running are the exception, and don't render composition layers by default.

In Edit mode, the Unity Editor always previews composition layers through emulation, in the **Game** view and the **Scene** view. To hide a layer in the **Scene** view, refer to [Hide layers in the Scene view](xref:xr-layers-enable#hide-in-scene-view).

## When layers render through emulation

In Play mode and in built players, a composition layer renders through emulation until a provider plug-in starts to composite layers. Layers render through emulation in situations such as the following:

* Play mode with XR disabled, or with XR Simulation.
* An XR device where the provider plug-in doesn't composite layers, such as when its composition layer support is disabled.
* A Windows, macOS, or Linux player with an XR display running and no provider plug-in compositing layers, such as a Windows build that runs on a virtual reality (VR) headset.
* A player for another platform, such as Android, with no XR display running.

In Play mode with a headset connected, layers don't render through emulation if a provider plug-in composites them.

Emulation approximates how layers look when a provider plug-in composites them on a device, so emulated layers can look different on a device.

<a id="emulation-in-standalone"></a>
## Windows, macOS, and Linux players with no XR display

By default, a Windows, macOS, or Linux player with no XR display running, such as the desktop version of a project that also ships on an XR device, doesn't render composition layers. Players for other platforms with no XR display running render composition layers through emulation. To keep composition layers out of a build on any platform, refer to [Keep layers out of builds without XR](xref:xr-layers-enable#keep-layers-out-of-builds-without-xr).

## Additional resources

* [Enable and disable layers](xref:xr-layers-enable)
* [Configure graphics settings](xref:xr-layers-graphics-settings)
* [Upgrade to Composition Layers 7.0](xref:xr-layers-upgrade-guide-7.0)
