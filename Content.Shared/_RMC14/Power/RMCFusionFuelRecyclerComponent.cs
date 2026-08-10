using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Power;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCFusionFuelRecyclerComponent : Component
{
    [DataField]
    public string LeftSlot = "rmc_fusion_recycler_left";

    [DataField]
    public string RightSlot = "rmc_fusion_recycler_right";

    [DataField]
    public float FuelPerCycle = 5;

    [DataField]
    public TimeSpan ProcessInterval = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public bool Working;

    [ViewVariables]
    public TimeSpan NextProcessAt;

    [DataField]
    public SoundSpecifier FinishSound = new SoundPathSpecifier("/Audio/Machines/ding.ogg");
}

[Serializable, NetSerializable]
public enum RMCFusionFuelRecyclerVisuals
{
    Working,
    LeftCell,
    RightCell,
    LeftCharging,
    RightCharging,
    LeftCharged,
    RightCharged,
}
