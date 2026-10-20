using System.Numerics;
using Content.Shared._RMC14.Humanoid;
using Content.Shared.DisplacementMap;
using Content.Shared.Hands.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Ghost;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class GhostHumanoidAppearanceComponent : Component
{
    [DataField, AutoNetworkedField]
    public RMCHumanoidAppearance Appearance = new();

    [DataField, AutoNetworkedField]
    public List<GhostClothingSnapshot> Clothing = new();

    [DataField, AutoNetworkedField]
    public List<GhostHeldItemSnapshot> HeldItems = new();
}

[DataDefinition]
[Serializable]
[NetSerializable]
public sealed partial class GhostClothingSnapshot
{
    [DataField]
    public string? PrototypeId;

    [DataField(required: true)]
    public string Slot = string.Empty;

    [DataField]
    public Vector2 SlotOffset;

    [DataField]
    public DisplacementData? Displacement;

    [DataField]
    public List<GhostAccessorySnapshot> Accessories = new();

    [DataField]
    public GhostWebbingSnapshot? Webbing;

    [DataField]
    public string? EquippedPrefix;

    [DataField]
    public string? EquippedState;

    [DataField]
    public string? ClothingRsiPath;
}

[DataDefinition]
[Serializable]
[NetSerializable]
public sealed partial class GhostHeldItemSnapshot
{
    [DataField]
    public string? PrototypeId;

    [DataField]
    public HandLocation Location;

    [DataField]
    public DisplacementData? Displacement;

    [DataField]
    public string? HeldPrefix;

    [DataField]
    public string? ItemRsiPath;
}

[DataDefinition]
[Serializable]
[NetSerializable]
public sealed partial class GhostAccessorySnapshot
{
    [DataField(required: true)]
    public ResPath Sprite;

    [DataField(required: true)]
    public string State = string.Empty;

    [DataField]
    public bool Visible = true;

    [DataField(required: true)]
    public string LayerKey = string.Empty;

    [DataField]
    public string? BookmarkKey;
}

[DataDefinition]
[Serializable]
[NetSerializable]
public sealed partial class GhostWebbingSnapshot
{
    [DataField(required: true)]
    public ResPath Sprite;

    [DataField(required: true)]
    public string State = string.Empty;

    [DataField]
    public bool IsOuter;

    [DataField]
    public string? BookmarkKey;
}
