using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Massacre;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoMassacreComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = 300;

    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(8);

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromMinutes(15);

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Voice/Xeno/alien_roar1.ogg", AudioParams.Default.WithVolume(-5));

    [DataField, AutoNetworkedField]
    public EntProtoId Effects = "RMCEffectGutting";

    [DataField, AutoNetworkedField]
    public float GibRange = 2.5f;

    [DataField]
    public List<(EntityUid ent, EntityUid effect)> Targets = new();

    [DataField, AutoNetworkedField]
    public int BurrowedPerGib = 1;
}
