using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.SupplyDrop;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SupplyDropPodComponent : Component
{
    /// <summary>
    ///     The item slot to put the spawned entity in.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string DeploySlotId = "supply_drop";

    /// <summary>
    ///     Whether the pod has landed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Landed;

    /// <summary>
    ///     How long after landing the pod will open.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan OpenTimeRemaining = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     The sound to play when the pod opens.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? OpenSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/Techpod/techpod_open.ogg");

    /// <summary>
    ///     The amount of damage dealt to entities near the area where this entity lands from a drop.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier? LandingDamage;
}
