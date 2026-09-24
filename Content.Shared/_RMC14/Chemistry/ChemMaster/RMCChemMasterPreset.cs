using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry.ChemMaster;

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RMCChemMasterPreset
{
    /// <summary>
    /// The display name of this preset.
    /// </summary>
    [DataField]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The label to apply to pill bottles.
    /// </summary>
    [DataField]
    public string BottleLabel { get; set; } = string.Empty;

    /// <summary>
    /// The color of the pill bottle.
    /// </summary>
    [DataField]
    public RMCPillBottleColors BottleColor { get; set; } = RMCPillBottleColors.Orange;

    /// <summary>
    /// The pill type/style (1-22).
    /// </summary>
    [DataField]
    public uint PillType { get; set; } = 1;

    /// <summary>
    /// If true, uses the preset name as the bottle label instead of BottleLabel.
    /// </summary>
    [DataField]
    public bool UsePresetNameAsLabel { get; set; }

    /// <summary>
    /// Quick access slot number (1-9). Null if not assigned to quick access.
    /// </summary>
    [DataField]
    public int? QuickAccessSlot { get; set; }

    /// <summary>
    /// Custom label shown on the quick access button (max 3 chars).
    /// If empty, uses slot number.
    /// </summary>
    [DataField]
    public string QuickAccessLabel { get; set; } = string.Empty;
}
