using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Reagent;

namespace Content.Shared._RMC14.Weapons.Ranged.Flamer;

/// <summary>
/// Totally overrides fuel reagent properties.
/// </summary>
/// <remarks>
/// Place on the flamer, not the tank.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCFlamerSystem))]
public sealed partial class RMCFlamerReagentOverrideComponent : Component
{
    [DataField, AutoNetworkedField]
    public int? Intensity;

    [DataField, AutoNetworkedField]
    public int? Duration;

    [DataField, AutoNetworkedField]
    public int? Range;

    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype>? Reagent;
}
