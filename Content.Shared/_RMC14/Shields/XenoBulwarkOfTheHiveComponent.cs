using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Content.Shared._RMC14.Maths;

namespace Content.Shared._RMC14.Shields;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoBulwarkOfTheHiveComponent : Component
{
    [DataField]
    public TimeSpan DecayTime = TimeSpan.FromSeconds(10);

    [DataField]
    public int DecayAmount = 100;

    [DataField]
    public float Range = RMCMathExtensions.CircleAreaFromSquareAbilityRange(6);

    [DataField]
    public FixedPoint2 ShieldAmount = FixedPoint2.New(200);

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/deep_alien_screech.ogg");

    [DataField]
    public string VisualState = "king-shield";

    [DataField]
    public TimeSpan LightningDuration = TimeSpan.FromSeconds(4);

    [DataField]
    public List<EntityUid> Supporting = new();
}
