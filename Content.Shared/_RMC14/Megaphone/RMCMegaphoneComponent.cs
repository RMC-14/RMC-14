using Content.Shared.Actions;
using Content.Shared.Inventory;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Megaphone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCMegaphoneSystem))]
public sealed partial class RMCMegaphoneComponent : Component
{
    /// <summary>
    /// Whether the megaphone is currently enabled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = false;

    /// <summary>
    /// The sound played when the megaphone is toggled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? ToggleSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/click.ogg");

    /// <summary>
    /// Whether the megaphone should be deactivated when the user unequips the item.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DeactivateOnUnequip = false;
}

[Serializable, NetSerializable]
public enum MegaphoneVisuals
{
    Light
}

[Serializable, NetSerializable]
public enum MegaphoneLightState
{
    Off,
    On
}
