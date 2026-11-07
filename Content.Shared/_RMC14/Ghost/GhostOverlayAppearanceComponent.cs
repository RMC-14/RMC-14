using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Ghost;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
[Access(typeof(RMCGhostAppearanceSystem))]
public sealed partial class GhostOverlayAppearanceComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId? SourcePrototype;

    [DataField, AutoNetworkedField]
    public GhostFireSnapshot? Fire;

    [DataField, AutoNetworkedField]
    public GhostDamageSnapshot? Damage;

    [DataField, AutoNetworkedField]
    public GhostXenoWoundSnapshot? XenoWound;
}

[DataDefinition]
[Serializable]
[NetSerializable]
public sealed partial class GhostFireSnapshot
{
    [DataField]
    public float FireStacks;

    [DataField]
    public Color? Color;
}

[DataDefinition]
[Serializable]
[NetSerializable]
public sealed partial class GhostDamageSnapshot
{
    [DataField]
    public Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> DamagePerGroup = new();

    [DataField]
    public Dictionary<ProtoId<DamageGroupPrototype>, Color> GroupColors = new();
}

[DataDefinition]
[Serializable]
[NetSerializable]
public sealed partial class GhostXenoWoundSnapshot
{
    [DataField(required: true)]
    public string Prefix = string.Empty;

    [DataField]
    public int States;

    [DataField]
    public int Level;
}
