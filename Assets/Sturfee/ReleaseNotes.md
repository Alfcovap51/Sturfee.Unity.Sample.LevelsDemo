# Sturfee StreetAR SDK
## Release Notes


### Version 1.0.0

**Bug Fixes**

- Debug building geometry `ON` by default (Tier 3); Will now be `OFF` by default
- `SturfeeXRLight` prefab's shadows are turned off
- Issue with light estimation from `DebugLightingProvider` (orientation and intensity)

**API Changes** 

_BREAKING Changes_
- Event `OnAlignmentComplete` changed to `OnLocalizationComplete`
- Event `OnAlignmentLoading` changed to `OnLocalizationLoading`
- Event `OnHitscanComplete` changed to `OnDetectSurfacePointComplete`
- Event `OnHitScanLoading` changed to `OnDetectSurfacePointLoading`
- Event `OnHitscanFailed` changed to `OnDetectSurfacePointFailed`
- Event `OnAlignmentCheckComplete` changed to `OnCoverageCheckComplete`
- Added `XRSessionManager.PerformLocalization()` changed to replace `XRSesssionManager.PerformAlignment()`
- Added `XRSessionManager.CheckCoverage()` changed to replace `XRSesssionManager.CheckAlignment()`
- Added `XRSessionManager.DetectSurfaceAtPoint()` changed to replace `XRSesssionManager.PerformHitscan()`

**General Changes**
- Multiframe scan added for more accurate Localization
- Moved `UnityARkitPlugin` from `Aseets/Sturfee/Plugins/External/` to `Assets/` folder
- Added Template code for new projects in `Assets/Sturfee/Templates` folder
- GPS Provider uses Native GPS on device instead of Unity's Input.location.lastData
- Logs added to get more info on status of session initialization
- Scenes changed to Examples with each scene having a Readme
- ArKit and ArCore updated to latest
- Tracked Pose Driver removed from ArCore Provider Set
- World Anchor updated to save edits in PlayMode. Gizmo added for World Anchor
- Exceptions added for Invalid requests

