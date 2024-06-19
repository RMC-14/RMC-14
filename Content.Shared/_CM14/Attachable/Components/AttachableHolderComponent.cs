using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;


namespace Content.Shared._CM14.Attachable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedAttachableHolderSystem))]
public sealed partial class AttachableHolderComponent : Component
{
    [ViewVariables]
    public EntityUid? SupercedingAttachable;
    
    //The key is one of the slot IDs at the bottom of this file. Each key is followed by a listing of all the attachables that fit into that slot.
    [DataField, AutoNetworkedField]
    public Dictionary<string, EntityWhitelist> Slots = new Dictionary<string, EntityWhitelist>();
}

/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 * Slot IDs should be named as follows: cm-aslot-SLOTNAME, for example: cm-aslot-barrel.                     *
 * Each slot ID must have a name attached to it in \Resources\Locale\en-US\_CM14\attachable\attachable.ftl   *
 * The slot list is below. If you add more, list them here so others can use the comment for reference.      *
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 * GUN SLOTS:
 *   cm-aslot-barrel
 *   cm-aslot-rail
 *   cm-aslot-stock
 *   cm-aslot-underbarrel
 */
