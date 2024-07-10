using Content.Shared._RMC14.Attachable.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableHolderSystem))]
public sealed partial class AttachableHolderComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? SupercedingAttachable;

    /// <summary>
    ///     The key is one of the slot IDs at the bottom of this file.
    ///     Each key is followed by the description of the slot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<string, AttachableSlot> Slots = new();
}

/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 * Slot IDs should be named as follows: rmc-aslot-SLOTNAME, for example: rmc-aslot-barrel.                   *
 * Each slot ID must have a name attached to it in \Resources\Locale\en-US\_RMC14\attachable\attachable.ftl  *
 * The slot list is below. If you add more, list them here so others can use the comment for reference.      *
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 * GUN SLOTS:
 *   rmc-aslot-barrel
 *   rmc-aslot-rail
 *   rmc-aslot-stock
 *   rmc-aslot-underbarrel
 */
