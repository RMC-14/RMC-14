using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.Squads;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Marines.GroundsideOperations;

/// <summary>
///     State owned by the unified Groundside Operations Console.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedGroundsideOperationsConsoleSystem))]
public sealed partial class GroundsideOperationsConsoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan HighCommandCooldown = TimeSpan.FromSeconds(30);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? LastHighCommand;

    [DataField, AutoNetworkedField]
    public List<LandingZone> LandingZones = new();

    [DataField, AutoNetworkedField]
    public List<GroundsideOverwatchSquadSummary> OverwatchSquads = new();
}

[Serializable, NetSerializable]
public readonly record struct GroundsideOverwatchSquadSummary(
    NetEntity Id,
    string Name,
    Color Color,
    string? Leader,
    int Members,
    int Alive,
    string PrimaryObjective,
    string SecondaryObjective);
