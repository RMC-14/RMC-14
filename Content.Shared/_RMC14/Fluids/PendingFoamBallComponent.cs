using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Fluids;

[RegisterComponent]
public sealed partial class PendingFoamBallComponent : Component
{
    [DataField(required: true)]
    public MapCoordinates Start;

    [DataField(required: true)]
    public MapCoordinates Target;

    [DataField(required: true)]
    public EntityCoordinates TargetCoordinates;

    [DataField(required: true)]
    public TimeSpan StartTime;

    [DataField(required: true)]
    public TimeSpan Duration;

    [DataField(required: true)]
    public EntProtoId FoamPrototype;

    [DataField(required: true)]
    public int Spread;

    [DataField(required: true)]
    public SoundSpecifier LandSound = default!;
}
