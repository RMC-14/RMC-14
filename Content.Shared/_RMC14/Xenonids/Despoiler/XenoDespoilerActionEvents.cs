using Content.Shared.Actions;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Despoiler;

public sealed partial class XenoDespoilerAcidBarrageActionEvent : InstantActionEvent;

public sealed partial class XenoDespoilerCausticEmbraceActionEvent : WorldTargetActionEvent;

public sealed partial class XenoDespoilerOozingWoundsActionEvent : InstantActionEvent;

public sealed partial class XenoDespoilerCatalyzeActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed class XenoDespoilerBarrageStartChargeRequest(NetCoordinates target) : EntityEventArgs
{
    public NetCoordinates Target = target;
}

[Serializable, NetSerializable]
public sealed class XenoDespoilerBarrageFireRequest(NetCoordinates target) : EntityEventArgs
{
    public NetCoordinates Target = target;
}
