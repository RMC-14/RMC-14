using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Overwatch;

/// <summary>
/// A portable camera which can be deployed as a static Overwatch target.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCOverwatchTripodCameraSystem), Other = AccessPermissions.ReadWrite)]
public sealed partial class RMCOverwatchTripodCameraComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Label = "Field Camera Tripod";

    [DataField, AutoNetworkedField]
    public EntityUid? Squad;

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
