using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Dropship;

[RegisterComponent]
public sealed partial class RMCDockingPortAirlockControlComponent : Component
{
    [DataField]
    public List<ProtoId<TagPrototype>> AirlockTags = [];

    [DataField]
    public float SearchRadius = 4f;

    [DataField]
    public bool OpenOnDock = true;

    [DataField]
    public bool CloseOnUndock = true;

    [DataField]
    public bool FallbackToNearbyDoors = true;

    [DataField]
    public bool WarnIfMissing = true;
}
