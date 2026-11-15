using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

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
    public OverwatchSavedLocation?[] SavedLocations = new OverwatchSavedLocation?[3];

    [DataField, AutoNetworkedField]
    public int LastLocation;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastMessage;

    [DataField, AutoNetworkedField]
    public TimeSpan MessageCooldown = TimeSpan.FromSeconds(0.5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastObjectiveUpdate;

    [DataField, AutoNetworkedField]
    public bool CanMessageSquad = true;

    [DataField, AutoNetworkedField]
    public bool HasOrbital;

    [DataField, AutoNetworkedField]
    public Vector2i OrbitalCoordinates;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextOrbitalLaunch;

    [DataField]
    public string Group = "UNMC";

    [DataField, AutoNetworkedField]
    public bool CanOrbitalBombardment = true;
}
