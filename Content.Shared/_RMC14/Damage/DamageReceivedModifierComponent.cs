using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Damage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCDamageableSystem))]
public sealed partial class DamageReceivedModifierComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Multiplier = 1;

    [DataField, AutoNetworkedField]
    public FixedPoint2 BruteMultiplier = 1;

    [DataField, AutoNetworkedField]
    public FixedPoint2 BurnMultiplier = 1;
}
