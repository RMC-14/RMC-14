using Content.Shared._RMC14.Dropship.Utility.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Dropship.Utility.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(RMCFultonSystem))]
public sealed partial class RMCActiveFultonComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan ReturnAt;

    [DataField, AutoNetworkedField]
    public EntityCoordinates ReturnTo;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ReturnSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/bamf.ogg");
}
