using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Construction.ResinWhisper;

[RegisterComponent, NetworkedComponent]
[Access([typeof(ResinWhispererSystem), typeof(SharedXenoConstructionSystem)])]
public sealed partial class ResinDoorComponent : Component
{
    // For pushing dead bodies out of the resin door
    public List<EntityUid> CollidingBodies = new();
}
