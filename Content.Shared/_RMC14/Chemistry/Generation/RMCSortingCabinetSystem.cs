using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Tools.Components;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Generation;

public sealed class RMCSortingCabinetSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCSortingCabinetComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RMCSortingCabinetComponent, InteractUsingEvent>(OnInteractUsing);

        Subs.BuiEvents<RMCSortingCabinetComponent>(RMCSortingCabinetUi.Key,
            subs =>
            {
                subs.Event<RMCSortingCabinetTakeOutBuiMsg>(OnTakeOut);
            });
    }

    private void OnInit(Entity<RMCSortingCabinetComponent> ent, ref ComponentInit args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
    }

    private void OnInteractUsing(Entity<RMCSortingCabinetComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || HasComp<ToolComponent>(args.Used))
            return;

        args.Handled = true;
        var user = args.User;
        var used = args.Used;

        if (!TryComp(used, out RMCChemResearchReportComponent? report))
        {
            _popup.PopupClient($"You try to slot {Name(used)} into a sorting tray, but it is refused.", ent, user, PopupType.SmallCaution);
            return;
        }

        if (!report.Valid || report.ReagentId == null)
        {
            _popup.PopupClient("You try to slot a document into a sorting tray, but it is refused.", ent, user, PopupType.SmallCaution);
            return;
        }

        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return;

        foreach (var contained in container.ContainedEntities)
        {
            if (TryComp(contained, out RMCChemResearchReportComponent? inside) &&
                inside.ReagentId == report.ReagentId)
            {
                _popup.PopupClient("You try to slot a document into a sorting tray, but there is an identical document already in the array.", ent, user, PopupType.SmallCaution);
                return;
            }
        }

        if (!_hands.CanDrop(user, used) || !_container.Insert(used, container))
            return;

        _popup.PopupClient($"You slot a document into a sorting tray, and the {Name(ent)} whirs to life.", ent, user);
        ent.Comp.CloseAt = _timing.CurTime + ent.Comp.OpenDuration;
        Dirty(ent);
        _appearance.SetData(ent, SharedBagOpenVisuals.BagState, SharedBagState.Open);
    }

    private void OnTakeOut(Entity<RMCSortingCabinetComponent> ent, ref RMCSortingCabinetTakeOutBuiMsg args)
    {
        if (!TryGetEntity(args.Document, out var document) ||
            !_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container) ||
            !container.Contains(document.Value) ||
            !_container.Remove(document.Value, container))
        {
            return;
        }

        _hands.PickupOrDrop(args.Actor, document.Value, dropNear: true);
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCSortingCabinetComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.CloseAt is not { } closeAt || time < closeAt)
                continue;

            comp.CloseAt = null;
            Dirty(uid, comp);
            _appearance.SetData(uid, SharedBagOpenVisuals.BagState, SharedBagState.Closed);
        }
    }
}
