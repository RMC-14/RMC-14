using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Biomass;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCBiomassOrganComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Value;

    [DataField, AutoNetworkedField]
    public string? Caste;
}
