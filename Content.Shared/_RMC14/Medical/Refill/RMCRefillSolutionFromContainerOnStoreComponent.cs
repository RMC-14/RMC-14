using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.Refill;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMRefillableSolutionSystem))]
public sealed partial class RMCRefillSolutionFromContainerOnStoreComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ContainerId = "pressurized_reagent_canister";

    [DataField, AutoNetworkedField]
    public bool CanFlush = false;

    [DataField, AutoNetworkedField]
    public TimeSpan FlushTime = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public float LayerOpacity = 0.75f;
}


[Serializable, NetSerializable]
public enum SolutionContainerStoreVisuals : byte
{
    Base,
    Color
}
