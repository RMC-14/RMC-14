using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Despoiler;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Xenonids.Despoiler;

public sealed class XenoDespoilerCatalyzeSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly XenoDespoilerHypertensionSystem _hyper = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoDespoilerComponent, XenoDespoilerCatalyzeActionEvent>(OnCatalyze);
        SubscribeLocalEvent<XenoDespoilerComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnCatalyze(EntityUid uid, XenoDespoilerComponent comp, XenoDespoilerCatalyzeActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<XenoDespoilerHypertensionComponent>(uid, out var hyper))
            return;

        if (!TryComp<XenoDespoilerCatalyzeActionComponent>(args.Action, out var action))
            return;

        if (!_hyper.TrySpendStacks(uid, hyper, action.HypertensionCost))
        {
            _popup.PopupEntity(Loc.GetString("rmc-despoiler-no-hypertension"), uid, uid);
            return;
        }

        if (!_rmcActions.TryUseAction(args))
            return;

        comp.NextAbilityEmpowered = true;
        comp.EmpowerExpiresAt = _timing.CurTime + action.BuffDuration;
        Dirty(uid, comp);

        var server = EnsureComp<XenoDespoilerServerComponent>(uid);
        DespawnVisual(server);

        var burst = Spawn(action.VisualProto, Transform(uid).Coordinates);
        _xform.SetParent(burst, uid);
        _hive.SetSameHive(uid, burst);
        server.CatalyzeVisual = burst;

        _popup.PopupEntity(Loc.GetString("rmc-despoiler-catalyze-active"), uid, uid);
        args.Handled = true;
    }

    private void OnShutdown(EntityUid uid, XenoDespoilerComponent comp, ComponentShutdown args)
    {
        if (TryComp<XenoDespoilerServerComponent>(uid, out var server))
            DespawnVisual(server);
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<XenoDespoilerComponent, XenoDespoilerServerComponent>();
        while (query.MoveNext(out var uid, out var comp, out var server))
        {
            if (comp.NextAbilityEmpowered && now > comp.EmpowerExpiresAt)
            {
                comp.NextAbilityEmpowered = false;
                Dirty(uid, comp);
            }

            if (!comp.NextAbilityEmpowered && server.CatalyzeVisual is not null)
                DespawnVisual(server);
        }
    }

    private void DespawnVisual(XenoDespoilerServerComponent server)
    {
        if (server.CatalyzeVisual is not { } visual)
            return;

        if (Exists(visual) && !TerminatingOrDeleted(visual))
            QueueDel(visual);

        server.CatalyzeVisual = null;
    }
}
