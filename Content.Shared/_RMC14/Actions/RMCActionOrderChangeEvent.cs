using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Actions;

[Serializable, NetSerializable]
public sealed class RMCActionOrderChangeEvent(List<EntProtoId> actions) : EntityEventArgs
{
    public readonly List<EntProtoId> Actions = actions;
}
