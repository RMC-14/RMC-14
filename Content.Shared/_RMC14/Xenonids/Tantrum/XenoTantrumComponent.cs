using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Tantrum;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoTantrumComponent : Component
{
    [DataField, AutoNetworkedField]
    public SoundSpecifier BuffSound = new SoundCollectionSpecifier("XenoRoar");

    [DataField, AutoNetworkedField]
    public int FuryCost = 75;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = FixedPoint2.New(100);

    [DataField, AutoNetworkedField]
    public int SelfArmorBoost = 10;

    [DataField, AutoNetworkedField]
    public TimeSpan SelfArmorDuration = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan OtherArmorDuration = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan OtherSpeedDuration = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public EntProtoId EnrageEffect = "RMCEffectEmpowerTantrum";

    [DataField, AutoNetworkedField]
    public Color EnrageColor = Color.FromHex("#A31010");

    [DataField, AutoNetworkedField]
    public float Range = 8;
}
