using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Overwatch;

/// <summary>
/// A portable camera which can be deployed as a static Overwatch target.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCOverwatchTripodCameraSystem), Other = AccessPermissions.ReadWrite)]
public sealed partial class RMCOverwatchTripodCameraComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? CustomLabel;

    [DataField, AutoNetworkedField]
    public EntityUid? Squad;

    [DataField(required: true)]
    public EntProtoId<SquadTeamComponent> DefaultSquad;

    [DataField(required: true)]
    public string SelectableSquadGroup = string.Empty;

    [DataField(required: true)]
    public EntProtoId<IFFFactionComponent> AssignmentFaction;

    [DataField, AutoNetworkedField]
    public bool Deployed;

    [DataField, AutoNetworkedField]
    public int XenoSlashes;

    [DataField, AutoNetworkedField]
    public int SlashesToBreak = 3;

    [DataField, AutoNetworkedField]
    public TimeSpan DeployDelay = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan PickupDelay = TimeSpan.FromSeconds(2);

    [DataField]
    public float CollapseExplosionDamage = 200f;

    [DataField]
    public float DestroyExplosionDamage = 400f;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? DeploySound;
}

[Serializable, NetSerializable]
public enum RMCOverwatchTripodCameraVisuals
{
    Deployed,
}
