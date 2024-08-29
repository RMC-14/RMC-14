using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Timing;
using Content.Shared.Whistle;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Whistle;

public sealed class RMCWhistleSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
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
        if (args.SlotFlags == SlotFlags.POCKET)
            return;

        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);
    }

    public void OnWhistleAction(EntityUid uid, RMCWhistleComponent comp, SoundActionEvent args)
    {
        if (!_timing.IsFirstTimePredicted || args.Handled)
            return;

        TryWhistle(uid, comp, args.Performer);
        args.Handled = true;
    }

    public void OnUseInHand(EntityUid uid, RMCWhistleComponent comp, UseInHandEvent args)
    {
        TryWhistle(uid, comp, args.User);
        args.Handled = true;
    }

    public void TryWhistle(EntityUid uid, RMCWhistleComponent comp, EntityUid user)
    {
        _whistle.TryMakeLoudWhistle(uid, user);

        if (TryComp<UseDelayComponent>(uid, out var useDelay))
        {
            _actions.SetCooldown(comp.Action, useDelay.Delay);
            _useDelay.SetLength(uid, useDelay.Delay);
            _useDelay.TryResetDelay((uid, useDelay));
        }
    }
}
