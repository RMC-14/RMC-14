namespace Content.Shared._RMC14.Botany;

[RegisterComponent]
public sealed partial class RMCSeedExtractorComponent : Component
{
    /// <summary>
    /// min amount of seed packets dropped
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int BaseMinSeeds = 2;

    /// <summary>
    /// max amount of seed packets droppe
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int BaseMaxSeeds = 5;
}
