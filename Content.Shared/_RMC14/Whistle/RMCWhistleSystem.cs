using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Coordinates;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Stealth.Components;
using Content.Shared.Timing;
using Content.Shared.Whistle;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Whistle;

public sealed class RMCWhistleSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly WhistleSystem _whistle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCWhistleComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<RMCWhistleComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<RMCWhistleComponent, SoundActionEvent>(OnWhistleAction);
    }

    private void OnGetItemActions(Entity<RMCWhistleComponent> ent, ref GetItemActionsEvent args)
    {
        var comp = ent.Comp;

        if (args.SlotFlags == SlotFlags.POCKET)
            return;

        args.AddAction(ref comp.Action, comp.ActionId);
    }

    public void OnWhistleAction(EntityUid uid, RMCWhistleComponent component, SoundActionEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Handled)
            return;

        if (TryComp<UseDelayComponent>(uid, out var useDelayComp))
        {
            _useDelay.SetLength(uid, useDelayComp.Delay);
            _useDelay.TryResetDelay((uid, useDelayComp));
        }

        _whistle.TryMakeLoudWhistle(uid, args.Performer);
        args.Handled = true;
    }

    public void OnUseInHand(EntityUid uid, RMCWhistleComponent component, UseInHandEvent args)
    {
        _whistle.TryMakeLoudWhistle(uid, args.User);

        if (TryComp<UseDelayComponent>(uid, out var useDelay))
        {
            _actions.SetCooldown(component.Action, useDelay.Delay);
        }
    }
}
