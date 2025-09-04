using Content.Shared._RMC14.Xenonids.Construction.ResinWhisper;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedXenoConstructionSystem), typeof(ResinWhispererSystem))]
public sealed partial class XenoCanAddPlasmaToConstructComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Range = 1.75;

    [DataField, AutoNetworkedField]
    public TimeSpan AddPlasmaDelay = TimeSpan.FromSeconds(3);
}
