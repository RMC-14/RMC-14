using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class TacticalMapUserComponent : Component
{
    public override bool SendOnlyToOwner => true;

    [DataField(required: true), AutoNetworkedField]
    public EntProtoId ActionId;

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public bool LiveUpdate;

    [DataField, AutoNetworkedField]
    public bool CanDraw;

    [DataField, AutoNetworkedField]
    public bool AllChannels;

    [DataField, AutoNetworkedField]
    public List<EntityUid> Channels = new();

    [DataField, AutoNetworkedField]
    public bool Live;

    [DataField, AutoNetworkedField]
    public EntityUid? SelfMap;

    [DataField, AutoNetworkedField]
    public Vector2i SelfIndices;
}
