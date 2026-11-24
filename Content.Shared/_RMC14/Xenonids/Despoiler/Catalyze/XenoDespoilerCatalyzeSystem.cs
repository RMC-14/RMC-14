using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Xenonids.Despoiler.Hypertension;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Despoiler.Catalyze;

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

    private void OnCatalyze(Entity<XenoDespoilerComponent> ent, ref XenoDespoilerCatalyzeActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<XenoDespoilerHypertensionComponent>(ent, out var hyper))
            return;

        if (!TryComp<XenoDespoilerCatalyzeActionComponent>(args.Action, out var action))
            return;

        if (!_hyper.TrySpendStacks((ent, hyper), action.HypertensionCost))
        {
            _popup.PopupEntity(Loc.GetString("rmc-despoiler-no-hypertension"), ent, ent);
            return;
        }

        if (!_rmcActions.TryUseAction(args))
            return;

        ent.Comp.NextAbilityEmpowered = true;
        ent.Comp.EmpowerExpiresAt = _timing.CurTime + action.BuffDuration;
        Dirty(ent);

        var server = EnsureComp<XenoDespoilerVisualComponent>(ent);
        DespawnVisual(server);

        var burst = Spawn(action.VisualProto, Transform(ent).Coordinates);
        _xform.SetParent(burst, ent);
        _hive.SetSameHive(ent.Owner, burst);
        server.CatalyzeVisual = burst;

        _popup.PopupEntity(Loc.GetString("rmc-despoiler-catalyze-active"), ent, ent);
        args.Handled = true;
    }

    private void OnShutdown(Entity<XenoDespoilerComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<XenoDespoilerVisualComponent>(ent, out var server))
            DespawnVisual(server);
    }

    private void DespawnVisual(XenoDespoilerVisualComponent comp)
    {
        if (comp.CatalyzeVisual is not { } visualEnt)
            return;

        if (Exists(visualEnt) && !TerminatingOrDeleted(visualEnt))
            QueueDel(visualEnt);

        comp.CatalyzeVisual = null;
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<XenoDespoilerComponent, XenoDespoilerVisualComponent>();
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
}
