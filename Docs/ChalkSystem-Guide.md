# Chalk System Guide

## Pipeline Detection
- Automatic detection happens in `Tools/Chalk/Run Setup`.
- Built-in when `GraphicsSettings.currentRenderPipeline == null`.
- SRP when a render pipeline asset is assigned.

## One-Click Setup
1. Open Unity.
2. Run `Tools/Chalk/Run Setup`.

This performs:
- Moves/copies external chalk assets into:
  - `Assets/Chalk/OnlyGFX`
  - `Assets/Chalk/Texturelabs/InkPaint_319`
- Chooses a main stroke texture and grain texture.
- Applies import settings.
- Creates sorting layer `ChalkOverlay`.
- Creates materials:
  - `Assets/Chalk/Materials/Mat_ChalkOverlay.mat`
  - `Assets/Chalk/Materials/Mat_ChalkTMP.mat`

## Demo Scene
1. Run `Tools/Chalk/Create Demo Scene`.
2. Open `Assets/Scenes/ChalkDemo.unity`.

The scene includes:
- Green felt background.
- 4 chalk zones:
  - Draw Pile
  - Discard
  - Player Area
  - Opponent Area
- Chalk TMP labels for each zone.

## Runtime Components
- `ChalkLine`:
  - Uses `LineRenderer` with tiling stroke texture.
  - Exposes `SetPoints(Vector3[] points, bool loop)`.
- `ChalkZone`:
  - Generates `RectZone`, `CircleZone`, and `PolylineZone`.
- `ChalkText`:
  - Applies chalk material to `TMP_Text`.
  - Grain modulates alpha for erased chalk look.

## Shader Assets
- `Assets/Shaders/Chalk/ChalkOverlay.shader`
- `Assets/Shaders/Chalk/ChalkTMP_SDFOverlay.shader`

Key exposed properties:
- `_Opacity`
- `_GrainTex`
- `_GrainScale`
- `_GrainStrength`
- `_ChalkTint`
