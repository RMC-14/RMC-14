using Content.Shared.Chemistry.Reagent;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Tools.Components;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Generation;

public sealed class RMCSortingCabinetSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCChemistryResearchSystem _research = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCSortingCabinetComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<RMCSortingCabinetComponent, InteractUsingEvent>(OnInteractUsing);

        Subs.BuiEvents<RMCSortingCabinetComponent>(RMCSortingCabinetUi.Key,
            subs =>
            {
                subs.Event<RMCSortingCabinetTakeOutBuiMsg>(OnTakeOut);
                subs.Event<RMCSortingCabinetSetSortBuiMsg>(OnSetSort);
                subs.Event<RMCSortingCabinetToggleOrderBuiMsg>(OnToggleOrder);
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

        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return;

        if (TryComp(used, out RMCChemSynthesisNotesComponent? notes))
        {
            if (!notes.Unique)
            {
                _popup.PopupClient("You try to slot the note into a sorting tray, but it is refused.", ent, user, PopupType.SmallCaution);
                return;
            }

            if (_net.IsClient)
                return;

            InsertUniqueNotes(ent, container, (used, notes), user);
            return;
        }

        if (!TryComp(used, out RMCChemResearchReportComponent? report))
        {
            _popup.PopupClient($"You try to slot {Name(used)} into a sorting tray, but it is refused.", ent, user, PopupType.SmallCaution);
            return;
        }

        if (!report.Valid || report.ReagentId is not { } reagentId)
        {
            _popup.PopupClient("You try to slot a document into a sorting tray, but it is refused.", ent, user, PopupType.SmallCaution);
            return;
        }

        if (HasDocument(container, reagentId))
        {
            _popup.PopupClient("You try to slot a document into a sorting tray, but there is an identical document already in the array.", ent, user, PopupType.SmallCaution);
            return;
        }

        if (!_hands.CanDrop(user, used) || !_container.Insert(used, container))
            return;

        _popup.PopupClient($"You slot a document into a sorting tray, and the {Name(ent)} whirs to life.", ent, user);
        Open(ent);
    }

    private void InsertUniqueNotes(Entity<RMCSortingCabinetComponent> ent, BaseContainer container, Entity<RMCChemSynthesisNotesComponent> notes, EntityUid user)
    {
        _research.EnsureNotesGenerated(notes);
        if (notes.Comp.ReagentId is not { } reagentId)
        {
            _popup.PopupEntity("The notes could not be converted into a valid report.", ent, user, PopupType.SmallCaution);
            return;
        }

        if (HasDocument(container, reagentId))
        {
            _popup.PopupEntity("You try to slot the document into a sorting tray, but there is an identical document already in the array.", ent, user, PopupType.SmallCaution);
            return;
        }

        var report = Spawn(ent.Comp.ReportProto, Transform(ent).Coordinates);
        if (TryComp(notes, out PaperComponent? paper))
        {
            _paper.SetContent(report, paper.Content);
            _paper.CopyStamps(notes.Owner, report);
        }

        _metaData.SetEntityName(report, Name(notes));
        _research.SetReportFromNotes(report, notes.Comp);

        if (!_container.Insert(report, container))
        {
            Del(report);
            return;
        }

        Del(notes);
        _popup.PopupEntity($"You slot a document into a sorting tray, and the {Name(ent)} whirs to life.", ent, user);
        Open(ent);
    }

    private bool HasDocument(BaseContainer container, ProtoId<ReagentPrototype> reagentId)
    {
        foreach (var contained in container.ContainedEntities)
        {
            if (TryComp(contained, out RMCChemResearchReportComponent? inside) &&
                inside.ReagentId == reagentId)
            {
                return true;
            }
        }

        return false;
    }

    private void Open(Entity<RMCSortingCabinetComponent> ent)
    {
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

    private void OnSetSort(Entity<RMCSortingCabinetComponent> ent, ref RMCSortingCabinetSetSortBuiMsg args)
    {
        if (args.Sort > RMCSortingCabinetSort.Class)
            return;

        ent.Comp.Sort = args.Sort;
        Dirty(ent);
    }

    private void OnToggleOrder(Entity<RMCSortingCabinetComponent> ent, ref RMCSortingCabinetToggleOrderBuiMsg args)
    {
        ent.Comp.Descending = !ent.Comp.Descending;
        Dirty(ent);
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
