# uGUI How-To Workflows for Prefabs

Use this file when implementing specific tasks from Unity's uGUI How Tos page:
<https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/UIHowTos.html>

## 1) Design UI for Multiple Resolutions

Source: <https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/HOWTO-UIMultiResolution.html>

Apply when:
- A prefab must remain usable across different aspect ratios/resolutions.
- A HUD/menu needs stable positioning without manual per-resolution prefab variants.

Workflow:
1. Define anchors first, based on intended attachment behavior.
2. Add/verify `Canvas Scaler` on root canvas:
- Use `Scale With Screen Size`.
- Set a meaningful `Reference Resolution`.
- Set `Screen Match Mode = Match Width Or Height` and tune `Match`.
3. Validate in Game view with multiple aspect ratios and low/high resolutions.

Watch-outs:
- Hardcoded offsets without anchor strategy cause drift across resolutions.
- Ignoring `Match` value often leads to unexpected distortion between portrait/landscape.

## 2) Make UI Elements Fit Their Content

Source: <https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/HOWTO-UIFitContentSize.html>

Apply when:
- Buttons/chips/cards must resize to text or dynamic child content.
- Prefab must avoid fixed dimensions that break on localization.

Workflow:
1. Use `Layout Group` on parent for child placement.
2. Use `Layout Element` on children to set min/preferred/flexible constraints.
3. Use `Content Size Fitter` where self-sizing is required.
4. Disable `Child Force Expand` if only selected children should stretch.

Watch-outs:
- Do not place `Content Size Fitter` on children controlled by parent layout group.
- Use pivots intentionally because fitter-driven resize happens around pivot.

## 3) Create UI in World Space

Source: <https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/HOWTO-UIWorldSpace.html>

Apply when:
- UI should appear as in-world panels, kiosks, labels, terminals, or floating widgets.

Workflow:
1. Set canvas to `World Space`.
2. Scale and size the canvas for in-world readability.
3. Position and orient panel relative to camera/player interaction points.
4. Validate draw order, readability distance, and interaction raycasting.

Watch-outs:
- Unscaled world-space canvas frequently appears too large or too small.
- Missing event/camera setup can break interaction unexpectedly.

## 4) Create UI Elements from Scripting

Source: <https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/HOWTO-UICreateFromScripting.html>

Apply when:
- You need dynamic lists, inventories, notifications, or generated menu options.

Workflow:
1. Build one prefab for the repeated element.
2. Instantiate at runtime.
3. Parent with `worldPositionStays = false`:
- `instance.transform.SetParent(parentTransform, false);`
4. Bind data and interactions after instantiate.
5. Let parent layout group drive placement unless manual positioning is required.

Watch-outs:
- Parenting with `worldPositionStays = true` often causes unexpected scaling/position.
- Manual rect manipulation can fight layout groups.

## 5) Create Screen Transitions

Source: <https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/HOWTO-UIScreenTransition.html>

Apply when:
- UI flows need state changes between screens, popups, or panel stacks.

Workflow:
1. Define transition states and ownership of activation/deactivation.
2. Separate static container prefab from animated transition logic.
3. Validate input lock/unlock timing while transitions play.

## 6) Create Custom UI Effects with Shader Graph

Source: <https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/HOWTO-UIScreenSpaceShader.html>

Apply when:
- A UI prefab needs custom visual treatments (glow, distortion, gradients, masks).

Workflow:
1. Keep base prefab structure/layout independent from shader complexity.
2. Apply material/shader variants through configurable references.
3. Test readability and performance on target hardware.

## Cross-Cutting Reminder

Implement prefab creation and edits through Unity MCP tooling first. Use raw YAML only as a last resort when Unity tooling cannot perform the operation.
