using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent, Access(typeof(RMCPreventSameFactionStickSystem))]
public sealed partial class RMCPreventSameFactionStickComponent : Component
{
    [DataField]
    public LocId Popup = "rmc-explosive-cannot-stick-same-faction";
}
