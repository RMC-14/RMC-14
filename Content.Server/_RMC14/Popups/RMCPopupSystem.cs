using Content.Shared._RMC14.Stealth;
using Content.Shared._RMC14.Xenonids.Invisibility;

namespace Content.Server._RMC14.Popups;

public sealed class RMCPopupSystem : EntitySystem
{
    public bool ShouldPopup(EntityUid recipient)
    {
        // Don't show popups to others while invisible.
        return !HasComp<EntityActiveInvisibleComponent>(recipient) &&
               !HasComp<XenoActiveInvisibleComponent>(recipient);
    }
}
