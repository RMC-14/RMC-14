using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

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
    public float StunRange = 7;

    [DataField, AutoNetworkedField]
    public float ParalyzeRange = 4;

    [DataField, AutoNetworkedField]
    public float ParasiteStunRange = 11.2838f;

    [DataField, AutoNetworkedField]
    public TimeSpan ParasiteStunTime = TimeSpan.FromSeconds(8);

    [DataField, AutoNetworkedField]
    public EntProtoId Effect = "CMEffectScreech";

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_queen_screech.ogg", AudioParams.Default.WithVolume(-7));
}
