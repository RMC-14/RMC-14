using Content.Shared._RMC14.Marines.Squads;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.SupplyDrop;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedSupplyDropSystem))]
public sealed partial class SupplyDropComputerComponent : Component
{
    [DataField, AutoNetworkedField]
    public Vector2i Coordinates;

    [DataField, AutoNetworkedField]
    public int MaxCoordinate = 1000;

    [DataField, AutoNetworkedField]
    public EntProtoId<SquadTeamComponent>? Squad;

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(500);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastLaunchAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextLaunchAt;

    [DataField, AutoNetworkedField]
    public bool HasCrate;

    [DataField, AutoNetworkedField]
    public TimeSpan UpdateEvery = TimeSpan.FromSeconds(0.5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate;
}
