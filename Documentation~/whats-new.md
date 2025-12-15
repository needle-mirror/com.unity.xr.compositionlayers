---
uid: xr-layers-whats-new
---
# What's new in version 2.2

This release includes the following significant changes:

## New Features

## Changes

## Deprecations

### URP compatibility mode is removed in Unity 6.4

- In Unity 6000.4 and newer Editor versions, all methods that depend on URP Compatibility Mode have been changed from `Obsolete(false)` to `Obsolete(true)`. URP Compatibility Mode is removed in Unity 6000.4, so these APIs are no longer supported in Unity 6000.4 or newer. The following methods are affected:
  - `EmulationLayerUniversalScriptableRendererPass.Execute`

For a full list of changes in this version including backwards-compatible bugfixes, refer to the package [changelog](xref:xr-layers-changelog).
