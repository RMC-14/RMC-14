using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class TacticalMapUserComponent : Component
{
    public override bool SendOnlyToOwner => true;

    [DataField(required: true), AutoNetworkedField]
    public EntProtoId ActionId;

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public EntityUid? Map;

    [DataField, AutoNetworkedField]
    public bool LiveUpdate;

    [DataField]
    public List<ProtoId<TacticalMapLayerPrototype>> BaseLayers = new();

    [DataField, AutoNetworkedField]
    public List<ProtoId<TacticalMapLayerPrototype>> VisibleLayers = new();

    [DataField, AutoNetworkedField]
    public ProtoId<TacticalMapLayerPrototype>? ActiveLayer;

    [DataField, AutoNetworkedField]
    public Dictionary<int, TacticalMapBlip> Blips = new();

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastAnnounceAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextAnnounceAt;

    [DataField, AutoNetworkedField]
    public bool CanDraw;

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("XenoQueenCommand", AudioParams.Default.WithVolume(-6));
}
