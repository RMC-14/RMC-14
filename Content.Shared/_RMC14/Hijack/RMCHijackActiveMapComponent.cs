using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Hijack;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class RMCHijackActiveMapComponent : Component
{
    [DataField]
    public HashSet<EntityUid> Pipes = new();

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan Next;

    [DataField]
    public TimeSpan NextDelay = TimeSpan.FromSeconds(15);

    [DataField]
    public bool InitialPipeBarragePending;

    [DataField]
    public List<EntityUid> Explode = new();

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? ExplodeAt;

    [DataField]
    public TimeSpan ExplodeDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public SoundSpecifier PipeHiss = new SoundPathSpecifier("/Audio/Ambience/Objects/gas_hiss.ogg");
}
