using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Overwatch;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedOverwatchConsoleSystem))]
public sealed partial class OverwatchConsoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public NetEntity? Squad;

    [DataField, AutoNetworkedField]
    public string? Operator;

    [DataField, AutoNetworkedField]
    public OverwatchLocation? Location;

    [DataField, AutoNetworkedField]
    public bool ShowDead = true;

    [DataField, AutoNetworkedField]
    public bool ShowHidden;

    [DataField, AutoNetworkedField]
    public HashSet<NetEntity> Hidden = new();

    [DataField, AutoNetworkedField]
    public OverwatchSupplyDropLocation?[] SupplyDropLocations = new OverwatchSupplyDropLocation?[3];

    [DataField, AutoNetworkedField]
    public int LastLocation;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastMessage;

    [DataField, AutoNetworkedField]
    public TimeSpan MessageCooldown = TimeSpan.FromSeconds(0.5);

    [DataField, AutoNetworkedField]
    public bool CanMessageSquad = true;
}
