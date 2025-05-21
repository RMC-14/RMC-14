using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Cassette;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCassetteSystem))]
public sealed partial class CassetteTapeComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public List<SoundSpecifier> Songs = new();

    [DataField, AutoNetworkedField]
    public bool Custom;

    [DataField]
    public object? CustomTrack;
}
