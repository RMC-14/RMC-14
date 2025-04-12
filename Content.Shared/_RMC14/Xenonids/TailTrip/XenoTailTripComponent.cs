using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.TailTrip;
[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoTailTripSystem))]
public sealed partial class XenoTailTripComponent : Component
{
    [DataField]
    public int PlasmaCost = 30;

    [DataField]
    public TimeSpan SlowTime = TimeSpan.FromSeconds(0.3);

    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(0.2);

    [DataField]
    public TimeSpan MarkedStunTime = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan MarkedDazeTime = TimeSpan.FromSeconds(4);

    [DataField]
    public EntProtoId TailEffect = "RMCEffectDisarm";

    [DataField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("XenoTailSwipe");

}
