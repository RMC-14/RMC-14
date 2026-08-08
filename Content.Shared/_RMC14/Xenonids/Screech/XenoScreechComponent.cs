using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using static Content.Shared._RMC14.Maths.RMCMathExtensions;

namespace Content.Shared._RMC14.Xenonids.Screech;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoScreechSystem))]
public sealed partial class XenoScreechComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = 250;

    [DataField, AutoNetworkedField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(6);

    [DataField, AutoNetworkedField]
    public TimeSpan ParalyzeTime = TimeSpan.FromSeconds(8);

    [DataField, AutoNetworkedField]
    public TimeSpan CloseDeafTime = TimeSpan.FromSeconds(7);

    [DataField, AutoNetworkedField]
    public TimeSpan FarDeafTime = TimeSpan.FromSeconds(4);

    // TODO RMC14 stun less within 4 tiles
    [DataField, AutoNetworkedField]
    public float StunRange = CircleAreaFromSquareAbilityRange(6f);

    [DataField, AutoNetworkedField]
    public float ParalyzeRange = CircleAreaFromSquareAbilityRange(4f);

    [DataField, AutoNetworkedField]
    public float ParasiteStunRange = CircleAreaFromSquareAbilityRange(9.5f);

    [DataField, AutoNetworkedField]
    public TimeSpan ParasiteStunTime = TimeSpan.FromSeconds(8);

    [DataField, AutoNetworkedField]
    public int CloseScreenShakeShakes = 12;

    [DataField, AutoNetworkedField]
    public int CloseScreenShakeStrength = 6;

    [DataField, AutoNetworkedField]
    public int FarScreenShakeShakes = 8;

    [DataField, AutoNetworkedField]
    public int FarScreenShakeStrength = 4;

    [DataField, AutoNetworkedField]
    public EntProtoId Effect = "CMEffectScreech";

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_queen_screech.ogg", AudioParams.Default.WithVolume(-7));
}
