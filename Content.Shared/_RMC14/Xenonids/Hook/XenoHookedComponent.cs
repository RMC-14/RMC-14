using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Hook;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoHookedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Source;

    [DataField, AutoNetworkedField]
    public EntProtoId TailProto;

    [DataField]
    public List<EntityUid> Tail = new();

    [DataField]
    public bool StopUpdating = false;
}


[Serializable, NetSerializable]
public enum HookedVisuals
{
    Hooked,
}
