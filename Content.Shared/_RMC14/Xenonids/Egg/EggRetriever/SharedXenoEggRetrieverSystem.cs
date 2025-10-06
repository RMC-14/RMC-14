using Content.Shared._RMC14.Actions;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Egg.EggRetriever;

public abstract partial class SharedXenoEggRetrieverSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] protected readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoEggRetrieverComponent, ExaminedEvent>(OnEggRetrieverExamine);

        SubscribeLocalEvent<XenoGenerateEggsComponent, XenoGenerateEggsActionEvent>(OnXenoProduceEggsAction);
        SubscribeLocalEvent<XenoGenerateEggsComponent, MobStateChangedEvent>(OnXenoProduceEggsDeath);
    }

    private void OnEggRetrieverExamine(Entity<XenoEggRetrieverComponent> retriever, ref ExaminedEvent args)
    {
        if (!HasComp<XenoComponent>(args.Examiner))
            return;

        using (args.PushGroup(nameof(XenoEggRetrieverComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-xeno-retrieve-egg-current", ("xeno", retriever),
                ("cur_eggs", retriever.Comp.CurEggs), ("max_eggs", retriever.Comp.MaxEggs)));
        }
    }

    private void OnXenoProduceEggsAction(Entity<XenoGenerateEggsComponent> xeno, ref XenoGenerateEggsActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_rmcActions.TryUseAction(args))
            return;

        args.Handled = true;
        ToggleProduceEggs(xeno, xeno.Comp);
        if (xeno.Comp.Active)
            _popup.PopupClient(Loc.GetString("rmc-xeno-produce-eggs-start"), xeno, xeno);
    }

    protected void ToggleProduceEggs(EntityUid xeno, XenoGenerateEggsComponent produce)
    {
        if (produce.Active && _net.IsServer)
        {
            produce.NextDrain = null;
            produce.NextEgg = null;
        }

        produce.Active = !produce.Active;
        _appearance.SetData(xeno, XenoEggStorageVisuals.Active, produce.Active);
        foreach (var action in _rmcActions.GetActionsWithEvent<XenoGenerateEggsActionEvent>(xeno))
        {
            _actions.SetToggled(action.AsNullable(), produce.Active);
        }

        Dirty(xeno, produce);
    }

    private void OnXenoProduceEggsDeath(Entity<XenoGenerateEggsComponent> xeno, ref MobStateChangedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.NewMobState != MobState.Dead)
            return;

        if (xeno.Comp.Active)
            ToggleProduceEggs(xeno, xeno.Comp);
    }
}
