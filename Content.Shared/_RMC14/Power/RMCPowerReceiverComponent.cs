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

    [DataField, AutoNetworkedField]
    public RMCPowerMode Mode = RMCPowerMode.Off;
}
