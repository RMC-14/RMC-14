using Content.Shared._RMC14.Maths;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Doom;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoDoomComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId Smoke = "RMCSmokeKingDoom";

    [DataField, AutoNetworkedField]
    public float Range = RMCMathExtensions.CircleAreaFromSquareAbilityRange(7);

    [DataField, AutoNetworkedField]
    public float ExtinguishTimePerDistanceMult = 0.1f;

    [DataField, AutoNetworkedField]
    public TimeSpan DazeTime = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan SlowTime = TimeSpan.FromSeconds(8);

    [DataField, AutoNetworkedField]
    public FixedPoint2 RemovalPerReagent = 100;

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/deep_alien_screech2.ogg");

    [DataField, AutoNetworkedField]
    public EntProtoId Effect = "RMCEffectScreechKing";

    [DataField, AutoNetworkedField]
    public int CameraShakeStrength = 1;

    [DataField, AutoNetworkedField]
    public string TargetSolution = "chemicals";

    [DataField, AutoNetworkedField]
    public TimeSpan OverlayTime = TimeSpan.FromSeconds(5);
}
