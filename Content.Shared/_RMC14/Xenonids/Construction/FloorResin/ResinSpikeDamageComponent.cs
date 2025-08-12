using Content.Shared._RMC14.Xenonids.Weeds;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Construction.FloorResin;

/// <summary>
/// Adjusts the damage resin spikes deal when triggered
/// </summary>

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedXenoWeedsSystem))]
public sealed partial class ResinSpikeDamageComponent : Component
{
    [DataField]
    public float OutsiderDamage;
}
