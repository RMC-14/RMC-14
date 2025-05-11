using Content.Shared.StatusEffect;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.FightOrFlight;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoFightOrFlightComponent : Component
{
    [DataField, AutoNetworkedField]
    public int LowRange = 4;

    [DataField, AutoNetworkedField]
    public int HighRange = 6;

    [DataField, AutoNetworkedField]
    public int FuryThreshold = 75;

    [DataField, AutoNetworkedField]
    public SoundSpecifier RoarSound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/xenos_roaring.ogg");

    [DataField, AutoNetworkedField]
    public EntProtoId RoarEffect = "RMCEffectScreechValkyrie";

    [DataField, AutoNetworkedField]
    public EntProtoId WeakRoarEffect = "RMCEffectScreechValkyrieWeak";

    [DataField, AutoNetworkedField]
    public EntProtoId HealEffect = "RMCEffectHealAilments";

    [DataField, AutoNetworkedField]
    public TimeSpan Jitter = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public ProtoId<StatusEffectPrototype>[] AilmentsRemove = ["KnockedDown", "Stun"];

    [DataField]
    public ComponentRegistry ComponentsRemove;
}
