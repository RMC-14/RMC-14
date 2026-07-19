# RMC CRT controls

The controls in this directory provide a reusable CM-SS13-inspired CRT theme for RMC14 interfaces. The theme is local to an `RMCCrtThemeScope`; it does not modify the global SS14 stylesheet.

## Preset palette

```xml
<crt:RMCCrtThemeScope Palette="Blue" Effects="HorizontalScanlines">
    <BoxContainer Orientation="Vertical">
        <crt:RMCCrtLabel Text="Console status" Heading="True" />
        <crt:RMCCrtActionButton Text="Run action" IconState="warning" Variant="Filled" />
        <crt:RMCCrtSeparator />
    </BoxContainer>
</crt:RMCCrtThemeScope>
```

Available presets are `Blue`, `Brown`, `Green`, `Purple`, `Red`, `Upp`, `White`, and `Yellow`.

## Custom palette

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
    <!-- Controls inherit this palette automatically. -->
</crt:RMCCrtThemeScope>
```

Nested theme scopes are supported. Dynamically added CRT controls find the nearest scope when entering the UI tree.

## Effects and icons

`HorizontalScanlines` is the safe default. `RgbSubpixels` is available but must be enabled explicitly. Combine effect flags with commas, for example `Effects="HorizontalScanlines, DiagonalStripes"`.

`RMCCrtActionButton` uses `/Textures/_RMC14/Interface/CRT/crt_icons.rsi` by default. Set `IconRsiPath` and `IconState` to use an icon from another RSI, or assign `IconTexture` from code.
