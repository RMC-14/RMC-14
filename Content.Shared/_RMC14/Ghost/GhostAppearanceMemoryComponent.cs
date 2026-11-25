using Content.Shared._RMC14.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Ghost;

[RegisterComponent]
[Access(typeof(RMCGhostAppearanceSystem))]
public sealed partial class GhostAppearanceMemoryComponent : Component
{
    [DataField]
    public RMCHumanoidAppearance? Humanoid;

    [DataField]
    public List<GhostClothingSnapshot> Clothing = new();

    [DataField]
    public List<GhostHeldItemSnapshot> HeldItems = new();

    [DataField]
    public EntProtoId? NonHumanoidPrototype;

    [DataField]
    public EntProtoId? OverlayPrototype;

    [DataField]
    public GhostFireSnapshot? Fire;

    [DataField]
    public GhostDamageSnapshot? Damage;

    [DataField]
    public GhostXenoWoundSnapshot? XenoWound;
}
