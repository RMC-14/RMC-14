using Content.Shared.Actions;
using Content.Shared.Coordinates;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Stealth.Components;
using Content.Shared.Timing;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Whistle;

public sealed class RMCWhistleSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UseDelaySystem _useDelaySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCWhistleComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<RMCWhistleComponent, WhistleActionEvent>(OnWhistleAction);
        SubscribeLocalEvent<RMCWhistleComponent, UseInHandEvent>(OnUseInHand);
    }

    private void ExclamateTarget(EntityUid target, RMCWhistleComponent component)
    {
        SpawnAttachedTo(component.Effect, target.ToCoordinates());
    }

    private void OnGetItemActions(Entity<RMCWhistleComponent> ent, ref GetItemActionsEvent args)
    {
        var comp = ent.Comp;

        if (args.SlotFlags == SlotFlags.POCKET)
            return;

        if (TryComp<UseDelayComponent>(ent, out var useDelay))
            _actions.SetUseDelay(comp.Action, useDelay.Delay);

        args.AddAction(ref comp.Action, comp.ActionId);
    }

    public void OnWhistleAction(EntityUid uid, RMCWhistleComponent component, WhistleActionEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Handled)
            return;

        if (TryComp<UseDelayComponent>(uid, out var useDelay))
        {
            _actions.SetCooldown(component.Action, useDelay.Delay);
        }

        _interaction.UseInHandInteraction(args.Performer, uid);
        args.Handled = true;
    }

    public void OnUseInHand(EntityUid uid, RMCWhistleComponent component, UseInHandEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Handled)
            return;

        if (TryComp<UseDelayComponent>(uid, out var useDelay))
        {
            _actions.SetCooldown(component.Action, useDelay.Delay);
        }

        TryMakeLoudWhistle(uid, args.User, component);
        args.Handled = true;
    }


    public bool TryMakeLoudWhistle(EntityUid uid, EntityUid owner, RMCWhistleComponent? component = null)
    {
        if (!Resolve(uid, ref component, false) || component.Distance <= 0)
            return false;

        MakeLoudWhistle(uid, owner, component);
        return true;
    }

    private void MakeLoudWhistle(EntityUid uid, EntityUid owner, RMCWhistleComponent component)
    {
        StealthComponent? stealth = null;

        foreach (var iterator in
            _entityLookup.GetEntitiesInRange<HumanoidAppearanceComponent>(_transform.GetMapCoordinates(uid), component.Distance))
        {
            //Avoid pinging invisible entities
            if (TryComp(iterator, out stealth) && stealth.Enabled)
                continue;

            //We don't want to ping user of whistle
            if (iterator.Owner == owner)
                continue;

            ExclamateTarget(iterator, component);
        }
    }
}
