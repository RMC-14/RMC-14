using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Power;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCPowerSystem))]
public sealed partial class RMCPowerReceiverComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Area;

    [DataField, AutoNetworkedField]
    public float IdleLoad;

    /// <summary>
    /// Additional watts consumed in <see cref="RMCPowerMode.Active"/> on top of <see cref="IdleLoad"/>.
    /// This matches CM13, where active usage is added to idle usage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ActiveLoad;

    /// <summary>
    /// Energy consumed once when a powered door starts opening or closing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DoorMovementEnergy;

    [DataField, AutoNetworkedField]
    public float LastLoad;

    [ViewVariables]
    public float PendingOneOffEnergy;

    [DataField, AutoNetworkedField]
    public RMCPowerChannel Channel;

    [DataField, AutoNetworkedField]
    public RMCPowerMode Mode = RMCPowerMode.Idle;
}
