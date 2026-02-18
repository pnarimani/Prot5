---
name: unity-ui-prefab-creator
description: Comprehensive workflow for creating, updating, and debugging Unity uGUI prefabs with responsive auto layout, resolution scaling, and runtime instantiation patterns. Use when Codex needs to build or refactor Unity UI prefab assets, configure Layout Element and layout groups for auto-resizing behavior, map Unity layout behavior to HTML/CSS concepts, or follow Unity uGUI How Tos while avoiding raw YAML edits.
---

# Unity UI Prefab Creator

## Overview

Create and refine Unity uGUI prefabs with predictable layout behavior across screen sizes and content lengths. Use Unity MCP tooling first, apply official uGUI 2.0 layout patterns, and treat raw YAML edits as a last-resort fallback only.

## Non-Negotiable Rules

1. Use Unity MCP tools to create/edit UI GameObjects, components, prefab assets, and prefab instances.
2. Do not edit `.prefab`, `.unity`, or `.meta` YAML directly when MCP tooling can perform the change.
3. Use raw YAML edits only as a last resort when MCP tooling is unavailable or broken, and then keep edits minimal.
4. Explicitly report any last-resort YAML edits and why MCP tooling could not be used.
5. Preserve project architecture and conventions while editing UI prefabs.

## Progressive Loading

1. Read `references/ugui-doc-map.md` to identify relevant official pages.
2. Read `references/howto-prefab-workflows.md` when the task matches a uGUI How To scenario.
3. Read `references/auto-layout-web-mapping.md` when layout auto-resize behavior or web mental-model mapping is required.

## Workflow

1. Confirm scope.
- Identify target prefab(s), target canvas mode (screen space or world space), and whether runtime instantiation is needed.
- Identify which regions are fixed-size and which must grow/shrink with content.

2. Build prefab structure with MCP.
- Open/create prefab through Unity tooling.
- Build hierarchy from containers to leaves (`Canvas`/panel -> groups -> controls -> text/images).
- Set anchors and pivots early before final spacing and offsets.

3. Apply layout controllers deliberately.
- Use `Horizontal Layout Group`, `Vertical Layout Group`, or `Grid Layout Group` on parent containers that should place children.
- Use `Layout Element` on children to override min/preferred/flexible behavior.
- Use `Content Size Fitter` when a GameObject should resize itself from its own layout data.
- Avoid controller conflicts: do not use `Content Size Fitter` on a child rect driven by a parent layout group.

4. Make it responsive.
- Use anchors to adapt to aspect-ratio shifts.
- Use `Canvas Scaler` (`Scale With Screen Size`) for density/resolution scaling.
- Tune `Match Width or Height` for balanced portrait/landscape behavior.

5. Implement dynamic prefab usage when needed.
- Instantiate prefab instances.
- Parent with `worldPositionStays = false` (`SetParent(parent, false)` or equivalent).
- Bind content/events after instantiate and let layout groups position children when applicable.

6. Validate.
- Test multiple Game view resolutions and aspect ratios.
- Validate text growth, overflow, truncation, and localization edge cases.
- Verify sibling order and interactive navigation behavior.

## Auto Layout Rules

1. Use `Layout Element` constraints intentionally.
- `Min Width/Height`: hard floor.
- `Preferred Width/Height`: target size once enough space exists.
- `Flexible Width/Height`: proportional share of remaining space.

2. Disable blanket expansion when selective growth is required.
- Disable `Child Force Expand` on layout groups.
- Add `Layout Element` flexible values only to children that should stretch.

3. Use layout priority to resolve component conflicts.
- If multiple components provide layout values on one GameObject, higher `Layout Priority` wins.
- If priorities are equal, Unity uses the highest value per property.

4. Control resize direction with pivots.
- `Content Size Fitter` resizes around the pivot.
- For top-left anchored widgets that should grow right/down, set pivot accordingly.

5. Avoid undefined ownership.
- Do not add `Content Size Fitter` to children managed by a parent layout group.
- Let one system own each RectTransform size at a time.

## HTML/CSS Mental Model Mapping

1. Treat Unity layout groups like high-level flex/grid containers.
2. Treat `Layout Element` values like CSS constraints:
- `Min` ~= `min-width` / `min-height`.
- `Preferred` ~= intrinsic/ideal size target.
- `Flexible` ~= `flex-grow` weighting across siblings.
3. Treat `Content Size Fitter` preferred sizing as similar to `fit-content` behavior.
4. Treat anchors as responsive attachment points relative to parent rect bounds.
5. Treat `Canvas Scaler` as root-level scale policy for screen-size adaptation.

Read `references/auto-layout-web-mapping.md` for detailed mapping and caveats.

## Prefab Recipes

### Auto-size text chip or badge
1. Root object: add `Horizontal Layout Group` and configure padding/spacing.
2. Root object: add `Content Size Fitter` (`Horizontal Fit = Preferred Size`, `Vertical Fit = Preferred Size`).
3. Child text: configure wrapping and overflow intentionally.
4. Optional: add child `Layout Element` min/preferred constraints for strict bounds.

### Mixed fixed + flexible horizontal row
1. Parent row: `Horizontal Layout Group` with `Child Force Expand` disabled.
2. Fixed children: set preferred widths (often with `Layout Element`).
3. Flexible child: set `Layout Element Flexible Width = 1` (or weighted flexible values across multiple fillers).

### Runtime-instantiated list item
1. Author one list-item prefab with full visuals and interactions.
2. Instantiate per data entry.
3. Parent using `SetParent(container, false)` or instantiate with parent and `false`.
4. Let parent layout group position children instead of setting manual anchored positions unless needed.

## How-To Coverage

Use `references/howto-prefab-workflows.md` for workflows from Unity's `UIHowTos` index:
- Designing UI for Multiple Resolutions
- Making UI elements fit the size of their content
- Creating a World Space UI
- Creating UI elements from scripting
- Creating Screen Transitions
- Creating Custom UI Effects With Shader Graph

## Output Expectations

Always report:
1. Prefab assets created/modified.
2. Components added/removed and key property values set.
3. Auto-layout/responsiveness strategy used.
4. Known risks (for example localization overflow or navigation edge cases).
5. Whether YAML fallback was used and why.
