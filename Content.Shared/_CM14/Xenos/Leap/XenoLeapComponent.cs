using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Xenos.Leap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoLeapSystem))]
public sealed partial class XenoLeapComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public FixedPoint2 Range = 6;

    [DataField, AutoNetworkedField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public SoundSpecifier? LeapSound;

    [DataField, AutoNetworkedField]
    public int Strength = 20;
}
