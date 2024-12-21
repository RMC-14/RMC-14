using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Heal;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoHealSystem))]
public sealed partial class XenoBeingHealedComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<XenoHealStack> HealStacks = new();

    [DataField, AutoNetworkedField]
    public bool ParallizeHealing = true;
}

[UsedImplicitly]
[DataDefinition, Serializable, NetSerializable]
public sealed partial class XenoHealStack
{
    [DataField]
    public FixedPoint2 HealAmount;

    [DataField]
    public int Charges;

    [DataField]
    public TimeSpan TimeBetweenHeals;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextHealAt;
}
