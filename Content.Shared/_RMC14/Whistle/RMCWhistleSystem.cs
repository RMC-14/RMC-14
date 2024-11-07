using Content.Shared._RMC14.Sound;
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

    public void OnWhistleAction(Entity<RMCWhistleComponent> ent, ref SoundActionEvent args)
    {
        if (!_timing.IsFirstTimePredicted || args.Handled)
            return;

        TryWhistle(ent, args.Performer);
        args.Handled = true;
    }

    public void OnUseInHand(Entity<RMCWhistleComponent> ent, ref UseInHandEvent args)
    {
        TryWhistle(ent, args.User);
        args.Handled = true;
    }

    public void TryWhistle(Entity<RMCWhistleComponent> ent, EntityUid user)
    {
        _whistle.TryMakeLoudWhistle(ent, user);

        if (TryComp<UseDelayComponent>(ent, out var useDelay))
        {
            _actions.SetCooldown(ent.Comp.Action, useDelay.Delay);
            _useDelay.SetLength(ent.Owner, useDelay.Delay);
            _useDelay.TryResetDelay((ent.Owner, useDelay));
        }
    }
}
