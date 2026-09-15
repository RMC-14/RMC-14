using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Botany;

[Serializable, NetSerializable]
[DataDefinition]
public partial struct SeedChemQuantity
{
    /// <summary>
    /// Min amount of chemical that is added to produce, regardless of the potency
    /// </summary>
    [DataField]
    public int Min;

    /// <summary>
    /// Max amount of chemical that can be produced after taking plant potency into account.
    /// </summary>
    [DataField]
    public int Max;

    [DataField]
    public int PotencyDivisor;

    [DataField]
    public bool Inherent = true;
}

/// <summary>
/// Chemical yield table & potency of a <see cref="RMCPlantComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, fieldDeltas: true)]
public sealed partial class RMCPlantChemicalsComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<string, SeedChemQuantity> Chemicals = new();

    [DataField, AutoNetworkedField]
    public float Potency = 1f;

    [DataField, AutoNetworkedField]
    public Color? ProductColor;

    /// <summary>
    /// Reagents that can only be found by mutating this plant.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> SpecialChemicals = new();

    [DataField, AutoNetworkedField]
    public float PotencyCounter;

    [DataField, AutoNetworkedField]
    public float ChemAddCounter;
}
