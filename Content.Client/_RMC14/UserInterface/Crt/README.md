# RMC14 CRT UI controls

This directory contains reusable client-side controls for building CM-SS13-inspired CRT interfaces in RMC14. The
library provides local palettes, semantic colors, panels, buttons, separators, icons, and optional display effects.
It does not modify the global SS14 stylesheet and contains no game-specific text or behavior.

Use these controls when several interfaces should share the same visual language without copying styles or depending
on another console's UI classes.

## Quick start

Add the CRT namespace to the window and wrap its contents in an `RMCCrtThemeScope`:

```xml
<DefaultWindow
    xmlns="https://spacestation14.io"
    xmlns:crt="clr-namespace:Content.Client._RMC14.UserInterface.Crt"
    Title="{Loc example-console-title}"
    MinSize="500 320">
    <crt:RMCCrtThemeScope Palette="Blue" Effects="HorizontalScanlines">
        <BoxContainer Orientation="Vertical" Margin="8" SeparationOverride="6">
            <crt:RMCCrtLabel Text="{Loc example-console-heading}"
                             Heading="True"
                             HorizontalAlignment="Center" />
            <crt:RMCCrtSeparator Thickness="2" />
            <crt:RMCCrtActionButton Name="ActionButton" Access="Public"
                                    Text="{Loc example-console-action}"
                                    IconState="warning"
                                    Variant="Filled"
                                    MinHeight="36" />
        </BoxContainer>
    </crt:RMCCrtThemeScope>
</DefaultWindow>
```

All user-facing text must remain in Fluent localization files. CRT controls only handle presentation.

## Controls

| Control | Purpose | Important properties |
| --- | --- | --- |
| `RMCCrtThemeScope` | Provides an isolated palette, stylesheet, background, border, and root display effects. | `Palette`, `Effects`, `BackgroundOpacity`, `BorderThickness` |
| `RMCCrtPanel` | Creates a themed surface or nested status area. | `Variant`, `Effects`, `BackgroundOpacity`, `BorderThickness` |
| `RMCCrtActionButton` | Displays a themed interactive action with an optional RSI icon. | `Variant`, `Tone`, `Selected`, `ContentAlignment`, `IconState`, `IconRsiPath` |
| `RMCCrtLabel` | Displays a heading, normal text, or semantic status. | `Heading`, `Tone` |
| `RMCCrtSeparator` | Draws a palette-colored horizontal or vertical line. | `Orientation`, `Thickness` |

Normal Robust properties such as `Disabled`, `ToggleMode`, `MinSize`, `HorizontalExpand`, `Margin`, and alignment
properties continue to work.

## Palettes and local theming

Available presets are `Blue`, `Brown`, `Green`, `Purple`, `Red`, `Upp`, `White`, and `Yellow`.

```xml
<crt:RMCCrtThemeScope Palette="Green" Effects="HorizontalScanlines">
    <!-- Every CRT control below this scope inherits the green palette. -->
</crt:RMCCrtThemeScope>
```

Theme scopes are local and may be nested. A dynamically added CRT control receives the palette of the nearest scope
when it enters the UI tree. Changing a scope's preset or custom colors at runtime reapplies the theme to its existing
CRT descendants; nested scopes retain their own palettes.

Use `Custom` when a menu needs its own color identity:

```xml
<crt:RMCCrtThemeScope Palette="Custom"
                      CustomForeground="#E8F4FF"
                      CustomBackground="#07131D"
                      CustomBorder="#4E90BD"
                      CustomFill="#2E6D98"
                      CustomFillForeground="#E8F4FF"
                      CustomGood="#39C66D"
                      CustomWarning="#E2B93B"
                      CustomDanger="#E05252"
                      CustomMuted="#688393"
                      CustomDisabledBackground="#293944"
                      CustomDisabledForeground="#7993A2">
    <!-- Menu contents. -->
</crt:RMCCrtThemeScope>
```

A custom palette should define all semantic colors. This keeps disabled, warning, danger, hover, and pressed states
readable instead of recoloring only the foreground.

## Panels

`RMCCrtPanel.Variant` accepts:

- `Surface` — the normal framed panel;
- `Inset` — a darker nested status area;
- `Transparent` — no panel background;
- `Warning` — a warning-colored panel.

```xml
<crt:RMCCrtPanel Variant="Inset">
    <crt:RMCCrtLabel Name="StatusLabel" Access="Public"
                     Text="{Loc example-console-status-ready}"
                     Tone="Good"
                     Margin="8" />
</crt:RMCCrtPanel>
```

For SS13-style warning stripes, enable the effect only on the warning panel:

```xml
<crt:RMCCrtPanel Variant="Warning"
                 Effects="DiagonalStripes"
                 StripeWidth="18">
    <crt:RMCCrtLabel Text="{Loc example-console-warning}"
                     HorizontalAlignment="Center"
                     Margin="8" />
</crt:RMCCrtPanel>
```

