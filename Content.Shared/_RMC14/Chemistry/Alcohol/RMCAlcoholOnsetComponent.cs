using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Alcohol;

[RegisterComponent]
[Access(typeof(RMCAlcoholOnsetSystem))]
public sealed partial class RMCAlcoholOnsetComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, RMCAlcoholOnset> Reagents = new();

    [DataField]
    public TimeSpan ResetAfter = TimeSpan.FromSeconds(3);
}

[DataDefinition]
public sealed partial class RMCAlcoholOnset
{
    [DataField]
    public float Accumulated;

    [DataField]
    public float Threshold;

    [DataField]
    public TimeSpan LastUpdate;
}
