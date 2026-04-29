using Content.Shared.DisplacementMap;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Ghost;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GhostHumanoidAppearanceComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<GhostHumanoidLayerSnapshot> Layers = new();
}

[DataDefinition]
[Serializable]
[NetSerializable]
public sealed partial class GhostHumanoidLayerSnapshot
{
    [DataField(required: true)]
    public string Key = string.Empty;

    [DataField(required: true)]
    public PrototypeLayerData Layer = new();

    [DataField]
    public DisplacementData? Displacement;

    [DataField]
    public bool BoostedAlpha;
}
