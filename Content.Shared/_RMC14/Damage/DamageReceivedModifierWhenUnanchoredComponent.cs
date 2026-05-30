using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Damage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCDamageableSystem))]
public sealed partial class DamageReceivedModifierWhenUnanchoredComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Multiplier = 3;
}
