using Content.Shared._RMC14.Stealth;
using Content.Shared._RMC14.Targeting;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Rangefinder.Spotting;

public abstract partial class SharedRMCSpottingSystem : EntitySystem
{
    [Dependency] private readonly SharedRMCTargetingSystem _targeting = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SpottingComponent, GetItemActionsEvent>(OnSpotterGetItemActions);
        SubscribeLocalEvent<SpottingComponent, SpotTargetActionEvent>(OnSpotTarget);
        SubscribeLocalEvent<SpottingComponent, TargetingFinishedEvent>(OnTargetingFinished);
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
    ///    Toggles the ability to start spotting.
    /// </summary>
    private void OnSpotTarget(Entity<SpottingComponent> ent, ref SpotTargetActionEvent args)
    {
        ent.Comp.Activated = !ent.Comp.Activated;
        Dirty(ent);

        _actions.SetToggled(ent.Comp.Action, ent.Comp.Activated);
        args.Handled = true;
    }

    /// <summary>
    ///     Stop targeting an entity when done spotting.
    /// </summary>
    private void OnTargetingFinished(Entity<SpottingComponent> ent, ref TargetingFinishedEvent args)
    {
        if(!TryComp(ent, out TargetingComponent? targeting))
            return;

        _targeting.StopTargeting(ent, args.Target, targeting);
    }

    /// <summary>
    ///     Try to spot the target, this will attach the <see cref="SpottedComponent"/> to the target.
    /// </summary>
    /// <param name="netSpottingTool">The equipment used to spot the target.</param>
    /// <param name="netUser">The user trying to spot the target.</param>
    /// <param name="netTarget">The target of the spot.</param>
    protected void SpotRequested(NetEntity netSpottingTool, NetEntity netUser, NetEntity netTarget)
    {
        var spottingTool = GetEntity(netSpottingTool);
        var user = GetEntity(netUser);
        var target = GetEntity(netTarget);

        if(!TryComp(spottingTool, out SpottingComponent? spotting))
            return;

        if(!CanSpot((spottingTool, spotting), target, user))
            return;

        // No laser if the user is invisible.
        if (TryComp(user, out EntityTurnInvisibleComponent? invisible) &&
            TryComp(spottingTool, out TargetingLaserComponent? targeting))
        {
            targeting.ShowLaser = !invisible.Enabled;
            Dirty(spottingTool, targeting);
        }

        // Set the cooldown.
        spotting.NextSpot = Timing.CurTime + spotting.SpottingCooldown;
        Dirty(spottingTool, spotting);

        // Make sure the target has the component that will increase the speed of an aimed shot.
        var spotted = EnsureComp<SpottedComponent>(target);
        Dirty(target, spotted);

        // Play audio and change the light on the rangefinder icon.
        _audio.PlayPredicted(spotting.SpottingSound, spottingTool, user);
        _appearance.TryGetData(spottingTool, RangefinderLayers.Layer, out var layer);
        if(layer != null)
            _appearance.SetData(spottingTool, RangefinderLayers.Layer, RangefinderMode.Spotter);

        // Start targeting the targeted entity.
        _targeting.Target(spottingTool, user, target, spotting.SpottingDuration, TargetedEffects.Spotted);
    }

    /// <summary>
    ///     Check if it's possible to spot the selected target.
    /// </summary>
    private bool CanSpot(Entity<SpottingComponent> ent, EntityUid target, EntityUid user)
    {
        // Can't spot if the target doesn't have the required component.
        if (!HasComp<SpottableComponent>(target))
            return false;

        // Can't spot a target you can't see.
        if (!_examine.InRangeUnOccluded(user, target, ent.Comp.SpottingRange))
            return false;

        // Can't spot for a certain amount of time after having spotted something.
        if (ent.Comp.NextSpot > Timing.CurTime)
            return false;

        // Only allow entities with the SpotterComponent to use the spotting function.
        if(!HasComp<SpotterWhitelistComponent>(user))
        {
            var message = Loc.GetString("rmc-action-popup-spotting-user-no-skill", ("rangefinder", ent));
            _popup.PopupClient(message, user, user);
            return false;
        }

        // Cancel the action if the entity isn't held.
        if (!_hands.TryGetActiveItem(user, out var heldItem) || heldItem != ent)
        {
            var message = Loc.GetString("rmc-action-popup-spotting-user-must-hold", ("rangefinder", ent));
            _popup.PopupClient(message, user, user);
            return false;
        }

        return true;
    }
}
