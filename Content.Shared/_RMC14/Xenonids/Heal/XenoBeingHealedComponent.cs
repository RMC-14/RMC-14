using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Heal;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoHealSystem))]
public sealed partial class XenoBeingHealedComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<XenoHealStack> HealStacks = new();

    [DataField, AutoNetworkedField]
    public bool ParallizeHealing = true;

    [DataField, AutoNetworkedField]
    public TimeSpan TimeBetweenHeals;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextHealAt;
}

[UsedImplicitly]
[DataDefinition, Serializable, NetSerializable]
public sealed partial class XenoHealStack
{
    [DataField]
    public FixedPoint2 HealAmount;

    [DataField]
    public int Charges;
}
