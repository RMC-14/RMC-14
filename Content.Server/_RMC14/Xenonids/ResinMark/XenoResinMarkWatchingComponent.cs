using Robust.Shared.GameStates;

namespace Content.Server._RMC14.Xenonids.ResinMark;

[RegisterComponent]
public sealed partial class XenoResinMarkWatchingComponent : Component
{
    [DataField]
    public EntityUid? Marker;
}

