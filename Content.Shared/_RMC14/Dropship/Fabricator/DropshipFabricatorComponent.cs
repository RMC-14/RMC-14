using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Dropship.Fabricator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(DropshipFabricatorSystem))]
public sealed partial class DropshipFabricatorComponent : Component
{
    [DataField]
    public EntityUid? Account;

    [DataField, AutoNetworkedField]
    public int Points;

    [DataField, AutoNetworkedField]
    public EntProtoId<DropshipFabricatorPrintableComponent>? Printing;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan PrintAt;

    [DataField, AutoNetworkedField]
    public Vector2i PrintOffset = new(1, 0);

    [DataField, AutoNetworkedField]
    public SoundSpecifier RecycleSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/fax.ogg");
}
