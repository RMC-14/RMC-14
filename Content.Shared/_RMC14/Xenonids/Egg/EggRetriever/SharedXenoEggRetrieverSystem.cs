using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Egg.EggRetriever;

public abstract partial class SharedXenoEggRetrieverSystem : EntitySystem
{

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] protected readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly RMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoEggRetrieverComponent, ExaminedEvent>(OnEggRetrieverExamine);

        SubscribeLocalEvent<XenoEggStorageVisualsComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<XenoEggStorageVisualsComponent, XenoRestEvent>(OnVisualsRest);
        SubscribeLocalEvent<XenoEggStorageVisualsComponent, KnockedDownEvent>(OnVisualsKnockedDown);
        SubscribeLocalEvent<XenoEggStorageVisualsComponent, StatusEffectEndedEvent>(OnVisualsStatusEffectEnded);

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

    protected virtual void OnMobStateChanged(Entity<XenoEggStorageVisualsComponent> xeno, ref MobStateChangedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        _appearance.SetData(xeno, XenoEggStorageVisuals.Downed, args.NewMobState != MobState.Alive);
        _appearance.SetData(xeno, XenoEggStorageVisuals.Dead, args.NewMobState == MobState.Dead);
    }

    private void OnVisualsRest(Entity<XenoEggStorageVisualsComponent> xeno, ref XenoRestEvent args)
    {
        if (_timing.ApplyingState)
            return;

        _appearance.SetData(xeno, XenoEggStorageVisuals.Resting, args.Resting);
    }

    private void OnVisualsKnockedDown(Entity<XenoEggStorageVisualsComponent> xeno, ref KnockedDownEvent args)
    {
        if (_timing.ApplyingState)
            return;

        _appearance.SetData(xeno, XenoEggStorageVisuals.Downed, true);
    }

    private void OnVisualsStatusEffectEnded(Entity<XenoEggStorageVisualsComponent> xeno, ref StatusEffectEndedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.Key == "KnockedDown")
            _appearance.SetData(xeno, XenoEggStorageVisuals.Downed, false);
    }

    private void OnXenoProduceEggsAction(Entity<XenoGenerateEggsComponent> xeno, ref XenoGenerateEggsActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_rmcActions.TryUseAction(xeno, args.Action))
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
        foreach (var (actionId, action) in _actions.GetActions(xeno))
        {
            if (action.BaseEvent is XenoGenerateEggsActionEvent)
                _actions.SetToggled(actionId, produce.Active);
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
