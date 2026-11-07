using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Destrain;


/// Lets a strained xeno reset its strain from the devolve menu
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoDestrainSystem))]
public sealed partial class XenoDestrainComponent : Component
{
    /// The base caste that this strain turns back into.
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId DestrainTo;

    /// How long after resetting a strain this xeno has to wait before it can reset one again. The first reset is always free.
    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromMinutes(40);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? LastDestrainAt;
}

[Serializable, NetSerializable]
public sealed class XenoDestrainBuiMsg : BoundUserInterfaceMessage;
