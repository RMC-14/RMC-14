using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._RMC14.Armor.Ghillie;

/// <summary>
/// Handles (un)equipping and provides some API functions.
/// </summary>
public abstract class SharedGhillieSuitSystem : EntitySystem
{
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhillieSuitComponent, ToggleClothingCheckEvent>(OnCloakCheck);
        SubscribeLocalEvent<GhillieSuitComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
        SubscribeLocalEvent<GhillieSuitComponent, GotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<GhillieSuitComponent, ShotAttemptedEvent>(OnShotAttempted);
    }

    /// <summary>
    /// Only add the action when equipped by a weapons specialist.
    /// </summary>
    private void OnCloakCheck(Entity<GhillieSuitComponent> ent, ref ToggleClothingCheckEvent args)
    {
        if (!_skills.HasSkills(args.User, in ent.Comp.SkillRequired))
            args.Cancelled = true;
    }

    /// <summary>
    /// Disable the abilities when the suit unequipped
    /// </summary>
    private void OnUnequipped(Entity<GhillieSuitComponent> ent, ref GotUnequippedEvent args)
    {
        var user = args.Equipee;
        _toggle.TryDeactivate(ent.Owner, user: user);
    }

    private void OnActivateAttempt(Entity<GhillieSuitComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (args.User != null && !_skills.HasSkills(args.User.Value, in ent.Comp.SkillRequired))
        {
            args.Cancelled = true;
            return;
        }

        if (IsDisabled((ent, ent.Comp, null)))
        {
            args.Cancelled = true;
            args.Popup = Loc.GetString("ninja-suit-cooldown");
        }
    }

    /// <summary>
    /// Returns true if the ghillie is not enabled
    /// </summary>
    public bool IsDisabled(Entity<GhillieSuitComponent?, UseDelayComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return false;

        return _useDelay.IsDelayed((ent, ent.Comp2), ent.Comp1.DisableDelayId);
    }

    protected bool CheckDisabled(Entity<GhillieSuitComponent> ent, EntityUid user)
    {
        if (IsDisabled((ent, ent.Comp, null)))
        {
            Popup.PopupEntity(Loc.GetString("ninja-suit-cooldown"), user, user, PopupType.Medium);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Force uncloaks the user.
    /// </summary>
    public void RevealUser(Entity<GhillieSuitComponent> ent, EntityUid user, bool disable = true)
    {
        var uid = ent.Owner;
        var comp = ent.Comp;
        if (_toggle.TryDeactivate(uid, user) || !disable)
            return;

        Popup.PopupClient(Loc.GetString("ninja-revealed"), user, user, PopupType.MediumCaution);
        _useDelay.TryResetDelay(uid, id: comp.DisableDelayId);
    }

    private void OnShotAttempted(Entity<GhillieSuitComponent> ent, ref ShotAttemptedEvent args)
    {
        RevealUser(ent, args.User);
    }
}