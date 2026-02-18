# Auto Layout + Web Mental Model

Primary sources:
- <https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/UIAutoLayout.html>
- <https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-LayoutElement.html>
- <https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/script-ContentSizeFitter.html>
- <https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/HOWTO-UIFitContentSize.html>

## Core Auto Layout Concepts

Unity auto layout works by combining:
1. Layout controllers on parents (`Horizontal/Vertical/Grid Layout Group`).
2. Layout element data on children (`min`, `preferred`, `flexible` sizes).
3. Self-sizing controllers (`Content Size Fitter`) on objects that should resize around their own content.

## Layout Element Deep Dive

`Layout Element` lets a child override normal layout inputs.

Key fields:
- `Min Width/Height`: never shrink below this size.
- `Preferred Width/Height`: desired size when enough space exists.
- `Flexible Width/Height`: share of remaining space after min/preferred allocation.
- `Layout Priority`: conflict resolver when multiple components define layout values.

Conflict resolution:
1. Higher `Layout Priority` component wins.
2. If priorities are equal, Unity uses the largest value for each property.

Practical rule:
- Set `Flexible` only on children that should absorb extra space.
- Keep `Flexible = 0` on children that should remain content-driven/fixed.

## Content Size Fitter Guidance

`Content Size Fitter` resizes a rect based on minimum or preferred values from local layout content.

Important behaviors:
- Resize happens around pivot.
- Pivot choice controls direction of growth/shrink.
- Combining fitter with parent-controlled child layout can cause conflicts.

Hard rule from Unity guidance:
- Do not place `Content Size Fitter` on children whose sizes are driven by a layout group parent.

## HTML/CSS Comparison

Use this as a mental mapping, not an exact one-to-one implementation model.

- `Horizontal Layout Group` ~= `display: flex; flex-direction: row;`
- `Vertical Layout Group` ~= `display: flex; flex-direction: column;`
- `Grid Layout Group` ~= grid-like placement with fixed/controlled cell metrics.
- `Layout Element Min` ~= `min-width` / `min-height`.
- `Layout Element Preferred` ~= intrinsic or ideal size target.
- `Layout Element Flexible` ~= `flex-grow` weight.
- `Child Force Expand` ~= forcing children toward uniform expansion (similar to applying growth broadly).
- `Content Size Fitter (Preferred)` ~= `fit-content` style sizing around content.
- Anchors ~= responsive attachment points relative to parent bounds.
- `Canvas Scaler` ~= root scaling policy for viewport/device diversity.

Key caveat:
- CSS layout engines and Unity layout rebuild timing are different systems. Use the mapping to reason, then verify behavior in Play Mode and Game view resolutions.

## Auto-Resize Patterns

## Pattern A: One Child Expands, Others Stay Fixed

1. Parent: `Horizontal Layout Group`.
2. Parent: disable `Child Force Expand`.
3. Fixed children: optional preferred widths.
4. Expanding child: `Layout Element Flexible Width = 1`.

Result:
- Fixed children hold size.
- Expanding child consumes leftover width.

## Pattern B: Weighted Expansion

1. Multiple children get non-zero `Flexible Width`.
2. Use relative values (for example 1, 2, 1) as proportions.

Result:
- Leftover space distributes by weight.

## Pattern C: Content-Driven Container

1. Container: `Content Size Fitter` preferred fit.
2. Container children: provide preferred sizes through text/image/layout data.
3. Optional: layout group on container for arrangement.

Result:
- Container wraps content, useful for chips/tooltips/badges.

## Debug Checklist

1. Confirm only one system controls each RectTransform size.
2. Confirm anchors and pivots match growth direction.
3. Confirm `Child Force Expand` matches intent.
4. Confirm `Flexible` values are set only where growth is desired.
5. Confirm no fitter-on-layout-child conflicts.
6. Confirm behavior in multiple resolutions.
