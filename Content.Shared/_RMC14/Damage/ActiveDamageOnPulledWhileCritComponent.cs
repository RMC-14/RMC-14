using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Damage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCDamageableSystem))]
public sealed partial class ActiveDamageOnPulledWhileCritComponent : Component
{
    [DataField, AutoNetworkedField]
    public DamageSpecifier? Damage;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? PullerWhitelist;

    [DataField, AutoNetworkedField]
    public double Every;

    [DataField, AutoNetworkedField]
    public bool Pulled;

    [DataField, AutoNetworkedField]
    public double Accumulator;
}
