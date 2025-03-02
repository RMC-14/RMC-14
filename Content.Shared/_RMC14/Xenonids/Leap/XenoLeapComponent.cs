using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Leap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoLeapSystem), typeof(SharedXenoParasiteSystem))]
public sealed partial class XenoLeapComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = FixedPoint2.Zero;

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

    [DataField, AutoNetworkedField]
    public bool KnockdownRequiresInvisibility;

    [DataField, AutoNetworkedField]
    public TimeSpan MoveDelayTime = TimeSpan.FromSeconds(.7);

    [DataField, AutoNetworkedField]
    public bool UnrootOnMelee = false;

    [DataField, AutoNetworkedField]
    public bool DestroyObjects = false;
}
