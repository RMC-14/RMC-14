using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.ManageHive.Boons;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(HiveBoonSystem))]
public sealed partial class HiveBoonFortifiableWallComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Heal = 75;

    [DataField, AutoNetworkedField]
    public TimeSpan HealEvery = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextHealAt;
}
