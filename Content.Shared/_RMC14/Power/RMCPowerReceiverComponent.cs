using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Power;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCPowerSystem))]
public sealed partial class RMCPowerReceiverComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Area;

    [DataField, AutoNetworkedField]
    public EntityUid? Map;

    [DataField, AutoNetworkedField]
    public int IdleLoad;

    [DataField, AutoNetworkedField]
    public int ActiveLoad;

    [DataField, AutoNetworkedField]
    public int LastLoad;

    [DataField, AutoNetworkedField]
    public RMCPowerChannel Channel;

    /// <summary>
    /// Actual mode after availability of power has been taken into account.
    /// </summary>
    [DataField, AutoNetworkedField]
    public RMCPowerMode Mode = RMCPowerMode.Off;

    /// <summary>
    /// Mode restored when power becomes available again.
    /// Defaults to active to preserve existing receiver behavior.
    /// </summary>
    [DataField, AutoNetworkedField]
    public RMCPowerMode RequestedMode = RMCPowerMode.Active;
}
