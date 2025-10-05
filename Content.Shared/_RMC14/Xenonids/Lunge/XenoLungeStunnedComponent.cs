using Content.Shared.StatusEffect;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Lunge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoLungeSystem))]
public sealed partial class XenoLungeStunnedComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<StatusEffectPrototype>[] Effects = new ProtoId<StatusEffectPrototype>[] {"Stun", "KnockedDown"};

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan ExpireAt;

    [DataField, AutoNetworkedField]
    public NetEntity? Stunner;
}
