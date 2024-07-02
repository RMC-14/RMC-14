using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared._RMC14.Wieldable;
using Content.Shared.Item;
using Content.Shared.Whitelist;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed partial class AttachableModifiersSystem : EntitySystem
{
    [Dependency] private readonly AttachableHolderSystem _attachableHolderSystem = default!;
    [Dependency] private readonly CMGunSystem _cmGunSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly RMCWieldableSystem _wieldableSystem = default!;
    [Dependency] private readonly SharedItemSystem _itemSystem = default!;

    public override void Initialize()
    {
        InitializeMelee();
        InitializeRanged();
        InitializeSize();
        InitializeSpeed();
        InitializeWieldDelay();
    }

    private bool CanApplyModifiers(EntityUid attachableUid, AttachableModifierConditions? conditions)
    {
        if (conditions == null)
            return true;

        _attachableHolderSystem.TryGetHolder(attachableUid, out var holderUid);

        if (holderUid != null)
        {
            TryComp(holderUid, out WieldableComponent? wieldableComponent);

            if (conditions.Value.UnwieldedOnly && wieldableComponent != null && wieldableComponent.Wielded)
                return false;
            else if (conditions.Value.WieldedOnly && (wieldableComponent == null || !wieldableComponent.Wielded))
                return false;
        }

        TryComp(attachableUid, out AttachableToggleableComponent? toggleableComponent);

        if (conditions.Value.InactiveOnly && toggleableComponent != null && toggleableComponent.Active)
            return false;
        else if (conditions.Value.ActiveOnly && (toggleableComponent == null || !toggleableComponent.Active))
            return false;


        if (holderUid != null)
        {
            if (conditions.Value.Whitelist != null && _whitelistSystem.IsWhitelistFail(conditions.Value.Whitelist, holderUid.Value))
                return false;

            if (conditions.Value.Blacklist != null && _whitelistSystem.IsWhitelistPass(conditions.Value.Blacklist, holderUid.Value))
                return false;
        }

        return true;
    }
}
