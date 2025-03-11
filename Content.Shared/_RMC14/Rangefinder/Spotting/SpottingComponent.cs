using Content.Shared._RMC14.Targeting;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Rangefinder.Spotting;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(TargetingSystem))]
public sealed partial class SpottingComponent : Component
{
    /// <summary>
    ///     The action prototype belonging to this action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionSpotTarget";

    /// <summary>
    ///     The laser Prototype.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId LaserProto = "RMCSpottingLaser";

    /// <summary>
    ///     The sound to be played when the action is used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier SpottingSound = new SoundPathSpecifier("/Audio/_RMC14/Binoculars/nightvision.ogg");

    /// <summary>
    ///     If the laser should be visible.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowLaser = true;

    /// <summary>
    ///     The action id.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    /// <summary>
    ///     How long the spotting should last if not interrupted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SpottingDuration = 10f;
}
