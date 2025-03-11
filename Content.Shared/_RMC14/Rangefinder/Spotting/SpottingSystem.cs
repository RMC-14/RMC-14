using Content.Shared._RMC14.Stealth;
using Content.Shared._RMC14.Targeting;
using Content.Shared.Actions;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._RMC14.Rangefinder.Spotting;

public sealed partial class SpottingSystem : EntitySystem
{
    [Dependency] private readonly TargetingSystem _targeting = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SpottingComponent, GetItemActionsEvent>(OnSpotterGetItemActions);
        SubscribeLocalEvent<SpottingComponent, SpotTargetActionEvent>(OnSpotTarget);
    }

    /// <summary>
    ///     Add an action to the entity holding this item.
    /// </summary>
    private void OnSpotterGetItemActions(Entity<SpottingComponent> ent, ref GetItemActionsEvent args)
    {
        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);
        Dirty(ent.Owner, ent.Comp);
    }

    /// <summary>
    ///    Applies the spotted component to the entity if it's a valid target and starts targeting it.
    /// </summary>
    private void OnSpotTarget(Entity<SpottingComponent> ent, ref SpotTargetActionEvent args)
    {
        var user = args.Performer;
        var target = args.Target;

        if(!HasComp<SpottableComponent>(args.Target))
            return;

        // Only allow entities with the SpotterComponent to use the spotting function.
        if(!HasComp<SpotterComponent>(user))
        {
            var message = Loc.GetString("rmc-action-popup-spotting-user-no-skill", ("rangefinder", ent));
            _popup.PopupClient(message, user, user);
            return;
        }

        // No laser if the user is invisible.
        if (TryComp(args.Performer, out EntityTurnInvisibleComponent? invisible) &&
            TryComp(ent, out TargetingLaserComponent? targeting))
        {
            targeting.ShowLaser = !invisible.Enabled;
            Dirty(ent, targeting);
        }


        // Cancel the action if the entity isn't held.
        if (!_hands.TryGetActiveItem(user, out var heldItem) || heldItem != ent)
        {
            var message = Loc.GetString("rmc-action-popup-spotting-user-must-hold", ("rangefinder", ent));
            _popup.PopupClient(message, user, user);
            return;
        }

        _targeting.Target(ent, user, target, ent.Comp.SpottingDuration, TargetedEffects.Spotted);

        _audio.PlayPredicted(ent.Comp.SpottingSound, ent, user);
        _appearance.SetData(ent, RangefinderLayers.Layer, RangefinderMode.Spotter);

        var spotted = EnsureComp<SpottedComponent>(target);
        spotted.Spotter = user;
        Dirty(target, spotted);

        args.Handled = true;
    }
}
