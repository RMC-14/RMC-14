# RMC14 CRT UI

Reusable client-side controls for CM-SS13-inspired interfaces. The library provides locally scoped palettes,
semantic colors, icons, and optional display effects without changing the global SS14 stylesheet.

## Usage

Wrap CRT controls in an `RMCCrtThemeScope`:

```xml
<crt:RMCCrtThemeScope Palette="Blue" Effects="HorizontalScanlines">
    <BoxContainer Orientation="Vertical">
        <crt:RMCCrtLabel Text="{Loc example-title}" Heading="True" />
        <crt:RMCCrtSeparator />
        <crt:RMCCrtActionButton Text="{Loc example-action}"
                                IconState="warning"
                                Variant="Filled" />
    </BoxContainer>
</crt:RMCCrtThemeScope>
```

All user-facing text must use Fluent localization. Normal Robust properties such as `Disabled`, `MinSize`,
`HorizontalExpand`, margins, and alignment continue to work.

Available reusable controls:

- `RMCCrtThemeScope` - local palette, stylesheet, background, border, and root effects;
- `RMCCrtPanel` - surface, inset, transparent, or warning panel;
- `RMCCrtActionButton` - outline, filled, navigation, or danger action;
- `RMCCrtLabel` - normal, heading, or semantic status text;
- `RMCCrtSeparator` - horizontal or vertical separator;
- `RMCCrtTwoColumnContainer` - two equal-width wrapping columns around a divider;
- `RMCCrtIcon` - palette-aware RSI icon.

Prefer semantic properties such as `Tone`, `Variant`, and `Selected` over directly changing child colors. Buttons use
`/Textures/_RMC14/Interface/CRT/crt_icons.rsi` by default; matching state constants are available in `RMCCrtIcons`.

## Appearance preferences

The Accessibility tab provides two archived client preferences, both enabled by default:

- `rmc.crt_theme_enabled` switches the library between CRT and standard Nano presentation;
- `rmc.crt_effects_enabled` controls scanlines, RGB subpixels, and diagonal warning stripes.

Disabling the theme always suppresses effects without overwriting the effects preference. Re-enabling it restores the
previous effects choice. Applied preference changes update open windows, and controls added later inherit the current
appearance from their nearest scope. Nested scopes keep their own palettes.

Nano mode preserves layout, content, icons, semantic tones, and interaction state while replacing CRT palettes,
fonts, borders, color overrides, and effects with standard Nano styling.

## Adding controls

New controls in this library must:

1. remain independent from a specific console, BUI, component, or localization key;
2. support both CRT and Nano presentation;
3. implement `IRMCCrtThemedControl` when consuming theme or semantic palette state;
4. resolve the nearest context through `RMCCrtThemeHelpers` when entering the UI tree;
5. avoid configuration lookups and allocations in per-frame drawing code;
6. preserve normal Robust measurement and invalidation behavior.

Use `RMCCrtPanel`, `RMCCrtLabel`, and standard Robust containers through composition before adding another control.
