using Content.Shared._RMC14.AlertLevel;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.OrbitalCannon;
using Robust.Shared.GameStates;
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
    public RMCAlertLevels AlertLevel = RMCAlertLevels.Green;

    [DataField, AutoNetworkedField]
    public TimeSpan HighCommandCooldown = TimeSpan.FromSeconds(30);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? LastHighCommand;

    [DataField, AutoNetworkedField]
    public List<LandingZone> LandingZones = new();

    [DataField, AutoNetworkedField]
    public string? PrimaryLandingZone;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextGeneralQuarters;

    [DataField, AutoNetworkedField]
    public bool HasOrbitalCannon;

    [DataField, AutoNetworkedField]
    public string? OrbitalWarhead;

    [DataField, AutoNetworkedField]
    public int OrbitalFuel;

    [DataField, AutoNetworkedField]
    public int? OrbitalRequiredFuel;

    [DataField, AutoNetworkedField]
    public OrbitalCannonStatus OrbitalStatus = OrbitalCannonStatus.Unloaded;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextOrbitalFire;

    [DataField, AutoNetworkedField]
    public bool OrbitalSafetyEngaged;
}