## Buttons

`RMCCrtActionButton.Variant` accepts:

- `Outline` — a secondary action on the CRT background;
- `Filled` — a primary action;
- `Navigation` — a section or page selector, normally combined with `Selected`;
- `Danger` — a destructive or emergency action.

Use `Tone` for semantic color without changing the role of the button. Available tones are `Default`, `Good`,
`Muted`, `Warning`, and `Danger`.

```xml
<crt:RMCCrtActionButton Name="NavigationButton" Access="Public"
                        Text="{Loc example-console-navigation}"
                        IconState="id_card"
                        Variant="Navigation"
                        ToggleMode="True"
                        ContentAlignment="Left" />
```

Update state through the public properties instead of directly tinting child controls:

```csharp
NavigationButton.Selected = true;
ActionButton.Disabled = !canPerformAction;
StatusLabel.Tone = RMCCrtTone.Good;
```

The button calculates its desired size from its icon, localized text, padding, and minimum size. Let the surrounding
containers participate in normal Robust layout so longer localized text can grow the button and its column.

## Icons

Buttons use `/Textures/_RMC14/Interface/CRT/crt_icons.rsi` by default. The bundled state names are:

`ban`, `bullhorn`, `cog`, `door_open`, `heartbeat`, `home`, `id_card`, `map`, `medal`, `paper_plane`, `users`, and
`warning`.

In C#, prefer the matching constants from `RMCCrtIcons`. To use a different RSI, set both `IconRsiPath` and
`IconState`. Code may also assign a texture through `IconTexture`. Setting `IconState` to `null` clears the icon.

```xml
<crt:RMCCrtActionButton Text="{Loc example-console-custom-icon}"
                        IconRsiPath="/Textures/_RMC14/Interface/example_icons.rsi"
                        IconState="terminal" />
```

Do not use emoji or Unicode symbols as icon replacements: their appearance and metrics vary between fonts and they do
not scale consistently with RSI assets.

## Display effects

`Effects` is a flags property. Multiple effects can be combined with commas:

```xml
Effects="HorizontalScanlines, DiagonalStripes"
```

Available effects:

- `HorizontalScanlines` — the normal CRT scanline layer and the recommended default;
- `RgbSubpixels` — an explicit RGB mask; disabled by default because it can reduce readability;
- `DiagonalStripes` — warning stripes, usually applied to a small warning panel;
- `None` — no effect layer.

The root scope exposes `ScanlineOpacity`, `ScanlineSpacing`, `ScanlineThickness`, `RgbOpacity`, `RgbWidth`, and
`StripeWidth`. Panels expose the same effect geometry options relevant to their local layer. Effect geometry is cached
and regenerated only when size, UI scale, enabled effects, or geometry settings change.

Keep effect opacity restrained. Semantic text colors and button states must remain readable at UI scaling values of
100%, 125%, and 150%.

## Responsive sizing

Robust UI already propagates `DesiredSize` from labels and icons through buttons, panels, containers, and the window.
For localized or dynamic CRT interfaces:

- set a sensible `MinSize` for the usable baseline;
- use `HorizontalExpand` and `VerticalExpand` where columns should share extra room;
- use `MinWidth` only for a genuine column baseline;
- do not set a fixed window `SetSize` unless clipping is intentionally part of the design;
- do not calculate title-bar, border, or content margins manually in C#.

Example:

```xml
<!-- Correct: starts at 700x420 and grows when localized content requires more room. -->
<DefaultWindow MinSize="700 420">
    <crt:RMCCrtThemeScope Palette="Blue">
        <!-- Content. -->
    </crt:RMCCrtThemeScope>
</DefaultWindow>
```

`DefaultWindow` includes its header and content margins in its own measurement. When text changes, the Robust layout
queue invalidates and recalculates the affected controls automatically. The final window is still constrained by the
available viewport; exceptionally long content should use wrapping, clipping, or a suitable scroll container rather
than overlapping neighboring columns.

## Extending the library

New reusable CRT controls belong in this directory and should:

1. remain independent from a specific console, BUI, component, or localization key;
2. implement `IRMCCrtThemedControl` when they consume semantic palette colors;
3. obtain the nearest palette through `RMCCrtThemeHelpers` on entering the UI tree;
4. expose semantic properties such as `Tone`, `Variant`, or `Selected` instead of raw per-state colors;
5. define structure in XAML where practical;
6. preserve normal Robust measurement and invalidation behavior;
7. avoid allocations in per-frame drawing code.

Before adding a new control, check whether composition from `RMCCrtPanel`, `RMCCrtLabel`, and standard Robust
containers is sufficient.
